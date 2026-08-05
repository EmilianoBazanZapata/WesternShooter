public class ZombieDeadState : IEnemyState
{
    public void Enter(ZombieStateMachine machine)
    {
        machine.StopMovement();
        machine.EnableFootsteps(false);
        machine.SetMovingAnimation(false);
        machine.TriggerDeathAnimation();
        machine.PlayDeathAudio();
        machine.DisableAgent();
    }

    public void Tick(ZombieStateMachine machine, float deltaTime)
    {
    }

    public void Exit(ZombieStateMachine machine)
    {
    }
}
