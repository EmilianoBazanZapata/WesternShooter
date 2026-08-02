using System;
using UnityEngine;

public class PlayerVoiceAudioController : MonoBehaviour
{
    [Serializable]
    private class VoiceCue
    {
        public string id;
        public AudioClip defaultClip;
        public AudioClip[] randomClips;
        [Min(0f)] public float cooldown = 0f;

        [NonSerialized] public float nextAllowedPlayTime;
        [NonSerialized] public int lastRandomIndex = -1;
    }

    [Header("Voice Source")]
    [SerializeField] private AudioSource voiceSource;

    [Header("Voice Cues")]
    [SerializeField] private VoiceCue[] cues;

    [Header("Playback")]
    [SerializeField] private bool stopCurrentClipBeforePlay = true;

    private int pendingVoiceSnapshotRequests;

    private void Awake()
    {
        if (voiceSource == null)
        {
            voiceSource = GetComponent<AudioSource>();
        }

        if (voiceSource == null)
        {
            Debug.LogWarning("[PlayerVoiceAudioController] No AudioSource assigned on player.", this);
        }
    }

    public bool TryPlayCue(string cueId)
    {
        if (string.IsNullOrWhiteSpace(cueId) || voiceSource == null)
        {
            return false;
        }

        VoiceCue cue = FindCue(cueId);
        if (cue == null)
        {
            return false;
        }

        if (Time.time < cue.nextAllowedPlayTime)
        {
            return false;
        }

        AudioClip clipToPlay = GetClip(cue);
        if (clipToPlay == null)
        {
            return false;
        }

        if (stopCurrentClipBeforePlay && voiceSource.isPlaying)
        {
            voiceSource.Stop();
        }

        GameAudioManager manager = GameAudioManager.Instance
            ?? FindFirstObjectByType<GameAudioManager>();

        manager?.OnCharacterVoicePlaybackStarted();
        pendingVoiceSnapshotRequests++;

        voiceSource.clip = clipToPlay;
        voiceSource.Play();
        StartCoroutine(WaitForVoicePlaybackEnd(manager));

        cue.nextAllowedPlayTime = Time.time + cue.cooldown;
        return true;
    }

    private System.Collections.IEnumerator WaitForVoicePlaybackEnd(GameAudioManager manager)
    {
        while (voiceSource != null && voiceSource.isPlaying)
        {
            yield return null;
        }

        NotifyVoicePlaybackEnded(manager);
    }

    private void OnDisable()
    {
        if (pendingVoiceSnapshotRequests <= 0)
        {
            return;
        }

        GameAudioManager manager = GameAudioManager.Instance
            ?? FindFirstObjectByType<GameAudioManager>();

        while (pendingVoiceSnapshotRequests > 0)
        {
            NotifyVoicePlaybackEnded(manager);
        }
    }

    private void NotifyVoicePlaybackEnded(GameAudioManager manager)
    {
        if (pendingVoiceSnapshotRequests <= 0)
        {
            return;
        }

        pendingVoiceSnapshotRequests--;
        manager?.OnCharacterVoicePlaybackEnded();
    }

    private VoiceCue FindCue(string cueId)
    {
        if (cues == null)
        {
            return null;
        }

        for (int i = 0; i < cues.Length; i++)
        {
            VoiceCue cue = cues[i];
            if (cue == null || string.IsNullOrWhiteSpace(cue.id))
            {
                continue;
            }

            if (string.Equals(cue.id, cueId, StringComparison.OrdinalIgnoreCase))
            {
                return cue;
            }
        }

        return null;
    }

    private AudioClip GetClip(VoiceCue cue)
    {
        if (cue.randomClips != null && cue.randomClips.Length > 0)
        {
            if (cue.randomClips.Length == 1)
            {
                cue.lastRandomIndex = 0;
                return cue.randomClips[0];
            }

            int index = UnityEngine.Random.Range(0, cue.randomClips.Length);
            if (index == cue.lastRandomIndex)
            {
                index = (index + 1) % cue.randomClips.Length;
            }

            cue.lastRandomIndex = index;
            return cue.randomClips[index];
        }

        return cue.defaultClip;
    }
}
