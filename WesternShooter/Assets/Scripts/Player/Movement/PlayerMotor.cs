using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMotor : MonoBehaviour
{
    private CharacterController characterController;
    private Transform cameraTransform;

    private float rotationSpeed;
    private float aimRotationSpeed;
    private float aimAlignSpeed;
    private float gravity;
    private float groundedStickForce;
    private float verticalVelocity;

    public bool IsGrounded => characterController != null && characterController.isGrounded;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
    }

    public void Configure(
        Transform resolvedCameraTransform,
        float configuredRotationSpeed,
        float configuredAimRotationSpeed,
        float configuredAimAlignSpeed,
        float configuredGravity,
        float configuredGroundedStickForce)
    {
        cameraTransform = resolvedCameraTransform;
        rotationSpeed = configuredRotationSpeed;
        aimRotationSpeed = configuredAimRotationSpeed;
        aimAlignSpeed = configuredAimAlignSpeed;
        gravity = configuredGravity;
        groundedStickForce = configuredGroundedStickForce;
    }

    public void SetCameraTransform(Transform resolvedCameraTransform)
    {
        cameraTransform = resolvedCameraTransform;
    }

    public void Move(Vector2 moveInput, float speed, float deltaTime)
    {
        Vector3 moveDirection = GetMoveDirection(moveInput);

        if (moveDirection.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * deltaTime);
        }

        Vector3 horizontalVelocity = moveDirection * speed;
        Vector3 finalVelocity = new Vector3(horizontalVelocity.x, verticalVelocity, horizontalVelocity.z);
        characterController.Move(finalVelocity * deltaTime);
    }

    public void ApplyGravity(bool isMovePressed, float deltaTime)
    {
        if (characterController.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = groundedStickForce;
        }

        verticalVelocity += gravity * deltaTime;

        if (!isMovePressed)
        {
            Vector3 verticalOnlyVelocity = new Vector3(0f, verticalVelocity, 0f);
            characterController.Move(verticalOnlyVelocity * deltaTime);
        }
    }

    public void RotateWhileAiming(float deltaTime)
    {
        if (cameraTransform != null)
        {
            Vector3 cameraForward = cameraTransform.forward;
            cameraForward.y = 0f;

            if (cameraForward.sqrMagnitude > 0.0001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(cameraForward.normalized, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, aimAlignSpeed * deltaTime);
                return;
            }
        }

        float yawInput = 0f;

        if (Mouse.current != null)
        {
            yawInput += Mouse.current.delta.ReadValue().x;
        }

        if (Gamepad.current != null)
        {
            yawInput += Gamepad.current.rightStick.ReadValue().x * 10f;
        }

        if (Mathf.Abs(yawInput) <= 0.001f)
        {
            return;
        }

        float yawDelta = yawInput * aimRotationSpeed * deltaTime;
        transform.Rotate(0f, yawDelta, 0f, Space.World);
    }

    private Vector3 GetMoveDirection(Vector2 moveInput)
    {
        Vector3 rawDirection = new Vector3(moveInput.x, 0f, moveInput.y);

        if (rawDirection.sqrMagnitude <= 0.0001f)
        {
            return Vector3.zero;
        }

        if (cameraTransform == null)
        {
            return rawDirection.normalized;
        }

        Vector3 cameraForward = cameraTransform.forward;
        Vector3 cameraRight = cameraTransform.right;
        cameraForward.y = 0f;
        cameraRight.y = 0f;

        cameraForward.Normalize();
        cameraRight.Normalize();

        Vector3 moveDirection = cameraForward * rawDirection.z + cameraRight * rawDirection.x;
        return moveDirection.normalized;
    }
}
