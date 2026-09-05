using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventorySlotUI : MonoBehaviour,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler,
    IDropHandler
{
    [Header("UI")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text amountText;
    [SerializeField] private GameObject selectedFrame;

    private InventoryManager.SlotArea slotArea;
    private int slotIndex;

    private Canvas parentCanvas;
    private Image dragIcon;

    private void Awake()
    {
        ConfigureSelectedFrame();
    }

    public void Init(
        InventoryManager.SlotArea area,
        int index,
        Canvas canvas)
    {
        slotArea = area;
        slotIndex = index;
        parentCanvas = canvas;
        ConfigureSelectedFrame();
    }

    private void ConfigureSelectedFrame()
    {
        if (selectedFrame == null)
            return;

        Image frameImage = selectedFrame.GetComponent<Image>();

        if (frameImage == null || frameImage.sprite != null)
            return;

        // Khung chọn trong scene là một Image không có sprite. Giữ một lớp
        // xanh rất nhẹ để không che icon/ô trống, còn Shadow tạo viền xanh.
        frameImage.color = new Color(0.15f, 0.85f, 0.35f, 0.08f);
        frameImage.raycastTarget = false;
    }

    public void SetSlot(
        InventoryManager.InventorySlot slot,
        bool selected)
    {
        if (slot == null || slot.IsEmpty)
        {
            if (iconImage != null)
            {
                iconImage.enabled = false;
                iconImage.sprite = null;
            }

            if (amountText != null)
                amountText.text = "";

            if (selectedFrame != null)
                selectedFrame.SetActive(selected);

            return;
        }

        if (iconImage != null)
        {
            iconImage.enabled = true;
            iconImage.sprite = slot.icon;
            iconImage.preserveAspect = true;
        }

        if (amountText != null)
        {
            amountText.text = slot.amount > 1
                ? slot.amount.ToString()
                : "";
        }

        if (selectedFrame != null)
            selectedFrame.SetActive(selected);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (InventoryManager.Instance == null)
            return;

        InventoryManager.InventorySlot slot =
            InventoryManager.Instance.GetSlot(slotArea, slotIndex);

        if (slot == null || slot.IsEmpty)
            return;

        CreateDragIcon(slot.icon);

        if (iconImage != null)
            iconImage.enabled = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (dragIcon == null)
            return;

        dragIcon.transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
{
    if (dragIcon != null)
        Destroy(dragIcon.gameObject);

    if (InventoryManager.Instance != null)
        InventoryManager.Instance.RefreshInventoryUI();
}

    public void OnDrop(PointerEventData eventData)
    {
        InventorySlotUI fromSlotUI =
            eventData.pointerDrag != null
                ? eventData.pointerDrag.GetComponent<InventorySlotUI>()
                : null;

        if (fromSlotUI == null)
            return;

        if (InventoryManager.Instance == null)
            return;

        InventoryManager.Instance.SwapSlots(
            fromSlotUI.slotArea,
            fromSlotUI.slotIndex,
            slotArea,
            slotIndex
        );
    }

    private void CreateDragIcon(Sprite sprite)
    {
        if (parentCanvas == null)
            parentCanvas = GetComponentInParent<Canvas>();

        GameObject obj = new GameObject("DraggingItemIcon");
        obj.transform.SetParent(parentCanvas.transform, false);
        obj.transform.SetAsLastSibling();

        dragIcon = obj.AddComponent<Image>();
        dragIcon.sprite = sprite;
        dragIcon.preserveAspect = true;
        dragIcon.raycastTarget = false;

        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(55, 55);
    }
}
