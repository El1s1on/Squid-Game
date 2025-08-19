using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class PlayerDalgonaData
{
    public GameObject Player { get; set; }
    public DalgonaCookie Cookie { get; set; }
    public PlayerData PlayerData { get; set; }
    public bool Completed { get; set; }
}

public class Dalgona : MonoBehaviour
{
    [SerializeField] private GameObject cutscene;
    [SerializeField] private Transform tinCanPivot;
    [SerializeField] private List<DalgonaCookie> cookiePrefabs;
    [SerializeField] private List<Guard> guards;
    [SerializeField] private float gameTime;
    private DalgonaCookie localPlayerCookie;
    private List<PlayerDalgonaData> playersDalgonaData = new List<PlayerDalgonaData>();
    
    private void OnEnable()
    {
        GameEvents.OnRoundStarted += OnRoundStarted;
        GameEvents.OnPlayerRespawned += OnPlayerRespawned;
        GameEvents.OnSceneLoad += OnSceneLoad;
    }

    private void OnDisable()
    {
        GameEvents.OnRoundStarted -= OnRoundStarted;
        GameEvents.OnPlayerRespawned -= OnPlayerRespawned;
        GameEvents.OnSceneLoad -= OnSceneLoad;
    }

    private void OnSceneLoad() => StartCoroutine(CutsceneCoroutine());

    private IEnumerator CutsceneCoroutine()
    {
        PlayerAvatar playerAvatar = cutscene.GetComponent<PlayerAvatar>();
        playerAvatar.LoadAvatar(PlayerManager.Instance.LocalPlayer());
        cutscene.SetActive(true);

        localPlayerCookie = cookiePrefabs[Random.Range(0, cookiePrefabs.Count)];
        var cookie = Instantiate(localPlayerCookie, tinCanPivot);
        GameObject crack = cookie.transform.Find("Crack0").gameObject;
        crack.SetActive(true);

        Animator cutsceneAnimator = cutscene.GetComponent<Animator>();

        var clipInfo = cutsceneAnimator.GetCurrentAnimatorClipInfo(0);
        float cutsceneDuration = clipInfo.Length > 0 ? clipInfo[0].clip.length : 0f;

        yield return BaseUI.Instance.Fade(0, 0.5f);

        yield return new WaitForSeconds(cutsceneDuration);

        yield return BaseUI.Instance.Fade(1, 0.5f);

        cutscene.SetActive(false);
        Destroy(cutscene);

        StartCoroutine(GameController.Instance.LoadPlayers());
    }

    private void OnRoundStarted() => StartCoroutine(InitializeGame());

    private IEnumerator InitializeGame()
    {
        yield return new WaitForEndOfFrame();
        InitializePlayers();
        yield return new WaitForSeconds(2f);
        StartMiniGames();

        GameController.Instance.StartTimer(gameTime);
        GameController.Instance.OnTimerFinished += OnTimerFinished;
    }

    private void OnTimerFinished()
    {
        foreach (var playerData in playersDalgonaData)
        {
            if (!playerData.Completed)
                StartCoroutine(playerData.Cookie.BreakCookie());
        }
    }

    private void InitializePlayers()
    {
        var players = GameObject.FindGameObjectsWithTag("Player").ToList();

        foreach (var player in players)
        {
            PlayerDalgonaData playerDalgona = new PlayerDalgonaData();
            playerDalgona.Player = player;
            playerDalgona.PlayerData = player.GetComponent<PlayerAvatar>().GetPlayerData();
            playerDalgona.Cookie = CreateCookieForPlayer(playerDalgona);

            SetupPlayerForMiniGame(player);
            playersDalgonaData.Add(playerDalgona);

            playerDalgona.Cookie.OnBreak += HandleCookieBreak;
            playerDalgona.Cookie.OnComplete += HandleCookieComplete;
        }
    }

    private DalgonaCookie CreateCookieForPlayer(PlayerDalgonaData playerDalgonaData)
    {
        var cookiePrefab = playerDalgonaData.PlayerData.IsBot ? cookiePrefabs[Random.Range(0, cookiePrefabs.Count)] : localPlayerCookie;
        var cookie = Instantiate(cookiePrefab, playerDalgonaData.Player.transform);
        cookie.transform.localPosition = playerDalgonaData.Player.transform.forward;
        cookie.Initialize(playerDalgonaData);
        return cookie;
    }

    private void SetupPlayerForMiniGame(GameObject player)
    {
        TogglePlayerComponents(player, false);
        var animator = player.GetComponent<Animator>();
        animator.SetBool("Grounded", true);
        animator.SetBool("Dalgona", true);
    }

    private void StartMiniGames()
    {
        foreach (var playerData in playersDalgonaData)
        {
            playerData.Cookie.StartMiniGame();
        }
    }

    private void HandleCookieBreak(PlayerDalgonaData playerDalgonaData)
    {
        var player = playersDalgonaData.FirstOrDefault(p => p.PlayerData == playerDalgonaData.PlayerData);
        player.Cookie.OnBreak -= HandleCookieBreak;
        player.Completed = false;
        player.Player.GetComponent<Animator>().SetTrigger("DalgonaFail");

        Guard closestGuard = GetClosestGuard(player.Player.transform.position);
        if (closestGuard != null)
        {
            StartCoroutine(closestGuard.Eliminate(player.Player));
        }
    }

    private Guard GetClosestGuard(Vector3 playerPosition)
    {
        if (guards == null || guards.Count == 0) return null;

        Guard closestGuard = null;
        float minDistance = Mathf.Infinity;

        foreach (var guard in guards)
        {
            if (guard == null) continue;

            float distance = Vector3.Distance(guard.transform.position, playerPosition);
            if (distance < minDistance)
            {
                minDistance = distance;
                closestGuard = guard;
            }
        }

        return closestGuard;
    }

    private void OnPlayerRespawned(PlayerData playerData)
    {
        var playerDalgonaData = playersDalgonaData.FirstOrDefault(p => p.PlayerData == playerData);
        if (playerDalgonaData != null)
        {
            playerDalgonaData.Completed = true;
            ResetPlayerToNormalState(playerDalgonaData.Player);
        }
    }

    private void HandleCookieComplete(PlayerDalgonaData playerDalgonaData)
    {
        var player = playersDalgonaData.FirstOrDefault(p => p.PlayerData == playerDalgonaData.PlayerData);
        player.Cookie.OnComplete -= HandleCookieComplete;
        player.Completed = true;
        ResetPlayerToNormalState(playerDalgonaData.Player);
    }

    private void ResetPlayerToNormalState(GameObject player)
    {
        var animator = player.GetComponent<Animator>();
        animator.SetBool("Dalgona", false);
        TogglePlayerComponents(player, true);
    }

    private void TogglePlayerComponents(GameObject player, bool enabled)
    {
        if (player.TryGetComponent<PlayerAnimations>(out var playerAnimations))
            playerAnimations.enabled = enabled;

        if (player.TryGetComponent<PlayerMovement>(out var playerMovement))
            playerMovement.enabled = enabled;
    }
}