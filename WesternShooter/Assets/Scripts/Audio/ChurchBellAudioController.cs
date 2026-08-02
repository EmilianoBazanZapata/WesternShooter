using UnityEngine;

public class ChurchBellAudioController : MonoBehaviour
{
    [SerializeField] private AudioSource bellSource;
    [SerializeField] private float ringIntervalSeconds = 180f;

    private float nextRingTime;

    private void Awake()
    {
        if (bellSource == null)
        {
            bellSource = GetComponent<AudioSource>();
        }

        if (bellSource == null)
        {
            Debug.LogWarning("ChurchBellAudioController: assign an AudioSource with a bell clip.", this);
            enabled = false;
            return;
        }

        bellSource.playOnAwake = false;
        bellSource.loop = false;
    }

    private void Start()
    {
        RingBell();
        nextRingTime = Time.time + Mathf.Max(0.1f, ringIntervalSeconds);
    }

    private void Update()
    {
        if (bellSource == null || bellSource.clip == null)
        {
            return;
        }

        if (Time.time < nextRingTime)
        {
            return;
        }

        RingBell();
        nextRingTime = Time.time + Mathf.Max(0.1f, ringIntervalSeconds);
    }

    private void RingBell()
    {
        bellSource.Play();
    }
}
