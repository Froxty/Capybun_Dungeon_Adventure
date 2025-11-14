using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventorySlotDragHandler : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Index of this slot in the inventory array")]
    [SerializeField] int slotIndex;

    [Header("Canvas used for drag icon")]
    [SerializeField] Canvas canvas;     // drag icon will be a child of this

    Image dragIcon;                     // floating image while dragging

    InventorySystem inventory;

    void Awake()
    {
        if (canvas == null)
            canvas = GetComponentInParent<Canvas>();

        inventory = InventorySystem.Instance;
        if (inventory == null)
            inventory = FindFirstObjectByType<InventorySystem>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (inventory == null) return;

        var slots = inventory.Slots;
        if (slotIndex < 0 || slotIndex >= slots.Length) return;

        var slot = slots[slotIndex];
        if (slot == null || slot.IsEmpty || slot.icon == null)
        {
            return; // nothing to drag
        }

        // Create drag icon object
        GameObject go = new GameObject("DragIcon", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(canvas.transform, false);

        dragIcon = go.GetComponent<Image>();
        dragIcon.sprite = slot.icon;
        dragIcon.raycastTarget = false;   

        // ⬇ NEW LOGIC: MATCH SLOT SIZE
        RectTransform slotRect = GetComponent<RectTransform>();
        RectTransform dragRect = dragIcon.rectTransform;

        dragRect.sizeDelta = slotRect.sizeDelta;
        dragRect.anchorMin = new Vector2(0.5f, 0.5f);
        dragRect.anchorMax = new Vector2(0.5f, 0.5f);
        dragRect.pivot = new Vector2(0.5f, 0.5f);

        dragRect.position = eventData.position;
    }
    public void OnDrag(PointerEventData eventData)
    {
        if (dragIcon != null)
        {
            dragIcon.rectTransform.position = eventData.position;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (dragIcon != null)
        {
            Destroy(dragIcon.gameObject);
            dragIcon = null;
        }

        if (inventory == null) return;

        // Where did we drop?
        GameObject hit = eventData.pointerCurrentRaycast.gameObject;
        if (hit == null) return;

        // Look for another slot handler in the parents of whatever we hit
        var targetSlot = hit.GetComponentInParent<InventorySlotDragHandler>();
        if (targetSlot == null) return;

        int targetIndex = targetSlot.slotIndex;
        if (targetIndex == slotIndex) return;

        inventory.SwapSlots(slotIndex, targetIndex);
        // UI will refresh via UIHandler listening to OnInventoryChanged
    }
}
