using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class GuardNoedifyAI : MonoBehaviour
{
    public GameStateManager gameStateManager;
    public EndGameManager endGameManager;
    public PauseMenu pauseMenu;

    private Noedify.Net net;
    private Noedify_Solver solver;
    private string saveDirectory;
    private string modelName = "GuardBrainModel";

    [Header("Statystki Stra¿nika")]
    public float currentViewDistance;
    public float currentHearDistance;

    [Header("Ustawienia Stra¿nika")]
    public Transform player;
    public float viewDistance = 15f;
    public float viewAngle = 90f;
    public float baseHearDistance = 5f;
    public LayerMask obstacleMask;
    public float moveSpeedMultiplier = 1f;
    public float suspicion = 0f;

    [Header("Modyfikatory")]
    public float sprintMultiplier = 1.5f;
    public float sprintHearMultiplier = 5f;
    public float crouchHearMultiplier = 0.3f;

    [Header("Ustawienia Wie¿ Stra¿nika")]
    public float towerBuffRange = 100f;
    public float towerVisionMultiplier = 4f;
    public float patrolReachDistance = 100f;
    private bool isNearTower = false;

    [Header("Ustawienia Œledztwa Stra¿nika")]
    public float investigateViewMultiplier = 1.5f;
    public float investigateAngleMultiplier = 1.5f;
    public float investigateHearMultiplier = 1.5f;

    [Header("System Patrolu")]
    public Transform[] patrolPoints;
    public float waitAtPointTime = 0.5f;
    private int currentPointIndex = 0;
    private float waitTimer = 0f;
    private bool isWaiting = false;

    [Header("Ustawienia Koñca Gry")]
    public float captureDistance = 1.5f;
    public GameObject deathUIScreen;

    [Header("Ustawienia Dodatkowe")]
    public float aiThinkInterval = 0.1f;
    private float aiThinkTimer = 0f;

    [Header("Trenowanie Sieci Neuronowej")]
    public bool trainingMode = true;
    public float learningRate = 0.05f;
    public int trainingEpochs = 2000;
    public int memoryBufferSize = 60;
    private Queue<float[]> memoryBufferInputs = new Queue<float[]>();

    private Vector3 lastKnownPosition;
    private NavMeshAgent agent;
    private Animator anim;
    private int speedHash;
    private bool isGameOver = false;

    private enum Action { Patrol = 0, Investigate = 1, Chase = 2 }
    private int currentAction = 0;
    private float actionLockTimer = 0f;
    private float minActionTime = 0.2f;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
        if (anim != null) speedHash = Animator.StringToHash("Speed");

        if (deathUIScreen != null) deathUIScreen.SetActive(false);

        if (agent != null)
        {
            agent.angularSpeed = 600f;
            agent.autoBraking = false;
        }
        lastKnownPosition = transform.position;
        saveDirectory = Application.persistentDataPath;
        solver = Noedify.CreateSolver();

        InitializeNoedifyNetwork();
        ApplyAgentSpeed();
    }

    void InitializeNoedifyNetwork()
    {
        net = new Noedify.Net();
        string fullPath = Path.Combine(saveDirectory, modelName + ".txt");

        Noedify.Layer inputLayer = new Noedify.Layer(Noedify.LayerType.Input, 5, "Input Layer");
        net.AddLayer(inputLayer);

        Noedify.Layer hidden1 = new Noedify.Layer(Noedify.LayerType.FullyConnected, 8, Noedify.ActivationFunction.Sigmoid, "Hidden 1");
        net.AddLayer(hidden1);

        Noedify.Layer hidden2 = new Noedify.Layer(Noedify.LayerType.FullyConnected, 4, Noedify.ActivationFunction.Sigmoid, "Hidden 2");
        net.AddLayer(hidden2);

        Noedify.Layer outputLayer = new Noedify.Layer(Noedify.LayerType.Output, 3, Noedify.ActivationFunction.SoftMax, "Output Layer");
        net.AddLayer(outputLayer);

        net.BuildNetwork();

        if (File.Exists(fullPath))
        {
            LoadNetworkModel();
        }
        else
        {
            if (trainingMode)
            {
                TrainNetworkFromData();
                SaveNetworkModel();
            }
        }
    }

    public void InitializePatrolPoints(Transform[] newPoints)
    {
        patrolPoints = newPoints;
        if (patrolPoints.Length > 0 && agent != null && agent.isOnNavMesh)
        {
            currentPointIndex = UnityEngine.Random.Range(0, patrolPoints.Length);
            agent.SetDestination(patrolPoints[currentPointIndex].position);
        }
    }

    void CalculateCurrentStats()
    {
        float tempView = viewDistance;
        if (isNearTower) tempView *= towerVisionMultiplier;
        if (Input.GetKey(KeyCode.LeftShift)) tempView *= sprintMultiplier;
        if (currentAction == (int)Action.Investigate) tempView *= investigateViewMultiplier;
        currentViewDistance = tempView;

        float tempHear = baseHearDistance;
        if (Input.GetKey(KeyCode.LeftShift)) tempHear *= sprintHearMultiplier;
        else if (Input.GetKey(KeyCode.LeftControl)) tempHear *= crouchHearMultiplier;
        if (currentAction == (int)Action.Investigate) tempHear *= investigateHearMultiplier;
        currentHearDistance = tempHear;
    }

    void Update()
    {
        if (player == null || isGameOver) return;

        isNearTower = CheckIfNearTower();
        CalculateCurrentStats();

        CheckCapture();
        if (isGameOver) return;

        float[] input = GetInputs();

        aiThinkTimer += Time.deltaTime;
        if (aiThinkTimer >= aiThinkInterval)
        {
            float[] output = GetNetworkPrediction(input);
            int proposedAction = ArgMax(output);

            if (suspicion >= 0.85f)
            {
                proposedAction = (int)Action.Chase;
            }
            else if (suspicion > 0.40f)
            {
                proposedAction = (int)Action.Investigate;
            }
            SaveToMemory(input);
            UpdateActionState(proposedAction);

            aiThinkTimer = 0f;
        }
        ExecuteAction(currentAction);
        UpdateSuspicionOverTime();

        if (anim != null && agent.isOnNavMesh)
        {
            anim.SetFloat(speedHash, agent.velocity.magnitude);
        }
    }

    void RewardAI()
    {
        List<float[,,]> memoryInputsList = new List<float[,,]>();
        List<float[]> expectedOutputsList = new List<float[]>();

        foreach (var stateInput in memoryBufferInputs)
        {
            memoryInputsList.Add(To3D(stateInput));

            float visible = stateInput[1];
            float pastSuspicion = stateInput[2];
            float hearingVolume = stateInput[4];

            if (visible > 0.5f || hearingVolume > 0.3f || pastSuspicion > 0.8f)
            {
                expectedOutputsList.Add(new float[] { 0f, 0f, 1f });
            }
            else
            {
                expectedOutputsList.Add(new float[] { 0f, 1f, 0f });
            }
        }
        if (solver != null && memoryInputsList.Count > 0)
        {
            solver.TrainNetwork(net, memoryInputsList, expectedOutputsList, 5, memoryInputsList.Count, learningRate, Noedify_Solver.CostFunction.MeanSquare, Noedify_Solver.SolverMethod.MainThread);
        }

        memoryBufferInputs.Clear();
        SaveNetworkModel();
    }

    [ContextMenu("Trenuj model Noedify")]
    public void TrainNetworkFromData()
    {
        List<float[,,]> inputsList = new List<float[,,]>();
        List<float[]> targetsList = new List<float[]>();

        inputsList.Add(To3D(new float[] { 1f, 0f, 0f, 1f, 0f })); targetsList.Add(new float[] { 1f, 0f, 0f });
        inputsList.Add(To3D(new float[] { 0.8f, 0f, 0.1f, 0.8f, 0f })); targetsList.Add(new float[] { 1f, 0f, 0f });
        inputsList.Add(To3D(new float[] { 0.5f, 0f, 0.4f, 0.2f, 1f })); targetsList.Add(new float[] { 0f, 1f, 0f });
        inputsList.Add(To3D(new float[] { 0.4f, 0f, 0.6f, 0.1f, 0f })); targetsList.Add(new float[] { 0f, 1f, 0f });
        inputsList.Add(To3D(new float[] { 0.1f, 1f, 0.8f, 0.1f, 0f })); targetsList.Add(new float[] { 0f, 0f, 1f });
        inputsList.Add(To3D(new float[] { 0.3f, 1f, 0.5f, 0.3f, 1f })); targetsList.Add(new float[] { 0f, 0f, 1f });

        if (solver != null)
        {
            solver.TrainNetwork(net, inputsList, targetsList, trainingEpochs, inputsList.Count, learningRate, Noedify_Solver.CostFunction.MeanSquare, Noedify_Solver.SolverMethod.MainThread);
        }
    }

    void CheckCapture()
    {
        if (Vector3.Distance(transform.position, player.position) <= captureDistance)
        {
            isGameOver = true;
            agent.isStopped = true;
            RewardAI();

            if (endGameManager != null)
            {
                endGameManager.EndGame();
            }
            ShowDeathScreen();
        }
    }

    void ShowDeathScreen()
    {
        if (deathUIScreen != null)
        {
            deathUIScreen.SetActive(true);
        }
    }

    void SaveToMemory(float[] input)
    {
        memoryBufferInputs.Enqueue(input);
        if (memoryBufferInputs.Count > memoryBufferSize) memoryBufferInputs.Dequeue();
    }

    float[] GetInputs()
    {
        float dist = Vector3.Distance(transform.position, player.position);
        float distanceNorm = Mathf.Clamp01(dist / currentViewDistance);
        float visible = IsPlayerVisible() ? 1f : 0f;
        float memoryDist = Vector3.Distance(transform.position, lastKnownPosition);
        float memoryNorm = Mathf.Clamp01(memoryDist / viewDistance);
        float hearingVolume = 0f;

        if (CanHearPlayer())
        {
            hearingVolume = Mathf.Clamp01(1f - (dist / currentHearDistance));
        }
        return new float[] { distanceNorm, visible, suspicion, memoryNorm, hearingVolume };
    }

    bool IsPlayerVisible()
    {
        Vector3 dir = player.position - transform.position;
        float dist = dir.magnitude;

        float currentViewAngle = viewAngle;
        if (currentAction == (int)Action.Investigate)
        {
            currentViewAngle *= investigateAngleMultiplier;
        }

        if (dist > currentViewDistance) return false;
        if (Vector3.Angle(transform.forward, dir) > currentViewAngle * 0.5f) return false;

        Vector3 eyePos = transform.position + Vector3.up * 1.5f;
        Vector3 targetPos = player.position + Vector3.up * 1.5f;
        if (Physics.Raycast(eyePos, (targetPos - eyePos).normalized, out RaycastHit hit, currentViewDistance, obstacleMask))
        {
            if (hit.transform != player) return false;
        }
        return true;
    }

    bool CanHearPlayer()
    {
        float dist = Vector3.Distance(transform.position, player.position);

        if (dist <= currentHearDistance)
        {
            return true;
        }
        return false;
    }

    void ExecuteAction(int a)
    {
        switch ((Action)a)
        {
            case Action.Patrol: Patrol(); break;
            case Action.Investigate: Investigate(); break;
            case Action.Chase: Chase(); break;
        }
    }

    void Patrol()
    {
        if (!agent.isOnNavMesh || patrolPoints == null || patrolPoints.Length == 0) return;

        agent.updateRotation = true;
        agent.isStopped = false;
        agent.speed = 2.5f * moveSpeedMultiplier;
        agent.acceleration = 10f;

        float distanceToPoint = Vector3.Distance(transform.position, patrolPoints[currentPointIndex].position);

        if (!agent.pathPending && distanceToPoint <= patrolReachDistance)
        {
            if (!isWaiting)
            {
                isWaiting = true;
                waitTimer = 0f;
            }

            waitTimer += Time.deltaTime;
            if (waitTimer >= waitAtPointTime)
            {
                isWaiting = false;

                int nextPoint = currentPointIndex;

                while (nextPoint == currentPointIndex)
                {
                    nextPoint = UnityEngine.Random.Range(0, patrolPoints.Length);
                }
                currentPointIndex = nextPoint;

                agent.SetDestination(patrolPoints[currentPointIndex].position);
            }
        }
        else if (!isWaiting && Vector3.Distance(agent.destination, patrolPoints[currentPointIndex].position) > 0.2f)
        {
            agent.SetDestination(patrolPoints[currentPointIndex].position);
        }
    }

    void Investigate()
    {
        if (!agent.isOnNavMesh) return;
        isWaiting = false;
        agent.isStopped = false;
        agent.speed = 3.5f * moveSpeedMultiplier;
        agent.acceleration = 15f;

        if (Vector3.Distance(agent.destination, lastKnownPosition) > 0.1f)
        {
            agent.SetDestination(lastKnownPosition);
        }

        float distToLastPos = Vector3.Distance(transform.position, lastKnownPosition);
        if (distToLastPos <= 1.5f)
        {
            suspicion -= Time.deltaTime * 0.2f;
        }
    }

    void Chase()
    {
        if (!agent.isOnNavMesh) return;
        isWaiting = false;
        agent.isStopped = false;
        agent.speed = 6.5f * moveSpeedMultiplier;
        agent.acceleration = 35f;

        Vector3 targetPos = (IsPlayerVisible() || CanHearPlayer()) ? player.position : lastKnownPosition;

        if (Vector3.Distance(agent.destination, targetPos) > 0.5f)
        {
            agent.SetDestination(targetPos);
            lastKnownPosition = targetPos;
        }
    }

    void UpdateSuspicionOverTime()
    {
        bool detected = IsPlayerVisible() || CanHearPlayer();
        if (detected)
        {
            suspicion += Time.deltaTime * (IsPlayerVisible() ? 2.0f : 1.0f);
            lastKnownPosition = player.position;
        }
        else
        {
            float decayRate = (currentAction == (int)Action.Investigate) ? 0.1f : 0.3f;
            suspicion -= Time.deltaTime * decayRate;
        }
        suspicion = Mathf.Clamp01(suspicion);
    }

    void UpdateActionState(int newAction)
    {
        actionLockTimer += Time.deltaTime;
        if (newAction != currentAction && actionLockTimer >= minActionTime)
        {
            currentAction = newAction;
            actionLockTimer = 0f;
        }
        else if (newAction == currentAction) actionLockTimer = 0f;
    }

    int ArgMax(float[] x)
    {
        int best = 0;
        for (int i = 1; i < x.Length; i++) if (x[i] > x[best]) best = i;
        return best;
    }

    void ApplyAgentSpeed()
    {
        if (agent != null) agent.speed *= moveSpeedMultiplier;
    }

    private void OnDestroy()
    {
        if (solver != null) Noedify.DestroySolver(solver);
    }

    float[] GetNetworkPrediction(float[] input)
    {
        if (solver == null) return new float[3] { 1f, 0f, 0f };

        solver.Evaluate(net, To3D(input), Noedify_Solver.SolverMethod.MainThread);

        if (solver.prediction != null && solver.prediction.Length > 0)
        {
            return solver.prediction;
        }

        return new float[3] { 1f, 0f, 0f };
    }

    void SaveNetworkModel()
    {
        net.SaveModel(modelName, saveDirectory);
    }

    void LoadNetworkModel()
    {
        net.LoadModel(modelName, saveDirectory);
    }

    float[,,] To3D(float[] flatArray)
    {
        float[,,] cube = new float[1, 1, flatArray.Length];
        for (int i = 0; i < flatArray.Length; i++)
        {
            cube[0, 0, i] = flatArray[i];
        }
        return cube;
    }

    bool CheckIfNearTower()
    {
        if (patrolPoints == null || patrolPoints.Length == 0) return false;

        foreach (Transform tower in patrolPoints)
        {
            if (Vector3.Distance(transform.position, tower.position) <= towerBuffRange)
            {
                return true;
            }
        }
        return false;
    }
}