using UnityEngine;

public class ZombieAttackState : IEnemyState
{
    public void Enter(ZombieStateMachine machine)
    {
        machine.StopMovement();
        machine.SetMovingAnimation(false);
    }

    public void Tick(ZombieStateMachine machine, float deltaTime)
    {
        if (machine.IsDead)
        {
            machine.SwitchState(machine.DeadState);
            return;
        }

        if (!machine.HasTarget)
        {
            machine.SwitchState(machine.ChaseState);
            return;
        }

        if (!machine.CanAttackTarget())
        {
            machine.SwitchState(machine.ChaseState);
            return;
        }

        machine.RotateTowardsTarget(deltaTime);
        machine.TryAttack();
    }

    public void Exit(ZombieStateMachine machine)
    {
    }
}
