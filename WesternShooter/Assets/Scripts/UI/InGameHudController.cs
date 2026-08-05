using System;
using UnityEngine;
using UnityEngine.UI;

public class InGameHudController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerMovementStateMachine player;
    [SerializeField] private ZombieSpawnerManager zombieSpawner;
    [SerializeField] private GameManager gameManager;

    [Header("UI")]
    [SerializeField] private Text healthText;
    [SerializeField] private Text enemiesText;
    [SerializeField] private Slider healthSlider;
    [SerializeField] private GameObject crosshairRoot;
    [SerializeField] private bool showCrosshairOnlyWhileAiming = true;

    [Header("Labels")]
    [SerializeField] private string healthLabel = "Health";
    [SerializeField] private string enemiesLabel = "Killed";

    private ZombieKillProgress killProgress;
    private GameState currentGameState = GameState.InGame;

    private void Awake()
    {
        if (string.Equals(enemiesLabel, "Enemies", System.StringComparison.OrdinalIgnoreCase))
        {
            enemiesLabel = "Killed";
        }

        if (player == null)
        {
            player = FindFirstObjectByType<PlayerMovementStateMachine>();
        }

        if (zombieSpawner == null)
        {
            zombieSpawner = FindFirstObjectByType<ZombieSpawnerManager>();
        }

        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<GameManager>();
        }
    }

    private void OnEnable()
    {
        ResolveReferences();
        SubscribeToEvents();

        RefreshHealthUi();
        RefreshEnemiesUi();
        RefreshCrosshairUi();
    }

    private void OnDisable()
    {
        UnsubscribeFromEvents();
    }

    private void ResolveReferences()
    {
        if (player == null)
        {
            player = FindFirstObjectByType<PlayerMovementStateMachine>();
        }

        if (zombieSpawner == null)
        {
            zombieSpawner = FindFirstObjectByType<ZombieSpawnerManager>();
        }

        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<GameManager>();
        }

        killProgress = zombieSpawner != null ? zombieSpawner.KillProgress : null;
        if (gameManager != null)
        {
            currentGameState = gameManager.CurrentState;
        }
    }

    private void SubscribeToEvents()
    {
        if (player != null)
        {
            player.HealthChanged -= OnPlayerHealthChanged;
            player.HealthChanged += OnPlayerHealthChanged;
            player.Died -= OnPlayerDied;
            player.Died += OnPlayerDied;
            player.AimChanged -= OnAimChanged;
            player.AimChanged += OnAimChanged;
        }

        if (killProgress != null)
        {
            killProgress.TotalKilledChanged -= OnTotalKilledChanged;
            killProgress.TotalKilledChanged += OnTotalKilledChanged;
        }

        if (gameManager != null)
        {
            gameManager.GameStateChanged -= OnGameStateChanged;
            gameManager.GameStateChanged += OnGameStateChanged;
        }
    }

    private void UnsubscribeFromEvents()
    {
        if (player != null)
        {
            player.HealthChanged -= OnPlayerHealthChanged;
            player.Died -= OnPlayerDied;
            player.AimChanged -= OnAimChanged;
        }

        if (killProgress != null)
        {
            killProgress.TotalKilledChanged -= OnTotalKilledChanged;
        }

        if (gameManager != null)
        {
            gameManager.GameStateChanged -= OnGameStateChanged;
        }
    }

    private void OnPlayerHealthChanged(float currentHealth, float maxHealth)
    {
        RefreshHealthUi(currentHealth, maxHealth);
    }

    private void OnPlayerDied()
    {
        RefreshHealthUi();
        RefreshCrosshairUi();
    }

    private void OnTotalKilledChanged(int totalKilled)
    {
        RefreshEnemiesUi(totalKilled);
    }

    private void OnGameStateChanged(GameState previousState, GameState newState)
    {
        currentGameState = newState;
        RefreshCrosshairUi();
    }

    private void OnAimChanged(bool isAiming)
    {
        RefreshCrosshairUi();
    }

    private void RefreshHealthUi()
    {
        if (player == null)
        {
            return;
        }

        RefreshHealthUi(player.CurrentHealth, player.MaxHealth);
    }

    private void RefreshHealthUi(float currentHealth, float maxHealth)
    {
        float clampedMaxHealth = Mathf.Max(1f, maxHealth);

        if (healthText != null)
        {
            healthText.text = $"{healthLabel}: {currentHealth:0}/{maxHealth:0}";
        }

        if (healthSlider != null)
        {
            healthSlider.minValue = 0f;
            healthSlider.maxValue = clampedMaxHealth;
            healthSlider.value = Mathf.Clamp(currentHealth, 0f, clampedMaxHealth);
        }
    }

    private void RefreshEnemiesUi()
    {
        int totalKilled = killProgress != null
            ? killProgress.TotalKilled
            : zombieSpawner != null
                ? zombieSpawner.TotalKilled
                : 0;

        RefreshEnemiesUi(totalKilled);
    }

    private void RefreshEnemiesUi(int totalKilled)
    {
        if (enemiesText != null)
        {
            enemiesText.text = $"{enemiesLabel}: {totalKilled}";
        }
    }

    private void RefreshCrosshairUi()
    {
        if (crosshairRoot == null)
        {
            return;
        }

        bool shouldShow = true;

        if (showCrosshairOnlyWhileAiming)
        {
            shouldShow = player != null && player.IsAimPressed && !player.IsDead;
        }

        if (currentGameState != GameState.InGame)
        {
            shouldShow = false;
        }

        if (crosshairRoot.activeSelf != shouldShow)
        {
            crosshairRoot.SetActive(shouldShow);
        }
    }
}
