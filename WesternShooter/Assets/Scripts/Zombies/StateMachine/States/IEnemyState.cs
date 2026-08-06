using WesternShooter.Zombies.StateMachine;

namespace WesternShooter.Zombies.StateMachine.States
{
    public interface IEnemyState
    {
        void Enter(ZombieStateMachine machine);
        void Tick(ZombieStateMachine machine, float deltaTime);
        void Exit(ZombieStateMachine machine);
    }

}
