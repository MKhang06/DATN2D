using System.Collections.Generic;
using UnityEngine;

namespace Khang
{
    public class InventoryManager : MonoBehaviour
    {
        public static InventoryManager Instance { get; private set; }

        [Header("Cấu Hình UI")]
        [SerializeField] private GameObject inventoryPanel;
        [SerializeField] private Transform slotParent; 

        private List<InventorySlot> slots = new List<InventorySlot>();

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            if (slotParent != null)
            {
                slots.AddRange(slotParent.GetComponentsInChildren<InventorySlot>());
            }

            if (inventoryPanel != null)
            {
                inventoryPanel.SetActive(false);
            }
        }

        private void Update()
        {
            // Bấm phím I hoặc TAB để bật/tắt túi đồ
            if (Input.GetKeyDown(KeyCode.I) || Input.GetKeyDown(KeyCode.Tab))
            {
                ToggleInventory();
            }
        }

        public void ToggleInventory()
        {
            if (inventoryPanel != null)
            {
                inventoryPanel.SetActive(!inventoryPanel.activeSelf);
            }
        }

        public bool AddItem(ItemData item, int amount = 1)
        {
            if (item == null) return false;

            // 1. Tìm ô đã có sẵn vật phẩm cùng loại để cộng dồn
            if (item.isStackable)
            {
                foreach (var slot in slots)
                {
                    if (!slot.IsEmpty && slot.Item == item)
                    {
                        slot.AddItemToSlot(item, amount);
                        return true;
                    }
                }
            }

            // 2. Tìm ô trống đầu tiên để chèn vào
            foreach (var slot in slots)
            {
                if (slot.IsEmpty)
                {
                    slot.AddItemToSlot(item, amount);
                    return true;
                }
            }

            Debug.LogWarning("Túi đồ đã đầy!");
            return false;
        }
    }
}