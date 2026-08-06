using System;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

namespace WesternShooter.Audio
{
    public class GameAudioManager : MonoBehaviour
    {
        [Header("Mixer")]
        [SerializeField] private AudioMixer audioMixer;

        [Header("Exposed Parameters")]
        [SerializeField] private string masterParameter = "MasterVolume";
        [SerializeField] private string musicParameter = "MusicVolume";
        [SerializeField] private string sfxParameter = "SFXVolume";
        [SerializeField] private string voiceParameter = "VoiceVolume";
        [SerializeField] private string ambienceParameter = "AmbienceVolume";

        [Header("Snapshots")]
        [SerializeField] private AudioMixerSnapshot peacefulSnapshot;
        [SerializeField] private AudioMixerSnapshot wantedSnapshot;
        [SerializeField] private AudioMixerSnapshot characterVoiceSnapshot;
        [SerializeField] private float snapshotTransitionTime = 0.35f;
        [SerializeField] private float characterVoiceTransitionTime = 0.08f;

        [Header("Adaptive Music")]
        [SerializeField] private AudioSource peacefulMusicSource;
        [SerializeField] private AudioSource wantedMusicSource;
        [SerializeField] private AudioSource victoryMusicSource;
        [SerializeField] private AudioSource defeatMusicSource;
        [SerializeField] private float musicCrossFadeDuration = 1.5f;

        [Header("Gameplay Snapshot Automation")]
        [SerializeField] private bool applyPeacefulOnGameplaySceneLoad = true;
        [SerializeField] private string gameplaySceneName = "Level";

        public static GameAudioManager Instance { get; private set; }

        private AudioMixerController mixerController;
        private GameplaySnapshotController snapshotController;
        private AdaptiveMusicController adaptiveMusicController;
        private NonMusicPauseController nonMusicPauseController;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            InitializeControllers();
        }

        private void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;

        private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;

            adaptiveMusicController?.StopActiveCrossFade();
            snapshotController?.ForceReleaseAllVoiceRequests();
            nonMusicPauseController?.ClearTrackedState();
        }

        public void SetVolume(AudioBusType busType, float linearValue) => mixerController?.SetVolume(busType, linearValue);

        public void SetPeacefulSnapshot()
        {
            snapshotController?.SetPeacefulSnapshot();
            adaptiveMusicController?.SetPeacefulMusic();
        }

        public void SetWantedSnapshot()
        {
            snapshotController?.SetWantedSnapshot();
            adaptiveMusicController?.SetWantedMusic();
        }

        public void OnCharacterVoicePlaybackStarted() => snapshotController?.OnCharacterVoicePlaybackStarted();

        public void OnCharacterVoicePlaybackEnded() => snapshotController?.OnCharacterVoicePlaybackEnded();

        public void OnGameplayAreaPeacefulTriggered() => SetPeacefulSnapshot();

        public void OnGameplayAreaWantedTriggered() => SetWantedSnapshot();

        public void OnPlayerVictoryTriggered() => PlayVictoryMusic();

        public void OnPlayerDefeatTriggered() => PlayDefeatMusic();

        public void PlayVictoryMusic()
        {
            if (victoryMusicSource == null)
                return;

            adaptiveMusicController?.PlayVictoryMusic();
        }

        public void PlayDefeatMusic()
        {
            if (defeatMusicSource == null)
                return;

            adaptiveMusicController?.PlayDefeatMusic();
        }

        public void SetNonMusicMuted(bool muted) => nonMusicPauseController?.SetNonMusicMuted(muted);

        private void OnSceneLoaded(Scene loadedScene, LoadSceneMode loadMode)
        {
            if (!applyPeacefulOnGameplaySceneLoad)
                return;

            if (!string.IsNullOrWhiteSpace(gameplaySceneName) && !string.Equals(loadedScene.name, gameplaySceneName, StringComparison.Ordinal))
                return;

            SetPeacefulSnapshot();
        }

        private void InitializeControllers()
        {
            mixerController = new AudioMixerController(
                audioMixer,
                masterParameter,
                musicParameter,
                sfxParameter,
                voiceParameter,
                ambienceParameter);

            snapshotController = new GameplaySnapshotController(
                peacefulSnapshot,
                wantedSnapshot,
                characterVoiceSnapshot,
                snapshotTransitionTime,
                characterVoiceTransitionTime);

            adaptiveMusicController = new AdaptiveMusicController(
                this,
                peacefulMusicSource,
                wantedMusicSource,
                victoryMusicSource,
                defeatMusicSource,
                musicCrossFadeDuration);

            nonMusicPauseController = new NonMusicPauseController();
        }
    }
}
