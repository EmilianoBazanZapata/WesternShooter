using UnityEngine.Audio;
using UnityEngine;

namespace WesternShooter.Player.Presentation
{
    public class PlayerFootstepAudio : MonoBehaviour
    {
        private const string FootstepAudioSourceChildName = "FootstepAudioSource";

        private AudioClip[] unifiedClips;
        private AudioClip[] walkClips;
        private AudioClip[] runClips;
        private float walkInterval;
        private float runInterval;
        private Vector2 pitchRange;
        private float volume;
        private AudioMixerGroup outputGroup;

        private AudioSource footstepSource;
        private float nextStepTime;
        private int lastFootstepClipIndex = -1;

        public void Configure(
            AudioClip[] configuredUnifiedClips,
            AudioClip[] configuredWalkClips,
            AudioClip[] configuredRunClips,
            float configuredWalkInterval,
            float configuredRunInterval,
            Vector2 configuredPitchRange,
            float configuredVolume,
            AudioMixerGroup configuredOutputGroup)
        {
            unifiedClips = configuredUnifiedClips;
            walkClips = configuredWalkClips;
            runClips = configuredRunClips;
            walkInterval = configuredWalkInterval;
            runInterval = configuredRunInterval;
            pitchRange = configuredPitchRange;
            volume = configuredVolume;
            outputGroup = configuredOutputGroup;

            footstepSource = CreateOrGetAudioSource(FootstepAudioSourceChildName);
        }

        public void Tick(bool isGrounded, bool isMoving, bool isRunning, bool isAiming, bool isDead, float currentTime)
        {
            if (isDead || isAiming || !isGrounded || !isMoving)
            {
                nextStepTime = 0f;
                return;
            }

            if (footstepSource == null)
            {
                footstepSource = CreateOrGetAudioSource(FootstepAudioSourceChildName);
                if (footstepSource == null)
                    return;
            }

            if (currentTime < nextStepTime)
                return;

            var stepClip = GetRandomFootstepClip(isRunning);
            if (stepClip == null)
                return;

            footstepSource.pitch = Random.Range(pitchRange.x, pitchRange.y);
            footstepSource.PlayOneShot(stepClip, volume);

            var interval = isRunning ? runInterval : walkInterval;
            nextStepTime = currentTime + Mathf.Max(0.05f, interval);
        }

        private AudioClip GetRandomFootstepClip(bool isRunning)
        {
            var source = GetFootstepSourcePool(isRunning);

            if (source == null || source.Length == 0)
                return null;

            int index;
            if (source.Length == 1)
                index = 0;
            else
            {
                index = Random.Range(0, source.Length);
                if (index == lastFootstepClipIndex)
                    index = (index + 1) % source.Length;
            }

            lastFootstepClipIndex = index;
            return source[index];
        }

        private AudioClip[] GetFootstepSourcePool(bool isRunning)
        {
            if (unifiedClips != null && unifiedClips.Length > 0)
                return unifiedClips;

            if (isRunning && runClips != null && runClips.Length > 0)
                return runClips;

            return walkClips;
        }

        private AudioSource CreateOrGetAudioSource(string childName)
        {
            var child = transform.Find(childName);
            if (child == null)
            {
                var childObject = new GameObject(childName);
                childObject.transform.SetParent(transform);
                childObject.transform.localPosition = Vector3.zero;
                childObject.transform.localRotation = Quaternion.identity;
                childObject.transform.localScale = Vector3.one;
                child = childObject.transform;
            }

            var source = child.GetComponent<AudioSource>();
            if (source == null)
                source = child.gameObject.AddComponent<AudioSource>();

            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 1f;
            source.dopplerLevel = 0f;
            source.rolloffMode = AudioRolloffMode.Logarithmic;
            source.minDistance = 1.5f;
            source.maxDistance = 18f;
            source.outputAudioMixerGroup = outputGroup;
            return source;
        }
    }

}


