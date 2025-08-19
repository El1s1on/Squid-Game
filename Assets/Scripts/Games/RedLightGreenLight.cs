using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum LightState
{
    None,
    Green,
    Red
}

public class RedLightGreenLight : MonoBehaviour
{
    private const float MIN_DELTA_TIME = 0.001f;
    private const float SHOOT_COOLDOWN = 0.035f;
    private const float FLASH_FADE_IN_DURATION = 0.075f;
    private const float FLASH_FADE_OUT_DURATION = 0.5f;
    private const float FLASH_MAX_ALPHA = 0.35f;
    private const float HEAD_ROTATION_SPEED = 5f;
    private const float INITIAL_DELAY = 1f;
    private const float TRAFFIC_LIGHT_ANIM_DURATION = 1f;
    private const float TRAFFIC_LIGHT_Y_POSITION = 45f;
    private const float RED_LIGHT_ACTIVATION_DELAY = 0.75f;

    [Header("References")]
    public Transform safeZone;
    [SerializeField] private InventoryItem pushSkill;
    [SerializeField] private GameObject barrier;
    [SerializeField] private Image flash;
    [SerializeField] private GameObject projectile;
    [SerializeField] private LayerMask whatIsPlayer;
    [SerializeField] private List<Transform> bulletPoints;
    [SerializeField] private AudioClip shootSFX;
    [SerializeField] private AudioClip greenSFX;
    [SerializeField] private AudioClip redSFX;
    [SerializeField] private AudioClip tickSFX;
    [SerializeField] private Transform dollHead;

    [Header("Game Settings")]
    [SerializeField] private float bulletForce = 50f;
    [SerializeField] private float gameTime = 150f;

    [Header("UI")]
    [SerializeField] private TMP_Text timeText;
    [SerializeField] private Image trafficLight;
    [SerializeField] private Sprite greenLight;
    [SerializeField] private Sprite redLight;

    [Header("Check Zone")]
    [SerializeField] private Transform checkZone;
    [SerializeField] private Vector3 checkZoneSize = Vector3.one;

    [Header("Movement Detection")]
    [SerializeField] private float movementThreshold = 0.5f;
    [SerializeField] private float maxValidMovement = 5f;

    private LightState currentLightState = LightState.None;
    private Quaternion targetHeadRotation;
    private AudioSource audioSource;
    private List<PlayerMovementData> players = new List<PlayerMovementData>();
    private Sprite noLightSprite;
    private bool canShoot = false;
    private int lastTickSecond;
    private bool gameIsRunning;

    public System.Action<LightState> onLightChanged;

    [System.Serializable]
    public class PlayerMovementData
    {
        public Transform transform;
        public Vector3 previousPosition;
        public Vector3 estimatedVelocity;
        public bool isNewPlayer;
    }

    private void OnEnable()
    {
        if (GameController.Instance != null)
        {
            GameController.Instance.OnTimerUpdate += UpdateTimer;
            GameController.Instance.OnTimerFinished += HandleGameEnd;
        }
    }

    private void OnDisable()
    {
        if (GameController.Instance != null)
        {
            GameController.Instance.OnTimerUpdate -= UpdateTimer;
            GameController.Instance.OnTimerFinished -= HandleGameEnd;
        }
    }

    #region Unity Lifecycle

    private void Start()
    {
        InitializeComponents();
        StartCoroutine(GameLoop());
    }

    private void Update()
    {
        UpdateHeadRotation();
    }

    private void OnDrawGizmos()
    {
        DrawCheckZoneGizmo();
    }

    #endregion

    #region Initialization

    private void InitializeComponents()
    {
        noLightSprite = trafficLight.sprite;
        audioSource = GetComponent<AudioSource>();
    }

    #endregion

    #region Game Loop

    private IEnumerator GameLoop()
    {
        yield return new WaitForSeconds(INITIAL_DELAY);

        AnimateTrafficLightEntrance();

        yield return new WaitForSeconds(5f);

        GameEvents.GiveItem?.Invoke(pushSkill);
        StartGame();
        safeZone.position = new Vector3(safeZone.position.x, safeZone.position.y, 241.8f);

        while (gameIsRunning)
        {
            SetGreenLight();
            yield return new WaitForSeconds(GetRandomGreenLightDuration());

            if (!gameIsRunning) break;

            yield return StartCoroutine(SetRedLight());

            if (!gameIsRunning) break;

            yield return new WaitForSeconds(GetRandomRedLightDuration());
        }

        HandleGameEnd();
    }

    private void StartGame()
    {
        barrier.SetActive(false);

        gameIsRunning = true;
        
        GameController.Instance.StartTimer(gameTime);

        StartCoroutine(MotionCheck());
    }

    private void HandleGameEnd()
    {
        gameIsRunning = false;
        SetNoLight();
        UpdatePlayers();
        StartCoroutine(ShootRemainingPlayers(new List<PlayerMovementData>(players)));
    }

    private IEnumerator ShootRemainingPlayers(List<PlayerMovementData> losingPlayersData)
    {
        while (losingPlayersData.Count > 0)
        {
            for (int i = losingPlayersData.Count - 1; i >= 0; i--)
            {
                PlayerMovementData player = losingPlayersData[i];

                if (!player.transform.gameObject.activeInHierarchy)
                {
                    losingPlayersData.RemoveAt(i);
                    continue;
                }

                UpdatePlayerMovementData(player);
                Shoot(player, true);
                yield return new WaitForSeconds(SHOOT_COOLDOWN);
            }
        }

        GameEvents.OnRoundEnded?.Invoke();
    }

    #endregion

    #region Light State Management

    private void SetGreenLight()
    {
        if (!gameIsRunning) return;

        canShoot = false;
        PlayLightSound(greenSFX);
        SetHeadRotation(0f);
        UpdateLightState(LightState.Green, greenLight, Color.green);
    }

    private IEnumerator SetRedLight()
    {
        if (!gameIsRunning) yield break;

        PlayLightSound(redSFX);
        SetHeadRotation(-180f);
        UpdateLightState(LightState.Red, redLight, Color.red);

        yield return new WaitForSeconds(RED_LIGHT_ACTIVATION_DELAY);
        canShoot = true;
    }

    private void SetNoLight()
    {
        canShoot = true;
        SetHeadRotation(0f);
        StopAllCoroutines();
        UpdateLightState(LightState.None, noLightSprite, Color.clear);
    }

    private void UpdateLightState(LightState newState, Sprite lightSprite, Color flashColor)
    {
        currentLightState = newState;
        trafficLight.sprite = lightSprite;
        onLightChanged?.Invoke(newState);

        if(newState != LightState.None)
            PlayFlashEffect(flashColor);
    }

    #endregion

    #region Player Detection & Shooting

    private IEnumerator MotionCheck()
    {
        while (gameIsRunning)
        {
            if (canShoot && currentLightState == LightState.Red)
            {
                UpdatePlayers();

                foreach (PlayerMovementData player in players)
                {
                    if (player.transform == null) continue;

                    if (player.isNewPlayer)
                    {
                        InitializeNewPlayer(player);
                        continue;
                    }

                    if (CheckPlayerMovement(player))
                    {
                        Shoot(player);
                        yield return new WaitForSeconds(0.01f);
                    }
                }
            }
            yield return new WaitForSeconds(0.1f);
        }
    }

    private void UpdatePlayers()
    {
        var existingPlayers = new Dictionary<Transform, PlayerMovementData>();
        foreach (var player in players)
        {
            if (player.transform != null)
            {
                existingPlayers[player.transform] = player;
            }
        }

        players.Clear();

        var playerColliders = Physics.OverlapBox(checkZone.position, checkZoneSize / 2, Quaternion.identity, whatIsPlayer);
        foreach (var collider in playerColliders)
        {
            if (collider.CompareTag("BodyPart") && collider.gameObject.name == "Torso")
            {
                var playerTransform = collider.transform;
                if (existingPlayers.TryGetValue(playerTransform, out var data))
                {
                    players.Add(data);
                }
                else
                {
                    players.Add(new PlayerMovementData()
                    {
                        transform = playerTransform,
                        previousPosition = playerTransform.position,
                        isNewPlayer = true
                    });
                }
            }
        }
    }

    private bool CheckPlayerMovement(PlayerMovementData player)
    {
        Vector3 currentPosition = player.transform.position;
        float distanceMoved = Vector3.Distance(currentPosition, player.previousPosition);
        UpdatePlayerMovementData(player);

        return distanceMoved > movementThreshold && distanceMoved < maxValidMovement;
    }

    private void UpdatePlayerMovementData(PlayerMovementData player)
    {
        Vector3 currentPosition = player.transform.position;
        float deltaTime = Mathf.Max(Time.deltaTime, MIN_DELTA_TIME);
        player.estimatedVelocity = (currentPosition - player.previousPosition) / deltaTime;
        player.previousPosition = currentPosition;
    }

    private void InitializeNewPlayer(PlayerMovementData player)
    {
        player.previousPosition = player.transform.position;
        player.isNewPlayer = false;
    }

    private void Shoot(PlayerMovementData target, bool ignoreOtherPlayers = false)
    {
        if (!canShoot || target.transform == null) return;

        Transform bulletPoint = GetOptimalBulletPoint(target.transform.position);
        Vector3 predictedPosition = CalculatePredictedPosition(target, bulletPoint.position);

        float distance = Vector3.Distance(bulletPoint.position, target.transform.position);
        if (distance < 75f)
        {
            predictedPosition = target.transform.position;
        }

        FireProjectile(bulletPoint, predictedPosition, target);
    }

    private Vector3 CalculatePredictedPosition(PlayerMovementData target, Vector3 bulletPosition)
    {
        Vector3 targetPosition = target.transform.position;
        float distanceToTarget = Vector3.Distance(bulletPosition, targetPosition);

        float predictionIntensity = Mathf.Lerp(0.05f, 0.2f, distanceToTarget / 10f);
        predictionIntensity = Mathf.Clamp(predictionIntensity, 0.05f, 0.2f);

        float bulletTime = distanceToTarget / bulletForce;
        Vector3 predictedOffset = target.estimatedVelocity * bulletTime * predictionIntensity;

        float maxPredictionDistance = Mathf.Lerp(0.5f, 2f, distanceToTarget / 10f);
        predictedOffset = Vector3.ClampMagnitude(predictedOffset, maxPredictionDistance);

        float accuracy = Mathf.Lerp(0.95f, 0.8f, distanceToTarget / 30f);
        Vector3 predictedPosition = targetPosition + predictedOffset;
        predictedPosition += Random.insideUnitSphere * (1 - accuracy) * 0.5f;

        return predictedPosition;
    }

    private void FireProjectile(Transform bulletPoint, Vector3 targetPosition, PlayerMovementData target)
    {
        Vector3 direction = (targetPosition - bulletPoint.position).normalized;
        float distance = Vector3.Distance(bulletPoint.position, targetPosition);

        if (distance < 50f)
        {
            direction = (target.transform.position - bulletPoint.position).normalized;
        }

        Transform projectileClone = Instantiate(projectile, bulletPoint.position, Quaternion.identity).transform;
        projectileClone.rotation = Quaternion.LookRotation(direction);
        projectileClone.GetComponent<Rigidbody>().linearVelocity = direction * bulletForce;

        if (currentLightState == LightState.None)
        {
            GameObject targetParent = target.transform.root.gameObject;
            Projectile projectile = projectileClone.GetComponent<Projectile>();
            projectile.SetTarget(targetParent.transform);
            projectile.SetDamage(1000);
        }

        PlayShootSound();
        Destroy(projectileClone.gameObject, 3f);
    }

    private Transform GetOptimalBulletPoint(Vector3 targetPosition)
    {
        Transform bestPoint = bulletPoints[0];
        float minDistance = float.MaxValue;

        foreach (Transform point in bulletPoints)
        {
            float distance = Vector3.Distance(point.position, targetPosition);
            if (distance < minDistance)
            {
                minDistance = distance;
                bestPoint = point;
            }
        }

        return bestPoint;
    }

    #endregion

    #region Timer & UI
    private void UpdateTimer(float remainingTime)
    {
        int currentSecond = Mathf.FloorToInt(remainingTime);
        if (currentSecond != lastTickSecond)
        {
            lastTickSecond = currentSecond;
            audioSource.PlayOneShot(tickSFX);
        }

        DisplayTime(remainingTime);
    }

    private void DisplayTime(float timeToDisplay)
    {
        float minutes = Mathf.FloorToInt(timeToDisplay / 60);
        float seconds = Mathf.FloorToInt(timeToDisplay % 60);
        timeText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    #endregion

    #region Visual & Audio Effects

    private void UpdateHeadRotation()
    {
        dollHead.localRotation = Quaternion.Slerp(dollHead.localRotation, targetHeadRotation, Time.deltaTime * HEAD_ROTATION_SPEED);
    }

    private void SetHeadRotation(float yRotation)
    {
        targetHeadRotation = Quaternion.Euler(transform.localEulerAngles.x, yRotation, transform.localEulerAngles.z);
    }

    private void PlayFlashEffect(Color color)
    {
        color.a = 0f;
        flash.color = color;

        Sequence flashSequence = DOTween.Sequence();
        flashSequence.Append(flash.DOFade(FLASH_MAX_ALPHA, FLASH_FADE_IN_DURATION).SetEase(Ease.InOutSine));
        flashSequence.Append(flash.DOFade(0f, FLASH_FADE_OUT_DURATION).SetEase(Ease.InOutSine));
        flashSequence.Play();
    }

    private void PlayLightSound(AudioClip clip)
    {
        audioSource.PlayOneShot(clip, Random.Range(0.9f, 1.1f));
    }

    private void PlayShootSound()
    {
        audioSource.PlayOneShot(shootSFX, Random.Range(0.1f, 0.3f));
    }

    private void AnimateTrafficLightEntrance()
    {
        trafficLight.rectTransform.DOAnchorPosY(TRAFFIC_LIGHT_Y_POSITION, TRAFFIC_LIGHT_ANIM_DURATION);
    }

    #endregion

    #region Helper Methods

    private float GetRandomRedLightDuration() => Random.Range(2f, 7f);
    private float GetRandomGreenLightDuration() => Random.Range(1f, 4f);

    private void DrawCheckZoneGizmo()
    {
        if (checkZone)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(checkZone.position, checkZoneSize);
        }
    }

    #endregion
}