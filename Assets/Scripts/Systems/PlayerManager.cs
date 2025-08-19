using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class PlayerData
{
    public int playerId;
    public string playerName;
    public bool IsAlive = true;
    public bool IsBot = false;

    public PlayerData(int id, string name, bool isBot)
    {
        playerId = id;
        playerName = name;
        IsBot = isBot;
    }
}

public class PlayerManager : SingletonMono<PlayerManager>
{
    private const int MinPlayerId = 1;
    private const int MaxPlayerId = 456;

    private void OnEnable()
    {
        GameEvents.OnPlayerRespawned += OnPlayerRespawned;
        GameEvents.OnPlayerEliminated += OnPlayerEliminated;
    }

    private void OnDisable()
    {
        GameEvents.OnPlayerRespawned -= OnPlayerRespawned;
        GameEvents.OnPlayerEliminated -= OnPlayerEliminated;
    }

    [SerializeField]
    private List<PlayerData> players = new List<PlayerData>();
    public IReadOnlyList<PlayerData> Players => players;

    public IEnumerator GeneratePlayers(int count)
    {
        if (count <= 0)
        {
            Debug.LogWarning("Count must be positive. No players generated.");
            yield break;
        }

        for (int i = 0; i < count - 1; i++)
        {
            GenerateBotPlayer();
            yield return null;
        }

        GenerateHumanPlayer();
    }

    private void GenerateBotPlayer() => GeneratePlayerData(isBot: true);
    private void GenerateHumanPlayer() => GeneratePlayerData(isBot: false);

    private void GeneratePlayerData(bool isBot)
    {
        int id = GenerateUniquePlayerId();
        string name = $"Player {id}";

        var playerData = new PlayerData(id, name, isBot);
        players.Add(playerData);
    }

    private int GenerateUniquePlayerId()
    {
        int id;
        int attempts = 0;
        const int maxAttempts = 100;

        do
        {
            id = Random.Range(MinPlayerId, MaxPlayerId + 1);
            attempts++;

            if (attempts >= maxAttempts)
            {
                Debug.LogError("Failed to generate unique player ID");
                return -1;
            }

        } while (players.Exists(p => p.playerId == id));

        return id;
    }

    private void OnPlayerEliminated(PlayerData player) => player.IsAlive = false;

    private void OnPlayerRespawned(PlayerData player) => player.IsAlive = true;

    public PlayerData LocalPlayer() => players.FirstOrDefault(p => p.IsBot == false);
}
