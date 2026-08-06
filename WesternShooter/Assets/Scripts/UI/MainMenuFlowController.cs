using System.Collections;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine;

namespace WesternShooter.UI
{
    public class MainMenuFlowController : MonoBehaviour
    {
        [Header("Scenes")]
        [SerializeField] private string levelSceneName = "Level";

        [Header("UI")]
        [SerializeField] private GameObject menuUiRoot;
        [SerializeField] private GameObject controlsUiRoot;

        private Coroutine loadingRoutine;

        public MainMenuState CurrentState { get; private set; } = MainMenuState.Menu;

        private void Start()
        {
            SetState(MainMenuState.Menu);
        }

        private void Update()
        {
            if (Keyboard.current == null)
                return;

            if (Keyboard.current.escapeKey.wasPressedThisFrame && CurrentState == MainMenuState.Controls)
                SetState(MainMenuState.Menu);
        }

        public void OnStartPressed()
        {
            if (loadingRoutine != null)
                return;

            loadingRoutine = StartCoroutine(LoadLevelAsync());
        }

        public void OnControlsPressed() => SetState(MainMenuState.Controls);

        public void OnBackFromControlsPressed() => SetState(MainMenuState.Menu);

        public void OnExitPressed()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private IEnumerator LoadLevelAsync()
        {
            Debug.Log($"[MainMenu] Starting async load for scene '{levelSceneName}'.");

            var loadOperation = SceneManager.LoadSceneAsync(levelSceneName);
            if (loadOperation == null)
            {
                Debug.LogError($"[MainMenu] Could not start loading scene '{levelSceneName}'. Check Build Settings.");
                loadingRoutine = null;
                yield break;
            }

            loadOperation.allowSceneActivation = false;

            var lastLoggedRemainingPercent = -1;

            while (!loadOperation.isDone)
            {
                var normalizedProgress = Mathf.Clamp01(loadOperation.progress / 0.9f);
                var remainingPercent = Mathf.Clamp(Mathf.CeilToInt((1f - normalizedProgress) * 100f), 0, 100);

                if (remainingPercent != lastLoggedRemainingPercent)
                {
                    Debug.Log($"[MainMenu] Loading '{levelSceneName}'... remaining {remainingPercent}%");
                    lastLoggedRemainingPercent = remainingPercent;
                }

                if (loadOperation.progress >= 0.9f)
                    loadOperation.allowSceneActivation = true;

                yield return null;
            }
        }

        private void SetState(MainMenuState newState)
        {
            CurrentState = newState;

            var showMenu = CurrentState == MainMenuState.Menu;
            var showControls = CurrentState == MainMenuState.Controls;

            if (menuUiRoot != null)
                menuUiRoot.SetActive(showMenu);

            if (controlsUiRoot != null)
                controlsUiRoot.SetActive(showControls);
        }
    }

}
