using UnityEngine;

public class PlayerRunState : IPlayerMovementState
{
    public void Enter(PlayerMovementStateMachine machine)
    {
    }

    public void Tick(PlayerMovementStateMachine machine, float deltaTime)
    {
        machine.ApplyGravity(deltaTime);
        machine.Move(machine.RunSpeed, deltaTime);

        if (!machine.IsMovePressed)
        {
            machine.SwitchState(machine.IdleState);
            return;
        }

        if (!machine.IsRunPressed)
        {
            machine.SwitchState(machine.WalkState);
        }
    }

    public void Exit(PlayerMovementStateMachine machine)
    {
    }
}
