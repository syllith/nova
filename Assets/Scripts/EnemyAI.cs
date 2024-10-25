using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    public Transform playerTransform;
    public float targetDistance = 10f;
    public float movementSpeed = 1.5f;
    public float obstacleRange = 5.0f;
    public LayerMask obstacleLayer;
    public int rayCount = 5;
    public float raySpreadAngle = 30f;
    public float avoidanceMultiplier = 0.5f;
    public float rotationSpeed = 2f;
    public float reevaluatePathDelay = 1f;

    private Animator animator;
    private Ray[] rays;
    private RaycastHit[] hits;
    private bool[] rayHits;
    private float timeSinceLastReevaluation = 0f;
    private Vector3 lastDirection;
    private float directionChangeCooldown = 0.5f; // Cooldown time in seconds before changing direction
    private float lastDirectionChangeTime = 0f;



    void Start()
    {
        animator = GetComponent<Animator>();
        rays = new Ray[rayCount];
        hits = new RaycastHit[rayCount];
        rayHits = new bool[rayCount];
        lastDirection = Vector3.zero;

        if (playerTransform == null)
        {
            playerTransform = GameObject.FindWithTag("Player").transform;
        }
    }


    void Update()
    {
        float distanceToPlayer = Vector3.Distance(playerTransform.position, transform.position);

        if (distanceToPlayer > targetDistance)
        {
            animator.SetBool("IsIdle", true);
            animator.SetBool("IsWalking", false);
        }
        else
        {
            animator.SetBool("IsIdle", false);
            animator.SetBool("IsWalking", true);

            Vector3 avoidanceVector = Vector3.zero;
            bool isPathClear = CastRaysForObstacleAvoidance(ref avoidanceVector);
            Vector3 finalDirection;

            if (!isPathClear)
            {
                timeSinceLastReevaluation += Time.deltaTime;
                if (timeSinceLastReevaluation >= reevaluatePathDelay)
                {
                    timeSinceLastReevaluation = 0f;
                    // Reverse the direction temporarily
                    lastDirection = -transform.forward;
                    finalDirection = lastDirection;
                }
                else
                {
                    // Continue with the current steering direction
                    finalDirection = GetSteeringDirection(avoidanceVector, isPathClear);
                }
            }
            else
            {
                timeSinceLastReevaluation = 0f;
                finalDirection = (playerTransform.position - transform.position).normalized;
                lastDirection = finalDirection;
            }

            SmoothMovement(finalDirection);
        }
    }



    Vector3 GetSteeringDirection(Vector3 avoidanceVector, bool isPathClear)
    {
        if (isPathClear)
        {
            lastDirection = (playerTransform.position - transform.position).normalized;
            lastDirectionChangeTime = Time.time;
            return lastDirection;
        }

        Vector3 weightedDirection = (avoidanceVector.normalized * avoidanceMultiplier +
                                     lastDirection * (1 - avoidanceMultiplier)).normalized;

        if (Time.time - lastDirectionChangeTime > directionChangeCooldown)
        {
            lastDirection = weightedDirection;
            lastDirectionChangeTime = Time.time;
        }

        return lastDirection;
    }

    void SmoothMovement(Vector3 finalDirection)
    {
        Quaternion lookRotation = Quaternion.LookRotation(finalDirection);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * rotationSpeed);
        transform.position += transform.forward * movementSpeed * Time.deltaTime;
    }

    bool CastRaysForObstacleAvoidance(ref Vector3 avoidanceVector)
    {
        bool clearPath = true;
        for (int i = 0; i < rayCount; i++)
        {
            float angle = -raySpreadAngle / 2 + (raySpreadAngle / (rayCount - 1)) * i;
            Vector3 rayDirection = Quaternion.Euler(0, angle, 0) * transform.forward;
            rays[i] = new Ray(transform.position, rayDirection);

            rayHits[i] = Physics.Raycast(rays[i], out hits[i], obstacleRange, obstacleLayer);
            if (rayHits[i])
            {
                clearPath = false;
                Vector3 perpendicular = Vector3.Cross(rayDirection, Vector3.up).normalized;
                perpendicular *= rayDirection.y < 0 ? -1f : 1f;
                avoidanceVector += perpendicular;
            }
        }

        if (!clearPath)
        {
            avoidanceVector = avoidanceVector.normalized * avoidanceMultiplier;
        }

        return clearPath;
    }

    void OnDrawGizmosSelected()
    {
        if (rays != null && rayHits != null)
        {
            for (int i = 0; i < rayCount; i++)
            {
                Gizmos.color = rayHits[i] ? Color.red : Color.green;
                Gizmos.DrawRay(rays[i].origin, rays[i].direction * obstacleRange);
            }
        }
    }



    public void TakeDamage(int damage)
    {
        // Implementation for taking damage
        return;
    }
}
