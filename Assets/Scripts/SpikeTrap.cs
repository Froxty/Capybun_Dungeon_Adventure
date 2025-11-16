using System.Collections;
using UnityEngine;

public class SpikeTrap : MonoBehaviour
{
    [Header("Spike Object")]
    [Tooltip("Assign the spikes child object that moves up/down.")]
    public Transform spike;

    [Header("Positions")]
    public Vector3 upLocalPos = new Vector3(0, 1f, 0);
    public Vector3 downLocalPos = Vector3.zero;

    [Header("Timing")]
    public float riseSpeed = 10f;
    public float stayUpTime = 1f;
    public float dropSpeed = 10f;

    [Header("Reset Settings")]
    public bool resettable = true;
    public float resetCooldown = 1f;

    [Header("Sound")]
    [Tooltip("If left empty, will try to find 'GameMaster_AudioSource' in the scene.")]
    [SerializeField] AudioSource audioSource;

    [Tooltip("Global audio source name to search for if audioSource is null.")]
    [SerializeField] string globalAudioSourceName = "GameMaster_AudioSource";

    [Tooltip("Sound that plays once when the spikes rise.")]
    public AudioClip spikeUpSFX;

    [Tooltip("Sound that plays once when the spikes drop.")]
    public AudioClip spikeDownSFX;

    bool isActive = false;

    void Start()
    {
        if (spike == null)
        {
            Debug.LogError("[SpikeTrap] No spike Transform assigned!");
            return;
        }

        // Make sure spikes start down
        spike.localPosition = downLocalPos;

        if (audioSource == null && !string.IsNullOrEmpty(globalAudioSourceName))
        {
            GameObject go = GameObject.Find(globalAudioSourceName);
            if (go != null)
            {
                audioSource = go.GetComponent<AudioSource>();
                if (audioSource == null)
                    Debug.LogWarning($"[SpikeTrap] '{globalAudioSourceName}' found but has no AudioSource.");
            }
            else
            {
                Debug.LogWarning($"[SpikeTrap] Could not find GameObject '{globalAudioSourceName}' for global audio.");
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (!isActive)
            StartCoroutine(SpikeRoutine());
    }

    IEnumerator SpikeRoutine()
    {
        isActive = true;

        // === RISE ===
        if (audioSource && spikeUpSFX)
            audioSource.PlayOneShot(spikeUpSFX);

        yield return StartCoroutine(LerpSpike(downLocalPos, upLocalPos, riseSpeed));

        // === HOLD ===
        yield return new WaitForSeconds(stayUpTime);

        // === DROP ===
        if (audioSource && spikeDownSFX)
            audioSource.PlayOneShot(spikeDownSFX);

        yield return StartCoroutine(LerpSpike(upLocalPos, downLocalPos, dropSpeed));

        // === RESET ===
        if (resettable)
        {
            yield return new WaitForSeconds(resetCooldown);
            isActive = false;
        }
    }

    IEnumerator LerpSpike(Vector3 start, Vector3 end, float speed)
    {
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * speed;
            spike.localPosition = Vector3.Lerp(start, end, t);
            yield return null;
        }
    }
}
