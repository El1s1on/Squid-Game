using UnityEngine;

[CreateAssetMenu(fileName = "SceneData", menuName = "ScriptableObjects/SceneData", order = 1)]
public class SceneData : ScriptableObject
{
    public string sceneName;
    public bool autoLoadPlayers;
    public GameObject botPrefab;
}