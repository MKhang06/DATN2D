using UnityEngine;

public class HeldItemWorld : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform heldItemRoot;
    [SerializeField] private SpriteRenderer heldItemRenderer;
    [SerializeField] private SpriteRenderer playerRenderer;

    [Header("Hand Position")]
    [SerializeField] private Vector3 rightHandPosition =
        new Vector3(0.3f, -0.05f, 0f);

    [SerializeField] private Vector3 leftHandPosition =
        new Vector3(-0.3f, -0.05f, 0f);

    [Header("Item Settings")]
    [SerializeField] private Vector3 itemScale =
        new Vector3(0.4f, 0.4f, 1f);

    [SerializeField] private int sortingOrder = 10;

    private InventoryManager inventoryManager;
    private bool temporarilyHidden;

    private void Start()
    {
        FindInventoryManager();
        RefreshHeldItem();
    }

    private void Update()
    {
        if (inventoryManager == null)
        {
            FindInventoryManager();
        }
    }

    private void LateUpdate()
    {
        UpdateHandDirection();
    }

    private void OnDestroy()
    {
        UnsubscribeEvents();
    }

    private void FindInventoryManager()
    {
        if (InventoryManager.Instance == null)
            return;

        if (inventoryManager == InventoryManager.Instance)
            return;

        UnsubscribeEvents();

        inventoryManager = InventoryManager.Instance;

        inventoryManager.OnInventoryChanged += RefreshHeldItem;
        inventoryManager.OnSelectedHotbarChanged += OnSelectedSlotChanged;

        RefreshHeldItem();
    }

    private void UnsubscribeEvents()
    {
        if (inventoryManager == null)
            return;

        inventoryManager.OnInventoryChanged -= RefreshHeldItem;
        inventoryManager.OnSelectedHotbarChanged -= OnSelectedSlotChanged;
    }

    private void OnSelectedSlotChanged(int index)
    {
        RefreshHeldItem();
    }

    private void RefreshHeldItem()
    {
        if (heldItemRenderer == null)
            return;

        if (inventoryManager == null)
        {
            HideHeldItem();
            return;
        }

        InventoryManager.InventorySlot selectedSlot =
            inventoryManager.SelectedSlot;

        if (selectedSlot == null ||
            selectedSlot.IsEmpty ||
            selectedSlot.icon == null)
        {
            HideHeldItem();
            return;
        }

        heldItemRenderer.sprite = selectedSlot.icon;
        heldItemRenderer.enabled = !temporarilyHidden;
        heldItemRenderer.sortingOrder = sortingOrder;

        if (heldItemRoot != null)
        {
            heldItemRoot.localScale = itemScale;
            heldItemRoot.gameObject.SetActive(!temporarilyHidden);
        }
    }

    private void UpdateHandDirection()
    {
        if (heldItemRoot == null || playerRenderer == null)
            return;

        bool facingLeft = playerRenderer.flipX;

        heldItemRoot.localPosition =
            facingLeft ? leftHandPosition : rightHandPosition;

        Vector3 scale = itemScale;

        if (facingLeft)
            scale.x = -Mathf.Abs(itemScale.x);
        else
            scale.x = Mathf.Abs(itemScale.x);

        heldItemRoot.localScale = scale;
    }

    private void HideHeldItem()
    {
        if (heldItemRenderer != null)
        {
            heldItemRenderer.sprite = null;
            heldItemRenderer.enabled = false;
        }
    }

    public void SetTemporarilyHidden(bool hidden)
    {
        temporarilyHidden = hidden;

        if (hidden)
        {
            if (heldItemRenderer != null)
                heldItemRenderer.enabled = false;
        }
        else
        {
            RefreshHeldItem();
        }
    }
}