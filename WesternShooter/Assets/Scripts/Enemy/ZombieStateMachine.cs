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
    private float currentHealth;
    private float nextAttackTime;
    private bool hasRaisedDeathEvent;

    public ZombieChaseState ChaseState { get; } = new ZombieChaseState();
    public ZombieAttackState AttackState { get; } = new ZombieAttackState();
    public ZombieDeadState DeadState { get; } = new ZombieDeadState();

    public bool IsDead { get; private set; }
    public bool HasTarget => target != null;
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

        if (target == null)
        {
            PlayerMovementStateMachine player = FindFirstObjectByType<PlayerMovementStateMachine>();
            if (player != null)
            {
                target = player.transform;
            }
        }

        currentHealth = maxHealth;
        navMeshAgent.stoppingDistance = attackRange * 0.9f;

        EnsureDamageCollider();
    }

    private void Start()
    {
        SwitchState(ChaseState);

        if (logHealthOnSpawn)
        {
            Debug.Log($"[Zombie] {name} spawned with {currentHealth:0.#}/{maxHealth:0.#} HP.", this);
        }
    }

    private void Update()
    {
        bool canPlayFootsteps = navMeshAgent != null && navMeshAgent.enabled && !navMeshAgent.isStopped;
        float velocitySqr = navMeshAgent != null ? navMeshAgent.velocity.sqrMagnitude : 0f;
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

        float appliedDamage = Mathf.Max(0f, damageAmount);
        currentHealth = Mathf.Max(0f, currentHealth - appliedDamage);

        if (logHealthOnHit)
        {
            Debug.Log($"[Zombie] {name} took {appliedDamage:0.#} damage. Remaining HP: {currentHealth:0.#}/{maxHealth:0.#}.", this);
        }

        if (currentHealth <= 0f)
        {
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
    }

    public void UpdateDestinationToTarget()
    {
        if (!HasTarget || navMeshAgent == null || !navMeshAgent.enabled)
        {
            return;
        }

        navMeshAgent.SetDestination(target.position);
    }

    public bool CanAttackTarget()
    {
        if (!HasTarget)
        {
            return false;
        }

        return Vector3.Distance(transform.position, target.position) <= attackRange;
    }

    public void StopMovement()
    {
        if (navMeshAgent == null || !navMeshAgent.enabled)
        {
            return;
        }

        navMeshAgent.isStopped = true;
        navMeshAgent.ResetPath();
    }

    public void ResumeMovement()
    {
        if (navMeshAgent == null || !navMeshAgent.enabled)
        {
            return;
        }

        navMeshAgent.isStopped = false;
    }

    public void DisableAgent()
    {
        if (navMeshAgent == null || !navMeshAgent.enabled)
        {
            return;
        }

        navMeshAgent.enabled = false;
    }

    public void RotateTowardsTarget(float deltaTime)
    {
        if (!HasTarget)
        {
            return;
        }

        Vector3 direction = target.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * deltaTime);
    }

    public void TryAttack()
    {
        if (Time.time < nextAttackTime)
        {
            return;
        }

        nextAttackTime = Time.time + attackCooldown;

        if (zombieAnimator != null)
        {
            zombieAnimator.SetTrigger(AttackTriggerHash);
        }

        zombieAudio?.PlayAttack();

        IDamageable damageableTarget = target != null ? target.GetComponentInParent<IDamageable>() : null;
        damageableTarget?.TakeDamage(attackDamage);
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
