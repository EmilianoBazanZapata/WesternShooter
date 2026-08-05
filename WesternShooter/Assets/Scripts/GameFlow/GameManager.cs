using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public enum GameState
{
    InGame,
    Pause,
    Options,
    Victory,
    Defeat
}

public class GameManager : MonoBehaviour
{
    [Header("Scenes")]
    [SerializeField] private string menuSceneName = "MainMenu";

    [Header("UI")]
    [SerializeField] private GameObject inGameUiRoot;
    [SerializeField] private GameObject pauseUiRoot;
    [SerializeField] private GameObject optionsUiRoot;
    [SerializeField] private GameObject victoryUiRoot;
    [SerializeField] private GameObject defeatUiRoot;

    [Header("Cursor")]
    [SerializeField] private bool lockCursorInGame = true;

    [Header("References")]
    [SerializeField] private PlayerMovementStateMachine playerMovement;
    [SerializeField] private ZombieSpawnerManager zombieSpawner;

    public GameState CurrentState { get; private set; } = GameState.InGame;
    public event Action<GameState, GameState> GameStateChanged;

    private bool defeatHandledFromPlayerDeath;
    private bool victoryHandledFromZombieProgression;
    private bool hasAppliedInitialState;

    private void OnEnable()
    {
        SubscribeToPlayerDeath();
        SubscribeToZombieProgression();
    }

    private void OnDisable()
    {
        UnsubscribeFromPlayerDeath();
        UnsubscribeFromZombieProgression();
    }

    private void Start()
    {
        SubscribeToPlayerDeath();
        SubscribeToZombieProgression();
        SetState(GameState.InGame);
    }

    private void Update()
    {
        if (Keyboard.current == null)
        {
            return;
        }

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            TogglePause();
        }
    }

    public void TogglePause()
    {
        if (CurrentState == GameState.Victory || CurrentState == GameState.Defeat)
        {
            return;
        }

        if (CurrentState == GameState.InGame)
        {
            SetState(GameState.Pause);
            return;
        }

        if (CurrentState == GameState.Options)
        {
            SetState(GameState.Pause);
            return;
        }

        SetState(GameState.InGame);
    }

    public void PauseGame()
    {
        SetState(GameState.Pause);
    }

    public void ResumeGame()
    {
        if (CurrentState == GameState.Victory || CurrentState == GameState.Defeat)
        {
            return;
        }

        Debug.Log("[GameManager] ResumeGame called.", this);
        SetState(GameState.InGame);
    }

    public void OpenOptions()
    {
        SetState(GameState.Options);
    }

    public void CloseOptions()
    {
        SetState(GameState.Pause);
    }

    public void ShowVictory()
    {
        SetState(GameState.Victory);
    }

    public void ShowDefeat()
    {
        SetState(GameState.Defeat);
    }

    private void SubscribeToPlayerDeath()
    {
        if (playerMovement == null)
        {
            playerMovement = FindFirstObjectByType<PlayerMovementStateMachine>();
        }

        if (playerMovement == null)
        {
            Debug.LogWarning("[GameManager] PlayerMovementStateMachine reference is missing.", this);
            return;
        }

        playerMovement.Died -= OnPlayerDied;
        playerMovement.Died += OnPlayerDied;
    }

    private void UnsubscribeFromPlayerDeath()
    {
        if (playerMovement == null)
        {
            return;
        }

        playerMovement.Died -= OnPlayerDied;
    }

    private void OnPlayerDied()
    {
        if (defeatHandledFromPlayerDeath)
        {
            return;
        }

        defeatHandledFromPlayerDeath = true;

        GameAudioManager manager = GameAudioManager.Instance
            ?? FindFirstObjectByType<GameAudioManager>();

        if (manager == null)
        {
            Debug.LogWarning("[GameManager] No GameAudioManager found for defeat music.", this);
            return;
        }

        manager.OnPlayerDefeatTriggered();
        ShowDefeat();
    }

    private void SubscribeToZombieProgression()
    {
        if (zombieSpawner == null)
        {
            zombieSpawner = FindFirstObjectByType<ZombieSpawnerManager>();
        }

        if (zombieSpawner == null || zombieSpawner.KillProgress == null)
        {
            return;
        }

        zombieSpawner.KillProgress.VictoryThresholdReached -= OnVictoryThresholdReached;
        zombieSpawner.KillProgress.VictoryThresholdReached += OnVictoryThresholdReached;
    }

    private void UnsubscribeFromZombieProgression()
    {
        if (zombieSpawner == null || zombieSpawner.KillProgress == null)
        {
            return;
        }

        zombieSpawner.KillProgress.VictoryThresholdReached -= OnVictoryThresholdReached;
    }

    private void OnVictoryThresholdReached(int totalKills)
    {
        if (victoryHandledFromZombieProgression)
        {
            return;
        }

        victoryHandledFromZombieProgression = true;

        GameAudioManager manager = GameAudioManager.Instance
            ?? FindFirstObjectByType<GameAudioManager>();

        if (manager == null)
        {
            Debug.LogWarning("[GameManager] No GameAudioManager found for victory music.", this);
            return;
        }

        manager.OnPlayerVictoryTriggered();
        ShowVictory();
    }

    public void ExitGame()
    {
        Debug.Log("[GameManager] ExitGame called.", this);
        Time.timeScale = 1f;

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void ReturnToMenu()
    {
        Time.timeScale = 1f;

        if (!string.IsNullOrWhiteSpace(menuSceneName) && Application.CanStreamedLevelBeLoaded(menuSceneName))
        {
            SceneManager.LoadScene(menuSceneName);
            return;
        }

        if (SceneManager.sceneCountInBuildSettings > 0)
        {
            Debug.LogWarning($"[GameManager] Could not load menu scene by name '{menuSceneName}'. Falling back to build index 0.", this);
            SceneManager.LoadScene(0);
            return;
        }

        Debug.LogError("[GameManager] No scenes configured in Build Settings.", this);
    }

    private void SetState(GameState newState)
    {
        GameState previousState = CurrentState;

        if (hasAppliedInitialState && previousState == newState)
        {
            return;
        }

        CurrentState = newState;

        bool isPaused = CurrentState == GameState.Pause
                     || CurrentState == GameState.Options
                     || CurrentState == GameState.Victory
                     || CurrentState == GameState.Defeat;
        bool showPauseUi = CurrentState == GameState.Pause;
        bool showOptionsUi = CurrentState == GameState.Options;
        bool showVictoryUi = CurrentState == GameState.Victory;
        bool showDefeatUi = CurrentState == GameState.Defeat;

        Time.timeScale = isPaused ? 0f : 1f;

        if (inGameUiRoot != null)
        {
            inGameUiRoot.SetActive(!showVictoryUi && !showDefeatUi);
        }

        if (pauseUiRoot != null)
        {
            pauseUiRoot.SetActive(showPauseUi);
        }

        if (optionsUiRoot != null)
        {
            optionsUiRoot.SetActive(showOptionsUi);
        }

        if (victoryUiRoot != null)
        {
            victoryUiRoot.SetActive(showVictoryUi);
        }

        if (defeatUiRoot != null)
        {
            defeatUiRoot.SetActive(showDefeatUi);
        }

        GameAudioManager.Instance?.SetNonMusicMuted(isPaused);

        UpdateCursorState(isPaused);

        if (hasAppliedInitialState && previousState != newState)
        {
            GameStateChanged?.Invoke(previousState, newState);
        }

        hasAppliedInitialState = true;
    }

    private void UpdateCursorState(bool isPaused)
    {
        if (isPaused)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return;
        }

        if (lockCursorInGame)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    private void OnDestroy()
    {
        if (Time.timeScale == 0f)
        {
            Time.timeScale = 1f;
        }
    }
}
