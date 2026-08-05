using UnityEngine;

public class ZombieAttackController
{
    private readonly Animator zombieAnimator;
    private readonly ZombieAudioController zombieAudio;
    private readonly int attackTriggerHash;
    private readonly float attackRange;
    private readonly float attackCooldown;
    private readonly float attackDamage;

    private float nextAttackTime;

    public ZombieAttackController(
        Animator zombieAnimator,
        ZombieAudioController zombieAudio,
        int attackTriggerHash,
        float attackRange,
        float attackCooldown,
        float attackDamage)
    {
        this.zombieAnimator = zombieAnimator;
        this.zombieAudio = zombieAudio;
        this.attackTriggerHash = attackTriggerHash;
        this.attackRange = attackRange;
        this.attackCooldown = attackCooldown;
        this.attackDamage = attackDamage;
    }

    public bool CanAttack(Vector3 origin, ZombieTarget zombieTarget)
    {
        if (zombieTarget == null || !zombieTarget.HasTarget)
        {
            return false;
        }

        return zombieTarget.DistanceFrom(origin) <= attackRange;
    }

    public void TryAttack(ZombieTarget zombieTarget)
    {
        if (Time.time < nextAttackTime)
        {
            return;
        }

        nextAttackTime = Time.time + attackCooldown;

        if (zombieAnimator != null)
        {
            zombieAnimator.SetTrigger(attackTriggerHash);
        }

        zombieAudio?.PlayAttack();

        Transform target = zombieTarget != null ? zombieTarget.Target : null;
        IDamageable damageableTarget = target != null ? target.GetComponentInParent<IDamageable>() : null;
        damageableTarget?.TakeDamage(attackDamage);
    }
}