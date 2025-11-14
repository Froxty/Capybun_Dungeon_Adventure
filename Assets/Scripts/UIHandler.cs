using UnityEngine;
using UnityEngine.UI;

public class UIHandler : MonoBehaviour
{
    [Header("UI Slot Images (in order)")]
    [SerializeField] Image[] slotImages;  // 4 entries, one per slot

    [Header("Optional empty sprite")]
    [SerializeField] Sprite emptySprite;

    InventorySystem inventory;

    void Start()
    {
        inventory = InventorySystem.Instance;
        if (inventory == null)
        {
            inventory = FindFirstObjectByType<InventorySystem>();
        }

        if (inventory != null)
        {
            inventory.OnInventoryChanged += OnInventoryChanged;
            RefreshAll();
        }
        else
        {
            Debug.LogWarning("UIHandler could not find InventorySystem.");
        }
    }

    void OnDestroy()
    {
        if (inventory != null)
            inventory.OnInventoryChanged -= OnInventoryChanged;
    }

    void OnInventoryChanged()
    {
        RefreshAll();
    }

    void RefreshAll()
    {
        if (inventory == null || slotImages == null) return;

        var slots = inventory.Slots;
        int max = Mathf.Min(slots.Length, slotImages.Length);

        for (int i = 0; i < max; i++)
        {
            var img = slotImages[i];
            if (img == null) continue;

            var slot = slots[i];

            if (!slot.IsEmpty && slot.icon != null)
            {
                img.enabled = true;
                img.sprite = slot.icon;
            }
            else
            {
                if (emptySprite != null)
                {
                    img.enabled = true;
                    img.sprite = emptySprite;
                }
                else
                {
                    img.enabled = false; // fully hide empty slots
                }
            }
        }
    }
}
