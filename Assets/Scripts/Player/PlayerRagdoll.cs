using System.Linq;
using UnityEngine;

public class PlayerRagdoll : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private LayerMask groundLayer;
    public Transform hipsBone;
    private float pushForceDuration = 2.5f;

    [Header("Component References")]
    private Animator animator;
    private CharacterController characterController;

    private Rigidbody[] ragdollRigidbodies;
    private PlayerHealth health;
    private Collider[] ragdollColliders;
    private float standUpHeight;
    private float targetHeight;
    private float ragdollTimeRemaining;
    private bool isPushed;

    public bool IsRagdoll { get; private set; }

    private void Awake()
    {
        health = GetComponent<PlayerHealth>();
        ValidateComponents();
        InitializeRagdollComponents();
    }

    private void ValidateComponents()
    {
        if (animator == null) animator = GetComponent<Animator>();
        if (characterController == null) characterController = GetComponent<CharacterController>();
        if (hipsBone == null) Debug.LogError("Hips bone reference is missing!", this);
    }

    private void InitializeRagdollComponents()
    {
        ragdollRigidbodies = GetComponentsInChildren<Rigidbody>();
        ragdollColliders = GetComponentsInChildren<Collider>()
            .Where(col => !(col is CharacterController)).ToArray();

        ToggleRagdoll(false);
    }

    public void HideBodyParts(bool state)
    {
        foreach (var rb in ragdollRigidbodies)
        {
            rb.gameObject.SetActive(!state);
        }
    }

    public void ToggleRagdoll(bool state)
    {
        IsRagdoll = state;

        animator.enabled = !state;

        if (characterController)
            characterController.enabled = !state;

        foreach (var rb in ragdollRigidbodies)
        {
            rb.isKinematic = !state;
            rb.useGravity = state;
        }

        foreach (var col in ragdollColliders)
        {
            if (col.gameObject.name != "Torso")
                col.enabled = state;
        }
    }

    public void Push(Vector3 direction)
    {
        if (health.IsDied()) return;

        ToggleRagdoll(true);
        isPushed = true;
        ragdollTimeRemaining = pushForceDuration;

        foreach (Rigidbody bodyPart in ragdollRigidbodies)
        {
            bodyPart.AddForce(direction, ForceMode.Impulse);
        }
    }

    public void StandUp()
    {
        if (!IsRagdoll || health.IsDied()) return;

        transform.position = CalculateStandUpPosition();
        ToggleRagdoll(false);
        isPushed = false;
    }

    private void Update()
    {
        UpdateStandUpHeight();

        if (isPushed && IsRagdoll)
        {
            ragdollTimeRemaining -= Time.deltaTime;
            if (ragdollTimeRemaining <= 0)
            {
                StandUp();
            }
        }
    }

    private void UpdateStandUpHeight()
    {
        targetHeight = Physics.Raycast(hipsBone.position, Vector3.down, out RaycastHit hit, 2, groundLayer)
            ? hit.point.y
            : hipsBone.position.y;

        standUpHeight = Mathf.Lerp(standUpHeight, targetHeight, 5f * Time.deltaTime);
    }

    public Vector3 CalculateStandUpPosition() => new Vector3(hipsBone.position.x, standUpHeight, hipsBone.position.z);
}