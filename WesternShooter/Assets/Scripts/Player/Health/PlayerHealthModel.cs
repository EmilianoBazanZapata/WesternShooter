using System;

namespace WesternShooter.Player.Health
{
    public class PlayerHealthModel
    {
        private readonly float maxHealth;
        private float currentHealth;
        private bool isDead;

        public PlayerHealthModel(float maxHealth)
        {
            this.maxHealth = maxHealth;
            currentHealth = maxHealth;
        }

        public float MaxHealth => maxHealth;
        public float CurrentHealth => currentHealth;
        public bool IsDead => isDead;

        public event Action<float, float> HealthChanged;
        public event Action Died;

        public void ApplyDamage(float damageAmount)
        {
            if (isDead)
                return;

            currentHealth -= damageAmount;

            if (currentHealth <= 0f)
            {
                currentHealth = 0f;
                HealthChanged?.Invoke(currentHealth, maxHealth);
                MarkDead();
                return;
            }

            HealthChanged?.Invoke(currentHealth, maxHealth);
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

