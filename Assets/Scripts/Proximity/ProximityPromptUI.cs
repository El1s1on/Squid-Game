using UnityEngine;
using TMPro;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class ProximityPromptUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TextMeshProUGUI actionText;
    [SerializeField] private TextMeshProUGUI keycodeText;
    [SerializeField] private Image holdProgressImage;

    [Header("Animation")]
    [SerializeField] private float fadeDuration = 0.18f;
    [SerializeField] private float scalePop = 1.08f;

    private Transform targetCamera;
    private Vector3 initialScale;
    private Coroutine fadeRoutine;

    void Reset()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }

    void Awake()
    {
        initialScale = transform.localScale;
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
        }
        if (holdProgressImage != null)
            holdProgressImage.fillAmount = 0f;
    }

    void Start()
    {
        if (Camera.main != null)
            targetCamera = Camera.main.transform;
    }

    void LateUpdate()
    {
        if (targetCamera != null)
        {
            transform.LookAt(targetCamera);
            transform.rotation = Quaternion.LookRotation(transform.position - targetCamera.position);
        }
    }

    public void SetActionText(string text, KeyCode key)
    {
        if (actionText != null)
            actionText.text = text;

        if (keycodeText != null)
            keycodeText.text = key.ToReadableString();
    }

    public void Show()
    {
        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(FadeUI(1f));
    }

    public void Hide()
    {
        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(FadeUI(0f));
    }

    public void SetHoldProgress(float progress)
    {
        if (holdProgressImage != null)
            holdProgressImage.fillAmount = Mathf.Clamp01(progress);
    }

    System.Collections.IEnumerator FadeUI(float targetAlpha)
    {
        if (canvasGroup == null) yield break;

        float startAlpha = canvasGroup.alpha;
        float time = 0f;

        if (targetAlpha > 0f)
        {
            canvasGroup.blocksRaycasts = true;
            transform.localScale = initialScale * scalePop;
        }
        else
        {
            canvasGroup.blocksRaycasts = false;
        }

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            float t = time / fadeDuration;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            transform.localScale = Vector3.Lerp(transform.localScale, initialScale, t);
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;
        transform.localScale = initialScale;
    }
}
