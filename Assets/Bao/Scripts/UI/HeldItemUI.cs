using UnityEngine;
using UnityEngine.UI;

public class HeldItemUI : MonoBehaviour
{
    [SerializeField] private Image heldItemIcon;

    private void Start()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged += Refresh;
            InventoryManager.Instance.OnSelectedHotbarChanged += OnHotbarChanged;
        }

        Refresh();
    }

    private void OnDestroy()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged -= Refresh;
            InventoryManager.Instance.OnSelectedHotbarChanged -= OnHotbarChanged;
        }
    }

    private void OnHotbarChanged(int index)
    {
        Refresh();
    }

    private void Refresh()
    {
        if (heldItemIcon == null)
            return;

        if (InventoryManager.Instance == null)
        {
            heldItemIcon.enabled = false;
            return;
        }

        InventoryManager.InventorySlot slot =
            InventoryManager.Instance.SelectedSlot;

        if (slot == null || slot.IsEmpty)
        {
            heldItemIcon.enabled = false;
            heldItemIcon.sprite = null;
            return;
        }

        heldItemIcon.enabled = true;
        heldItemIcon.sprite = slot.icon;
        heldItemIcon.preserveAspect = true;
    }
}