using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace WesternShooter.Audio
{
    internal sealed class NonMusicPauseController
    {
        private readonly HashSet<int> pausedNonMusicSourceIds = new HashSet<int>();

        public void SetNonMusicMuted(bool muted)
        {
            if (muted)
            {
                PauseNonMusicSources();
                return;
            }

            ResumePausedNonMusicSources();
        }

        public void ClearTrackedState() => pausedNonMusicSourceIds.Clear();

        private void PauseNonMusicSources()
        {
            var allSources = UnityEngine.Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None);
            for (var i = 0; i < allSources.Length; i++)
            {
                var source = allSources[i];
                if (source == null || !source.isPlaying || IsMusicSource(source))
                    continue;

                source.Pause();
                pausedNonMusicSourceIds.Add(source.GetInstanceID());
            }
        }

        private void ResumePausedNonMusicSources()
        {
            if (pausedNonMusicSourceIds.Count == 0)
                return;

            var allSources = UnityEngine.Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None);
            for (var i = 0; i < allSources.Length; i++)
            {
                var source = allSources[i];
                if (source == null)
                    continue;

                var sourceId = source.GetInstanceID();
                if (!pausedNonMusicSourceIds.Contains(sourceId))
                    continue;

                source.UnPause();
            }

            pausedNonMusicSourceIds.Clear();
        }

        private static bool IsMusicSource(AudioSource source)
        {
            var outputGroup = source.outputAudioMixerGroup;
            return outputGroup != null && string.Equals(outputGroup.name, "Music", StringComparison.OrdinalIgnoreCase);
        }
    }
}

