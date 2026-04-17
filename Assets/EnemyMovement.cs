using UnityEngine;
using UnityEngine.AI;

public class EnemyMovement : MonoBehaviour
{
    public NavMeshAgent agent;
    public Animator anim;

    // Tworzymy zmienn¹ przechowuj¹c¹ identyfikator parametru
    private int speedParameterHash;

    void Start()
    {
        // Generujemy unikalny numer dla parametru o nazwie Speed
        // Dziêki temu unikamy wpisywania tekstu w funkcji Update
        speedParameterHash = Animator.StringToHash("Speed");
    }

    void Update()
    {
        if (agent != null && anim != null)
        {
            float currentSpeed = agent.velocity.magnitude;

            // Przekazujemy wartoœæ u¿ywaj¹c wygenerowanego wczeœniej ID
            anim.SetFloat(speedParameterHash, currentSpeed);
        }
    }
}