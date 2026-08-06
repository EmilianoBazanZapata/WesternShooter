using System;
using UnityEngine;

namespace WesternShooter.Audio
{
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

        private Coroutine voiceLifecycleCoroutine;
        private GameAudioManager activeSnapshotManager;
        private bool hasActiveSnapshotRequest;

        private void Awake()
        {
            if (voiceSource == null)
                voiceSource = GetComponent<AudioSource>();

            if (voiceSource == null)
                Debug.LogWarning("[PlayerVoiceAudioController] No AudioSource assigned on player.", this);
        }

        public bool TryPlayCue(string cueId)
        {
            if (string.IsNullOrWhiteSpace(cueId) || voiceSource == null)
                return false;

            var cue = FindCue(cueId);
            if (cue == null)
                return false;

            if (Time.time < cue.nextAllowedPlayTime)
                return false;

            var clipToPlay = GetClip(cue);
            if (clipToPlay == null)
                return false;

            var manager = GameAudioManager.Instance != null ? GameAudioManager.Instance : FindFirstObjectByType<GameAudioManager>();

            if (stopCurrentClipBeforePlay && voiceSource.isPlaying)
                voiceSource.Stop();

            AcquireVoiceSnapshotRequest(manager);

            voiceSource.clip = clipToPlay;
            voiceSource.Play();
            StartVoiceLifecycleCoroutine();

            cue.nextAllowedPlayTime = Time.time + cue.cooldown;
            return true;
        }

        private System.Collections.IEnumerator WaitForVoicePlaybackEnd()
        {
            while (voiceSource != null && voiceSource.isPlaying)
            {
                yield return null;
            }

            voiceLifecycleCoroutine = null;
            ReleaseVoiceSnapshotRequest();
        }

        private void OnDisable()
        {
            StopVoiceLifecycleAndReleaseSnapshot();
        }

        private void OnDestroy()
        {
            StopVoiceLifecycleAndReleaseSnapshot();
        }

        private void StartVoiceLifecycleCoroutine()
        {
            if (voiceLifecycleCoroutine != null)
            {
                StopCoroutine(voiceLifecycleCoroutine);
                voiceLifecycleCoroutine = null;
            }

            voiceLifecycleCoroutine = StartCoroutine(WaitForVoicePlaybackEnd());
        }

        private void StopVoiceLifecycleAndReleaseSnapshot()
        {
            if (voiceLifecycleCoroutine != null)
            {
                StopCoroutine(voiceLifecycleCoroutine);
                voiceLifecycleCoroutine = null;
            }

            ReleaseVoiceSnapshotRequest();
        }

        private void AcquireVoiceSnapshotRequest(GameAudioManager manager)
        {
            if (hasActiveSnapshotRequest)
                return;

            hasActiveSnapshotRequest = true;
            activeSnapshotManager = manager;
            manager?.OnCharacterVoicePlaybackStarted();
        }

        private void ReleaseVoiceSnapshotRequest()
        {
            if (!hasActiveSnapshotRequest)
                return;

            hasActiveSnapshotRequest = false;

            if (activeSnapshotManager != null)
                activeSnapshotManager.OnCharacterVoicePlaybackEnded();

            activeSnapshotManager = null;
        }

        private VoiceCue FindCue(string cueId)
        {
            if (cues == null)
                return null;

            for (var i = 0; i < cues.Length; i++)
            {
                var cue = cues[i];
                if (cue == null || string.IsNullOrWhiteSpace(cue.id))
                    continue;

                if (string.Equals(cue.id, cueId, StringComparison.OrdinalIgnoreCase))
                    return cue;
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

                var index = UnityEngine.Random.Range(0, cue.randomClips.Length);
                if (index == cue.lastRandomIndex)
                    index = (index + 1) % cue.randomClips.Length;

                cue.lastRandomIndex = index;
                return cue.randomClips[index];
            }

            return cue.defaultClip;
        }
    }

}

