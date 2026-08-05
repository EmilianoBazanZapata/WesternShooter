using UnityEngine;

public class ZombieAudioController : MonoBehaviour
{
    private const float FootstepIntervalSeconds = 0.45f;
    private const float MinVelocitySqrForFootstep = 0.05f;

    [Header("Sources")]
    [SerializeField] private AudioSource footstepSource;
    [SerializeField] private AudioSource attackSource;
    [SerializeField] private AudioSource deathSource;

    [Header("Clips")]
    [SerializeField] private AudioClip[] footstepClips;

    private bool footstepsEnabled;
    private float nextFootstepTime;
    private int lastFootstepIndex = -1;

    private void Awake()
    {
        if (attackSource == null)
        {
            Debug.LogWarning("ZombieAudioController: Attack Source is not assigned.", this);
        }
    }

    public void SetFootstepsEnabled(bool enabled)
    {
        footstepsEnabled = enabled;

        if (!enabled)
        {
            nextFootstepTime = 0f;
        }
    }

    public void UpdateFootsteps(bool canPlay, float movementVelocitySqr)
    {
        if (!footstepsEnabled || !canPlay)
        {
            return;
        }

        if (movementVelocitySqr < MinVelocitySqrForFootstep)
        {
            return;
        }

        if (Time.time < nextFootstepTime)
        {
            return;
        }

        AudioClip clip = GetRandomFootstepClip();
        if (footstepSource == null || clip == null)
        {
            return;
        }

        footstepSource.PlayOneShot(clip);

        nextFootstepTime = Time.time + FootstepIntervalSeconds;
    }

    public void PlayAttack()
    {
        if (attackSource == null || attackSource.clip == null)
        {
            return;
        }

        if (attackSource.isPlaying)
        {
            attackSource.Stop();
        }

        attackSource.Play();
    }

    public void PlayDeath()
    {
        if (deathSource == null || deathSource.clip == null)
        {
            return;
        }

        if (deathSource.isPlaying)
        {
            deathSource.Stop();
        }

        deathSource.Play();
    }

    private AudioClip GetRandomFootstepClip()
    {
        if (footstepClips == null || footstepClips.Length == 0)
        {
            return null;
        }

        int index;
        if (footstepClips.Length == 1)
        {
            index = 0;
        }
        else
        {
            index = Random.Range(0, footstepClips.Length);
            if (index == lastFootstepIndex)
            {
                index = (index + 1) % footstepClips.Length;
            }
        }

        lastFootstepIndex = index;
        return footstepClips[index];
    }
}
