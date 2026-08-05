using System;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class ZombieStateMachine : MonoBehaviour, IDamageable
{
    private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");
    private static readonly int DeathTriggerHash = Animator.StringToHash("DeathTrigger");
    private static readonly int AttackTriggerHash = Animator.StringToHash("AttackTrigger");

    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Stats")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float attackCooldown = 1.2f;
    [SerializeField] private float turnSpeed = 8f;

    [Header("Damage")]
    [SerializeField] private float attackDamage = 10f;

    [Header("Debug")]
    [SerializeField] private bool logHealthOnSpawn = true;
    [SerializeField] private bool logHealthOnHit = true;

    [Header("References")]
    [SerializeField] private Animator zombieAnimator;
    [SerializeField] private ZombieAudioController zombieAudio;

    private NavMeshAgent navMeshAgent;
    private IEnemyState currentState;
    private ZombieHealthModel zombieHealth;
    private ZombieTarget zombieTarget;
    private ZombieNavigation zombieNavigation;
    private ZombieAttackController zombieAttackController;
    private bool hasRaisedDeathEvent;

    public ZombieChaseState ChaseState { get; } = new ZombieChaseState();
    public ZombieAttackState AttackState { get; } = new ZombieAttackState();
    public ZombieDeadState DeadState { get; } = new ZombieDeadState();

    public bool IsDead { get; private set; }
    public float CurrentHealth => zombieHealth != null ? zombieHealth.CurrentHealth : maxHealth;
    public float MaxHealth => zombieHealth != null ? zombieHealth.MaxHealth : maxHealth;
    public bool HasTarget => zombieTarget != null && zombieTarget.HasTarget;
    public event Action<ZombieStateMachine> ZombieDied;

    private void Awake()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();

        if (zombieAnimator == null)
        {
            zombieAnimator = GetComponentInChildren<Animator>();
        }

        if (zombieAudio == null)
        {
            zombieAudio = GetComponent<ZombieAudioController>();

            if (zombieAudio == null)
            {
                Debug.LogWarning($"[Zombie] {name} has no ZombieAudioController assigned.", this);
            }
        }

        zombieTarget = new ZombieTarget(target);
        if (!zombieTarget.TryResolveFallbackTarget())
        {
            Debug.LogWarning($"[Zombie] {name} has no target and could not resolve player fallback.", this);
        }
        else if (target == null)
        {
            target = zombieTarget.Target;
        }

        zombieHealth = new ZombieHealthModel(maxHealth);
        zombieNavigation = new ZombieNavigation(navMeshAgent, transform, turnSpeed);
        zombieNavigation.SetStoppingDistance(attackRange * 0.9f);

        zombieAttackController = new ZombieAttackController(
            zombieAnimator,
            zombieAudio,
            AttackTriggerHash,
            attackRange,
            attackCooldown,
            attackDamage);

        EnsureDamageCollider();
    }

    private void Start()
    {
        SwitchState(ChaseState);

        if (logHealthOnSpawn)
        {
            Debug.Log($"[Zombie] {name} spawned with {CurrentHealth:0.#}/{MaxHealth:0.#} HP.", this);
        }
    }

    private void Update()
    {
        bool canPlayFootsteps = zombieNavigation != null && zombieNavigation.CanPlayFootsteps;
        float velocitySqr = zombieNavigation != null ? zombieNavigation.CurrentVelocitySqrMagnitude : 0f;
        zombieAudio?.UpdateFootsteps(canPlayFootsteps, velocitySqr);

        currentState?.Tick(this, Time.deltaTime);
    }

    public void SwitchState(IEnemyState newState)
    {
        if (currentState == newState)
        {
            return;
        }

        currentState?.Exit(this);
        currentState = newState;
        currentState.Enter(this);
    }

    public void TakeDamage(float damageAmount)
    {
        if (IsDead)
        {
            return;
        }

        if (zombieHealth == null)
        {
            return;
        }

        bool becameDead = zombieHealth.ApplyDamage(damageAmount, out float appliedDamage);

        if (logHealthOnHit)
        {
            Debug.Log($"[Zombie] {name} took {appliedDamage:0.#} damage. Remaining HP: {CurrentHealth:0.#}/{MaxHealth:0.#}.", this);
        }

        if (becameDead)
        {
            OnHealthDepleted();
        }
    }

    private void OnHealthDepleted()
    {
        if (IsDead)
        {
            return;
        }

        IsDead = true;

        if (!hasRaisedDeathEvent)
        {
            hasRaisedDeathEvent = true;
            ZombieDied?.Invoke(this);
        }

        if (logHealthOnHit)
        {
            Debug.Log($"[Zombie] {name} is dead.", this);
        }

        SwitchState(DeadState);
    }

    public void UpdateDestinationToTarget()
    {
        if (!HasTarget || zombieNavigation == null)
        {
            return;
        }

        zombieNavigation.UpdateDestination(zombieTarget.Position);
    }

    public bool CanAttackTarget()
    {
        if (!HasTarget || zombieAttackController == null)
        {
            return false;
        }

        return zombieAttackController.CanAttack(transform.position, zombieTarget);
    }

    public void StopMovement()
    {
        zombieNavigation?.StopMovement();
    }

    public void ResumeMovement()
    {
        zombieNavigation?.ResumeMovement();
    }

    public void DisableAgent()
    {
        zombieNavigation?.DisableAgent();
    }

    public void RotateTowardsTarget(float deltaTime)
    {
        if (!HasTarget || zombieNavigation == null)
        {
            return;
        }

        zombieNavigation.RotateTowards(zombieTarget.Position, deltaTime);
    }

    public void TryAttack()
    {
        zombieAttackController?.TryAttack(zombieTarget);
    }

    public void SetTarget(Transform newTarget)
    {
        if (zombieTarget == null)
        {
            zombieTarget = new ZombieTarget(newTarget);
        }
        else
        {
            zombieTarget.SetTarget(newTarget);
        }

        target = newTarget;
    }

    public void ClearTarget()
    {
        zombieTarget?.ClearTarget();
        target = null;
    }

    public void SetMovingAnimation(bool isMoving)
    {
        if (zombieAnimator == null)
        {
            return;
        }

        zombieAnimator.SetBool(IsMovingHash, isMoving);
    }

    public void TriggerDeathAnimation()
    {
        if (zombieAnimator == null)
        {
            return;
        }

        zombieAnimator.SetTrigger(DeathTriggerHash);
    }

    public void EnableFootsteps(bool enabled)
    {
        zombieAudio?.SetFootstepsEnabled(enabled);
    }

    public void PlayDeathAudio()
    {
        zombieAudio?.PlayDeath();
    }

    private void EnsureDamageCollider()
    {
        Collider existingCollider = GetComponentInChildren<Collider>();
        if (existingCollider != null)
        {
            return;
        }

        CapsuleCollider capsuleCollider = gameObject.AddComponent<CapsuleCollider>();
        capsuleCollider.center = new Vector3(0f, 1f, 0f);
        capsuleCollider.height = 2f;
        capsuleCollider.radius = 0.35f;
        capsuleCollider.isTrigger = false;

        Debug.LogWarning($"[Zombie] {name} had no collider. Added CapsuleCollider automatically for bullet hits.", this);
    }
}
