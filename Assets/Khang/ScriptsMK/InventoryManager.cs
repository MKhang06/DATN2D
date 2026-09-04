using System.Collections.Generic;
using UnityEngine;

namespace Khang
{
    public class InventoryManager : MonoBehaviour
    {
        public static InventoryManager Instance { get; private set; }

        [Header("Cấu Hình UI Túi Đồ")]
        [SerializeField] private GameObject inventoryPanel;
        [SerializeField] private Transform slotParent;

        [Header("Cấu Hình Thanh Hotbar (Phím 1 - 9)")]
        [SerializeField] private Transform hotbarSlotParent;       // GameObject chứa các ô Hotbar
        [SerializeField] private RectTransform hotbarSelector;      // Khung viền trắng highlight ô đang chọn
        private List<InventorySlot> hotbarSlots = new List<InventorySlot>();
        private int selectedHotbarIndex = 0;

        [Header("Vật Phẩm Khởi Đầu (4 Mầm Cây)")]
        [SerializeField] private List<ItemData> startingSeeds = new List<ItemData>();
        [SerializeField] private int startingAmount = 5;            // Số lượng hạt mỗi loại cho sẵn

        [Header("Phím tắt")]
        [SerializeField] private KeyCode openKey = KeyCode.I;
        [SerializeField] private KeyCode toggleKey = KeyCode.Tab;

        private List<InventorySlot> allSlots = new List<InventorySlot>();

        public bool IsInventoryOpen => inventoryPanel != null && inventoryPanel.activeSelf;

        // Trả về ô Hotbar đang được chọn
        public InventorySlot SelectedSlot
        {
            get
            {
                if (hotbarSlots.Count > 0 && selectedHotbarIndex >= 0 && selectedHotbarIndex < hotbarSlots.Count)
                {
                    return hotbarSlots[selectedHotbarIndex];
                }
                return null;
            }
        }

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Start()
        {
            // 1. Quét các ô của thanh Hotbar
            if (hotbarSlotParent != null)
            {
                hotbarSlots.AddRange(hotbarSlotParent.GetComponentsInChildren<InventorySlot>(true));
            }

            // 2. Quét toàn bộ ô trong balo
            if (slotParent != null)
            {
                allSlots.AddRange(slotParent.GetComponentsInChildren<InventorySlot>(true));
            }

            // Nếu hotbarSlots để trống, tự lấy 9 ô đầu tiên của balo làm Hotbar
            if (hotbarSlots.Count == 0 && allSlots.Count > 0)
            {
                int count = Mathf.Min(9, allSlots.Count);
                for (int i = 0; i < count; i++) hotbarSlots.Add(allSlots[i]);
            }

            // Cập nhật vị trí khung viền trắng ban đầu vào ô số 1
            SelectHotbarSlot(0);

            // 3. Tặng sẵn 4 mầm cây vào Hotbar khi bắt đầu game
            if (startingSeeds != null && startingSeeds.Count > 0)
            {
                foreach (var seed in startingSeeds)
                {
                    if (seed != null) AddItem(seed, startingAmount);
                }
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

            // Nhận diện phím số từ 1 đến 9 trên bàn phím
            for (int i = 0; i < 9; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                {
                    SelectHotbarSlot(i);
                    break;
                }
            }
        }

        public void SelectHotbarSlot(int index)
        {
            if (index < 0 || index >= hotbarSlots.Count) return;

            selectedHotbarIndex = index;

            // Di chuyển khung viền trắng tới ô đang chọn
            if (hotbarSelector != null && hotbarSlots[selectedHotbarIndex] != null)
            {
                hotbarSelector.position = hotbarSlots[selectedHotbarIndex].transform.position;
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

            // Ưu tiên nạp vào Hotbar trước
            List<InventorySlot> targetList = hotbarSlots.Count > 0 ? hotbarSlots : allSlots;

            if (item.isStackable)
            {
                foreach (var slot in targetList)
                {
                    if (!slot.IsEmpty && slot.Item == item)
                    {
                        slot.AddItemToSlot(item, amount);
                        return true;
                    }
                }
                foreach (var slot in allSlots)
                {
                    if (!slot.IsEmpty && slot.Item == item)
                    {
                        slot.AddItemToSlot(item, amount);
                        return true;
                    }
                }
            }

            foreach (var slot in targetList)
            {
                if (slot.IsEmpty)
                {
                    slot.AddItemToSlot(item, amount);
                    return true;
                }
            }

            foreach (var slot in allSlots)
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