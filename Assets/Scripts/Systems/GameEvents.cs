using System;

public static class GameEvents
{
    public static Action<PlayerData> OnPlayerEliminated;
    public static Action<PlayerData> OnPlayerRespawned;
    public static Action<InventoryItem> GiveItem;
    public static Action OnRoundEnded;
    public static Action OnRoundStarted;
    public static Action OnSceneLoad;
}