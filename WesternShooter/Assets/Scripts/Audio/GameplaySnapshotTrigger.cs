using UnityEngine;

public enum GameplaySnapshotType
{
    Peaceful,
    Wanted
}

[RequireComponent(typeof(Collider))]
public class GameplaySnapshotTrigger : MonoBehaviour
{
    [SerializeField] private GameplaySnapshotType snapshotType = GameplaySnapshotType.Wanted;
    [SerializeField] private bool triggerOnlyOnce = true;
    [SerializeField] private bool startSpawnerOnWanted = true;
    [SerializeField] private ZombieSpawnerManager wantedSpawner;

    private bool hasTriggered;

    private void Reset()
    {
        Collider areaCollider = GetComponent<Collider>();
        areaCollider.isTrigger = true;
    }

    private void Awake()
    {
        Collider areaCollider = GetComponent<Collider>();
        areaCollider.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (triggerOnlyOnce && hasTriggered)
        {
            return;
        }

        // Player detection: CharacterController is exclusive to the player.
        // GetComponentInParent also checks the collider's own GameObject.
        bool isPlayer = other.GetComponentInParent<PlayerMovementStateMachine>() != null
                     || other.GetComponentInParent<CharacterController>() != null;

        if (!isPlayer)
        {
            return;
        }

        GameAudioManager manager = GameAudioManager.Instance
            ?? FindFirstObjectByType<GameAudioManager>();

        if (manager == null)
        {
            Debug.LogWarning($"[GameplaySnapshotTrigger] No GameAudioManager found in scene.", this);
            return;
        }

        if (snapshotType == GameplaySnapshotType.Peaceful)
        {
            manager.OnGameplayAreaPeacefulTriggered();
        }
        else
        {
            manager.OnGameplayAreaWantedTriggered();

            if (startSpawnerOnWanted)
            {
                ZombieSpawnerManager spawner = wantedSpawner
                    ?? FindFirstObjectByType<ZombieSpawnerManager>();

                if (spawner == null)
                {
                    Debug.LogWarning("[GameplaySnapshotTrigger] No ZombieSpawnerManager found for wanted trigger.", this);
                }
                else
                {
                    spawner.StartSpawning();
                }
            }
        }

        Debug.Log($"[GameplaySnapshotTrigger] Snapshot '{snapshotType}' triggered by '{other.name}'.", this);
        hasTriggered = true;
    }
}
