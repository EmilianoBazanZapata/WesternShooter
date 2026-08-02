using UnityEngine;

public class OptionsMenuController : MonoBehaviour
{
    public void OnMasterVolumeChanged(float value)
    {
        GameAudioManager.Instance?.SetVolume(AudioBusType.Master, value);
    }

    public void OnMusicVolumeChanged(float value)
    {
        GameAudioManager.Instance?.SetVolume(AudioBusType.Music, value);
    }

    public void OnSfxVolumeChanged(float value)
    {
        GameAudioManager.Instance?.SetVolume(AudioBusType.Sfx, value);
    }

    public void OnVoiceVolumeChanged(float value)
    {
        GameAudioManager.Instance?.SetVolume(AudioBusType.Voice, value);
    }

    public void OnAmbienceVolumeChanged(float value)
    {
        GameAudioManager.Instance?.SetVolume(AudioBusType.Ambience, value);
    }

    public void OnSetPeacefulSnapshotPressed()
    {
        GameAudioManager.Instance?.SetPeacefulSnapshot();
    }

    public void OnSetWantedSnapshotPressed()
    {
        GameAudioManager.Instance?.SetWantedSnapshot();
    }
}
