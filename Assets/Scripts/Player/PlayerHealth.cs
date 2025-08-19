using DG.Tweening;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using YG;

public class PlayerHealth : MonoBehaviour
{
    private const string RESPAWN_ID = "respawn";

    [Header("Health Bar")]
    [SerializeField] private Color[] fillColors;
    [SerializeField] private GameObject healthBar;
    [SerializeField] private Image healthFill;

    [Header("Main")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float regenerationTime = 1f;
    [SerializeField] private float regenerationAmount = 0.5f;
    private PlayerRagdoll ragdoll;
    private PlayerAvatar avatar;
    private float currentHealth;
    private bool died;

    [Header("Death Screen")]
    [SerializeField] private GameObject deathScreen;
    [SerializeField] private TMP_Text gameOverText;
    [SerializeField] private Button respawnButton;
    [SerializeField] private Button backButton;
    [SerializeField] private Image[] vignette;

    private void Start()
    {
        ragdoll = GetComponent<PlayerRagdoll>();
        avatar = GetComponent<PlayerAvatar>();
        currentHealth = maxHealth;

        if (respawnButton)
            respawnButton.onClick.AddListener(RespawnRequest);

        if (backButton && GameController.Instance)
            backButton.onClick.AddListener(GameController.Instance.QuitGames);

        UpdateHealthUI();
        StartCoroutine(Regeneration());
    }

    private void RespawnRequest()
    {
        YG2.RewardedAdvShow(RESPAWN_ID, () =>
        {
            if (RESPAWN_ID == "respawn")
                Respawn();
        });
    }

    private void Respawn()
    {
        HandleDeathScreen(false);
        Heal(maxHealth);
        ragdoll.ToggleRagdoll(false);
        ragdoll.HideBodyParts(false);
        died = false;

        GameEvents.OnPlayerRespawned?.Invoke(avatar.GetPlayerData());
    }

    private IEnumerator Regeneration()
    {
        while (true)
        {
            if(!IsDied())
            {
                Heal(regenerationAmount);
            }

            yield return new WaitForSeconds(regenerationTime);
        }
    }

    public void TakeDamage(float amount)
    {
        if (died) return;

        currentHealth = Mathf.Max(currentHealth - amount, 0);

        if (currentHealth <= 0)
        {
            died = true;
            StartCoroutine(Die());
        }

        UpdateHealthUI();
    }

    private void HandleDeathScreen(bool show)
    {
        if (deathScreen == null || gameOverText == null || backButton == null || respawnButton == null) return;

        deathScreen.SetActive(show);
        gameOverText.rectTransform.anchoredPosition = new Vector2(0, 150);

        foreach (var vignetteItem in vignette)
        {
            Color color = Color.black;
            color.a = 0f;

            vignetteItem.color = color;
        }

        backButton.transform.localScale = Vector3.zero;
        respawnButton.transform.localScale = Vector3.zero;

        if (show)
        {
            gameOverText.rectTransform.DOAnchorPos(new Vector2(0f, -32f), 1.5f);

            foreach (var vignetteItem in vignette)
            {
                Color color = Color.black;
                color.a = 1f;
                vignetteItem.color = color;
                vignetteItem.DOFade(1f, 1f);
            }

            backButton.transform.DOScale(1f, 0.5f);
            respawnButton.transform.DOScale(1f, 0.5f);
        }
    }

    public void Heal(float amount)
    {
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        UpdateHealthUI();
    }

    private IEnumerator Die()
    {
        ragdoll.ToggleRagdoll(true);
        GameEvents.OnPlayerEliminated?.Invoke(avatar.GetPlayerData());

        yield return new WaitForSeconds(5f);
        ragdoll.HideBodyParts(true);
        HandleDeathScreen(true);
    }

    private void UpdateHealthUI()
    {
        if (fillColors == null || fillColors.Length == 0 || healthFill == null || healthBar == null)
            return;

        healthBar.SetActive(currentHealth < maxHealth);

        float healthPercentage = currentHealth / maxHealth;
        healthFill.fillAmount = healthPercentage;

        int colorIndex = CalculateColorIndex(healthPercentage);
        healthFill.color = fillColors[colorIndex];
    }

    private int CalculateColorIndex(float healthPercentage)
    {
        if (fillColors.Length == 1) return 0;

        float segmentSize = 1f / fillColors.Length;
        int index = Mathf.FloorToInt(healthPercentage / segmentSize);

        index = Mathf.Clamp(index, 0, fillColors.Length - 1);
        return index;
    }

    public bool IsDied() => died;
}
