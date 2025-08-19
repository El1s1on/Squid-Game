using UnityEngine;

public class PlayerAnimations : MonoBehaviour
{
    [SerializeField] private Transform avatar;
    [SerializeField] private Transform orientation;
    [SerializeField] private bool leaning = true;
    private CharacterController characterController;
    private PlayerMovement movement;
    private PlayerLook look;
    private Animator animator;
    private PlayerRagdoll ragdoll;

    private void Start()
    {
        look = GetComponent<PlayerLook>();
        characterController = GetComponent<CharacterController>();
        movement = GetComponent<PlayerMovement>();
        animator = GetComponent<Animator>();
        ragdoll = GetComponent<PlayerRagdoll>();
    }

    private void Update()
    {
        AvatarRotation();
        animator.SetBool("Move", characterController.velocity.magnitude > .1f);
        animator.SetBool("Running", movement.CanRun() && characterController.velocity.magnitude > .1f);
        animator.SetBool("Grounded", movement.IsGrounded());
    }

    public void OnJump() => animator.SetTrigger("Jump");

    private void AvatarRotation()
    {
        if (ragdoll.IsRagdoll) return;

        Quaternion targetRotation;

        if (look && look.IsLocked)
        {
            float newY = orientation.localRotation.eulerAngles.y;
            float newZ = 0f;

            if (leaning)
            {
                bool isMovingBackward = Input.GetAxisRaw("Vertical") < 0;
                float horizontalInput = Input.GetAxisRaw("Horizontal");
                newZ = horizontalInput * (isMovingBackward ? 0 : -1) * 3.5f;
            }

            targetRotation = Quaternion.Euler(0, newY, newZ);
        }
        else
        {
            Vector3 movementDirection = orientation.TransformDirection(movement.InputVector());
            targetRotation = movementDirection.magnitude > 0.1f
                ? Quaternion.LookRotation(movementDirection)
                : avatar.localRotation;
        }

        avatar.localRotation = Quaternion.Slerp(avatar.localRotation, targetRotation, Time.deltaTime * 10f);
    }
}
