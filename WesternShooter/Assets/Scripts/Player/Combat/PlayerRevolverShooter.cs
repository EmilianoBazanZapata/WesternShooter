using WesternShooter.Player.Input;
using WesternShooter.Shared.Combat;
using UnityEngine;
using WesternShooter.Player.StateMachine;

namespace WesternShooter.Player.Combat
{
    public class PlayerRevolverShooter : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerMovementStateMachine playerMovement;
        [SerializeField] private Transform muzzlePoint;
        [SerializeField] private BulletProjectile bulletPrefab;
        [SerializeField] private AudioSource shotAudioSource;

        [Header("Firing")]
        [SerializeField] private float fireCooldown = 0.2f;
        [SerializeField] private float aimRayDistance = 250f;
        [SerializeField] private LayerMask aimLayers = ~0;

        private PlayerInputReader playerInputReader;
        private UnityEngine.Camera mainCamera;
        private float nextFireTime;
        private Collider[] ownerColliders;

        private void Awake()
        {
            if (playerMovement == null)
                playerMovement = GetComponent<PlayerMovementStateMachine>();

            playerInputReader = GetComponent<PlayerInputReader>();

            mainCamera = UnityEngine.Camera.main;
            ownerColliders = GetComponentsInChildren<Collider>();
        }

        private void Update()
        {
            if (playerMovement == null || playerInputReader == null || muzzlePoint == null || bulletPrefab == null)
                return;

            playerInputReader.EnsurePolledThisFrame();

            if (playerMovement.IsDead)
                return;

            if (!playerInputReader.IsAimPressed)
                return;

            if (!playerInputReader.IsShootPressedThisFrame)
                return;

            if (Time.time < nextFireTime)
                return;

            nextFireTime = Time.time + fireCooldown;

            var shootDirection = GetShootDirection();
            var spawnedBullet = Instantiate(bulletPrefab, muzzlePoint.position, Quaternion.LookRotation(shootDirection));
            spawnedBullet.SetOwner(transform);
            spawnedBullet.IgnoreCollisionsWith(ownerColliders);
            spawnedBullet.Launch(shootDirection);
            PlayShotAudioSource();
        }

        private Vector3 GetShootDirection()
        {
            var targetPoint = GetAimTargetPoint();
            var direction = (targetPoint - muzzlePoint.position).normalized;
            if (direction.sqrMagnitude <= 0.0001f)
                direction = muzzlePoint.forward;

            return direction;
        }

        private Vector3 GetAimTargetPoint()
        {
            if (mainCamera == null)
                mainCamera = UnityEngine.Camera.main;

            if (mainCamera == null)
                return muzzlePoint.position + (muzzlePoint.forward * aimRayDistance);

            var cameraRay = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            var targetPoint = cameraRay.origin + cameraRay.direction * aimRayDistance;

            if (Physics.Raycast(cameraRay, out RaycastHit hit, aimRayDistance, aimLayers, QueryTriggerInteraction.Ignore))
            {
                targetPoint = hit.point;
            }

            return targetPoint;
        }

        private void PlayShotAudioSource()
        {
            if (shotAudioSource == null || shotAudioSource.clip == null)
                return;

            if (shotAudioSource.isPlaying)
                shotAudioSource.Stop();

            shotAudioSource.Play();
        }

    }

}

