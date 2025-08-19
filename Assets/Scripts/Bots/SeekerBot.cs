using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

public class SeekerBot : BotBase
{
    [Header("Vision Settings")]
    [SerializeField] private Transform eyes; // позиция глаз
    [SerializeField] private float viewRadius = 10f;
    [SerializeField] private float viewAngle = 90f;
    [SerializeField] private float peripheralRadius = 4f;
    [SerializeField] private float interest = 1.5f;
    [SerializeField] private LayerMask whatIsPlayer;
    [SerializeField] private LayerMask obstacleMask;

    [Header("Attack Settings")]
    [SerializeField] private Knife knife;
    [SerializeField] private float attackRangeMin, attackRangeMax;
    [SerializeField] private float attackCooldown = 1.5f;
    private float attackRange;

    [Header("Patrol Settings")]
    [SerializeField] private float patrolRange = 15f;
    [SerializeField] private float minDelay = 2f;
    [SerializeField] private float maxDelay = 5f;
    [SerializeField, Range(0f, 1f)] private float runProbability = 0.3f;

    [Header("Memory Settings")]
    [SerializeField] private float memoryTime = 3f;

    private Transform targetPlayer;
    private Vector3 lastKnownPosition;
    private float lastSeenTime;
    private List<Transform> visibleTargets = new List<Transform>();
    private float lastAttackTime;
    private Coroutine patrolRoutine;
    private bool isChasing;

    protected override void Awake()
    {
        base.Awake();
        attackRange = Random.Range(attackRangeMin, attackRangeMax);
        if (eyes == null)
            eyes = transform;
    }

    private void Start()
    {
        StartPatrolling();

        knife.SetPlayer(gameObject);
        knife.Equip();
        knife.gameObject.SetActive(true);
    }

    protected override void Update()
    {
        base.Update();

        agent.isStopped = ragdoll.IsRagdoll;

        FindVisibleTargets();

        if (visibleTargets.Count > 0)
        {
            targetPlayer = visibleTargets[0];
            lastKnownPosition = targetPlayer.position;
            lastSeenTime = Time.time;

            if (!isChasing)
            {
                StopPatrolling();
                isChasing = true;
            }
        }

        if (isChasing)
            ChaseWithMemory();
    }

    private void FindVisibleTargets()
    {
        visibleTargets.Clear();

        Collider[] targetsInViewRadius = Physics.OverlapSphere(eyes.position, viewRadius, whatIsPlayer);

        foreach (Collider targetCollider in targetsInViewRadius)
        {
            // Игнорируем свои собственные части тела
            if (targetCollider.transform.root == transform)
                continue;

            Transform targetTorso = FindChildByName(targetCollider.transform.root, "Torso");
            if (targetTorso == null)
                continue;

            Vector3 dirToTarget = (targetTorso.position - eyes.position).normalized;
            float angleToTarget = Vector3.Angle(eyes.forward, dirToTarget);
            float dstToTarget = Vector3.Distance(eyes.position, targetTorso.position);

            if (dstToTarget <= peripheralRadius * interest)
            {
                visibleTargets.Add(targetTorso);
                continue;
            }

            if (angleToTarget > viewAngle / 2f)
                continue;

            if (Physics.Raycast(eyes.position, dirToTarget, dstToTarget, obstacleMask))
                continue;

            visibleTargets.Add(targetTorso);
        }
    }

    public Transform FindChildByName(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name)
                return child;

            Transform found = FindChildByName(child, name);
            if (found != null)
                return found;
        }
        return null;
    }

    private void ChaseWithMemory()
    {
        agent.speed = runSpeed;

        if (targetPlayer != null)
        {
            lastKnownPosition = targetPlayer.position;
            agent.SetDestination(targetPlayer.position);

            if (Vector3.Distance(transform.position, targetPlayer.position) <= attackRange && Time.time - lastAttackTime >= attackCooldown)
                StartCoroutine(TryAttack());

            if (!visibleTargets.Contains(targetPlayer))
                targetPlayer = null;
        }
        else
        {

            if (Time.time - lastSeenTime <= memoryTime)
            {
                if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.05f)
                {
                    Vector3 randomSearch = lastKnownPosition + Random.insideUnitSphere * Random.Range(1f,5f);
                    randomSearch.y = transform.position.y;

                    if (NavMesh.SamplePosition(randomSearch, out NavMeshHit hit, 5f, NavMesh.AllAreas))
                    {
                        agent.SetDestination(hit.position);
                    }
                }
            }
            else
            {
                if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.05f)
                {
                    Vector3 randomSearch = lastKnownPosition + Random.insideUnitSphere * 10f;
                    randomSearch.y = transform.position.y;

                    if (NavMesh.SamplePosition(randomSearch, out NavMeshHit hit, 3f, NavMesh.AllAreas))
                    {
                        agent.SetDestination(hit.position);
                    }

                    if (Vector3.Distance(transform.position, lastKnownPosition) > 3f)
                    {
                        isChasing = false;
                        StartPatrolling();
                    }
                }
            }
        }
    }

    private IEnumerator TryAttack()
    {
        lastAttackTime = Time.time;

        yield return new WaitForSeconds(Random.Range(.1f, .5f));
        
        knife.Attack();
        attackRange = Random.Range(attackRangeMin, attackRangeMax);
    }

    private void StartPatrolling()
    {
        if (patrolRoutine != null)
            StopCoroutine(patrolRoutine);

        patrolRoutine = StartCoroutine(PatrolWithPauses());
        isPatrolling = true;
    }

    private IEnumerator PatrolWithPauses()
    {
        isPatrolling = true;

        while (isPatrolling)
        {
            bool shouldRun = Random.value <= runProbability;
            agent.speed = shouldRun ? runSpeed : walkSpeed;

            Vector3 randomDirection = Random.insideUnitSphere * patrolRange + transform.position;
            if (NavMesh.SamplePosition(randomDirection, out NavMeshHit hit, patrolRange, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);

                while (agent.pathPending || agent.remainingDistance > agent.stoppingDistance)
                    yield return null;

                if (Random.value < 0.4f)
                    yield return new WaitForSeconds(Random.Range(minDelay, maxDelay));
            }
            yield return null;
        }
    }

    public override void StopPatrolling()
    {
        base.StopPatrolling();
        if (patrolRoutine != null)
        {
            StopCoroutine(patrolRoutine);
            patrolRoutine = null;
        }
    }
}