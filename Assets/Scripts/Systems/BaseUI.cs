using DG.Tweening;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BaseUI : SingletonMono<BaseUI>
{
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private Image fadeImage;

    private void Start()
    {
        GameController.Instance.OnTimerUpdate += UpdateTimerDisplay;
        GameController.Instance.OnTimerFinished += OnTimerFinished;
    }

    private void UpdateTimerDisplay(float remainingTime)
    {
        timerText.gameObject.SetActive(true);
        int minutes = Mathf.FloorToInt(remainingTime / 60);
        int seconds = Mathf.FloorToInt(remainingTime % 60);
        timerText.text = $"{minutes:00}:{seconds:00}";
    }

    private void OnTimerFinished()
    {
        timerText.text = "00:00";
        timerText.gameObject.SetActive(false);
    }

    public IEnumerator Fade(float alpha, float duration = 1)
    {
        if (fadeImage == null) yield break;

        yield return fadeImage.DOFade(alpha, duration).WaitForCompletion();
    }
}
