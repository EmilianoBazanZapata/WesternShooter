using UnityEngine;
using WesternShooter.Zombies.Health;

namespace WesternShooter.Standalone
{
    public sealed class StandaloneTarget : MonoBehaviour
    {
        private readonly ZombieHealthModel health = new ZombieHealthModel(100f);

        public bool Damage()
        {
            if (health.IsDead) return false;
            var died = health.ApplyDamage(25f, out _);
            if (died) Destroy(gameObject);
            return died;
        }
    }
}
