using UnityEngine;
using UnityEngine.Audio;

namespace WesternShooter.Audio
{
    internal sealed class GameplaySnapshotController
    {
        private enum GameplaySnapshotState
        {
            Peaceful,
            Wanted
        }

        private readonly AudioMixerSnapshot peacefulSnapshot;
        private readonly AudioMixerSnapshot wantedSnapshot;
        private readonly AudioMixerSnapshot characterVoiceSnapshot;
        private readonly float snapshotTransitionTime;
        private readonly float characterVoiceTransitionTime;

        private GameplaySnapshotState currentGameplaySnapshotState = GameplaySnapshotState.Peaceful;
        private int activeVoiceSnapshotRequests;
        private bool isCharacterVoiceSnapshotActive;

        public GameplaySnapshotController(
            AudioMixerSnapshot peacefulSnapshot,
            AudioMixerSnapshot wantedSnapshot,
            AudioMixerSnapshot characterVoiceSnapshot,
            float snapshotTransitionTime,
            float characterVoiceTransitionTime)
        {
            this.peacefulSnapshot = peacefulSnapshot;
            this.wantedSnapshot = wantedSnapshot;
            this.characterVoiceSnapshot = characterVoiceSnapshot;
            this.snapshotTransitionTime = snapshotTransitionTime;
            this.characterVoiceTransitionTime = characterVoiceTransitionTime;
        }

        public void SetPeacefulSnapshot()
        {
            currentGameplaySnapshotState = GameplaySnapshotState.Peaceful;
            ApplyGameplaySnapshotIfAvailable();
        }

        public void SetWantedSnapshot()
        {
            currentGameplaySnapshotState = GameplaySnapshotState.Wanted;
            ApplyGameplaySnapshotIfAvailable();
        }

        public void OnCharacterVoicePlaybackStarted()
        {
            if (characterVoiceSnapshot == null)
                return;

            activeVoiceSnapshotRequests++;

            if (isCharacterVoiceSnapshotActive)
                return;

            isCharacterVoiceSnapshotActive = true;
            characterVoiceSnapshot.TransitionTo(characterVoiceTransitionTime);
        }

        public void OnCharacterVoicePlaybackEnded()
        {
            if (activeVoiceSnapshotRequests > 0)
                activeVoiceSnapshotRequests--;

            if (activeVoiceSnapshotRequests > 0)
                return;

            if (!isCharacterVoiceSnapshotActive)
                return;

            isCharacterVoiceSnapshotActive = false;
            ApplyGameplaySnapshotIfAvailable();
        }

        public void ForceReleaseAllVoiceRequests()
        {
            activeVoiceSnapshotRequests = 0;

            if (!isCharacterVoiceSnapshotActive)
                return;

            isCharacterVoiceSnapshotActive = false;
            ApplyGameplaySnapshotIfAvailable();
        }

        private void ApplyGameplaySnapshotIfAvailable()
        {
            if (isCharacterVoiceSnapshotActive)
                return;

            var snapshot = currentGameplaySnapshotState == GameplaySnapshotState.Wanted
                ? wantedSnapshot
                : peacefulSnapshot;

            if (snapshot != null)
                snapshot.TransitionTo(snapshotTransitionTime);
        }
    }
}

