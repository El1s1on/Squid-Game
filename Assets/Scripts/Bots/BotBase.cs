using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public abstract class BotBase : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] protected float walkSpeed = 2f;
    [SerializeField] protected float runSpeed = 5f;

    protected NavMeshAgent agent;
    protected Animator animator;
    protected PlayerRagdoll ragdoll;
    protected bool isPatrolling;

    protected virtual void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        ragdoll = GetComponent<PlayerRagdoll>();
    }

    protected virtual void Update()
    {
        UpdateAnimations();
    }

    protected void UpdateAnimations()
    {
        animator.SetBool("Grounded", true);
        bool isMoving = agent.velocity.magnitude > 0.1f;
        animator.SetBool("Running", agent.speed > walkSpeed && isMoving);
        animator.SetBool("Move", isMoving);
    }

    protected IEnumerator PatrolRandomly(float patrolRange, float minDelay, float maxDelay, float runProbability)
    {
        isPatrolling = true;

        while (isPatrolling)
        {
            if (ragdoll && ragdoll.IsRagdoll)
                yield break;

            bool shouldRun = Random.value <= runProbability;
            agent.speed = shouldRun ? runSpeed : walkSpeed;

            Vector3 randomDirection = Random.insideUnitSphere * patrolRange + transform.position;
            if (NavMesh.SamplePosition(randomDirection, out NavMeshHit hit, patrolRange, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);

                float startTime = Time.time;
                float waitTime = Random.Range(minDelay, maxDelay);

                while (Time.time - startTime < waitTime && agent.remainingDistance > agent.stoppingDistance)
                {
                    yield return null;
                }
            }
            yield return null;
        }
    }

    public virtual void StopPatrolling()
    {
        isPatrolling = false;
        agent.ResetPath();
    }
}
