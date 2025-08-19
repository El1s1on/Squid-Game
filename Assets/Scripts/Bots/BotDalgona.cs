using System.Collections;
using UnityEngine;

public class BotDalgona : BotBase
{
    private DalgonaCookie cookie;
    [SerializeField, Range(0f, 1f)] private float winProbability = 0.85f;
    [SerializeField, Range(0f, 1f)] private float crackedWinProbability = 0.5f;

    [Header("Patrol Settings")]
    [SerializeField] private float patrolRange = 15f;
    [SerializeField] private float minPatrolDelay = 2f;
    [SerializeField] private float maxPatrolDelay = 6f;
    [SerializeField] private float runProbability = 0.3f;

    protected override void Awake()
    {
        base.Awake();
    }

    private void Start()
    {
        StartCoroutine(WaitForCookie());
    }

    private IEnumerator WaitForCookie()
    {
        yield return new WaitUntil(HasCookie);
        cookie = GetComponentInChildren<DalgonaCookie>();

        yield return new WaitUntil(() => cookie.CanCut());
        StartCoroutine(BotPlayRoutine());
    }

    private IEnumerator BotPlayRoutine()
    {
        yield return new WaitForSeconds(Random.Range(3f, 8f));

        bool willWin = Random.value <= winProbability;

        if (willWin)
            yield return BotWinBehavior();
        else
            yield return BotLoseBehavior();

        yield return new WaitForSeconds(Random.Range(1f, 2f));

        if (willWin)
            StartCoroutine(PatrolRandomly(patrolRange, minPatrolDelay, maxPatrolDelay, runProbability));
    }

    private IEnumerator BotWinBehavior()
    {
        bool willHaveCracks = Random.value <= crackedWinProbability;
        float duration = Random.Range(15f, 45f);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (willHaveCracks && elapsed > duration * 0.4f && cookie.currentCrackStage < 2)
            {
                if (Random.value < 0.08f)
                {
                    yield return cookie.AdvanceCrackStage();
                    yield return new WaitForSeconds(0.5f);
                }
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        yield return cookie.CompleteCookie();
    }

    private IEnumerator BotLoseBehavior()
    {
        yield return new WaitForSeconds(Random.Range(5f, 15f));
        yield return cookie.AdvanceCrackStage();

        yield return new WaitForSeconds(Random.Range(5f, 20f));
        yield return cookie.AdvanceCrackStage();

        yield return new WaitForSeconds(Random.Range(3f, 15f));
        yield return cookie.AdvanceCrackStage();
    }

    public bool HasCookie()
    {
        foreach (Transform child in transform)
        {
            if (child.gameObject.layer == LayerMask.NameToLayer("Cookie"))
                return true;
        }
        return false;
    }
}
