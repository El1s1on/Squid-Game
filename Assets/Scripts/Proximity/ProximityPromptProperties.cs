using UnityEngine;

[CreateAssetMenu(fileName = "NewProximityPromptProperties", menuName = "ProximityPrompt/Properties")]
public class ProximityPromptProperties : ScriptableObject
{
    [Header("Основные параметры")]
    public string actionText = "Взаимодействовать";
    public float activationDistance = 3f;
    public Vector3 uiOffset = new Vector3(0, 2f, 0);

    [Header("Тип взаимодействия")]
    [Tooltip("0 = мгновенно, >0 = удержание (в секундах)")]
    public float holdDuration = 0f;
    public bool useMultipleAction = false;
    public string alternateActionText = "Отключить";

    [Header("Управление")]
    public KeyCode activationKey = KeyCode.E;
}
