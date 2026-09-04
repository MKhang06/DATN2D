using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Khang
{
    public class InventorySlot : MonoBehaviour
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI quantityText;

        private ItemData currentItem;
        private int currentQuantity;

        public bool IsEmpty => currentItem == null;
        public ItemData Item => currentItem;
        public int Quantity => currentQuantity;

        public void AddItemToSlot(ItemData item, int amount)
        {
            currentItem = item;
            currentQuantity += amount;
            UpdateSlotUI();
        }

        public void ClearSlot()
        {
            currentItem = null;
            currentQuantity = 0;
            UpdateSlotUI();
        }

        private void UpdateSlotUI()
        {
            if (currentItem != null)
            {
                if (iconImage != null)
                {
                    iconImage.sprite = currentItem.icon;
                    iconImage.enabled = true;
                }

                if (quantityText != null)
                {
                    if (currentItem.isStackable && currentQuantity > 1)
                    {
                        quantityText.text = currentQuantity.ToString();
                        quantityText.enabled = true;
                    }
                    else
                    {
                        quantityText.enabled = false;
                    }
                }
            }
            else
            {
                if (iconImage != null) iconImage.enabled = false;
                if (quantityText != null) quantityText.enabled = false;
            }
        }
    }
}