using System.Collections;
using UnityEngine;

public class LobbyBot : BotBase
{
    [SerializeField] private float patrolRange = 15f;
    [SerializeField] private float minPatrolDelay = 2f;
    [SerializeField] private float maxPatrolDelay = 6f;
    [SerializeField] private float runProbability = 0.3f;

    private Lobby lobby;

    private void Start()
    {
        lobby = FindFirstObjectByType<Lobby>();
        StartCoroutine(PatrolRandomly(patrolRange, minPatrolDelay, maxPatrolDelay, runProbability));

        if (lobby)
            lobby.onLobbyReady += OnLobbyReady;
    }

    private void OnDestroy()
    {
        if (lobby)
            lobby.onLobbyReady -= OnLobbyReady;
    }

    private void OnLobbyReady()
    {
        StopPatrolling();
        MoveToElevator();
    }

    private void MoveToElevator()
    {
        if (lobby.entryZone != null)
        {
            agent.speed = runSpeed;
            agent.SetDestination(lobby.entryZone.position);
        }
    }

    protected override void Update()
    {
        agent.isStopped = ragdoll.IsRagdoll;
        base.Update();
    }
}
