using System;
using UnityEngine;

namespace WesternShooter.Zombies.Health
{
    public class ZombieHealthModel
    {
        private readonly float maxHealth;
        private float currentHealth;
        private bool isDead;

        public ZombieHealthModel(float maxHealth)
        {
            this.maxHealth = maxHealth;
            currentHealth = maxHealth;
        }

        public float MaxHealth => maxHealth;
        public float CurrentHealth => currentHealth;
        public bool IsDead => isDead;

        public event Action<float, float> HealthChanged;
        public event Action Died;

        public bool ApplyDamage(float damageAmount, out float appliedDamage)
        {
            if (isDead)
            {
                appliedDamage = 0f;
                return false;
            }

            appliedDamage = Mathf.Max(0f, damageAmount);
            if (appliedDamage <= 0f)
                return false;

            currentHealth = Mathf.Max(0f, currentHealth - appliedDamage);
            HealthChanged?.Invoke(currentHealth, maxHealth);

            if (currentHealth <= 0f)
            {
                MarkDead();
                return true;
            }

            return false;
        }

        public void MarkDead()
        {
            if (isDead)
                return;

            isDead = true;
            Died?.Invoke();
        }
    }

}
