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

    [Header("Agent Settings")]
    public Transform player;
    public float viewDistance = 15f;
    public float viewAngle = 90f;
    public float baseHearDistance = 5f;
    public LayerMask obstacleMask;
    public float moveSpeedMultiplier = 1f;
    public float suspicion = 0f;

    [Header("Modifiers")]
    public float sprintMultiplier = 1.5f;
    public float sprintHearMultiplier = 5f;
    public float crouchHearMultiplier = 0.3f;

    [Header("Investigate Settings (Wyostrzone zmys³y)")]
    public float investigateViewMultiplier = 1.5f;
    public float investigateAngleMultiplier = 1.5f;
    public float investigateHearMultiplier = 1.5f;

    [Header("Game Over Settings")]
    public float captureDistance = 1.5f;
    public GameObject deathUIScreen;

    [Header("Patrol System")]
    public Transform[] patrolPoints;
    public float waitAtPointTime = 0.5f;
    private int currentPointIndex = 0;
    private float waitTimer = 0f;
    private bool isWaiting = false;

    [Header("Performance Settings")]
    public float aiThinkInterval = 0.1f;
    private float aiThinkTimer = 0f;

    [Header("Training Settings")]
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

        if (agent != null && agent.isOnNavMesh && patrolPoints.Length > 0)
        {
            agent.SetDestination(patrolPoints[currentPointIndex].position);
        }
    }

    void InitializeNoedifyNetwork()
    {
        net = new Noedify.Net();
        string fullPath = Path.Combine(saveDirectory, modelName + ".txt");

        Debug.Log($"Œcie¿ka zapisu/odczytu pliku AI: {fullPath}");

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
            Debug.Log($"<color=green>ZNALEZIONO PLIK AI!</color> Wczytywanie wytrenowanych wag z {fullPath}");
            LoadNetworkModel();

            if (trainingMode)
            {
                Debug.Log("Tryb treningu wci¹¿ aktywny: Stra¿nik bêdzie siê dalej uczy³ podczas gry (Ci¹g³a Nauka).");
            }
        }
        else
        {
            Debug.Log("<color=orange>BRAK PLIKU AI.</color> Sieæ startuje od zera. Rozpoczynam pierwszy trening...");

            if (trainingMode)
            {
                TrainNetworkFromData();
                SaveNetworkModel(); // Tworzy plik pierwszy raz
                Debug.Log($"<color=green>Zapisano nowo wytrenowany model do pliku!</color>");
            }
        }
    }

    void Update()
    {
        if (player == null || isGameOver) return;

        CheckCapture();
        if (isGameOver) return;

        float[] input = GetInputs();

        aiThinkTimer += Time.deltaTime;
        if (aiThinkTimer >= aiThinkInterval)
        {
            float[] output = GetNetworkPrediction(input);
            int proposedAction = ArgMax(output);

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
        Debug.Log("Gracz z³apany! Douczam model w locie na b³êdach gracza...");

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
        Debug.Log("<color=green>Zaktualizowano plik z doœwiadczeniem Stra¿nika!</color>");
    }

    [ContextMenu("Trenuj model Noedify")]
    public void TrainNetworkFromData()
    {
        Debug.Log("Rozpoczynam wstêpny trening Noedify...");

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

        Debug.Log("Trening wstêpny zakoñczony.");
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
            else
            {
                Debug.LogError("Brak podpiêtego EndGameManager w GuardNoedifyAI!");
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

        float currentMaxView = viewDistance;
        if (Input.GetKey(KeyCode.LeftShift)) currentMaxView *= sprintMultiplier;
        if (currentAction == (int)Action.Investigate) currentMaxView *= investigateViewMultiplier;

        float distanceNorm = Mathf.Clamp01(dist / currentMaxView);
        float visible = IsPlayerVisible() ? 1f : 0f;
        float memoryDist = Vector3.Distance(transform.position, lastKnownPosition);
        float memoryNorm = Mathf.Clamp01(memoryDist / viewDistance);

        float hearingVolume = 0f;
        if (CanHearPlayer())
        {
            float currentHearDistance = baseHearDistance;
            if (Input.GetKey(KeyCode.LeftShift)) currentHearDistance *= sprintHearMultiplier;
            else if (Input.GetKey(KeyCode.LeftControl)) currentHearDistance *= crouchHearMultiplier;

            if (currentAction == (int)Action.Investigate) currentHearDistance *= investigateHearMultiplier;

            hearingVolume = Mathf.Clamp01(1f - (dist / currentHearDistance));
        }

        return new float[] { distanceNorm, visible, suspicion, memoryNorm, hearingVolume };
    }

    bool IsPlayerVisible()
    {
        Vector3 dir = player.position - transform.position;
        float dist = dir.magnitude;

        float currentMaxDist = viewDistance;
        float currentViewAngle = viewAngle;

        if (Input.GetKey(KeyCode.LeftShift)) currentMaxDist *= sprintMultiplier;

        if (currentAction == (int)Action.Investigate)
        {
            currentMaxDist *= investigateViewMultiplier;
            currentViewAngle *= investigateAngleMultiplier;
        }

        if (dist > currentMaxDist) return false;
        if (Vector3.Angle(transform.forward, dir) > currentViewAngle * 0.5f) return false;

        Vector3 eyePos = transform.position + Vector3.up * 1.5f;
        Vector3 targetPos = player.position + Vector3.up * 1.5f;
        if (Physics.Raycast(eyePos, (targetPos - eyePos).normalized, out RaycastHit hit, currentMaxDist, obstacleMask))
        {
            if (hit.transform != player) return false;
        }
        return true;
    }

    bool CanHearPlayer()
    {
        float dist = Vector3.Distance(transform.position, player.position);
        float currentHearDistance = baseHearDistance;

        if (Input.GetKey(KeyCode.LeftShift)) currentHearDistance *= sprintHearMultiplier;
        else if (Input.GetKey(KeyCode.LeftControl)) currentHearDistance *= crouchHearMultiplier;

        if (currentAction == (int)Action.Investigate)
        {
            currentHearDistance *= investigateHearMultiplier;
        }

        if (dist <= currentHearDistance)
        {
            Vector3 eyePos = transform.position + Vector3.up * 1.5f;
            Vector3 targetPos = player.position + Vector3.up * 1.5f;
            if (Physics.Raycast(eyePos, (targetPos - eyePos).normalized, out RaycastHit hit, currentHearDistance, obstacleMask))
            {
                if (hit.transform != player) return false;
            }
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
        if (!agent.isOnNavMesh || patrolPoints.Length == 0) return;

        agent.updateRotation = true;
        agent.isStopped = false;

        agent.speed = 2.5f * moveSpeedMultiplier;
        agent.acceleration = 10f;

        if (!agent.pathPending && agent.remainingDistance < 0.7f)
        {
            if (!isWaiting) { isWaiting = true; waitTimer = 0f; }

            waitTimer += Time.deltaTime;
            if (waitTimer >= waitAtPointTime)
            {
                isWaiting = false;
                currentPointIndex = (currentPointIndex + 1) % patrolPoints.Length;
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

        agent.updateRotation = true;
        agent.isStopped = false;

        if (Vector3.Distance(agent.destination, patrolPoints[currentPointIndex].position) > 1.0f)
        {
            FindClosestPatrolPoint();
            if (patrolPoints.Length > 0)
            {
                agent.SetDestination(patrolPoints[currentPointIndex].position);
            }
        }
    }

    void Chase()
    {
        if (!agent.isOnNavMesh) return;
        isWaiting = false;

        agent.updateRotation = true;
        agent.isStopped = false;

        agent.speed = 6.5f * moveSpeedMultiplier;
        agent.acceleration = 35f;

        if (IsPlayerVisible() || CanHearPlayer())
        {
            lastKnownPosition = player.position;
            agent.SetDestination(player.position);
        }
        else
        {
            if (Vector3.Distance(transform.position, lastKnownPosition) > 1.0f)
            {
                agent.SetDestination(lastKnownPosition);
            }
        }
    }

    void FindClosestPatrolPoint()
    {
        float minDist = Mathf.Infinity;
        for (int i = 0; i < patrolPoints.Length; i++)
        {
            float d = Vector3.Distance(transform.position, patrolPoints[i].position);
            if (d < minDist) { minDist = d; currentPointIndex = i; }
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
}