using UnityEngine;
using UnityEngine.AI;

public class ZombieNavigation
{
    private readonly NavMeshAgent navMeshAgent;
    private readonly Transform ownerTransform;
    private readonly float turnSpeed;

    public ZombieNavigation(NavMeshAgent navMeshAgent, Transform ownerTransform, float turnSpeed)
    {
        this.navMeshAgent = navMeshAgent;
        this.ownerTransform = ownerTransform;
        this.turnSpeed = turnSpeed;
    }

    public bool IsAvailable => navMeshAgent != null && navMeshAgent.enabled;

    public void SetStoppingDistance(float stoppingDistance)
    {
        if (navMeshAgent == null)
        {
            return;
        }

        navMeshAgent.stoppingDistance = stoppingDistance;
    }

    public void UpdateDestination(Vector3 destination)
    {
        if (!IsAvailable)
        {
            return;
        }

        navMeshAgent.SetDestination(destination);
    }

    public void StopMovement()
    {
        if (!IsAvailable)
        {
            return;
        }

        navMeshAgent.isStopped = true;
        navMeshAgent.ResetPath();
    }

    public void ResumeMovement()
    {
        if (!IsAvailable)
        {
            return;
        }

        navMeshAgent.isStopped = false;
    }

    public void DisableAgent()
    {
        if (!IsAvailable)
        {
            return;
        }

        navMeshAgent.enabled = false;
    }

    public float CurrentVelocitySqrMagnitude
    {
        get
        {
            if (navMeshAgent == null)
            {
                return 0f;
            }

            return navMeshAgent.velocity.sqrMagnitude;
        }
    }

    public bool CanPlayFootsteps
    {
        get
        {
            if (navMeshAgent == null)
            {
                return false;
            }

            return navMeshAgent.enabled && !navMeshAgent.isStopped;
        }
    }

    public void RotateTowards(Vector3 targetPosition, float deltaTime)
    {
        if (ownerTransform == null)
        {
            return;
        }

        Vector3 direction = targetPosition - ownerTransform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        ownerTransform.rotation = Quaternion.Slerp(ownerTransform.rotation, targetRotation, turnSpeed * deltaTime);
    }
}