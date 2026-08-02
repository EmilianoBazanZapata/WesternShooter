using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

public class GameAudioManager : MonoBehaviour
{
    private const float MinDb = -80f;
    private const float MaxDb = 0f;

    private enum GameplaySnapshotState
    {
        Peaceful,
        Wanted
    }

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

    private readonly HashSet<int> pausedNonMusicSourceIds = new HashSet<int>();
    private Coroutine crossFadeCoroutine;
    private GameplaySnapshotState currentGameplaySnapshotState = GameplaySnapshotState.Peaceful;
    private int activeVoiceSnapshotRequests;
    private bool isCharacterVoiceSnapshotActive;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    public void SetVolume(AudioBusType busType, float linearValue)
    {
        float clamped = Mathf.Clamp(linearValue, 0.0001f, 1f);

        switch (busType)
        {
            case AudioBusType.Master:
                ApplyMixerVolume(masterParameter, clamped);
                break;
            case AudioBusType.Music:
                ApplyMixerVolume(musicParameter, clamped);
                break;
            case AudioBusType.Sfx:
                ApplyMixerVolume(sfxParameter, clamped);
                break;
            case AudioBusType.Voice:
                ApplyMixerVolume(voiceParameter, clamped);
                break;
            case AudioBusType.Ambience:
                ApplyMixerVolume(ambienceParameter, clamped);
                break;
        }
    }

    public void SetPeacefulSnapshot()
    {
        currentGameplaySnapshotState = GameplaySnapshotState.Peaceful;
        ApplyGameplaySnapshotIfAvailable();

        CrossFadeMusic(peacefulMusicSource, wantedMusicSource);
    }

    public void SetWantedSnapshot()
    {
        currentGameplaySnapshotState = GameplaySnapshotState.Wanted;
        ApplyGameplaySnapshotIfAvailable();

        CrossFadeMusic(wantedMusicSource, peacefulMusicSource);
    }

    public void OnCharacterVoicePlaybackStarted()
    {
        if (characterVoiceSnapshot == null)
        {
            return;
        }

        activeVoiceSnapshotRequests++;

        if (isCharacterVoiceSnapshotActive)
        {
            return;
        }

        isCharacterVoiceSnapshotActive = true;
        characterVoiceSnapshot.TransitionTo(characterVoiceTransitionTime);
    }

    public void OnCharacterVoicePlaybackEnded()
    {
        if (activeVoiceSnapshotRequests > 0)
        {
            activeVoiceSnapshotRequests--;
        }

        if (activeVoiceSnapshotRequests > 0)
        {
            return;
        }

        if (!isCharacterVoiceSnapshotActive)
        {
            return;
        }

        isCharacterVoiceSnapshotActive = false;
        ApplyGameplaySnapshotIfAvailable();
    }

    private void CrossFadeMusic(AudioSource fadeIn, AudioSource fadeOut)
    {
        if (fadeIn == null && fadeOut == null)
        {
            return;
        }

        if (crossFadeCoroutine != null)
        {
            StopCoroutine(crossFadeCoroutine);
        }

        crossFadeCoroutine = StartCoroutine(CrossFadeRoutine(fadeIn, fadeOut));
    }

    private IEnumerator CrossFadeRoutine(AudioSource fadeIn, AudioSource fadeOut)
    {
        float startVolumeIn = 0f;
        float startVolumeOut = fadeOut != null ? fadeOut.volume : 0f;

        if (fadeIn != null && !fadeIn.isPlaying)
        {
            fadeIn.volume = 0f;
            fadeIn.Play();
        }

        float elapsed = 0f;
        while (elapsed < musicCrossFadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / musicCrossFadeDuration);

            if (fadeIn != null)
            {
                fadeIn.volume = Mathf.Lerp(startVolumeIn, 1f, t);
            }

            if (fadeOut != null)
            {
                fadeOut.volume = Mathf.Lerp(startVolumeOut, 0f, t);
            }

            yield return null;
        }

        if (fadeOut != null)
        {
            fadeOut.Stop();
            fadeOut.volume = 1f;
        }

        crossFadeCoroutine = null;
    }

    public void OnGameplayAreaPeacefulTriggered()
    {
        SetPeacefulSnapshot();
    }

    public void OnGameplayAreaWantedTriggered()
    {
        SetWantedSnapshot();
    }

    public void OnPlayerVictoryTriggered()
    {
        PlayVictoryMusic();
    }

    public void OnPlayerDefeatTriggered()
    {
        PlayDefeatMusic();
    }

    public void PlayVictoryMusic()
    {
        PlayOutcomeMusic(victoryMusicSource);
    }

    public void PlayDefeatMusic()
    {
        PlayOutcomeMusic(defeatMusicSource);
    }

    private void PlayOutcomeMusic(AudioSource outcomeSource)
    {
        if (outcomeSource == null)
        {
            Debug.LogWarning("[GameAudioManager] Outcome music source is not assigned.", this);
            return;
        }

        if (crossFadeCoroutine != null)
        {
            StopCoroutine(crossFadeCoroutine);
            crossFadeCoroutine = null;
        }

        StopMusicSource(peacefulMusicSource);
        StopMusicSource(wantedMusicSource);
        StopMusicSource(victoryMusicSource, outcomeSource);
        StopMusicSource(defeatMusicSource, outcomeSource);

        outcomeSource.volume = 1f;
        outcomeSource.pitch = 1f;
        outcomeSource.Stop();
        outcomeSource.Play();
    }

    public void SetNonMusicMuted(bool muted)
    {
        if (muted)
        {
            PauseNonMusicSources();
            return;
        }

        ResumePausedNonMusicSources();
    }

    private void PauseNonMusicSources()
    {
        AudioSource[] allSources = FindObjectsByType<AudioSource>(FindObjectsSortMode.None);
        for (int i = 0; i < allSources.Length; i++)
        {
            AudioSource source = allSources[i];
            if (source == null || !source.isPlaying || IsMusicSource(source))
            {
                continue;
            }

            source.Pause();
            pausedNonMusicSourceIds.Add(source.GetInstanceID());
        }
    }

    private void ResumePausedNonMusicSources()
    {
        if (pausedNonMusicSourceIds.Count == 0)
        {
            return;
        }

        AudioSource[] allSources = FindObjectsByType<AudioSource>(FindObjectsSortMode.None);
        for (int i = 0; i < allSources.Length; i++)
        {
            AudioSource source = allSources[i];
            if (source == null)
            {
                continue;
            }

            int sourceId = source.GetInstanceID();
            if (!pausedNonMusicSourceIds.Contains(sourceId))
            {
                continue;
            }

            source.UnPause();
        }

        pausedNonMusicSourceIds.Clear();
    }

    private static bool IsMusicSource(AudioSource source)
    {
        AudioMixerGroup outputGroup = source.outputAudioMixerGroup;
        return outputGroup != null && string.Equals(outputGroup.name, "Music", System.StringComparison.OrdinalIgnoreCase);
    }

    private static void StopMusicSource(AudioSource source, AudioSource except = null)
    {
        if (source == null || source == except)
        {
            return;
        }

        source.Stop();
        source.volume = 1f;
    }

    private void OnSceneLoaded(Scene loadedScene, LoadSceneMode loadMode)
    {
        if (!applyPeacefulOnGameplaySceneLoad)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(gameplaySceneName) && !string.Equals(loadedScene.name, gameplaySceneName, System.StringComparison.Ordinal))
        {
            return;
        }

        SetPeacefulSnapshot();
    }

    private void ApplyGameplaySnapshotIfAvailable()
    {
        if (isCharacterVoiceSnapshotActive)
        {
            return;
        }

        AudioMixerSnapshot snapshot = currentGameplaySnapshotState == GameplaySnapshotState.Wanted
            ? wantedSnapshot
            : peacefulSnapshot;

        if (snapshot != null)
        {
            snapshot.TransitionTo(snapshotTransitionTime);
        }
    }

    private void ApplyMixerVolume(string parameterName, float linearValue)
    {
        if (audioMixer == null || string.IsNullOrWhiteSpace(parameterName))
        {
            return;
        }

        float dbValue = Mathf.Clamp(Mathf.Log10(linearValue) * 20f, MinDb, MaxDb);
        audioMixer.SetFloat(parameterName, dbValue);
    }
}
