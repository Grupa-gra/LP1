using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class GuardNeuralNetworkAI : MonoBehaviour
{
    float[,] W1 = new float[8, 4];
    float[,] W2 = new float[4, 8];
    float[,] W3 = new float[3, 4];

    float[] B1 = new float[8];
    float[] B2 = new float[4];
    float[] B3 = new float[3];

    [Header("Agent Settings")]
    public Transform player;
    public float viewDistance = 15f;
    public float viewAngle = 90f;
    public LayerMask obstacleMask;
    public float moveSpeedMultiplier = 1f;
    public float suspicion = 0f;
    public float patrolRadius = 100f;

    [Header("Training Settings")]
    public bool trainingMode = false;
    public float learningRate = 0.1f;
    public int trainingEpochs = 5000;

    private Vector3 lastKnownPosition;
    private NavMeshAgent agent;
    private StreamWriter writer;

    private enum Action
    {
        Patrol = 0,
        Investigate = 1,
        Chase = 2
    }

    private int currentAction = 0;
    private float actionLockTimer = 0f;
    private float minActionTime = 1.2f;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        InitWeights();
        TrainNetworkFromMenu();
        ApplyAgentSpeed();

        lastKnownPosition = transform.position;

        if (trainingMode)
        {
            writer = new StreamWriter(Application.dataPath + "/dataset.csv");
            writer.WriteLine("distance,visible,suspicion,memory,action");
        }
    }

    void Update()
    {
        if (player == null) return;

        float[] input = GetInputs();
        float[] output = Forward(input);

        int proposedAction = ArgMax(output);

        UpdateActionState(proposedAction);
        ExecuteAction(currentAction);
        UpdateSuspicionOverTime();

        if (trainingMode)
            LogData(input, currentAction);
    }

    float[] GetInputs()
    {
        float dist = Vector3.Distance(transform.position, player.position);
        float distanceNorm = Mathf.Clamp01(dist / viewDistance);
        float visible = IsPlayerVisible() ? 1f : 0f;
        float memoryDist = Vector3.Distance(transform.position, lastKnownPosition);
        float memoryNorm = Mathf.Clamp01(memoryDist / viewDistance);

        return new float[] { distanceNorm, visible, suspicion, memoryNorm };
    }

    public void AddSuspicionFromEvent(float amount)
    {
        suspicion += amount;
        suspicion = Mathf.Clamp01(suspicion);
        if (player != null) lastKnownPosition = player.position;
    }

    void InitWeights()
    {
        for (int i = 0; i < 8; i++)
        {
            B1[i] = UnityEngine.Random.Range(-0.5f, 0.5f);
            for (int j = 0; j < 4; j++) W1[i, j] = UnityEngine.Random.Range(-0.5f, 0.5f);
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

    [ContextMenu("Rozpocznij Trening Sieci (Samouczenie)")]
    public void TrainNetworkFromMenu()
    {
        List<TrainingExample> dataset = GenerateTrainingData();

        for (int epoch = 0; epoch < trainingEpochs; epoch++)
        {
            foreach (var example in dataset)
            {
                Backpropagate(example.inputs, example.expectedOutputs);
            }
        }
        Debug.Log("AI przeszkolone pomyœlnie.");
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
            for (int j = 0; j < 4; j++) W1[i, j] -= learningRate * dZ1[i] * input[j];
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
        data.Add(new TrainingExample { inputs = new float[] { 0.1f, 1f, 0.5f, 0.1f }, expectedOutputs = new float[] { 0f, 0f, 1f } });
        data.Add(new TrainingExample { inputs = new float[] { 0.5f, 0f, 0.7f, 0.1f }, expectedOutputs = new float[] { 0f, 1f, 0f } });
        data.Add(new TrainingExample { inputs = new float[] { 0.9f, 0f, 0.0f, 0.9f }, expectedOutputs = new float[] { 1f, 0f, 0f } });
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
            for (int j = 0; j < 4; j++) s += W1[i, j] * x[j];
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
        if (!agent.isOnNavMesh) return;
        agent.speed = 2.5f * moveSpeedMultiplier;

        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            Vector3 point = RandomNavMeshPoint(transform.position, patrolRadius);
            agent.SetDestination(point);
        }
    }

    void Investigate()
    {
        if (!agent.isOnNavMesh) return;
        agent.speed = 3.5f * moveSpeedMultiplier;
        agent.SetDestination(lastKnownPosition);
    }

    void Chase()
    {
        if (!agent.isOnNavMesh) return;
        agent.speed = 6.5f * moveSpeedMultiplier;
        agent.acceleration = 12f;

        if (IsPlayerVisible())
        {
            lastKnownPosition = player.position;
            agent.SetDestination(player.position);
        }
        else
        {
            agent.SetDestination(lastKnownPosition);
        }
    }

    bool IsPlayerVisible()
    {
        Vector3 dir = player.position - transform.position;
        if (dir.magnitude > viewDistance) return false;
        if (Vector3.Angle(transform.forward, dir) > viewAngle * 0.5f) return false;
        if (Physics.Raycast(transform.position + Vector3.up * 1.5f, dir.normalized, dir.magnitude, obstacleMask)) return false;
        return true;
    }

    void UpdateSuspicionOverTime()
    {
        if (IsPlayerVisible())
        {
            suspicion += Time.deltaTime * 0.6f;
            lastKnownPosition = player.position;
        }
        else suspicion -= Time.deltaTime * 0.25f;
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

    float Sigmoid(float x) => 1f / (1f + Mathf.Exp(-x));

    float[] Softmax(float[] x)
    {
        float max = Mathf.Max(x[0], Mathf.Max(x[1], x[2]));
        float sum = 0f;
        float[] r = new float[3];
        for (int i = 0; i < 3; i++) { r[i] = Mathf.Exp(x[i] - max); sum += r[i]; }
        for (int i = 0; i < 3; i++) r[i] /= sum;
        return r;
    }

    int ArgMax(float[] x)
    {
        int best = 0;
        for (int i = 1; i < x.Length; i++) if (x[i] > x[best]) best = i;
        return best;
    }

    Vector3 RandomNavMeshPoint(Vector3 origin, float radius)
    {
        Vector3 randomDir = UnityEngine.Random.insideUnitSphere * radius;
        randomDir += origin;
        NavMeshHit hit;
        NavMesh.SamplePosition(randomDir, out hit, radius, NavMesh.AllAreas);
        return hit.position;
    }

    void ApplyAgentSpeed()
    {
        if (agent != null) agent.speed *= moveSpeedMultiplier;
    }

    void LogData(float[] input, int action)
    {
        if (writer == null) return;
        writer.WriteLine($"{input[0]},{input[1]},{input[2]},{input[3]},{action}");
    }

    private void OnDestroy()
    {
        if (writer != null) writer.Close();
    }
}