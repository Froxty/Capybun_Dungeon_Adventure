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
    [SerializeField] string itemId = "HeartPiece_Bottom"; // or Left/Right per prefab
    [SerializeField] Sprite itemIcon;
    [SerializeField] int amount = 1;

    [Header("Prompt UI")]
    [SerializeField] TMP_Text promptLabel;

    [Header("Pickup Mode")]
    [SerializeField] PickupMode pickupMode = PickupMode.Auto;

    bool playerInRange = false;

    void Start()
    {
        if (promptLabel != null)
            promptLabel.gameObject.SetActive(false);
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
        if (!other.CompareTag("Player")) return;

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
        if (!other.CompareTag("Player")) return;

        playerInRange = false;
        if (promptLabel != null)
            promptLabel.gameObject.SetActive(false);
    }

    void TryCollect()
    {
        var inventory = InventorySystem.Instance;
        if (inventory == null)
        {
            Debug.LogWarning("No InventorySystem in scene.");
            return;
        }

        bool added = inventory.AddItemToFirstFreeSlot(itemId, itemIcon, amount);
        if (!added)
        {
            // optional: show "Inventory Full" message here
            return;
        }

        if (promptLabel != null)
            promptLabel.gameObject.SetActive(false);

        Destroy(gameObject);
    }
}
