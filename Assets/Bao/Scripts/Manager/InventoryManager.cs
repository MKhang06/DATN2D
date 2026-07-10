using System;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance;

    [Serializable]
    public class InventorySlot
    {
        public string itemName;
        public Sprite icon;
        public int amount;
        public int maxStack = 99;

        public bool IsEmpty => string.IsNullOrEmpty(itemName) || amount <= 0;

        public void SetItem(string newName, Sprite newIcon, int newAmount)
        {
            itemName = newName;
            icon = newIcon;
            amount = newAmount;
        }

        public void Clear()
        {
            itemName = "";
            icon = null;
            amount = 0;
            maxStack = 99;
        }
    }

    [Header("Slots")]
    [SerializeField] private InventorySlot[] hotbarSlots = new InventorySlot[9];
    [SerializeField] private InventorySlot[] bagSlots = new InventorySlot[24];

    [Header("Selected")]
    [SerializeField] private int selectedHotbarIndex = 0;

    public InventorySlot[] HotbarSlots => hotbarSlots;
    public InventorySlot[] BagSlots => bagSlots;
    public int SelectedHotbarIndex => selectedHotbarIndex;
    public InventorySlot SelectedSlot => hotbarSlots[selectedHotbarIndex];

    public event Action OnInventoryChanged;
    public event Action<int> OnSelectedHotbarChanged;
    public enum SlotArea
{
    Hotbar,
    Bag
}

    private void Awake()
    {
        Instance = this;

        InitSlots(hotbarSlots);
        InitSlots(bagSlots);
    }

    private void Update()
    {
        HandleHotbarInput();

        if (Input.GetKeyDown(KeyCode.B))
        {
            MoveSelectedHotbarToBag();
        }
    }

    private void InitSlots(InventorySlot[] slots)
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null)
                slots[i] = new InventorySlot();
        }
    }

    private void HandleHotbarInput()
{
    for (int i = 0; i < hotbarSlots.Length && i < 9; i++)
    {
        KeyCode key = KeyCode.Alpha1 + i;

        if (Input.GetKeyDown(key))
        {
            SelectHotbar(i);
        }
    }
}

    public void SelectHotbar(int index)
{
    if (index < 0 || index >= hotbarSlots.Length)
        return;

    selectedHotbarIndex = index;

    OnSelectedHotbarChanged?.Invoke(selectedHotbarIndex);
    OnInventoryChanged?.Invoke();

    InventorySlot slot = hotbarSlots[selectedHotbarIndex];

    if (!slot.IsEmpty)
        Debug.Log("Đang cầm: " + slot.itemName);
    else
        Debug.Log("Tay trống.");
}

    public bool AddItem(string itemName, Sprite icon, int amount)
    {
        if (string.IsNullOrEmpty(itemName) || amount <= 0)
            return false;

        int remaining = amount;

        remaining = AddToExistingStack(hotbarSlots, itemName, icon, remaining);
        remaining = AddToExistingStack(bagSlots, itemName, icon, remaining);

        remaining = AddToEmptySlot(hotbarSlots, itemName, icon, remaining);
        remaining = AddToEmptySlot(bagSlots, itemName, icon, remaining);

        OnInventoryChanged?.Invoke();

        return remaining <= 0;
    }

    private int AddToExistingStack(
        InventorySlot[] slots,
        string itemName,
        Sprite icon,
        int amount)
    {
        for (int i = 0; i < slots.Length; i++)
        {
            InventorySlot slot = slots[i];

            if (slot.IsEmpty)
                continue;

            if (slot.itemName != itemName)
                continue;

            int canAdd = slot.maxStack - slot.amount;

            if (canAdd <= 0)
                continue;

            int addAmount = Mathf.Min(canAdd, amount);
            slot.amount += addAmount;
            amount -= addAmount;

            if (amount <= 0)
                return 0;
        }

        return amount;
    }

    private int AddToEmptySlot(
        InventorySlot[] slots,
        string itemName,
        Sprite icon,
        int amount)
    {
        for (int i = 0; i < slots.Length; i++)
        {
            InventorySlot slot = slots[i];

            if (!slot.IsEmpty)
                continue;

            int addAmount = Mathf.Min(slot.maxStack, amount);

            slot.SetItem(itemName, icon, addAmount);

            amount -= addAmount;

            if (amount <= 0)
                return 0;
        }

        return amount;
    }

    public void MoveSelectedHotbarToBag()
    {
        InventorySlot hotbarSlot = hotbarSlots[selectedHotbarIndex];

        if (hotbarSlot.IsEmpty)
        {
            Debug.Log("Không có item để bỏ vào túi.");
            return;
        }

        bool added = AddToBagOnly(
            hotbarSlot.itemName,
            hotbarSlot.icon,
            hotbarSlot.amount
        );

        if (!added)
        {
            Debug.Log("Túi đồ đã đầy.");
            return;
        }

        Debug.Log("Đã bỏ vào túi: " + hotbarSlot.itemName);

        hotbarSlot.Clear();

        OnInventoryChanged?.Invoke();
    }

    private bool AddToBagOnly(string itemName, Sprite icon, int amount)
    {
        int remaining = amount;

        remaining = AddToExistingStack(bagSlots, itemName, icon, remaining);
        remaining = AddToEmptySlot(bagSlots, itemName, icon, remaining);

        return remaining <= 0;
    }

    public void MoveBagToHotbar(int bagIndex)
    {
        if (bagIndex < 0 || bagIndex >= bagSlots.Length)
            return;

        InventorySlot bagSlot = bagSlots[bagIndex];

        if (bagSlot.IsEmpty)
            return;

        int emptyHotbarIndex = GetEmptyHotbarIndex();

        if (emptyHotbarIndex == -1)
        {
            Debug.Log("Thanh tay đã đầy.");
            return;
        }

        hotbarSlots[emptyHotbarIndex].SetItem(
            bagSlot.itemName,
            bagSlot.icon,
            bagSlot.amount
        );

        bagSlot.Clear();

        OnInventoryChanged?.Invoke();
    }

    private int GetEmptyHotbarIndex()
    {
        for (int i = 0; i < hotbarSlots.Length; i++)
        {
            if (hotbarSlots[i].IsEmpty)
                return i;
        }

        return -1;
    }

    public void RemoveSelectedItem(int amount)
    {
        InventorySlot slot = hotbarSlots[selectedHotbarIndex];

        if (slot.IsEmpty)
            return;

        slot.amount -= amount;

        if (slot.amount <= 0)
            slot.Clear();

        OnInventoryChanged?.Invoke();
    }
    public InventorySlot GetSlot(SlotArea area, int index)
{
    InventorySlot[] slots = area == SlotArea.Hotbar
        ? hotbarSlots
        : bagSlots;

    if (index < 0 || index >= slots.Length)
        return null;

    return slots[index];
}

public void SwapSlots(
    SlotArea fromArea,
    int fromIndex,
    SlotArea toArea,
    int toIndex)
{
    InventorySlot fromSlot = GetSlot(fromArea, fromIndex);
    InventorySlot toSlot = GetSlot(toArea, toIndex);

    if (fromSlot == null || toSlot == null)
        return;

    if (fromSlot.IsEmpty)
        return;

    // Nếu cùng loại item thì gộp stack
    if (!toSlot.IsEmpty && toSlot.itemName == fromSlot.itemName)
    {
        int canAdd = toSlot.maxStack - toSlot.amount;

        if (canAdd > 0)
        {
            int moveAmount = Mathf.Min(canAdd, fromSlot.amount);

            toSlot.amount += moveAmount;
            fromSlot.amount -= moveAmount;

            if (fromSlot.amount <= 0)
                fromSlot.Clear();

            OnInventoryChanged?.Invoke();
            return;
        }
    }

    // Nếu khác item thì đổi chỗ
    string tempName = toSlot.itemName;
    Sprite tempIcon = toSlot.icon;
    int tempAmount = toSlot.amount;
    int tempMaxStack = toSlot.maxStack;

    toSlot.itemName = fromSlot.itemName;
    toSlot.icon = fromSlot.icon;
    toSlot.amount = fromSlot.amount;
    toSlot.maxStack = fromSlot.maxStack;

    fromSlot.itemName = tempName;
    fromSlot.icon = tempIcon;
    fromSlot.amount = tempAmount;
    fromSlot.maxStack = tempMaxStack;

    OnInventoryChanged?.Invoke();
}
public void RefreshInventoryUI()
{
    OnInventoryChanged?.Invoke();
}
}