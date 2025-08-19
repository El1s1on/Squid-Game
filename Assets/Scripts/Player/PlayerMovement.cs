using System.Collections;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private Transform orientation;
    [SerializeField] private float walkSpeed, runSpeed;
    private float speed;
    [SerializeField] private float jumpForce;
    private bool readyForJump = true;
    private CharacterController characterController;
    private PlayerAnimations animations;
    private PlayerLook look;
    private PlayerRagdoll ragdoll;
    private Vector3 moveInput;
    private float gravity;

    [Header("Ground Check")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundRadius;

    private void Start()
    {
        look = GetComponent<PlayerLook>();
        animations = GetComponent<PlayerAnimations>();
        ragdoll = GetComponent<PlayerRagdoll>();
        characterController = GetComponent<CharacterController>();
    }

    private void Update()
    {
        moveInput = InputVector() * speed;
        moveInput = Vector3.ClampMagnitude(moveInput, speed);
        moveInput.y = gravity;

        if(characterController && characterController.enabled)
            characterController.Move(orientation.TransformDirection(moveInput) * Time.deltaTime);

        speed = CanRun() ? runSpeed : walkSpeed;

        if(IsGrounded())
        {
            if (Input.GetKeyDown(KeyCode.Space) && readyForJump)
            {
                StartCoroutine(Jump());
            }
        }

        if (!IsGrounded())
            gravity += Physics.gravity.y * Time.deltaTime;

        if (ragdoll.IsRagdoll)
            gravity = 0f;
    }

    public Vector3 InputVector()
    {
        return new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));
    }

    public bool IsGrounded()
    {
        return Physics.CheckSphere(transform.position, groundRadius, groundLayer);
    }

    public bool CanRun()
    {
        if (Input.GetAxisRaw("Vertical") < 0 && look && look.IsLocked)
            return false;

        if (!Input.GetKey(KeyCode.LeftShift))
            return false;

        return true;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, groundRadius);
    }

    private IEnumerator Jump()
    {
        readyForJump = false;
        gravity = 0f;
        gravity += jumpForce;
        animations.OnJump();

        yield return new WaitForSeconds(0.5f);
        readyForJump = true;
    }
}
