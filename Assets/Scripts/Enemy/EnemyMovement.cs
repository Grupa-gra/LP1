using UnityEngine;
using UnityEngine.AI;

public class EnemyMovement : MonoBehaviour
{
    public NavMeshAgent agent;
    public Animator anim;
    private int speedParameterHash;

    void Start()
    {
        speedParameterHash = Animator.StringToHash("Speed");
    }

    void Update()
    {
        if (agent != null && anim != null)
        {
            float currentSpeed = agent.velocity.magnitude;
            anim.SetFloat(speedParameterHash, currentSpeed);
        }
    }
}