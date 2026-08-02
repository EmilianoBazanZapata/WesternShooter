using UnityEngine;

public class PlayerIdleState : IPlayerMovementState
{
    public void Enter(PlayerMovementStateMachine machine)
    {
    }

    public void Tick(PlayerMovementStateMachine machine, float deltaTime)
    {
        machine.ApplyGravity(deltaTime);

        if (machine.IsMovePressed)
        {
            if (machine.IsRunPressed)
            {
                machine.SwitchState(machine.RunState);
                return;
            }

            machine.SwitchState(machine.WalkState);
        }
    }

    public void Exit(PlayerMovementStateMachine machine)
    {
    }
}
