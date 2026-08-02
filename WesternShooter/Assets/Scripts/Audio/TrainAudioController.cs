using UnityEngine;

public class TrainAudioController : MonoBehaviour
{
    [Header("Timing")]
    [SerializeField] private float steamIntervalSeconds = 15f;

    private AudioSource engineSource;
    private AudioSource steamSource;
    private float nextSteamPlayTime;

    private void Awake()
    {
        AudioSource[] sources = GetComponents<AudioSource>();

        if (sources.Length == 0)
        {
            Debug.LogWarning("TrainAudioController: add at least one AudioSource on the train object.", this);
            enabled = false;
            return;
        }

        engineSource = sources[0];
        steamSource = sources.Length > 1 ? sources[1] : sources[0];
    }

    private void Start()
    {
        ConfigureSteamSource();
        StartEngineLoop();
        nextSteamPlayTime = Time.time + Mathf.Max(0.1f, steamIntervalSeconds);
    }

    private void Update()
    {
        if (steamSource == null || steamSource.clip == null)
        {
            return;
        }

        if (Time.time < nextSteamPlayTime)
        {
            return;
        }

        PlaySteamBurst();
        nextSteamPlayTime = Time.time + Mathf.Max(0.1f, steamIntervalSeconds);
    }

    private void StartEngineLoop()
    {
        if (engineSource == null || engineSource.clip == null)
        {
            return;
        }

        engineSource.loop = true;

        if (!engineSource.isPlaying)
        {
            engineSource.Play();
        }
    }

    private void PlaySteamBurst()
    {
        if (steamSource == null || steamSource.clip == null)
        {
            return;
        }

        if (steamSource == engineSource)
        {
            return;
        }

        steamSource.loop = false;
        steamSource.PlayOneShot(steamSource.clip, 1f);
    }

    private void ConfigureSteamSource()
    {
        if (steamSource == null || steamSource == engineSource)
        {
            return;
        }

        // Steam playback must be timer-driven only.
        steamSource.playOnAwake = false;
        steamSource.loop = false;
        steamSource.Stop();
    }
}