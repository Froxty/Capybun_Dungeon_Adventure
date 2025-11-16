using System.Collections;
using UnityEngine;

public class InteractKeyDoorOpener : MonoBehaviour
{
    [Header("Door To Open")]
    [Tooltip("Door Transform that will rotate open.")]
    public Transform door;

    [Tooltip("World rotation the door should end at when fully open.")]
    public Vector3 openRotation = new Vector3(0, 90, 0);

    [Tooltip("How fast the door rotates from closed to open.")]
    public float openSpeed = 2f;

    [Header("Key Requirement")]
    [Tooltip("Item ID required to open this door (must be in InventorySystem).")]
    public string requiredItemId = "Capy_Key";

    [Tooltip("How many of this item are required.")]
    public int requiredAmount = 1;

    [Tooltip("If true, the key will be removed from the inventory when the door opens.")]
    public bool consumeKeyOnUse = true;

    [Header("Timing")]
    [Tooltip("Delay after the player hits the door before anything happens (for anim, etc.).")]
    public float delayBeforeCameraPan = 0.25f;

    [Tooltip("Delay after the camera pan STARTS before the door begins opening.")]
    public float delayBeforeDoorOpens = 0.5f;

    [Header("Camera Focus")]
    [Tooltip("PartySwitchManager that controls Capy/Bun cameras and the interact camera.")]
    public PartySwitchManager partySwitchManager;

    [Tooltip("If true, the camera will pan to the door using the interact camera, then return.")]
    public bool focusCameraOnDoor = true;

    [Header("Sound")]
    [Tooltip("If left empty, will try to find 'GameMaster_AudioSource' in the scene.")]
    [SerializeField] AudioSource audioSource;

    [Tooltip("Global audio source name to search for if audioSource is null.")]
    [SerializeField] string globalAudioSourceName = "GameMaster_AudioSource";

    [Tooltip("Sound that plays once when the door starts to open.")]
    public AudioClip openSFX;

    [Tooltip("Optional sound when the player tries to open without the key.")]
    public AudioClip lockedSFX;

    [Header("Trigger")]
    [Tooltip("Trigger collider used for auto-open. Assign explicitly to avoid disabling the wrong one.")]
    [SerializeField] Collider triggerCollider;

    bool opened = false;

    // Prevent locked sound from spamming
    bool lockedSFXPlayed = false;

    void Start()
    {
        if (!door)
            Debug.LogError($"[KeyDoorOpener] No door assigned on {name}!");

        // If triggerCollider not assigned, fall back to this GameObject's collider
        if (triggerCollider == null)
            triggerCollider = GetComponent<Collider>();

        // Hook up global audio if needed
        if (audioSource == null && !string.IsNullOrEmpty(globalAudioSourceName))
        {
            GameObject go = GameObject.Find(globalAudioSourceName);
            if (go != null)
            {
                audioSource = go.GetComponent<AudioSource>();
                if (audioSource == null)
                    Debug.LogWarning($"[KeyDoorOpener] '{globalAudioSourceName}' found but has no AudioSource.");
            }
            else
            {
                Debug.LogWarning($"[KeyDoorOpener] Could not find GameObject '{globalAudioSourceName}' for global audio.");
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (opened) return;  // door already opened, ignore further triggers

        Debug.Log("[KeyDoorOpener] Player entered trigger – attempting auto open.");
        TryInteract(); // auto-open style: touching the gate uses the key
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        Debug.Log("[KeyDoorOpener] Player left trigger.");
    }

    /// <summary>
    /// Called either by trigger or by PlayerController raycast Interact.
    /// </summary>
    public void TryInteract()
    {
        if (opened)
        {
            Debug.Log("[KeyDoorOpener] TryInteract called but door already opened. Ignoring.");
            return;
        }

        if (!door)
        {
            Debug.LogError("[KeyDoorOpener] TryInteract called but door is NULL.");
            return;
        }

        var inventory = InventorySystem.Instance;
        if (inventory == null)
        {
            Debug.LogWarning("[KeyDoorOpener] No InventorySystem found in scene.");
            return;
        }

        // ---- NO KEY BRANCH ----
        if (!inventory.HasItem(requiredItemId))
        {
            Debug.Log("[KeyDoorOpener] Player does NOT have required key: " + requiredItemId);

            // Only ever play locked SFX once for this door until it opens
            if (!lockedSFXPlayed && audioSource != null && lockedSFX != null)
            {
                audioSource.PlayOneShot(lockedSFX);
                lockedSFXPlayed = true;
            }

            return;
        }

        // ---- SUCCESS BRANCH ----
        Debug.Log("[KeyDoorOpener] Player HAS key, proceeding to open door.");

        opened = true;

        // Consume key if needed
        if (consumeKeyOnUse)
        {
            bool consumed = inventory.ConsumeItem(requiredItemId, requiredAmount);
            Debug.Log("[KeyDoorOpener] ConsumeItem returned: " + consumed);

            if (!consumed)
            {
                Debug.LogWarning("[KeyDoorOpener] Had key but failed to consume it. Check inventory setup.");
                opened = false; // optional: allow retry if consumption fails
                return;
            }
        }

        // Play open SFX once on successful unlock
        if (audioSource != null && openSFX != null)
        {
            audioSource.PlayOneShot(openSFX);
        }

        // Immediately disable the trigger so no more auto-opens fire on THIS collider
        if (triggerCollider != null)
        {
            triggerCollider.enabled = false;
            Debug.Log("[KeyDoorOpener] Trigger collider DISABLED after successful open.");
        }

        StartCoroutine(DoorSequence());
    }

    IEnumerator DoorSequence()
    {
        if (delayBeforeCameraPan > 0f)
            yield return new WaitForSeconds(delayBeforeCameraPan);

        if (focusCameraOnDoor && partySwitchManager != null)
        {
            partySwitchManager.FocusOnTarget(door);
        }

        if (delayBeforeDoorOpens > 0f)
            yield return new WaitForSeconds(delayBeforeDoorOpens);

        Quaternion startRot = door.rotation;
        Quaternion targetRot = Quaternion.Euler(openRotation);
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime * openSpeed;
            door.rotation = Quaternion.Slerp(startRot, targetRot, t);
            yield return null;
        }

        Debug.Log("[KeyDoorOpener] Door fully opened.");

        // Script never needed again
        enabled = false;
    }
}
