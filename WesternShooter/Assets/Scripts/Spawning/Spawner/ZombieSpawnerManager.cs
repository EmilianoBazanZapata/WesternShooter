using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using WesternShooter.Zombies.StateMachine;
using WesternShooter.Spawning.Progression;

namespace WesternShooter.Spawning
{
    public class ZombieSpawnerManager : MonoBehaviour
    {
        [Header("Setup")]
        [SerializeField] private ZombieStateMachine zombiePrefab;
        [SerializeField] private Transform[] spawnPoints;

        [Header("Population")]
        [SerializeField] private int maxAliveZombies = 10;
        [SerializeField] private int initialSpawnCount = 10;
        [SerializeField] private float respawnDelaySeconds = 3f;
        [SerializeField] private float spawnIntervalSeconds = 5f;

        [Header("Debug")]
        [SerializeField] private bool logSpawnerEvents = false;

        [Header("Spawn Control")]
        [SerializeField] private bool spawnOnStart = true;

        [Header("Progression Audio")]
        [SerializeField] private int victoryKillThreshold = 3;

        private readonly List<ZombieStateMachine> aliveZombies = new List<ZombieStateMachine>();
        private int pendingSpawnRequests;
        private ZombieKillProgress killProgress;
        private Coroutine spawnQueueRoutine;
        private bool spawningEnabled;

        public int AliveCount => aliveZombies.Count;
        public int MaxAliveZombies => maxAliveZombies;
        public int PendingSpawnCount => pendingSpawnRequests;
        public int TotalKilled => killProgress != null ? killProgress.TotalKilled : 0;
        public ZombieKillProgress KillProgress => killProgress;

        private void Awake()
        {
            killProgress = new ZombieKillProgress();
            killProgress.Configure(victoryKillThreshold);
        }

        private void Start()
        {
            if (zombiePrefab == null)
            {
                Debug.LogWarning("ZombieSpawnerManager: zombiePrefab is not assigned.", this);
                enabled = false;
                return;
            }

            if (spawnPoints == null || spawnPoints.Length == 0)
            {
                Debug.LogWarning("ZombieSpawnerManager: no spawn points assigned.", this);
                enabled = false;
                return;
            }

            RegisterExistingZombiesInScene();
            spawnQueueRoutine = StartCoroutine(ProcessSpawnQueue());

            if (spawnOnStart)
                StartSpawning();
        }

        private void OnDestroy()
        {
            if (spawnQueueRoutine != null)
            {
                StopCoroutine(spawnQueueRoutine);
                spawnQueueRoutine = null;
            }

            for (var i = 0; i < aliveZombies.Count; i++)
            {
                if (aliveZombies[i] != null)
                    aliveZombies[i].ZombieDied -= OnZombieDied;
            }
        }

        private void OnZombieDied(ZombieStateMachine deadZombie)
        {
            if (deadZombie != null)
            {
                deadZombie.ZombieDied -= OnZombieDied;
                aliveZombies.Remove(deadZombie);
                killProgress?.RegisterKill();
            }

            if (logSpawnerEvents)
                Debug.Log($"[Spawner] Zombie died. Alive now: {aliveZombies.Count}/{maxAliveZombies}", this);

            StartCoroutine(QueueRespawnAfterDelay());
        }

        private IEnumerator QueueRespawnAfterDelay()
        {
            yield return new WaitForSeconds(respawnDelaySeconds);

            if (aliveZombies.Count + pendingSpawnRequests >= maxAliveZombies)
            {
                yield break;
            }

            pendingSpawnRequests++;

            if (logSpawnerEvents)
                Debug.Log($"[Spawner] Queued respawn. Pending: {pendingSpawnRequests}", this);
        }

        private IEnumerator ProcessSpawnQueue()
        {
            while (true)
            {
                if (!spawningEnabled)
                {
                    yield return null;
                    continue;
                }

                if (pendingSpawnRequests <= 0)
                {
                    yield return null;
                    continue;
                }

                if (aliveZombies.Count >= maxAliveZombies)
                {
                    yield return null;
                    continue;
                }

                if (SpawnZombie())
                {
                    pendingSpawnRequests--;
                    yield return new WaitForSeconds(spawnIntervalSeconds);
                    continue;
                }

                yield return null;
            }
        }

        private bool SpawnZombie()
        {
            if (aliveZombies.Count >= maxAliveZombies)
                return false;

            var spawnPoint = GetRandomSpawnPoint();
            if (spawnPoint == null)
                return false;

            var spawnedZombie = Instantiate(
                zombiePrefab,
                spawnPoint.position,
                spawnPoint.rotation);

            RegisterZombie(spawnedZombie);

            if (logSpawnerEvents)
                Debug.Log($"[Spawner] Spawned zombie at {spawnPoint.name}. Alive: {aliveZombies.Count}/{maxAliveZombies}", this);

            return true;
        }

        private Transform GetRandomSpawnPoint()
        {
            if (spawnPoints == null || spawnPoints.Length == 0)
                return null;

            var index = Random.Range(0, spawnPoints.Length);
            return spawnPoints[index];
        }

        private void RegisterExistingZombiesInScene()
        {
            var zombiesInScene = FindObjectsByType<ZombieStateMachine>(FindObjectsSortMode.None);
            for (var i = 0; i < zombiesInScene.Length; i++)
            {
                RegisterZombie(zombiesInScene[i]);
            }
        }

        private void RegisterZombie(ZombieStateMachine zombie)
        {
            if (zombie == null || zombie.IsDead)
                return;

            zombie.ZombieDied -= OnZombieDied;
            zombie.ZombieDied += OnZombieDied;

            if (!aliveZombies.Contains(zombie))
                aliveZombies.Add(zombie);
        }

        public void StartSpawning()
        {
            if (spawningEnabled)
                return;

            spawningEnabled = true;
            var availableSlots = Mathf.Max(0, maxAliveZombies - aliveZombies.Count);
            pendingSpawnRequests = Mathf.Clamp(initialSpawnCount, 0, availableSlots);

            if (logSpawnerEvents)
                Debug.Log("[Spawner] Spawning started.", this);
        }

        public void StopSpawning()
        {
            spawningEnabled = false;

            if (logSpawnerEvents)
                Debug.Log("[Spawner] Spawning stopped.", this);
        }

    }

}

