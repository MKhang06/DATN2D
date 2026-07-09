using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventorySlotUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text amountText;
    [SerializeField] private GameObject selectedFrame;

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
}