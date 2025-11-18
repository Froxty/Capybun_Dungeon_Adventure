using UnityEngine;

public class HeartCombineOnContact : MonoBehaviour
{
    [Header("Heart piece item IDs (the 3 separate pieces)")]
    [Tooltip("Item IDs that must all be present to combine (e.g., Heart_Bottom, Heart_Left, Heart_Right).")]
    public string[] heartPieceItemIds;

    [Header("Combined Heart Item")]
    [Tooltip("Item ID for the combined heart.")]
    public string combinedHeartItemId = "Heart_Combined";

    [Tooltip("Sprite for the combined heart icon.")]
    public Sprite combinedHeartIcon;

    [Header("Sound")]
    [Tooltip("If left empty, will try to find 'GameMaster_AudioSource' in the scene.")]
    [SerializeField] AudioSource audioSource;

    [Tooltip("Global audio source name to search for if audioSource is null.")]
    [SerializeField] string globalAudioSourceName = "GameMaster_AudioSource";

    [Tooltip("Sound that plays once when the heart successfully combines.")]
    public AudioClip combineSFX;

    [Header("OneTimeCombine Only")]
    public bool oneTimeOnly = true;

    bool hasCombined = false;

    void Start()
    {
        // Auto-find global audio source if none assigned
        if (audioSource == null && !string.IsNullOrEmpty(globalAudioSourceName))
        {
            GameObject go = GameObject.Find(globalAudioSourceName);
            if (go != null)
            {
                audioSource = go.GetComponent<AudioSource>();
                if (audioSource == null)
                    Debug.LogWarning($"[HeartCombine] '{globalAudioSourceName}' found but has no AudioSource component.");
            }
            else
            {
                Debug.LogWarning($"[HeartCombine] Could not find GameObject '{globalAudioSourceName}' for global audio.");
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (oneTimeOnly && hasCombined) return;

        TryCombineHearts();
    }

    void TryCombineHearts()
    {
        var inventory = InventorySystem.Instance;
        if (inventory == null)
        {
            Debug.LogWarning("[HeartCombine] No InventorySystem found.");
            return;
        }

        // If we already have the combined heart, don't try again
        if (!string.IsNullOrEmpty(combinedHeartItemId) && inventory.HasItem(combinedHeartItemId))
        {
            hasCombined = true;
            Debug.Log("[HeartCombine] Combined heart already in inventory. Skipping recombine.");
            return;
        }

        // Check for all pieces
        if (heartPieceItemIds == null || heartPieceItemIds.Length == 0)
        {
            Debug.LogWarning("[HeartCombine] No heartPieceItemIds set.");
            return;
        }

        foreach (var id in heartPieceItemIds)
        {
            if (string.IsNullOrEmpty(id)) continue;

            if (!inventory.HasItem(id))
            {
                Debug.Log("[HeartCombine] Missing heart piece: " + id);
                return;
            }
        }

        // Remove required heart pieces
        foreach (var id in heartPieceItemIds)
        {
            if (string.IsNullOrEmpty(id)) continue;

            bool consumed = inventory.ConsumeItem(id, 1);
            if (!consumed)
                Debug.LogWarning("[HeartCombine] Failed to consume: " + id);
        }

        // Add combined heart
        if (!string.IsNullOrEmpty(combinedHeartItemId) && combinedHeartIcon != null)
        {
            bool added = inventory.AddItemToFirstFreeSlot(combinedHeartItemId, combinedHeartIcon, 1);
            //Debug.Log("[HeartCombine] Combined heart created. Added: " + added);
        }
        else
        {
            Debug.LogWarning("[HeartCombine] Combined heart item ID or icon not set!");
        }

        // Play SFX once on successful combine
        if (audioSource != null && combineSFX != null)
        {
            audioSource.PlayOneShot(combineSFX);
        }

        hasCombined = true;
    }
}
