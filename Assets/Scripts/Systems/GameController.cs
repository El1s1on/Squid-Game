using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using YG;

public class GameController : SingletonMono<GameController>
{
    [SerializeField] private List<SceneData> gameSequence;
    [SerializeField] private SceneData lobbyScene;
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private int maxPlayers = 55;
    [SerializeField] private int minPlayers = 45;
    private SceneData currentScene;
    private int currentSpawnIndex = 0;
    private int gameIndex = -1;

    [Header("Timer")]
    private Coroutine timerCoroutine;
    private float timeRemaining;
    private bool isRunning = false;
    public event System.Action OnTimerFinished;
    public event System.Action<float> OnTimerUpdate;

    private void Start() => StartCoroutine(BootstrapRoutine());

    private void OnEnable() => GameEvents.OnRoundEnded += OnRoundEnded;

    private void OnDisable() => GameEvents.OnRoundEnded -= OnRoundEnded;

    private IEnumerator BootstrapRoutine()
    {
        int playersInGame = Random.Range(minPlayers, maxPlayers + 1);
        yield return PlayerManager.Instance.GeneratePlayers(playersInGame);
        yield return LoadScene(lobbyScene);
    }

    private IEnumerator LoadScene(SceneData scene)
    {
        if (!LocalPlayerIsAlive())
        {
            QuitGames();
            yield break;
        }

        yield return BaseUI.Instance.Fade(1, .5f);

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(scene.sceneName);
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        OnLoadScene(scene);
    }

    private void OnLoadScene(SceneData scene)
    {
        currentScene = scene;

        if (scene == lobbyScene)
            StartCoroutine(InitLobby());

        if (scene.autoLoadPlayers)
            StartCoroutine(LoadPlayers(scene.botPrefab));

        GameEvents.OnSceneLoad?.Invoke();
    }

    public IEnumerator LoadPlayers(GameObject botPrefab = null)
    {
        if (botPrefab == null && currentScene.botPrefab != null)
            botPrefab = currentScene.botPrefab;

        foreach (PlayerData player in PlayerManager.Instance.Players)
        {
            if (!player.IsAlive) continue;

            if (player.IsBot && botPrefab != null)
            {
                GameObject playerObject = Instantiate(botPrefab, GetSpawnPoint().position, GetSpawnPoint().rotation);
                playerObject.GetComponent<PlayerAvatar>().LoadAvatar(player);
            }
            else if (!player.IsBot && playerPrefab != null)
            {
                GameObject playerObject = Instantiate(playerPrefab, GetSpawnPoint().position, GetSpawnPoint().rotation);
                playerObject.GetComponent<PlayerAvatar>().LoadAvatar(player);
            }
        }

        yield return new WaitForSeconds(1f);
        yield return BaseUI.Instance.Fade(0, .5f);

        GameEvents.OnRoundStarted?.Invoke();
    }

    private bool LocalPlayerIsAlive()
    {
        foreach (PlayerData player in PlayerManager.Instance.Players)
        {
            if (!player.IsBot && !player.IsAlive)
                return false;
        }

        return true;
    }

    private void OnRoundEnded()
    {
        StartCoroutine(LoadScene(lobbyScene));
    }

    private IEnumerator InitLobby()
    {
        yield return new WaitForSeconds(1f);
        
        Lobby lobby = FindFirstObjectByType<Lobby>();
        lobby.Init();
    }

    public void RunGame()
    {
        gameIndex++;
        StartCoroutine(LoadScene(gameSequence[gameIndex]));
    }

    private Transform GetSpawnPoint()
    {
        GameObject[] spawnPoints = GameObject.FindGameObjectsWithTag("Spawnpoint");
        Transform spawnPoint = spawnPoints[currentSpawnIndex].transform;
        currentSpawnIndex = (currentSpawnIndex + 1) % spawnPoints.Length;

        return spawnPoint;
    }

    public void StartTimer(float duration)
    {
        timeRemaining = duration + 1;
        isRunning = true;

        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
        }

        OnTimerUpdate?.Invoke(timeRemaining);

        timerCoroutine = StartCoroutine(TimerRoutine());
    }

    private IEnumerator TimerRoutine()
    {
        while (timeRemaining > 0)
        {
            if (!isRunning)
            {
                yield break;
            }

            timeRemaining -= Time.deltaTime;
            if (timeRemaining < 0) timeRemaining = 0;

            OnTimerUpdate?.Invoke(timeRemaining);
            yield return null;
        }

        isRunning = false;
        OnTimerFinished?.Invoke();
        timerCoroutine = null;
    }

    public void StopTimer()
    {
        if (isRunning)
        {
            if (timerCoroutine != null)
            {
                StopCoroutine(timerCoroutine);
            }

            isRunning = false;
            timerCoroutine = null;
            OnTimerFinished?.Invoke();
        }
    }

    public void QuitGames() => StartCoroutine(QuitGamesCoroutine());

    private IEnumerator QuitGamesCoroutine()
    {
        yield return BaseUI.Instance.Fade(1, .5f);

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("Menu");
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        yield return BaseUI.Instance.Fade(0, .5f);

        Destroy(Instance.gameObject);
        Destroy(PlayerManager.Instance.gameObject);

        YG2.InterstitialAdvShow();
    }
}
