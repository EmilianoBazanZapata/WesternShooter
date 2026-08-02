using UnityEngine;
using UnityEngine.UI;

public class InGameHudController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerMovementStateMachine player;
    [SerializeField] private ZombieSpawnerManager zombieSpawner;

    [Header("UI")]
    [SerializeField] private Text healthText;
    [SerializeField] private Text enemiesText;
    [SerializeField] private Slider healthSlider;
    [SerializeField] private GameObject crosshairRoot;
    [SerializeField] private bool showCrosshairOnlyWhileAiming = true;

    [Header("Labels")]
    [SerializeField] private string healthLabel = "Health";
    [SerializeField] private string enemiesLabel = "Killed";

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
    }

    private void Update()
    {
        RefreshHealthUi();
        RefreshEnemiesUi();
        RefreshCrosshairUi();
    }

    private void RefreshHealthUi()
    {
        if (player == null)
        {
            return;
        }

        if (healthText != null)
        {
            healthText.text = $"{healthLabel}: {player.CurrentHealth:0}/{player.MaxHealth:0}";
        }

        if (healthSlider != null)
        {
            healthSlider.minValue = 0f;
            healthSlider.maxValue = Mathf.Max(1f, player.MaxHealth);
            healthSlider.value = player.CurrentHealth;
        }
    }

    private void RefreshEnemiesUi()
    {
        if (zombieSpawner == null)
        {
            return;
        }

        if (enemiesText == null)
        {
            return;
        }

        enemiesText.text = $"{enemiesLabel}: {zombieSpawner.TotalKilled}";
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

        if (crosshairRoot.activeSelf != shouldShow)
        {
            crosshairRoot.SetActive(shouldShow);
        }
    }
}
