using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventorySlotUI : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text amountText;

    public void SetItem(Sprite sprite, int amount)
    {
        if (icon != null)
        {
            icon.enabled = true;
            icon.sprite = sprite;
        }

        if (amountText != null)
            amountText.text = amount.ToString();
    }

    public void Clear()
    {
        if (icon != null)
        {
            icon.enabled = false;
            icon.sprite = null;
        }

        if (amountText != null)
            amountText.text = "";
    }
}