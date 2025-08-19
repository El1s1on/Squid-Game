using UnityEngine;

[RequireComponent(typeof(PlayerRagdoll))]
public class PlayerLook : MonoBehaviour
{
    [Header("Lock Mode Settings")]
    [SerializeField] private KeyCode lockModeKey = KeyCode.LeftControl;
    public bool IsLocked { get; private set; }

    [Header("Camera Collision")]
    [SerializeField] private Vector2 cameraDistanceMinMax = new Vector2(0, 1);
    [SerializeField] private LayerMask obstacleLayer;
    private Vector3 cameraDirection;
    private Vector3 cameraCollision;
    private float cameraDistance;


    [Header("Camera Settings")]
    [SerializeField] private Transform orientation;
    [SerializeField] private Transform cameraHolder;
    [SerializeField] private Camera mainCamera;
    private const float CameraLerpSpeed = 10f;
    private const float VerticalLookClamp = 85f;

    private float yRotation;
    private float xRotation;
    private PlayerRagdoll ragdoll;
    public bool IsCameraUncontrolled { get; private set; }

    private void Awake()
    {
        cameraDirection = mainCamera.transform.localPosition;
        ragdoll = GetComponent<PlayerRagdoll>();
    }

    private void LateUpdate()
    {
        HandleLockMode();
        HandleCameraLook();
        CameraCollision();

        if (!IsCameraUncontrolled)
        {
            UpdateCameraPosition();
        }
    }

    private void HandleLockMode()
    {
        if (Input.GetKeyDown(lockModeKey))
        {
            IsLocked = !IsLocked;
        }

        Cursor.lockState = IsLocked ? CursorLockMode.Locked : CursorLockMode.None;
    }

    private void HandleCameraLook()
    {
        if (ShouldProcessInput())
        {
            ProcessMouseInput();
        }

        if (!IsCameraUncontrolled)
        {
            UpdateCameraRotation();
            UpdateOrientationPosition();
        }
    }

    private bool ShouldProcessInput()
    {
        return IsLocked || Input.GetMouseButton(1);
    }

    private void ProcessMouseInput()
    {
        xRotation = Input.GetAxis("Mouse X");
        yRotation -= Input.GetAxis("Mouse Y");
        yRotation = Mathf.Clamp(yRotation, -VerticalLookClamp, VerticalLookClamp);
    }

    private void UpdateCameraRotation()
    {
        cameraHolder.localRotation = Quaternion.Euler(yRotation, 0f, 0f);

        if (ShouldProcessInput())
        {
            orientation.Rotate(xRotation * Vector3.up);
        }
    }

    private void UpdateOrientationPosition()
    {
        orientation.position = ragdoll.IsRagdoll
            ? ragdoll.CalculateStandUpPosition()
            : transform.position;
    }

    private void UpdateCameraPosition()
    {
        float targetXOffset = IsLocked ? 0f : 0f;

        Vector3 currentLocalPos = mainCamera.transform.localPosition;
        Vector2 newXY = Vector2.Lerp(
            new Vector2(currentLocalPos.x, currentLocalPos.y),
            new Vector2(targetXOffset, 0f),
            Time.deltaTime * CameraLerpSpeed);

        mainCamera.transform.localPosition = new Vector3(newXY.x, newXY.y, cameraCollision.z);

        mainCamera.transform.localRotation = Quaternion.Lerp(
            mainCamera.transform.localRotation,
            Quaternion.identity,
            Time.deltaTime * CameraLerpSpeed);
    }

    private void CameraCollision()
    {
        float targetXOffset = IsLocked ? 0f : 0f;

        Vector3 safeRayOrigin = cameraHolder.position;

        Vector3 desiredCameraPos = safeRayOrigin +
                                cameraHolder.right * targetXOffset +
                                cameraHolder.TransformDirection(cameraDirection.normalized * cameraDistanceMinMax.y);

        Vector3 rayDirection = desiredCameraPos - safeRayOrigin;
        float maxDistance = rayDirection.magnitude;

        RaycastHit hit;
        bool hasHit = Physics.SphereCast(safeRayOrigin, 0.2f, rayDirection.normalized, out hit, maxDistance, obstacleLayer);

        if (!hasHit && Physics.CheckSphere(mainCamera.transform.position, 0.1f, obstacleLayer))
        {
            Vector3 escapeDirection = (mainCamera.transform.position - cameraHolder.position).normalized;
            hasHit = Physics.SphereCast(cameraHolder.position, 0.2f, escapeDirection, out hit, cameraDistanceMinMax.y, obstacleLayer);
        }

        if (hasHit)
        {
            float hitDistance = Vector3.Distance(safeRayOrigin, hit.point);
            float safeDistance = hitDistance - 0.3f;
            cameraDistance = Mathf.Clamp(safeDistance, cameraDistanceMinMax.x, cameraDistanceMinMax.y);

            Debug.DrawLine(safeRayOrigin, hit.point, Color.red, 0.1f);
            Debug.DrawRay(hit.point, hit.normal * 0.3f, Color.blue, 0.1f);
        }
        else
        {
            cameraDistance = cameraDistanceMinMax.y;
        }

        cameraCollision = cameraDirection.normalized * cameraDistance;
    }

    public void SetCameraUncontrolled(bool state) => IsCameraUncontrolled = state;
}