using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class ItemPickup : MonoBehaviour
{
    public enum PickupMode
    {
        Auto,
        RequireInput
    }

    [Header("Item Settings")]
    [SerializeField] string itemId = "HeartPiece_Bottom";
    [SerializeField] Sprite itemIcon;
    [SerializeField] int amount = 1;

    [Header("Prompt UI")]
    [SerializeField] TMP_Text promptLabel;

    [Header("Pickup Mode")]
    [SerializeField] PickupMode pickupMode = PickupMode.Auto;

    [Header("Sound")]
    [Tooltip("If left empty, will try to find 'GameMaster_AudioSource' in the scene.")]
    [SerializeField] AudioSource audioSource;

    [Tooltip("SFX played when the item is collected.")]
    [SerializeField] AudioClip pickupSFX;

    // Name of the global AudioSource object
    [SerializeField] string globalAudioSourceName = "GameMaster_AudioSource";

    bool playerInRange = false;

    void Start()
    {
        // Hide prompt on start
        if (promptLabel != null)
            promptLabel.gameObject.SetActive(false);

        // If no AudioSource assigned, try to grab the global one
        if (audioSource == null && !string.IsNullOrEmpty(globalAudioSourceName))
        {
            GameObject go = GameObject.Find(globalAudioSourceName);
            if (go != null)
            {
                audioSource = go.GetComponent<AudioSource>();
                if (audioSource == null)
                    Debug.LogWarning($"ItemPickup: '{globalAudioSourceName}' found but has no AudioSource component.");
            }
            else
            {
                Debug.LogWarning($"ItemPickup: Could not find {globalAudioSourceName}  for global audio.");
            }
        }
    }

    void Update()
    {
        if (!playerInRange || pickupMode != PickupMode.RequireInput)
            return;

        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            TryCollect();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (pickupMode == PickupMode.Auto)
        {
            TryCollect();
        }
        else
        {
            playerInRange = true;
            if (promptLabel != null)
                promptLabel.gameObject.SetActive(true);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerInRange = false;

        if (promptLabel != null)
            promptLabel.gameObject.SetActive(false);
    }

    void TryCollect()
    {
        var inventory = InventorySystem.Instance;
        if (inventory == null)
            return;

        bool added = inventory.AddItemToFirstFreeSlot(itemId, itemIcon, amount);
        if (!added)
            return;

        // Play pickup sound from the global / assigned AudioSource
        if (audioSource != null && pickupSFX != null)
        {
            audioSource.PlayOneShot(pickupSFX);
        }

        // Hide label if needed
        if (promptLabel != null)
            promptLabel.gameObject.SetActive(false);

        // Remove object from world
        Destroy(gameObject);
    }
}
