using System;
using System.Collections.Generic;
using UnityEngine;

public enum KeyType
{
    Circle,
    Triangle,
    Square
}

public class PlayerHideAndSeek : MonoBehaviour
{
    [SerializeField] private List<KeyType> keys = new List<KeyType>();

    private void Start()
    {
        keys.Add(RandomKey());
    }

    public static KeyType RandomKey()
    {
        var values = Enum.GetValues(typeof(KeyType));
        int random = UnityEngine.Random.Range(0, values.Length);
        return (KeyType)values.GetValue(random);
    }
}
