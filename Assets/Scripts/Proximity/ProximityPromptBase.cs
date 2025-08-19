using UnityEngine;
using UnityEngine.Events;

public abstract class ProximityPromptBase : MonoBehaviour
{
    [Header("Core")]
    [SerializeField] private ProximityPromptProperties properties;
    [SerializeField] private GameObject promptUIPrefab;

    [Header("Events (optional)")]
    public UnityEvent onPromptButtonHoldBegan;
    public UnityEvent onPromptButtonHoldEnded;
    public UnityEvent onPromptTriggered;
    public UnityEvent onPromptTriggerEnded;
    public UnityEvent onPromptShown;
    public UnityEvent onPromptHidden;

    protected Transform player;
    protected ProximityPromptUI promptUIController;

    [HideInInspector] public bool isPlayerNearby;
    [HideInInspector] public bool isHolding;
    [HideInInspector] public float holdTimer;
    [HideInInspector] public bool toggleState;

    protected virtual void Start()
    {
        if (Camera.main != null)
            player = Camera.main.transform.root;

        if (promptUIPrefab != null)
        {
            GameObject uiInstance = Instantiate(promptUIPrefab, transform);
            promptUIController = uiInstance.GetComponent<ProximityPromptUI>();

            if (promptUIController != null && properties != null)
                promptUIController.SetActionText(properties.actionText, properties.activationKey);
        }
    }

    protected virtual void Update()
    {
        if (player == null || properties == null) return;

        float dist = Vector3.Distance(player.position, transform.position);
        if (dist <= properties.activationDistance)
        {
            if (!isPlayerNearby)
            {
                isPlayerNearby = true;
                ShowPrompt();
            }

            if (promptUIController != null)
                promptUIController.transform.position = transform.position + properties.uiOffset;

            HandleInput();
        }
        else
        {
            if (isPlayerNearby)
            {
                isPlayerNearby = false;
                HidePrompt();
            }
        }
    }

    private void HandleInput()
    {
        if (properties.holdDuration > 0f)
        {
            if (Input.GetKeyDown(properties.activationKey))
            {
                holdTimer = 0f;
                isHolding = true;
                UpdateHoldProgress(0f);
                onPromptButtonHoldBegan?.Invoke();
            }

            if (isHolding)
            {
                holdTimer += Time.deltaTime;
                UpdateHoldProgress(Mathf.Clamp01(holdTimer / properties.holdDuration));

                if (holdTimer >= properties.holdDuration)
                {
                    isHolding = false;
                    onPromptButtonHoldEnded?.Invoke();
                    TriggerPrompt();
                }
            }

            if (Input.GetKeyUp(properties.activationKey))
            {
                if (isHolding)
                {
                    isHolding = false;
                    holdTimer = 0f;
                    onPromptTriggerEnded?.Invoke();
                    UpdateHoldProgress(0f);
                }
            }
        }
        else
        {
            if (Input.GetKeyDown(properties.activationKey))
                TriggerPrompt();
        }
    }

    private void TriggerPrompt()
    {
        if (properties.useMultipleAction)
        {
            toggleState = !toggleState;
            if (promptUIController != null)
            {
                promptUIController.SetActionText(toggleState
                    ? properties.alternateActionText
                    : properties.actionText, properties.activationKey);
            }
        }

        onPromptTriggered?.Invoke();
        PromptTriggered(player != null ? player.gameObject : null);
    }

    protected virtual void ShowPrompt()
    {
        if (promptUIController != null)
        {
            promptUIController.SetActionText(properties.useMultipleAction && toggleState
                ? properties.alternateActionText
                : properties.actionText, properties.activationKey);
            promptUIController.SetHoldProgress(0f);
            promptUIController.Show();
        }

        onPromptShown?.Invoke();
    }

    protected virtual void HidePrompt()
    {
        isHolding = false;
        holdTimer = 0f;
        UpdateHoldProgress(0f);

        if (promptUIController != null)
            promptUIController.Hide();

        onPromptHidden?.Invoke();
    }

    protected abstract void PromptTriggered(GameObject instigator);

    protected virtual void UpdateHoldProgress(float progress)
    {
        if (promptUIController != null)
            promptUIController.SetHoldProgress(progress);
    }
}
