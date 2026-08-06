using UnityEngine;
using WesternShooter.Zombies.StateMachine;

namespace WesternShooter.Zombies.StateMachine.States
{
    public class ZombieChaseState : IEnemyState
    {
        public void Enter(ZombieStateMachine machine)
        {
            machine.ResumeMovement();
            machine.SetMovingAnimation(true);
            machine.EnableFootsteps(true);
        }

        public void Tick(ZombieStateMachine machine, float deltaTime)
        {
            if (machine.IsDead)
            {
                machine.SwitchState(machine.DeadState);
                return;
            }

            if (!machine.HasTarget)
                return;

            machine.UpdateDestinationToTarget();

            if (machine.CanAttackTarget())
                machine.SwitchState(machine.AttackState);
        }

        public void Exit(ZombieStateMachine machine)
        {
            machine.EnableFootsteps(false);
            machine.SetMovingAnimation(false);
        }
    }

}
