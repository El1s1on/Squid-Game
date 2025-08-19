using DG.Tweening;
using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class Lobby : MonoBehaviour
{
    [SerializeField] private TMP_Text monitorText;
    [SerializeField] private GameObject rightDoor;
    [SerializeField] private GameObject leftDoor;

    [Header("Entry Zone")]
    public Transform entryZone;
    [SerializeField] private LayerMask whatIsPlayer;
    [SerializeField] private Vector3 entryZoneSize;
    private Coroutine entryZoneCoroutine;
    public Action onLobbyReady;

    public void Init()
    {
        GameController.Instance.StartTimer(2f);
        GameController.Instance.OnTimerFinished += OpenDoors;

        monitorText.gameObject.SetActive(true);
        monitorText.text = "WAITING FOR PARTICIPANTS";
    }

    private IEnumerator EntryZoneHandler()
    {
        while (true)
        {
            var playerColliders = Physics.OverlapBox(entryZone.position, entryZoneSize / 2, Quaternion.identity, whatIsPlayer);

            foreach (var collider in playerColliders)
            {
                if(collider.transform.root.GetComponent<Collider>())
                {
                    GameController.Instance.StopTimer();
                    StopCoroutine(entryZoneCoroutine);
                    yield break;
                }
                else
                {
                    Destroy(collider.transform.root.gameObject);
                }
            }

            yield return new WaitForSeconds(.1f);
        }
    }

    private void OpenDoors()
    {
        monitorText.text = "Make your way into the elevator for the game";
        GameController.Instance.OnTimerFinished -= OpenDoors;

        leftDoor.transform.DOLocalMoveX(-10f, 1f);
        rightDoor.transform.DOLocalMoveX(10f, 1f);

        GameController.Instance.StartTimer(2f);
        GameController.Instance.OnTimerFinished += EnterGame;

        entryZoneCoroutine = StartCoroutine(EntryZoneHandler());
        onLobbyReady?.Invoke();
    }

    private void EnterGame()
    {
        GameController.Instance.OnTimerFinished -= EnterGame;
        GameController.Instance.RunGame();
    }

    private void OnDrawGizmos()
    {
        if(entryZone)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(entryZone.position, entryZoneSize);
        }
    }
}
