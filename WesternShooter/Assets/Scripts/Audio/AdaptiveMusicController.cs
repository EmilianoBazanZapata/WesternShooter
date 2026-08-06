using System.Collections;
using UnityEngine;

namespace WesternShooter.Audio
{
    public class AdaptiveMusicController
    {
        private readonly MonoBehaviour coroutineHost;
        private readonly AudioSource peacefulMusicSource;
        private readonly AudioSource wantedMusicSource;
        private readonly AudioSource victoryMusicSource;
        private readonly AudioSource defeatMusicSource;
        private readonly float musicCrossFadeDuration;

        private Coroutine crossFadeCoroutine;

        public AdaptiveMusicController(
            MonoBehaviour coroutineHost,
            AudioSource peacefulMusicSource,
            AudioSource wantedMusicSource,
            AudioSource victoryMusicSource,
            AudioSource defeatMusicSource,
            float musicCrossFadeDuration)
        {
            this.coroutineHost = coroutineHost;
            this.peacefulMusicSource = peacefulMusicSource;
            this.wantedMusicSource = wantedMusicSource;
            this.victoryMusicSource = victoryMusicSource;
            this.defeatMusicSource = defeatMusicSource;
            this.musicCrossFadeDuration = musicCrossFadeDuration;
        }

        public void SetPeacefulMusic() => CrossFadeMusic(peacefulMusicSource, wantedMusicSource);

        public void SetWantedMusic() => CrossFadeMusic(wantedMusicSource, peacefulMusicSource);

        public void PlayVictoryMusic() => PlayOutcomeMusic(victoryMusicSource);

        public void PlayDefeatMusic() => PlayOutcomeMusic(defeatMusicSource);

        public void StopActiveCrossFade()
        {
            if (crossFadeCoroutine == null || coroutineHost == null)
                return;

            coroutineHost.StopCoroutine(crossFadeCoroutine);
            crossFadeCoroutine = null;
        }

        private void CrossFadeMusic(AudioSource fadeIn, AudioSource fadeOut)
        {
            if (fadeIn == null && fadeOut == null)
                return;

            if (coroutineHost == null)
                return;

            if (crossFadeCoroutine != null)
                coroutineHost.StopCoroutine(crossFadeCoroutine);

            crossFadeCoroutine = coroutineHost.StartCoroutine(CrossFadeRoutine(fadeIn, fadeOut));
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
                    fadeIn.volume = Mathf.Lerp(startVolumeIn, 1f, t);

                if (fadeOut != null)
                    fadeOut.volume = Mathf.Lerp(startVolumeOut, 0f, t);

                yield return null;
            }

            if (fadeOut != null)
            {
                fadeOut.Stop();
                fadeOut.volume = 1f;
            }

            crossFadeCoroutine = null;
        }

        private void PlayOutcomeMusic(AudioSource outcomeSource)
        {
            if (outcomeSource == null)
                return;

            StopActiveCrossFade();

            StopMusicSource(peacefulMusicSource);
            StopMusicSource(wantedMusicSource);
            StopMusicSource(victoryMusicSource, outcomeSource);
            StopMusicSource(defeatMusicSource, outcomeSource);

            outcomeSource.volume = 1f;
            outcomeSource.pitch = 1f;
            outcomeSource.Stop();
            outcomeSource.Play();
        }

        private static void StopMusicSource(AudioSource source, AudioSource except = null)
        {
            if (source == null || source == except)
                return;

            source.Stop();
            source.volume = 1f;
        }
    }
}

