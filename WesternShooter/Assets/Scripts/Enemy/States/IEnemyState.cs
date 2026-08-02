public interface IEnemyState
{
    void Enter(ZombieStateMachine machine);
    void Tick(ZombieStateMachine machine, float deltaTime);
    void Exit(ZombieStateMachine machine);
}
