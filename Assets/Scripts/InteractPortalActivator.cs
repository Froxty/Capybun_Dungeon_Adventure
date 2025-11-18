using UnityEngine;
using System.Collections.Generic;

public class InteractPortalActivator : MonoBehaviour
{
    [Header("Heart Requirement")]
    [Tooltip("Item ID required to open the portal (combined heart).")]
    public string requiredItemId = "Heart_Combined";

    [Tooltip("How many of this item are required.")]
    public int requiredAmount = 1;

    [Tooltip("If true, the heart item will be removed when the portal opens.")]
    public bool consumeOnUse = true;

    [Header("Player Requirement")]
    [Tooltip("Require BOTH players to be inside the trigger to activate.")]
    public bool requireBothPlayersInTrigger = true;

    [Tooltip("Tag used by both player characters.")]
    public string playerTag = "Player";

    [Header("Portal Visuals")]
    [Tooltip("Mesh renderers whose emission will be changed when players stand on the platform.")]
    public List<MeshRenderer> emissiveRenderers = new List<MeshRenderer>();

    [Tooltip("Emission strength when platform is not ready (not enough players).")]
    public float emissionOffStrength = 0f;

    [Tooltip("Emission strength when both players are standing on the platform.")]
    public float emissionOnStrength = 3f;

    [Tooltip("GameObjects (e.g., 3 heart pieces) that become visible when the portal is ACTIVATED (not just when players stand).")]
    public List<GameObject> portalPieces = new List<GameObject>();

    [Header("Portal Animator")]
    [Tooltip("Animator that controls the portal state.")]
    public Animator portalAnimator;

    [Tooltip("Name of the bool parameter that turns the portal on in the Animator.")]
    public string portalStateBoolName = "PortalState";

    [Tooltip("If true, this interaction can only happen once.")]
    public bool oneTimeOnly = true;

    [Header("Sound")]
    [Tooltip("If left empty, will try to find 'GameMaster_AudioSource' in the scene.")]
    [SerializeField] AudioSource audioSource;

    [Tooltip("Global audio source name to search for if audioSource is null.")]
    [SerializeField] string globalAudioSourceName = "GameMaster_AudioSource";

    [Tooltip("Sound that plays when the portal successfully opens.")]
    public AudioClip openPortalSFX;

    [Tooltip("Optional sound if the interaction fails (missing heart or missing players).")]
    public AudioClip failSFX;

    [Header("Trigger (Optional)")]
    [Tooltip("Trigger collider used for proximity. If null, uses the collider on this GameObject.")]
    [SerializeField] Collider triggerCollider;

    [Header("Debug")]
    public bool debugLogs = false;   // off by default to reduce spam

    bool activated = false;
    int playersInTrigger = 0;

    const string EmissionProperty = "_EmissionStrength";

    void Start()
    {
        // Ensure we have a trigger collider
        if (triggerCollider == null)
            triggerCollider = GetComponent<Collider>();

        if (triggerCollider == null)
        {
            Debug.LogWarning($"[PortalActivator:{name}] No trigger collider assigned or found.");
        }
        else if (!triggerCollider.isTrigger)
        {
            Debug.LogWarning($"[PortalActivator:{name}] Collider is not set as Trigger.");
        }

        // Global audio
        if (audioSource == null && !string.IsNullOrEmpty(globalAudioSourceName))
        {
            GameObject go = GameObject.Find(globalAudioSourceName);
            if (go != null)
                audioSource = go.GetComponent<AudioSource>();
        }

        // Animator auto-find (if not assigned)
        if (portalAnimator == null)
            portalAnimator = GetComponent<Animator>();

        // HARD RESET visual state to OFF
        SetEmission(emissionOffStrength);
        SetPortalPiecesActive(false);

        if (portalAnimator != null && !string.IsNullOrEmpty(portalStateBoolName))
            portalAnimator.SetBool(portalStateBoolName, false);

        if (debugLogs)
            Debug.Log($"[PortalActivator:{name}] Initialized. Portal OFF.");
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        playersInTrigger++;
        if (playersInTrigger < 0) playersInTrigger = 0;

        if (debugLogs)
            Debug.Log($"[PortalActivator:{name}] OnTriggerEnter by {other.name}. playersInTrigger={playersInTrigger}");

        UpdatePlatformEmissive();
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        playersInTrigger--;
        if (playersInTrigger < 0) playersInTrigger = 0;

        if (debugLogs)
            Debug.Log($"[PortalActivator:{name}] OnTriggerExit by {other.name}. playersInTrigger={playersInTrigger}");

        UpdatePlatformEmissive();
    }

    public void TryInteract()
    {
        if (debugLogs)
            Debug.Log($"[PortalActivator:{name}] TryInteract. activated={activated}, playersInTrigger={playersInTrigger}");

        if (oneTimeOnly && activated)
            return;

        // 1) Check players in range (still required to actually open portal)
        if (requireBothPlayersInTrigger && playersInTrigger < 2)
        {
            if (debugLogs)
                Debug.Log($"[PortalActivator:{name}] Not enough players on platform to activate.");
            PlayFailSFX();
            return;
        }

        // 2) Check inventory and heart
        var inventory = InventorySystem.Instance;
        if (inventory == null)
        {
            Debug.LogWarning($"[PortalActivator:{name}] No InventorySystem found.");
            PlayFailSFX();
            return;
        }

        if (!inventory.HasItem(requiredItemId))
        {
            if (debugLogs)
                Debug.Log($"[PortalActivator:{name}] Missing heart item: {requiredItemId}");
            PlayFailSFX();
            return;
        }

        // 3) Consume the heart
        if (consumeOnUse)
        {
            bool consumed = inventory.ConsumeItem(requiredItemId, requiredAmount);
            if (debugLogs)
                Debug.Log($"[PortalActivator:{name}] ConsumeItem({requiredItemId}) => {consumed}");

            if (!consumed)
            {
                Debug.LogWarning($"[PortalActivator:{name}] Had heart but failed to consume.");
                PlayFailSFX();
                return;
            }
        }

        // 4) Fully activate portal: pieces + animator
        SetPortalPiecesActive(true);

        if (portalAnimator != null && !string.IsNullOrEmpty(portalStateBoolName))
            portalAnimator.SetBool(portalStateBoolName, true);

        if (audioSource != null && openPortalSFX != null)
            audioSource.PlayOneShot(openPortalSFX);

        activated = true;

        if (debugLogs)
            //Debug.Log($"[PortalActivator:{name}] Portal ACTIVATED.");

        if (triggerCollider != null && oneTimeOnly)
            triggerCollider.enabled = false;
    }

    // --------------------
    // Helpers
    // --------------------

    /// <summary>
    /// Emissive is ON when both players are standing on the platform,
    /// OFF otherwise. This is purely visual, no heart check.
    /// </summary>
    void UpdatePlatformEmissive()
    {
        bool enoughPlayers =
            requireBothPlayersInTrigger ? (playersInTrigger >= 2) : (playersInTrigger >= 1);

        float targetEmission = enoughPlayers ? emissionOnStrength : emissionOffStrength;
        SetEmission(targetEmission);

        if (debugLogs)
            Debug.Log($"[PortalActivator:{name}] UpdatePlatformEmissive → playersInTrigger={playersInTrigger}, emission={targetEmission}");
    }

    void SetEmission(float strengthValue)
    {
        if (emissiveRenderers == null) return;

        foreach (MeshRenderer renderer in emissiveRenderers)
        {
            if (renderer == null) continue;

            Material mat = renderer.material;
            if (mat != null && mat.HasProperty(EmissionProperty))
                mat.SetFloat(EmissionProperty, strengthValue);
        }
    }

    void SetPortalPiecesActive(bool active)
    {
        if (portalPieces == null) return;

        foreach (var go in portalPieces)
        {
            if (go == null) continue;
            go.SetActive(active);
        }
    }

    void PlayFailSFX()
    {
        if (audioSource != null && failSFX != null)
            audioSource.PlayOneShot(failSFX);
    }
}
