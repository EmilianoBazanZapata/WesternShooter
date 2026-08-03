public class PlayerWalkState : IPlayerMovementState
{
    public void Enter(PlayerMovementStateMachine machine)
    {
    }

    public void Tick(PlayerMovementStateMachine machine, float deltaTime)
    {
        machine.ApplyGravity(deltaTime);
        machine.Move(machine.WalkSpeed, deltaTime);

        if (!machine.IsMovePressed)
        {
            machine.SwitchState(machine.IdleState);
            return;
        }

        if (machine.IsRunPressed)
        {
            machine.SwitchState(machine.RunState);
        }
    }

    public void Exit(PlayerMovementStateMachine machine)
    {
    }
}
