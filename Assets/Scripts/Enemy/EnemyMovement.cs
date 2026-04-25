using UnityEngine;
using UnityEngine.AI;

public class EnemyMovement : MonoBehaviour
{
    public NavMeshAgent agent;
    public Animator anim;
    private int speedParameterHash;

    [Header("Terrain Alignment")]
    [Tooltip("Wybierz warstwê, na której znajduje siê pod³o¿e (aby Raycast ignorowa³ samego gracza/wrogów)")]
    public LayerMask groundLayer = -1;
    public float raycastOffset = 1f;
    public float raycastLength = 2f;
    public float alignmentSpeed = 10f;

    void Start()
    {
        speedParameterHash = Animator.StringToHash("Speed");
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (anim == null) anim = GetComponent<Animator>();
    }

    void Update()
    {
        if (agent != null && anim != null)
        {
            float currentSpeed = agent.velocity.magnitude;
            anim.SetFloat(speedParameterHash, currentSpeed);
        }
    }
    void LateUpdate()
    {
        AlignToTerrain();
    }

    void AlignToTerrain()
    {
        Vector3 rayStart = transform.position + Vector3.up * raycastOffset;

        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, raycastLength, groundLayer))
        {
            Vector3 projectedForward = Vector3.ProjectOnPlane(transform.forward, hit.normal).normalized;

            if (projectedForward != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(projectedForward, hit.normal);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * alignmentSpeed);
            }
        }
    }
}