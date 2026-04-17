using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class GuardNeuralNetworkAI : MonoBehaviour
{
    float[,] W1 = new float[8, 5];
    float[,] W2 = new float[4, 8];
    float[,] W3 = new float[3, 4];

    float[] B1 = new float[8];
    float[] B2 = new float[4];
    float[] B3 = new float[3];

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
    public float crouchHearMultiplier = 0.3f; // Mnożnik słuchu podczas kucania (Ctrl)

    [Header("Game Over Settings")]
    public float captureDistance = 1.5f; // Dystans potrzebny do złapania gracza
    public GameObject deathUIScreen;     // Panel UI, który ma się pojawić po śmierci

    [Header("Patrol System")]
    public Transform[] patrolPoints;
    public float waitAtPointTime = 3f;
    private int currentPointIndex = 0;
    private float waitTimer = 0f;
    private bool isWaiting = false;

    [Header("Training Settings")]
    public bool trainingMode = false;
    public float learningRate = 0.1f;
    public int trainingEpochs = 5000;

    // --- Pamięć do nauki w trakcie gry ---
    private Queue<float[]> memoryBuffer = new Queue<float[]>();
    public int memoryBufferSize = 60; // Pamięta ostatnie ~60 klatek decyzyjnych

    private Vector3 lastKnownPosition;
    private NavMeshAgent agent;
    private Animator anim;
    private StreamWriter writer;
    private int speedHash;
    private bool isGameOver = false;

    private enum Action { Patrol = 0, Investigate = 1, Chase = 2 }

    private int currentAction = 0;
    private float actionLockTimer = 0f;
    private float minActionTime = 0.5f;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
        speedHash = Animator.StringToHash("Speed");

        if (deathUIScreen != null) deathUIScreen.SetActive(false);

        InitWeights();
        TrainNetworkFromMenu();
        ApplyAgentSpeed();

        lastKnownPosition = transform.position;

        if (agent != null && agent.isOnNavMesh && patrolPoints.Length > 0)
        {
            agent.SetDestination(patrolPoints[currentPointIndex].position);
        }

        if (trainingMode)
        {
            writer = new StreamWriter(Application.dataPath + "/dataset.csv");
            writer.WriteLine("distance,visible,suspicion,memory,hearing,action");
        }
    }

    void Update()
    {
        if (player == null || isGameOver) return;

        CheckCapture();

        if (isGameOver) return;

        float[] input = GetInputs();
        float[] output = Forward(input);
        int proposedAction = ArgMax(output);

        SaveToMemory(input);

        UpdateActionState(proposedAction);
        ExecuteAction(currentAction);
        UpdateSuspicionOverTime();

        if (anim != null)
        {
            anim.SetFloat(speedHash, agent.velocity.magnitude);
        }

        if (trainingMode) LogData(input, currentAction);
    }

    void CheckCapture()
    {
        if (Vector3.Distance(transform.position, player.position) <= captureDistance)
        {
            isGameOver = true;
            agent.isStopped = true;

            RewardAI();
            ShowDeathScreen();
        }
    }

    void ShowDeathScreen()
    {
        if (deathUIScreen != null)
        {
            deathUIScreen.SetActive(true);
        }

        // --- NOWE: Odblokowanie i pokazanie kursora myszy ---
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // Zatrzymujemy czas w grze. Pamiętaj ustawić Time.timeScale = 1f przy restarcie!
        Time.timeScale = 0f;
    }

    void SaveToMemory(float[] input)
    {
        memoryBuffer.Enqueue(input);
        if (memoryBuffer.Count > memoryBufferSize)
        {
            memoryBuffer.Dequeue();
        }
    }

    void RewardAI()
    {
        Debug.Log("Gracz złapany! Nagradzam AI i trenuję na bazie ostatnich decyzji.");

        // Uczymy AI, że w stanach z bufora poprawną akcją był pościg (Chase = {0,0,1})
        float[] expectedChaseOutput = new float[] { 0f, 0f, 1f };

        foreach (var state in memoryBuffer)
        {
            Backpropagate(state, expectedChaseOutput);
        }

        memoryBuffer.Clear();
    }

    float[] GetInputs()
    {
        float dist = Vector3.Distance(transform.position, player.position);
        float distanceNorm = Mathf.Clamp01(dist / (viewDistance * sprintMultiplier));
        float visible = IsPlayerVisible() ? 1f : 0f;
        float memoryDist = Vector3.Distance(transform.position, lastKnownPosition);
        float memoryNorm = Mathf.Clamp01(memoryDist / viewDistance);
        float hearing = CanHearPlayer() ? 1f : 0f;

        return new float[] { distanceNorm, visible, suspicion, memoryNorm, hearing };
    }

    bool IsPlayerVisible()
    {
        Vector3 dir = player.position - transform.position;
        float dist = dir.magnitude;
        float currentMaxDist = Input.GetKey(KeyCode.LeftShift) ? viewDistance * sprintMultiplier : viewDistance;

        if (dist > currentMaxDist) return false;
        if (Vector3.Angle(transform.forward, dir) > viewAngle * 0.5f) return false;

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

        if (Input.GetKey(KeyCode.LeftShift))
        {
            currentHearDistance *= sprintHearMultiplier;
        }
        else if (Input.GetKey(KeyCode.LeftControl)) // Dodane kucanie pod Ctrl
        {
            currentHearDistance *= crouchHearMultiplier;
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

        agent.speed = 2.5f * moveSpeedMultiplier;

        if (!agent.pathPending && agent.remainingDistance < 0.7f)
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
                currentPointIndex = (currentPointIndex + 1) % patrolPoints.Length;
                agent.SetDestination(patrolPoints[currentPointIndex].position);
            }
        }
        else if (!isWaiting)
        {
            if (Vector3.Distance(agent.destination, patrolPoints[currentPointIndex].position) > 0.2f)
            {
                agent.SetDestination(patrolPoints[currentPointIndex].position);
            }
        }
    }

    void Investigate()
    {
        if (!agent.isOnNavMesh) return;
        isWaiting = false;
        agent.speed = 3.5f * moveSpeedMultiplier;

        agent.SetDestination(lastKnownPosition);

        if (!agent.pathPending && agent.remainingDistance < 1f)
        {
            suspicion -= Time.deltaTime * 1.5f;
            if (suspicion <= 0.1f) FindClosestPatrolPoint();
        }
    }

    void Chase()
    {
        if (!agent.isOnNavMesh) return;
        isWaiting = false;
        agent.speed = 6.5f * moveSpeedMultiplier;
        agent.acceleration = 12f;

        if (IsPlayerVisible() || CanHearPlayer())
        {
            lastKnownPosition = player.position;
            agent.SetDestination(player.position);
        }
        else
        {
            agent.SetDestination(lastKnownPosition);
        }
    }

    void FindClosestPatrolPoint()
    {
        float minDist = Mathf.Infinity;
        for (int i = 0; i < patrolPoints.Length; i++)
        {
            float d = Vector3.Distance(transform.position, patrolPoints[i].position);
            if (d < minDist)
            {
                minDist = d;
                currentPointIndex = i;
            }
        }
    }

    void UpdateSuspicionOverTime()
    {
        bool detected = IsPlayerVisible() || CanHearPlayer();

        if (detected)
        {
            suspicion += Time.deltaTime * (IsPlayerVisible() ? 0.8f : 0.5f);
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
        if (newAction != currentAction)
        {
            if (actionLockTimer >= minActionTime)
            {
                currentAction = newAction;
                actionLockTimer = 0f;
            }
        }
        else actionLockTimer = 0f;
    }

    void InitWeights()
    {
        for (int i = 0; i < 8; i++)
        {
            B1[i] = UnityEngine.Random.Range(-0.5f, 0.5f);
            for (int j = 0; j < 5; j++) W1[i, j] = UnityEngine.Random.Range(-0.5f, 0.5f);
        }
        for (int i = 0; i < 4; i++)
        {
            B2[i] = UnityEngine.Random.Range(-0.5f, 0.5f);
            for (int j = 0; j < 8; j++) W2[i, j] = UnityEngine.Random.Range(-0.5f, 0.5f);
        }
        for (int i = 0; i < 3; i++)
        {
            B3[i] = UnityEngine.Random.Range(-0.5f, 0.5f);
            for (int j = 0; j < 4; j++) W3[i, j] = UnityEngine.Random.Range(-0.5f, 0.5f);
        }
    }

    [ContextMenu("Rozpocznij Trening Sieci")]
    public void TrainNetworkFromMenu()
    {
        List<TrainingExample> dataset = GenerateTrainingData();
        for (int epoch = 0; epoch < trainingEpochs; epoch++)
        {
            foreach (var example in dataset) Backpropagate(example.inputs, example.expectedOutputs);
        }
        Debug.Log("AI przeszkolone pomyślnie na rozszerzonym zbiorze danych.");
    }

    void Backpropagate(float[] input, float[] expected)
    {
        float[] h1, h2, o;
        ForwardWithActivations(input, out h1, out h2, out o);

        float[] dZ3 = new float[3];
        for (int i = 0; i < 3; i++) dZ3[i] = o[i] - expected[i];

        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 4; j++) W3[i, j] -= learningRate * dZ3[i] * h2[j];
            B3[i] -= learningRate * dZ3[i];
        }

        float[] dH2 = new float[4];
        for (int j = 0; j < 4; j++)
            for (int i = 0; i < 3; i++) dH2[j] += W3[i, j] * dZ3[i];

        float[] dZ2 = new float[4];
        for (int j = 0; j < 4; j++) dZ2[j] = dH2[j] * h2[j] * (1f - h2[j]);

        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 8; j++) W2[i, j] -= learningRate * dZ2[i] * h1[j];
            B2[i] -= learningRate * dZ2[i];
        }

        float[] dH1 = new float[8];
        for (int j = 0; j < 8; j++)
            for (int i = 0; i < 4; i++) dH1[j] += W2[i, j] * dZ2[i];

        float[] dZ1 = new float[8];
        for (int j = 0; j < 8; j++) dZ1[j] = dH1[j] * h1[j] * (1f - h1[j]);

        for (int i = 0; i < 8; i++)
        {
            for (int j = 0; j < 5; j++) W1[i, j] -= learningRate * dZ1[i] * input[j];
            B1[i] -= learningRate * dZ1[i];
        }
    }

    struct TrainingExample
    {
        public float[] inputs;
        public float[] expectedOutputs;
    }

    List<TrainingExample> GenerateTrainingData()
    {
        List<TrainingExample> data = new List<TrainingExample>();
        // 1. Spokój (Patrol)
        data.Add(new TrainingExample { inputs = new float[] { 1f, 0f, 0f, 1f, 0f }, expectedOutputs = new float[] { 1f, 0f, 0f } });
        data.Add(new TrainingExample { inputs = new float[] { 0.8f, 0f, 0.1f, 0.8f, 0f }, expectedOutputs = new float[] { 1f, 0f, 0f } });
        // 2. Investigate
        data.Add(new TrainingExample { inputs = new float[] { 0.5f, 0f, 0.4f, 0.2f, 1f }, expectedOutputs = new float[] { 0f, 1f, 0f } });
        data.Add(new TrainingExample { inputs = new float[] { 0.4f, 0f, 0.6f, 0.1f, 0f }, expectedOutputs = new float[] { 0f, 1f, 0f } });
        // 3. Chase
        data.Add(new TrainingExample { inputs = new float[] { 0.1f, 1f, 0.8f, 0.1f, 0f }, expectedOutputs = new float[] { 0f, 0f, 1f } });
        data.Add(new TrainingExample { inputs = new float[] { 0.3f, 1f, 0.5f, 0.3f, 1f }, expectedOutputs = new float[] { 0f, 0f, 1f } });

        return data;
    }

    float[] Forward(float[] x)
    {
        float[] h1, h2, o;
        return ForwardWithActivations(x, out h1, out h2, out o);
    }

    float[] ForwardWithActivations(float[] x, out float[] h1, out float[] h2, out float[] o)
    {
        h1 = new float[8]; h2 = new float[4]; o = new float[3];
        for (int i = 0; i < 8; i++)
        {
            float s = B1[i];
            for (int j = 0; j < 5; j++) s += W1[i, j] * x[j];
            h1[i] = Sigmoid(s);
        }
        for (int i = 0; i < 4; i++)
        {
            float s = B2[i];
            for (int j = 0; j < 8; j++) s += W2[i, j] * h1[j];
            h2[i] = Sigmoid(s);
        }
        for (int i = 0; i < 3; i++)
        {
            float s = B3[i];
            for (int j = 0; j < 4; j++) s += W3[i, j] * h2[j];
            o[i] = s;
        }
        o = Softmax(o);
        return o;
    }

    float Sigmoid(float x) => 1f / (1f + Mathf.Exp(-x));

    float[] Softmax(float[] x)
    {
        float max = Mathf.Max(x[0], Mathf.Max(x[1], x[2]));
        float sum = 0f;
        float[] r = new float[3];
        for (int i = 0; i < 3; i++)
        {
            r[i] = Mathf.Exp(x[i] - max);
            sum += r[i];
        }
        for (int i = 0; i < 3; i++) r[i] /= sum;
        return r;
    }

    int ArgMax(float[] x)
    {
        int best = 0;
        for (int i = 1; i < x.Length; i++)
            if (x[i] > x[best]) best = i;
        return best;
    }

    void ApplyAgentSpeed()
    {
        if (agent != null) agent.speed *= moveSpeedMultiplier;
    }

    void LogData(float[] input, int action)
    {
        if (writer == null) return;
        writer.WriteLine($"{input[0]},{input[1]},{input[2]},{input[3]},{input[4]},{action}");
    }

    private void OnDestroy()
    {
        if (writer != null) writer.Close();
    }
}