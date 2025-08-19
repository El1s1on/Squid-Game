using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public enum BotBehavior
{
    Careful,
    Aggressive,
    Dumb
}

[RequireComponent(typeof(NavMeshAgent), typeof(PlayerRagdoll))]
public class BotRLGL : BotBase
{
    [Header("RLGL Movement Settings")]
    [SerializeField] private float walkSpeedRLGL = 3f;
    [SerializeField] private float runSpeedRLGL = 6f;

    [Header("Zone Settings")]
    [SerializeField] private float zoneWidth = 20f;
    [SerializeField] private float zoneLength = 10f;
    private Transform zoneCenter;

    [Header("Patrol Settings")]
    [SerializeField] private float waitTime = 2f;
    [SerializeField] private float stoppingDistance = 0.5f;

    [Header("Push Settings")]
    [SerializeField] private float pushRange = 2f;
    [SerializeField] private float pushCooldown = 5f;

    private BotBehavior behavior;
    private RedLightGreenLight gameController;
    private PushSkill pushSkill;

    private bool isRedLight;
    private bool isInZone;
    private bool isZonePatrolling;
    private bool desireRun;
    private float lastPushTime = -Mathf.Infinity;
    private Vector3 currentDestination;

    protected override void Awake()
    {
        base.Awake();
        walkSpeed = walkSpeedRLGL;
        runSpeed = runSpeedRLGL;
        behavior = GetRandomBehavior();
    }

    private void Start()
    {
        gameController = FindFirstObjectByType<RedLightGreenLight>();
        gameController.onLightChanged += OnLightChanged;
        zoneCenter = gameController.safeZone;

        agent.stoppingDistance = stoppingDistance;
        InvokeRepeating(nameof(ToggleRunDesire), 0f, Random.Range(1, 10f));
    }

    private void Update()
    {
        base.Update();
        UpdateMovement();
        HandleAggressiveBehavior();
        HandleZoneNavigation();
    }

    private void OnDestroy()
    {
        if (gameController != null)
            gameController.onLightChanged -= OnLightChanged;
    }

    private void ToggleRunDesire() => desireRun = !desireRun;

    private void OnLightChanged(LightState light) =>
        StartCoroutine(ProcessLightChange(light));

    private IEnumerator ProcessLightChange(LightState light)
    {
        yield return new WaitForSeconds(GetThinkTimeBasedOnBehavior());
        isRedLight = (light == LightState.Red);
    }

    private float GetThinkTimeBasedOnBehavior() => behavior switch
    {
        BotBehavior.Careful => Random.Range(0.25f, 0.75f),
        BotBehavior.Aggressive => Random.Range(0.45f, 0.9f),
        _ => Random.Range(0.5f, 1f)
    };

    private void UpdateMovement()
    {
        agent.speed = CanRun() ? runSpeed : walkSpeed;
        agent.isStopped = ShouldStopAgent();
    }

    private void HandleAggressiveBehavior()
    {
        if (behavior != BotBehavior.Aggressive || Time.time <= lastPushTime + pushCooldown) return;

        GameObject nearestPlayer = FindNearestPlayer();
        if (nearestPlayer != null)
            PushPlayer(nearestPlayer);
    }

    private void HandleZoneNavigation()
    {
        isInZone = IsInRectangleZone(transform.position);

        if (!isInZone && !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            MoveToRandomPointInZone();
        }
        else if (isInZone && !isZonePatrolling)
        {
            StartCoroutine(PatrolRoutine());
        }
    }

    private void MoveToRandomPointInZone()
    {
        currentDestination = GetRandomPointInZone();
        agent.SetDestination(currentDestination);
        isZonePatrolling = false;
    }

    private GameObject FindNearestPlayer()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        GameObject nearestPlayer = null;
        float minDistance = Mathf.Infinity;

        foreach (GameObject player in players)
        {
            if (player == gameObject) continue;

            float distance = Vector3.Distance(transform.position, player.transform.position);
            if (distance < pushRange && distance < minDistance)
            {
                minDistance = distance;
                nearestPlayer = player;
            }
        }
        return nearestPlayer;
    }

    private bool CanRun()
    {
        if (behavior == BotBehavior.Dumb || isInZone)
            return desireRun;
        return true;
    }

    private void PushPlayer(GameObject player)
    {
        agent.SetDestination(player.transform.position);

        if (agent.remainingDistance <= agent.stoppingDistance)
        {
            lastPushTime = Time.time;
            pushSkill ??= GetComponentInChildren<PushSkill>();
            pushSkill?.UseSkill();
        }
    }

    private IEnumerator PatrolRoutine()
    {
        isZonePatrolling = true;

        while (isInZone && !ShouldStopAgent())
        {
            currentDestination = GetRandomPointInZone();
            agent.SetDestination(currentDestination);

            yield return new WaitUntil(() =>
                agent.remainingDistance <= agent.stoppingDistance ||
                !IsInRectangleZone(transform.position) ||
                ShouldStopAgent());

            if (!IsInRectangleZone(transform.position))
            {
                isZonePatrolling = false;
                yield break;
            }

            if (ShouldStopAgent()) yield break;

            yield return new WaitForSeconds(waitTime);
        }
        isZonePatrolling = false;
    }

    private bool IsInRectangleZone(Vector3 position)
    {
        if (zoneCenter == null) return false;
        Vector3 localPos = position - zoneCenter.position;
        return Mathf.Abs(localPos.x) <= zoneWidth / 2f &&
               Mathf.Abs(localPos.z) <= zoneLength / 2f;
    }

    private Vector3 GetRandomPointInZone()
    {
        if (zoneCenter == null) return transform.position;

        Vector3 randomPoint = zoneCenter.position + new Vector3(
            Random.Range(-zoneWidth / 2f, zoneWidth / 2f),
            0,
            Random.Range(-zoneLength / 2f, zoneLength / 2f)
        );

        if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, zoneWidth, NavMesh.AllAreas))
            return hit.position;

        return zoneCenter.position;
    }

    private bool ShouldStopAgent() =>
        (isRedLight && !isInZone) || ragdoll.IsRagdoll;

    private BotBehavior GetRandomBehavior()
    {
        var behaviors = System.Enum.GetValues(typeof(BotBehavior));
        return (BotBehavior)Random.Range(0, behaviors.Length);
    }

    private void OnDrawGizmosSelected()
    {
        if (zoneCenter == null) return;

        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(zoneCenter.position, new Vector3(zoneWidth, 0.1f, zoneLength));

        if (Application.isPlaying)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(currentDestination, 0.5f);
            Gizmos.DrawLine(transform.position, currentDestination);
        }
    }
}
