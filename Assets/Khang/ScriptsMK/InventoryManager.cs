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

        [Header("Phím tắt")]
        [SerializeField] private KeyCode openKey = KeyCode.I;
        [SerializeField] private KeyCode toggleKey = KeyCode.Tab;

        private List<InventorySlot> slots = new List<InventorySlot>();

        public bool IsInventoryOpen => inventoryPanel != null && inventoryPanel.activeSelf;

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
                slots.AddRange(slotParent.GetComponentsInChildren<InventorySlot>(true));
            }

            if (inventoryPanel != null)
            {
                inventoryPanel.SetActive(false);
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(openKey) || Input.GetKeyDown(toggleKey))
            {
                ToggleInventory();
            }
        }

        public void ToggleInventory()
        {
            if (inventoryPanel == null) return;
            inventoryPanel.SetActive(!inventoryPanel.activeSelf);
        }

        public void OpenInventory()
        {
            if (inventoryPanel == null) return;
            inventoryPanel.SetActive(true);
        }

        public void CloseInventory()
        {
            if (inventoryPanel == null) return;
            inventoryPanel.SetActive(false);
        }

        public bool AddItem(ItemData item, int amount = 1)
        {
            if (item == null || amount <= 0) return false;

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