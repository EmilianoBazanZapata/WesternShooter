public interface IPlayerMovementState
{
    void Enter(PlayerMovementStateMachine machine);
    void Tick(PlayerMovementStateMachine machine, float deltaTime);
    void Exit(PlayerMovementStateMachine machine);
}
