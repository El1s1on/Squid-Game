using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PlayerAvatar : MonoBehaviour
{
    [SerializeField] private List<TMP_Text> numberTexts;
    private PlayerData playerData;

    public void LoadAvatar(PlayerData player)
    {
        playerData = player;

        string formattedId = player.playerId.ToString("D3");
        foreach (TMP_Text numberText in numberTexts)
        {
            numberText.text = formattedId;
        }
    }

    public PlayerData GetPlayerData() => playerData;
}
