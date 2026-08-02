using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ProximityVoiceTrigger : MonoBehaviour
{
    [SerializeField] private string voiceCueId;

    private bool hasPlayed;

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasPlayed)
        {
            return;
        }

        PlayerVoiceAudioController playerVoice =
            other.GetComponentInParent<PlayerVoiceAudioController>();

        if (playerVoice == null)
        {
            return;
        }

        if (playerVoice.TryPlayCue(voiceCueId))
        {
            hasPlayed = true;
        }
    }
}