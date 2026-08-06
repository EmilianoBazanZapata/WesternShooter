using UnityEngine;
using WesternShooter.Player.StateMachine;

namespace WesternShooter.Spawning
{
    /// <summary>
    /// Coloca este componente en un GameObject con Collider (IsTrigger = true).
    /// Cuando el jugador entra, activa (o detiene) el ZombieSpawnerManager indicado.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class SpawnerActivationTrigger : MonoBehaviour
    {
        [SerializeField] private ZombieSpawnerManager spawner;
        [SerializeField] private bool stopOnEnter = false;
        [SerializeField] private bool triggerOnlyOnce = true;

        private bool hasTriggered;

        private void OnTriggerEnter(Collider other)
        {
            if (triggerOnlyOnce && hasTriggered)
                return;

            var isPlayer = other.GetComponentInParent<PlayerMovementStateMachine>() != null
                        || other.GetComponentInParent<CharacterController>() != null;

            if (!isPlayer)
                return;

            if (spawner == null)
            {
                Debug.LogWarning("[SpawnerActivationTrigger] No ZombieSpawnerManager assigned.", this);
                return;
            }

            if (stopOnEnter)
                spawner.StopSpawning();
            else
            {
                spawner.StartSpawning();
            }

            Debug.Log($"[SpawnerActivationTrigger] Spawner '{(stopOnEnter ? "stopped" : "started")}' by '{other.name}'.", this);
            hasTriggered = true;
        }
    }

}
