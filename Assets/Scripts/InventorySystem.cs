using System;
using UnityEngine;

public class InventorySystem : MonoBehaviour
{
    public static InventorySystem Instance { get; private set; }

    [Serializable]
    public class InventorySlotData
    {
        public string itemId;
        public Sprite icon;
        public int count;

        public bool IsEmpty => string.IsNullOrEmpty(itemId) || count <= 0;
    }

    [Header("Inventory Setup")]
    [SerializeField] int slotCount = 4;
    [SerializeField] InventorySlotData[] slots;

    public event Action OnInventoryChanged;

    public InventorySlotData[] Slots => slots;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (slots == null || slots.Length == 0)
        {
            slots = new InventorySlotData[slotCount];
            for (int i = 0; i < slotCount; i++)
                slots[i] = new InventorySlotData();
        }
    }

    /// <summary>Adds an item to the first empty slot. Returns true if it fit.</summary>
    public bool AddItemToFirstFreeSlot(string itemId, Sprite icon, int amount = 1)
    {
        if (amount <= 0 || string.IsNullOrEmpty(itemId))
            return false;

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i].IsEmpty)
            {
                slots[i].itemId = itemId;
                slots[i].icon = icon;
                slots[i].count = amount;

                OnInventoryChanged?.Invoke();
                return true;
            }
        }

        Debug.Log("Inventory full, could not add " + itemId);
        return false;
    }

    /// <summary>Returns true if any slot contains this itemId.</summary>
    public bool HasItem(string itemId)
    {
        if (string.IsNullOrEmpty(itemId)) return false;

        for (int i = 0; i < slots.Length; i++)
        {
            var slot = slots[i];
            if (!slot.IsEmpty && slot.itemId == itemId)
                return true;
        }

        return false;
    }

    /// <summary>Consumes amount of this itemId from the first matching slot. Returns true if successful.</summary>
    public bool ConsumeItem(string itemId, int amount = 1)
    {
        if (amount <= 0 || string.IsNullOrEmpty(itemId)) return false;

        for (int i = 0; i < slots.Length; i++)
        {
            var slot = slots[i];
            if (slot.IsEmpty || slot.itemId != itemId)
                continue;

            slot.count -= amount;
            if (slot.count <= 0)
            {
                slot.count = 0;
                slot.itemId = null;
                slot.icon = null;
            }

            // 🔔 TELL UI TO REFRESH
            OnInventoryChanged?.Invoke();
            return true;
        }

        return false;
    }

    /// <summary>Swap contents of two slots (used by drag & drop).</summary>
    public void SwapSlots(int indexA, int indexB)
    {
        if (indexA == indexB) return;
        if (indexA < 0 || indexA >= slots.Length) return;
        if (indexB < 0 || indexB >= slots.Length) return;

        var temp = slots[indexA];
        slots[indexA] = slots[indexB];
        slots[indexB] = temp;

        OnInventoryChanged?.Invoke();
    }
}
