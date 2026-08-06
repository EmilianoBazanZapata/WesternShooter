using UnityEngine.InputSystem;
using UnityEngine;
using WesternShooter.Player.Input;

namespace WesternShooter.Camera
{
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

        private UnityEngine.Camera cameraComponent;
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
        private PlayerInputReader playerInputReader;
        private bool hasWarnedMissingInputReader;

        private void Start()
        {
            if (target == null)
            {
                enabled = false;
                return;
            }

            ResolvePlayerInputReaderFromTarget();

            var euler = transform.eulerAngles;
            yaw = euler.y;
            pitch = euler.x;

            cameraComponent = GetComponent<UnityEngine.Camera>();
            if (cameraComponent != null)
                currentFieldOfView = cameraComponent.fieldOfView;
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
                ResolvePlayerInputReaderFromTarget();

            playerInputReader?.EnsurePolledThisFrame();

            HandleShoulderSwapInput();

            var isAiming = IsAiming();
            var lookInput = GetLookInput();
            var lookSensitivity = isAiming ? aimLookSensitivity : normalLookSensitivity;

            yaw += lookInput.x * lookSensitivity * Time.deltaTime;
            var pitchDelta = lookInput.y * lookSensitivity * Time.deltaTime;
            pitch += invertY ? pitchDelta : -pitchDelta;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

            var targetDistance = isAiming ? aimDistance : normalDistance;
            var targetHeight = isAiming ? aimHeight : normalHeight;
            var sideSign = useLeftShoulder ? -1f : 1f;
            var targetSideOffset = (isAiming ? aimSideOffset : normalSideOffset) * sideSign;

            currentDistance = Mathf.SmoothDamp(currentDistance, targetDistance, ref distanceVelocity, profileSmoothTime);
            currentHeight = Mathf.SmoothDamp(currentHeight, targetHeight, ref heightVelocity, profileSmoothTime);
            currentSideOffset = Mathf.SmoothDamp(currentSideOffset, targetSideOffset, ref sideOffsetVelocity, profileSmoothTime);

            var rotation = Quaternion.Euler(pitch, yaw, 0f);
            var targetPosition = target.position + Vector3.up * currentHeight;
            var desiredPosition = targetPosition - (rotation * Vector3.forward * currentDistance) + (rotation * Vector3.right * currentSideOffset);
            var resolvedPosition = ResolveCollisionPosition(targetPosition, desiredPosition);

            transform.position = Vector3.SmoothDamp(transform.position, resolvedPosition, ref currentVelocity, positionSmoothTime);
            var lookTarget = targetPosition + (rotation * Vector3.forward * 15f);
            transform.rotation = Quaternion.LookRotation(lookTarget - transform.position, Vector3.up);

            if (cameraComponent != null)
            {
                var targetFieldOfView = isAiming ? aimFieldOfView : normalFieldOfView;
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
                return;

            if (playerInputReader == null)
                return;

            if (playerInputReader.GetShoulderSwapPressedThisFrame(shoulderSwapKey))
                useLeftShoulder = !useLeftShoulder;
        }

        private Vector3 ResolveCollisionPosition(Vector3 pivotPosition, Vector3 desiredPosition)
        {
            var castDirection = desiredPosition - pivotPosition;
            var castDistance = castDirection.magnitude;

            if (castDistance <= 0.001f)
                return desiredPosition;

            castDirection /= castDistance;

            var hits = Physics.SphereCastAll(pivotPosition, collisionSphereRadius, castDirection, castDistance, collisionLayers, QueryTriggerInteraction.Ignore);
            if (hits.Length > 0)
            {
                var closestValidHit = default(RaycastHit);
                var foundValidHit = false;

                for (var i = 0; i < hits.Length; i++)
                {
                    var currentHit = hits[i];

                    if (target != null && currentHit.transform.IsChildOf(target))
                        continue;

                    if (!foundValidHit || currentHit.distance < closestValidHit.distance)
                    {
                        closestValidHit = currentHit;
                        foundValidHit = true;
                    }
                }

                if (foundValidHit)
                {
                    var safeDistance = Mathf.Max(minimumCollisionDistance, closestValidHit.distance - collisionBuffer);
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
                return;

            playerInputReader = target.GetComponentInParent<PlayerInputReader>();
            if (playerInputReader != null)
                return;

            if (hasWarnedMissingInputReader)
                return;

            hasWarnedMissingInputReader = true;
        }
    }

}

