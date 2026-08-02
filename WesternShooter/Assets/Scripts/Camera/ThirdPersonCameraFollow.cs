using UnityEngine;
using UnityEngine.InputSystem;

public enum CameraHeightPreset
{
    Low,
    Normal,
    High,
    Custom
}

public enum CameraSidePreset
{
    Left,
    Center,
    Right,
    Custom
}

public class ThirdPersonCameraFollow : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Look")]
    [SerializeField] private float normalLookSensitivity = 150f;
    [SerializeField] private float aimLookSensitivity = 95f;
    [SerializeField] private float minPitch = -35f;
    [SerializeField] private float maxPitch = 70f;
    [SerializeField] private bool invertY;

    [Header("Explore Profile")]
    [SerializeField] private float normalDistance = 4.8f;
    [SerializeField] private float normalHeight = 1.8f;
    [SerializeField] private float normalSideOffset = 1.4f;
    [SerializeField] private float normalFieldOfView = 60f;

    [Header("Aim Profile")]
    [SerializeField] private float aimDistance = 3.2f;
    [SerializeField] private float aimHeight = 1.65f;
    [SerializeField] private float aimSideOffset = 2.4f;
    [SerializeField] private float aimFieldOfView = 42f;

    [Header("Smoothing")]
    [SerializeField] private float positionSmoothTime = 0.06f;
    [SerializeField] private float profileSmoothTime = 0.09f;
    [SerializeField] private float fieldOfViewSmoothTime = 0.12f;

    [Header("Shoulder")]
    [SerializeField] private bool allowShoulderSwap = true;
    [SerializeField] private Key shoulderSwapKey = Key.Q;

    [Header("Collision")]
    [SerializeField] private LayerMask collisionLayers = ~0;
    [SerializeField] private float collisionSphereRadius = 0.25f;
    [SerializeField] private float collisionBuffer = 0.15f;
    [SerializeField] private float minimumCollisionDistance = 0.6f;

    private Camera cameraComponent;
    private float yaw;
    private float pitch = 15f;
    private Vector3 currentVelocity;
    private float currentDistance;
    private float currentHeight;
    private float currentSideOffset;
    private float currentFieldOfView;
    private float distanceVelocity;
    private float heightVelocity;
    private float sideOffsetVelocity;
    private float fieldOfViewVelocity;
    private bool useLeftShoulder;
    private bool lastGamepadShoulderSwapPressed;
    private PlayerInputReader playerInputReader;
    private bool hasWarnedMissingInputReader;

    private void Start()
    {
        if (target == null)
        {
            Debug.LogWarning("ThirdPersonCameraFollow: target is not assigned.");
            enabled = false;
            return;
        }

        ResolvePlayerInputReaderFromTarget();

        Vector3 euler = transform.eulerAngles;
        yaw = euler.y;
        pitch = euler.x;

        cameraComponent = GetComponent<Camera>();
        if (cameraComponent != null)
        {
            currentFieldOfView = cameraComponent.fieldOfView;
        }
        else
        {
            Debug.LogWarning("ThirdPersonCameraFollow: no Camera component found on this GameObject.");
        }

        currentDistance = normalDistance;
        currentHeight = normalHeight;
        currentSideOffset = normalSideOffset;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void LateUpdate()
    {
        if (playerInputReader == null && target != null)
        {
            ResolvePlayerInputReaderFromTarget();
        }

        playerInputReader?.EnsurePolledThisFrame();

        HandleShoulderSwapInput();

        bool isAiming = IsAiming();
        Vector2 lookInput = GetLookInput();
        float lookSensitivity = isAiming ? aimLookSensitivity : normalLookSensitivity;

        yaw += lookInput.x * lookSensitivity * Time.deltaTime;
        float pitchDelta = lookInput.y * lookSensitivity * Time.deltaTime;
        pitch += invertY ? pitchDelta : -pitchDelta;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        float targetDistance = isAiming ? aimDistance : normalDistance;
        float targetHeight = isAiming ? aimHeight : normalHeight;
        float sideSign = useLeftShoulder ? -1f : 1f;
        float targetSideOffset = (isAiming ? aimSideOffset : normalSideOffset) * sideSign;

        currentDistance = Mathf.SmoothDamp(currentDistance, targetDistance, ref distanceVelocity, profileSmoothTime);
        currentHeight = Mathf.SmoothDamp(currentHeight, targetHeight, ref heightVelocity, profileSmoothTime);
        currentSideOffset = Mathf.SmoothDamp(currentSideOffset, targetSideOffset, ref sideOffsetVelocity, profileSmoothTime);

        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 targetPosition = target.position + Vector3.up * currentHeight;
        Vector3 desiredPosition = targetPosition - (rotation * Vector3.forward * currentDistance) + (rotation * Vector3.right * currentSideOffset);
        Vector3 resolvedPosition = ResolveCollisionPosition(targetPosition, desiredPosition);

        transform.position = Vector3.SmoothDamp(transform.position, resolvedPosition, ref currentVelocity, positionSmoothTime);
        Vector3 lookTarget = targetPosition + (rotation * Vector3.forward * 15f);
        transform.rotation = Quaternion.LookRotation(lookTarget - transform.position, Vector3.up);

        if (cameraComponent != null)
        {
            float targetFieldOfView = isAiming ? aimFieldOfView : normalFieldOfView;
            currentFieldOfView = Mathf.SmoothDamp(currentFieldOfView, targetFieldOfView, ref fieldOfViewVelocity, fieldOfViewSmoothTime);
            cameraComponent.fieldOfView = currentFieldOfView;
        }
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        hasWarnedMissingInputReader = false;
        ResolvePlayerInputReaderFromTarget();
    }

    private Vector2 GetLookInput()
    {
        return playerInputReader != null ? playerInputReader.LookInput : Vector2.zero;
    }

    private void HandleShoulderSwapInput()
    {
        if (!allowShoulderSwap)
        {
            return;
        }

        bool keyboardPressed = Keyboard.current != null && Keyboard.current[shoulderSwapKey].wasPressedThisFrame;

        bool gamepadPressed = false;
        if (Gamepad.current != null)
        {
            bool currentPress = Gamepad.current.rightStickButton.isPressed;
            gamepadPressed = currentPress && !lastGamepadShoulderSwapPressed;
            lastGamepadShoulderSwapPressed = currentPress;
        }
        else
        {
            lastGamepadShoulderSwapPressed = false;
        }

        if (keyboardPressed || gamepadPressed)
        {
            useLeftShoulder = !useLeftShoulder;
        }
    }

    private Vector3 ResolveCollisionPosition(Vector3 pivotPosition, Vector3 desiredPosition)
    {
        Vector3 castDirection = desiredPosition - pivotPosition;
        float castDistance = castDirection.magnitude;

        if (castDistance <= 0.001f)
        {
            return desiredPosition;
        }

        castDirection /= castDistance;

        RaycastHit[] hits = Physics.SphereCastAll(pivotPosition, collisionSphereRadius, castDirection, castDistance, collisionLayers, QueryTriggerInteraction.Ignore);
        if (hits.Length > 0)
        {
            RaycastHit closestValidHit = default;
            bool foundValidHit = false;

            for (int i = 0; i < hits.Length; i++)
            {
                RaycastHit currentHit = hits[i];

                if (target != null && currentHit.transform.IsChildOf(target))
                {
                    continue;
                }

                if (!foundValidHit || currentHit.distance < closestValidHit.distance)
                {
                    closestValidHit = currentHit;
                    foundValidHit = true;
                }
            }

            if (foundValidHit)
            {
                float safeDistance = Mathf.Max(minimumCollisionDistance, closestValidHit.distance - collisionBuffer);
                return pivotPosition + castDirection * safeDistance;
            }
        }

        return desiredPosition;
    }

    private bool IsAiming()
    {
        return playerInputReader != null && playerInputReader.IsAimPressed;
    }

    private void ResolvePlayerInputReaderFromTarget()
    {
        playerInputReader = null;

        if (target == null)
        {
            return;
        }

        playerInputReader = target.GetComponentInParent<PlayerInputReader>();
        if (playerInputReader != null)
        {
            return;
        }

        if (hasWarnedMissingInputReader)
        {
            return;
        }

        hasWarnedMissingInputReader = true;
        Debug.LogWarning("ThirdPersonCameraFollow: target hierarchy has no PlayerInputReader; look and aim input will be disabled.", this);
    }
}
