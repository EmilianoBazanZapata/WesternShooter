using UnityEngine;
using UnityEngine.Audio;

namespace WesternShooter.Audio
{
    internal sealed class AudioMixerController
    {
        private const float MinDb = -80f;
        private const float MaxDb = 0f;

        private readonly AudioMixer audioMixer;
        private readonly string masterParameter;
        private readonly string musicParameter;
        private readonly string sfxParameter;
        private readonly string voiceParameter;
        private readonly string ambienceParameter;

        public AudioMixerController(
            AudioMixer audioMixer,
            string masterParameter,
            string musicParameter,
            string sfxParameter,
            string voiceParameter,
            string ambienceParameter)
        {
            this.audioMixer = audioMixer;
            this.masterParameter = masterParameter;
            this.musicParameter = musicParameter;
            this.sfxParameter = sfxParameter;
            this.voiceParameter = voiceParameter;
            this.ambienceParameter = ambienceParameter;
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

        private void ApplyMixerVolume(string parameterName, float linearValue)
        {
            if (audioMixer == null || string.IsNullOrWhiteSpace(parameterName))
                return;

            float dbValue = Mathf.Clamp(Mathf.Log10(linearValue) * 20f, MinDb, MaxDb);
            audioMixer.SetFloat(parameterName, dbValue);
        }
    }
}

