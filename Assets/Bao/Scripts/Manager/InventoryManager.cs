using System;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance;

    public enum SlotArea
    {
        Hotbar,
        Bag
    }

    [Serializable]
    public class InventorySlot
    {
        [Tooltip(
            "Dữ liệu vật phẩm. Các slot cũ có thể để trống " +
            "và hệ thống sẽ đối chiếu bằng Item Name."
        )]
        public InventoryItemData itemData;

        public string itemName;
        public Sprite icon;
        public int amount;
        public int maxStack = 99;

        public bool IsEmpty =>
            string.IsNullOrEmpty(itemName) ||
            amount <= 0;

        public void SetItem(
            string newName,
            Sprite newIcon,
            int newAmount)
        {
            SetItem(
                null,
                newName,
                newIcon,
                newAmount,
                99
            );
        }

        public void SetItem(
            InventoryItemData newItem,
            int newAmount)
        {
            if (newItem == null)
            {
                Clear();
                return;
            }

            SetItem(
                newItem,
                newItem.DisplayName,
                newItem.Icon,
                newAmount,
                Mathf.Max(1, newItem.MaxStack)
            );
        }

        public void SetItem(
            InventoryItemData newItem,
            string newName,
            Sprite newIcon,
            int newAmount,
            int newMaxStack)
        {
            itemData = newItem;
            itemName = newName;
            icon = newIcon;
            amount = Mathf.Max(0, newAmount);
            maxStack = Mathf.Max(1, newMaxStack);
        }

        public bool Matches(
            InventoryItemData item)
        {
            if (item == null || IsEmpty)
                return false;

            if (itemData != null)
            {
                if (itemData == item)
                    return true;

                if (!string.IsNullOrWhiteSpace(
                        itemData.ItemId) &&
                    !string.IsNullOrWhiteSpace(
                        item.ItemId))
                {
                    return string.Equals(
                        itemData.ItemId,
                        item.ItemId,
                        StringComparison
                            .OrdinalIgnoreCase
                    );
                }
            }

            return string.Equals(
                itemName,
                item.DisplayName,
                StringComparison
                    .CurrentCultureIgnoreCase
            );
        }

        public void Clear()
        {
            itemData = null;
            itemName = string.Empty;
            icon = null;
            amount = 0;
            maxStack = 99;
        }
    }

    [Header("Slots")]
    [SerializeField]
    private InventorySlot[] hotbarSlots =
        new InventorySlot[9];

    [SerializeField]
    private InventorySlot[] bagSlots =
        new InventorySlot[24];

    [Header("Selected")]
    [SerializeField]
    private int selectedHotbarIndex;

    public InventorySlot[] HotbarSlots =>
        hotbarSlots;

    public InventorySlot[] BagSlots =>
        bagSlots;

    public int SelectedHotbarIndex =>
        selectedHotbarIndex;

    public InventorySlot SelectedSlot
    {
        get
        {
            if (hotbarSlots == null ||
                hotbarSlots.Length == 0)
            {
                return null;
            }

            selectedHotbarIndex =
                Mathf.Clamp(
                    selectedHotbarIndex,
                    0,
                    hotbarSlots.Length - 1
                );

            return hotbarSlots[
                selectedHotbarIndex
            ];
        }
    }

    public event Action OnInventoryChanged;
    public event Action<int>
        OnSelectedHotbarChanged;

    private void Awake()
    {
        if (Instance != null &&
            Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        InitSlots(hotbarSlots);
        InitSlots(bagSlots);

        if (hotbarSlots.Length > 0)
        {
            selectedHotbarIndex =
                Mathf.Clamp(
                    selectedHotbarIndex,
                    0,
                    hotbarSlots.Length - 1
                );
        }
    }

    private void Update()
    {
        HandleHotbarInput();

        if (Input.GetKeyDown(KeyCode.B))
            MoveSelectedHotbarToBag();
    }

    private static void InitSlots(
        InventorySlot[] slots)
    {
        if (slots == null)
            return;

        for (int i = 0;
             i < slots.Length;
             i++)
        {
            if (slots[i] == null)
                slots[i] =
                    new InventorySlot();
        }
    }

    private void HandleHotbarInput()
    {
        int keyCount =
            Mathf.Min(
                hotbarSlots.Length,
                9
            );

        for (int i = 0;
             i < keyCount;
             i++)
        {
            KeyCode key =
                KeyCode.Alpha1 + i;

            if (Input.GetKeyDown(key))
                SelectHotbar(i);
        }
    }

    public void SelectHotbar(int index)
    {
        if (index < 0 ||
            index >= hotbarSlots.Length)
        {
            return;
        }

        selectedHotbarIndex = index;

        OnSelectedHotbarChanged?.Invoke(
            selectedHotbarIndex
        );

        OnInventoryChanged?.Invoke();

        InventorySlot slot =
            hotbarSlots[
                selectedHotbarIndex
            ];

        Debug.Log(
            slot != null &&
            !slot.IsEmpty
                ? "Đang cầm: " +
                  slot.itemName
                : "Tay trống."
        );
    }

    // =========================================================
    // API DÙNG CHO INVENTORY ITEM DATA
    // =========================================================

    public bool HasItem(
        InventoryItemData item)
    {
        return GetItemAmount(item) > 0;
    }

    public int GetItemAmount(
        InventoryItemData item)
    {
        if (item == null)
            return 0;

        return GetItemAmountFromSlots(
                   hotbarSlots,
                   item
               ) +
               GetItemAmountFromSlots(
                   bagSlots,
                   item
               );
    }

    public int GetMaxAddableAmount(
        InventoryItemData item)
    {
        if (item == null)
            return 0;

        if (item.UniqueOwnership)
        {
            if (HasItem(item))
                return 0;

            return HasEmptySlot(
                hotbarSlots
            ) ||
            HasEmptySlot(
                bagSlots
            )
                ? 1
                : 0;
        }

        int result = 0;

        result +=
            GetAddableAmountInSlots(
                hotbarSlots,
                item
            );

        result +=
            GetAddableAmountInSlots(
                bagSlots,
                item
            );

        return result;
    }

    public bool CanAddItem(
        InventoryItemData item,
        int amount,
        out string reason)
    {
        reason = string.Empty;

        if (item == null)
        {
            reason =
                "Vật phẩm không hợp lệ.";

            return false;
        }

        if (amount <= 0)
        {
            reason =
                "Số lượng phải lớn hơn 0.";

            return false;
        }

        if (item.UniqueOwnership)
        {
            if (amount != 1)
            {
                reason =
                    "Trang bị chỉ được sở hữu một món.";

                return false;
            }

            if (HasItem(item))
            {
                reason =
                    "Bạn đã sở hữu trang bị này.";

                return false;
            }
        }

        if (GetMaxAddableAmount(item) <
            amount)
        {
            reason =
                "Túi đồ không đủ chỗ.";

            return false;
        }

        return true;
    }

    public bool TryAddItem(
        InventoryItemData item,
        int amount)
    {
        return TryAddItem(
            item,
            amount,
            out _
        );
    }

    public bool TryAddItem(
        InventoryItemData item,
        int amount,
        out string reason)
    {
        if (!CanAddItem(
                item,
                amount,
                out reason))
        {
            return false;
        }

        int remaining = amount;

        if (!item.UniqueOwnership)
        {
            remaining =
                AddToExistingStack(
                    hotbarSlots,
                    item,
                    remaining
                );

            remaining =
                AddToExistingStack(
                    bagSlots,
                    item,
                    remaining
                );
        }

        remaining =
            AddToEmptySlot(
                hotbarSlots,
                item,
                remaining
            );

        remaining =
            AddToEmptySlot(
                bagSlots,
                item,
                remaining
            );

        if (remaining > 0)
        {
            reason =
                "Không thể thêm đủ vật phẩm.";

            return false;
        }

        OnInventoryChanged?.Invoke();
        return true;
    }

    public bool RemoveItem(
        InventoryItemData item,
        int amount)
    {
        return RemoveItem(
            item,
            amount,
            out _
        );
    }

    public bool RemoveItem(
        InventoryItemData item,
        int amount,
        out string reason)
    {
        reason = string.Empty;

        if (item == null)
        {
            reason =
                "Vật phẩm không hợp lệ.";

            return false;
        }

        if (amount <= 0)
        {
            reason =
                "Số lượng phải lớn hơn 0.";

            return false;
        }

        if (GetItemAmount(item) <
            amount)
        {
            reason =
                "Không đủ " +
                item.DisplayName +
                " trong túi.";

            return false;
        }

        int remaining = amount;

        // Ưu tiên lấy trong Bag trước.
        remaining =
            RemoveFromSlots(
                bagSlots,
                item,
                remaining
            );

        remaining =
            RemoveFromSlots(
                hotbarSlots,
                item,
                remaining
            );

        if (remaining > 0)
        {
            reason =
                "Không thể xóa đủ vật phẩm.";

            return false;
        }

        OnInventoryChanged?.Invoke();
        return true;
    }

    public bool TryAddCaughtItem(
        InventoryItemData item,
        float weightKg,
        out string reason)
    {
        // Inventory hiện tại chưa lưu cân nặng riêng trong slot.
        // Vẫn thêm đúng InventoryItemData của cá.
        return TryAddItem(
            item,
            1,
            out reason
        );
    }

    private static int
        GetItemAmountFromSlots(
            InventorySlot[] slots,
            InventoryItemData item)
    {
        int result = 0;

        if (slots == null)
            return result;

        foreach (InventorySlot slot
                 in slots)
        {
            if (slot != null &&
                slot.Matches(item))
            {
                result +=
                    Mathf.Max(
                        0,
                        slot.amount
                    );
            }
        }

        return result;
    }

    private static int
        GetAddableAmountInSlots(
            InventorySlot[] slots,
            InventoryItemData item)
    {
        if (slots == null ||
            item == null)
        {
            return 0;
        }

        int maxStack =
            Mathf.Max(
                1,
                item.MaxStack
            );

        int result = 0;

        foreach (InventorySlot slot
                 in slots)
        {
            if (slot == null)
                continue;

            if (slot.IsEmpty)
            {
                result += maxStack;
                continue;
            }

            if (!slot.Matches(item))
                continue;

            int slotLimit =
                Mathf.Max(
                    1,
                    slot.maxStack
                );

            result +=
                Mathf.Max(
                    0,
                    slotLimit -
                    slot.amount
                );
        }

        return result;
    }

    private static bool HasEmptySlot(
        InventorySlot[] slots)
    {
        if (slots == null)
            return false;

        foreach (InventorySlot slot
                 in slots)
        {
            if (slot != null &&
                slot.IsEmpty)
            {
                return true;
            }
        }

        return false;
    }

    private static int
        AddToExistingStack(
            InventorySlot[] slots,
            InventoryItemData item,
            int amount)
    {
        if (slots == null ||
            item == null)
        {
            return amount;
        }

        for (int i = 0;
             i < slots.Length &&
             amount > 0;
             i++)
        {
            InventorySlot slot =
                slots[i];

            if (slot == null ||
                slot.IsEmpty ||
                !slot.Matches(item))
            {
                continue;
            }

            int canAdd =
                Mathf.Max(
                    0,
                    slot.maxStack -
                    slot.amount
                );

            if (canAdd <= 0)
                continue;

            int addAmount =
                Mathf.Min(
                    canAdd,
                    amount
                );

            slot.amount += addAmount;
            amount -= addAmount;

            if (slot.itemData == null)
                slot.itemData = item;
        }

        return amount;
    }

    private static int
        AddToEmptySlot(
            InventorySlot[] slots,
            InventoryItemData item,
            int amount)
    {
        if (slots == null ||
            item == null)
        {
            return amount;
        }

        int maxStack =
            Mathf.Max(
                1,
                item.MaxStack
            );

        for (int i = 0;
             i < slots.Length &&
             amount > 0;
             i++)
        {
            InventorySlot slot =
                slots[i];

            if (slot == null ||
                !slot.IsEmpty)
            {
                continue;
            }

            int addAmount =
                Mathf.Min(
                    maxStack,
                    amount
                );

            slot.SetItem(
                item,
                item.DisplayName,
                item.Icon,
                addAmount,
                maxStack
            );

            amount -= addAmount;

            if (item.UniqueOwnership)
                break;
        }

        return amount;
    }

    private static int RemoveFromSlots(
        InventorySlot[] slots,
        InventoryItemData item,
        int amount)
    {
        if (slots == null ||
            item == null)
        {
            return amount;
        }

        for (int i =
                 slots.Length - 1;
             i >= 0 && amount > 0;
             i--)
        {
            InventorySlot slot =
                slots[i];

            if (slot == null ||
                !slot.Matches(item))
            {
                continue;
            }

            int removeAmount =
                Mathf.Min(
                    slot.amount,
                    amount
                );

            slot.amount -=
                removeAmount;

            amount -=
                removeAmount;

            if (slot.amount <= 0)
                slot.Clear();
        }

        return amount;
    }

    // =========================================================
    // API CŨ: ADD ITEM BẰNG TÊN
    // =========================================================

    public bool AddItem(
        string itemName,
        Sprite icon,
        int amount)
    {
        if (string.IsNullOrEmpty(
                itemName) ||
            amount <= 0)
        {
            return false;
        }

        int remaining = amount;

        remaining =
            AddToExistingStackByName(
                hotbarSlots,
                itemName,
                icon,
                remaining
            );

        remaining =
            AddToExistingStackByName(
                bagSlots,
                itemName,
                icon,
                remaining
            );

        remaining =
            AddToEmptySlotByName(
                hotbarSlots,
                itemName,
                icon,
                remaining
            );

        remaining =
            AddToEmptySlotByName(
                bagSlots,
                itemName,
                icon,
                remaining
            );

        OnInventoryChanged?.Invoke();

        return remaining <= 0;
    }

    private static int
        AddToExistingStackByName(
            InventorySlot[] slots,
            string itemName,
            Sprite icon,
            int amount)
    {
        if (slots == null)
            return amount;

        for (int i = 0;
             i < slots.Length &&
             amount > 0;
             i++)
        {
            InventorySlot slot =
                slots[i];

            if (slot == null ||
                slot.IsEmpty ||
                !string.Equals(
                    slot.itemName,
                    itemName,
                    StringComparison
                        .CurrentCultureIgnoreCase))
            {
                continue;
            }

            int canAdd =
                slot.maxStack -
                slot.amount;

            if (canAdd <= 0)
                continue;

            int addAmount =
                Mathf.Min(
                    canAdd,
                    amount
                );

            slot.amount += addAmount;
            amount -= addAmount;

            if (slot.icon == null)
                slot.icon = icon;
        }

        return amount;
    }

    private static int
        AddToEmptySlotByName(
            InventorySlot[] slots,
            string itemName,
            Sprite icon,
            int amount)
    {
        if (slots == null)
            return amount;

        for (int i = 0;
             i < slots.Length &&
             amount > 0;
             i++)
        {
            InventorySlot slot =
                slots[i];

            if (slot == null ||
                !slot.IsEmpty)
            {
                continue;
            }

            int addAmount =
                Mathf.Min(
                    slot.maxStack,
                    amount
                );

            slot.SetItem(
                null,
                itemName,
                icon,
                addAmount,
                slot.maxStack
            );

            amount -= addAmount;
        }

        return amount;
    }

    // =========================================================
    // DI CHUYỂN SLOT
    // =========================================================

    public void MoveSelectedHotbarToBag()
    {
        InventorySlot hotbarSlot =
            SelectedSlot;

        if (hotbarSlot == null ||
            hotbarSlot.IsEmpty)
        {
            Debug.Log(
                "Không có item để bỏ vào túi."
            );

            return;
        }

        bool added =
            AddToBagOnly(hotbarSlot);

        if (!added)
        {
            Debug.Log(
                "Túi đồ đã đầy."
            );

            return;
        }

        Debug.Log(
            "Đã bỏ vào túi: " +
            hotbarSlot.itemName
        );

        hotbarSlot.Clear();
        OnInventoryChanged?.Invoke();
    }

    private bool AddToBagOnly(
        InventorySlot source)
    {
        if (source == null ||
            source.IsEmpty)
        {
            return false;
        }

        int remaining =
            source.amount;

        if (source.itemData != null)
        {
            if (!source.itemData
                    .UniqueOwnership)
            {
                remaining =
                    AddToExistingStack(
                        bagSlots,
                        source.itemData,
                        remaining
                    );
            }

            remaining =
                AddToEmptySlot(
                    bagSlots,
                    source.itemData,
                    remaining
                );
        }
        else
        {
            remaining =
                AddToExistingStackByName(
                    bagSlots,
                    source.itemName,
                    source.icon,
                    remaining
                );

            remaining =
                AddToEmptySlotByName(
                    bagSlots,
                    source.itemName,
                    source.icon,
                    remaining
                );
        }

        return remaining <= 0;
    }

    public void MoveBagToHotbar(
        int bagIndex)
    {
        if (bagIndex < 0 ||
            bagIndex >= bagSlots.Length)
        {
            return;
        }

        InventorySlot bagSlot =
            bagSlots[bagIndex];

        if (bagSlot == null ||
            bagSlot.IsEmpty)
        {
            return;
        }

        int emptyHotbarIndex =
            GetEmptyHotbarIndex();

        if (emptyHotbarIndex == -1)
        {
            Debug.Log(
                "Thanh tay đã đầy."
            );

            return;
        }

        hotbarSlots[
            emptyHotbarIndex
        ].SetItem(
            bagSlot.itemData,
            bagSlot.itemName,
            bagSlot.icon,
            bagSlot.amount,
            bagSlot.maxStack
        );

        bagSlot.Clear();
        OnInventoryChanged?.Invoke();
    }

    private int GetEmptyHotbarIndex()
    {
        for (int i = 0;
             i < hotbarSlots.Length;
             i++)
        {
            if (hotbarSlots[i] != null &&
                hotbarSlots[i].IsEmpty)
            {
                return i;
            }
        }

        return -1;
    }

    public void RemoveSelectedItem(
        int amount)
    {
        InventorySlot slot =
            SelectedSlot;

        if (slot == null ||
            slot.IsEmpty ||
            amount <= 0)
        {
            return;
        }

        slot.amount -= amount;

        if (slot.amount <= 0)
            slot.Clear();

        OnInventoryChanged?.Invoke();
    }

    public InventorySlot GetSlot(
        SlotArea area,
        int index)
    {
        InventorySlot[] slots =
            area == SlotArea.Hotbar
                ? hotbarSlots
                : bagSlots;

        if (index < 0 ||
            index >= slots.Length)
        {
            return null;
        }

        return slots[index];
    }

    public void SwapSlots(
        SlotArea fromArea,
        int fromIndex,
        SlotArea toArea,
        int toIndex)
    {
        InventorySlot fromSlot =
            GetSlot(
                fromArea,
                fromIndex
            );

        InventorySlot toSlot =
            GetSlot(
                toArea,
                toIndex
            );

        if (fromSlot == null ||
            toSlot == null ||
            fromSlot.IsEmpty)
        {
            return;
        }

        bool sameItem =
            !toSlot.IsEmpty &&
            (
                fromSlot.itemData != null &&
                toSlot.Matches(
                    fromSlot.itemData
                ) ||
                fromSlot.itemData == null &&
                string.Equals(
                    toSlot.itemName,
                    fromSlot.itemName,
                    StringComparison
                        .CurrentCultureIgnoreCase
                )
            );

        if (sameItem)
        {
            int canAdd =
                toSlot.maxStack -
                toSlot.amount;

            if (canAdd > 0)
            {
                int moveAmount =
                    Mathf.Min(
                        canAdd,
                        fromSlot.amount
                    );

                toSlot.amount +=
                    moveAmount;

                fromSlot.amount -=
                    moveAmount;

                if (fromSlot.amount <= 0)
                    fromSlot.Clear();

                OnInventoryChanged?.Invoke();
                return;
            }
        }

        InventoryItemData tempItem =
            toSlot.itemData;

        string tempName =
            toSlot.itemName;

        Sprite tempIcon =
            toSlot.icon;

        int tempAmount =
            toSlot.amount;

        int tempMaxStack =
            toSlot.maxStack;

        toSlot.SetItem(
            fromSlot.itemData,
            fromSlot.itemName,
            fromSlot.icon,
            fromSlot.amount,
            fromSlot.maxStack
        );

        fromSlot.SetItem(
            tempItem,
            tempName,
            tempIcon,
            tempAmount,
            tempMaxStack
        );

        OnInventoryChanged?.Invoke();
    }

    public void RefreshInventoryUI()
    {
        OnInventoryChanged?.Invoke();
    }
}