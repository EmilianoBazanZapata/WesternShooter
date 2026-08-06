using UnityEngine;

namespace WesternShooter.Shared.Combat
{
    [RequireComponent(typeof(Rigidbody))]
    public class BulletProjectile : MonoBehaviour
    {
        [SerializeField] private float speed = 80f;
        [SerializeField] private float lifeTime = 4f;
        [SerializeField] private float damage = 25f;

        private Rigidbody projectileRigidbody;
        private Transform ownerRoot;
        private bool hasImpacted;

        private void Awake()
        {
            projectileRigidbody = GetComponent<Rigidbody>();

            if (projectileRigidbody != null)
            {
                projectileRigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                projectileRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            }
        }

        private void OnEnable()
        {
            Destroy(gameObject, lifeTime);
        }

        public void Launch(Vector3 direction)
        {
            if (projectileRigidbody == null)
                return;

            projectileRigidbody.linearVelocity = direction.normalized * speed;
        }

        public void SetOwner(Transform owner) => ownerRoot = owner;

        public void IgnoreCollisionsWith(Collider[] collidersToIgnore)
        {
            if (collidersToIgnore == null || collidersToIgnore.Length == 0)
                return;

            var projectileColliders = GetComponentsInChildren<Collider>();
            for (var i = 0; i < projectileColliders.Length; i++)
            {
                var projectileCollider = projectileColliders[i];
                if (projectileCollider == null)
                    continue;

                for (var j = 0; j < collidersToIgnore.Length; j++)
                {
                    var ownerCollider = collidersToIgnore[j];
                    if (ownerCollider == null)
                        continue;

                    Physics.IgnoreCollision(projectileCollider, ownerCollider, true);
                }
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (hasImpacted)
                return;

            if (ownerRoot != null && collision.transform.IsChildOf(ownerRoot))
                return;

            var damageable = collision.collider.GetComponentInParent<IDamageable>();
            damageable?.TakeDamage(damage);
            hasImpacted = true;
            Destroy(gameObject);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (hasImpacted)
                return;

            if (ownerRoot != null && other.transform.IsChildOf(ownerRoot))
                return;

            var damageable = other.GetComponentInParent<IDamageable>();
            damageable?.TakeDamage(damage);
            hasImpacted = true;
            Destroy(gameObject);
        }
    }

}

