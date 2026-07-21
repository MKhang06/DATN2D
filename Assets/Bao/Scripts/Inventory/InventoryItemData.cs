using UnityEngine;

public enum InventoryItemType
{
    Bait = 0,
    Fish = 1,
    Junk = 2,
    Equipment = 3,
    Other = 4
}

public enum InventoryStackMode
{
    Stackable = 0,
    Separate = 1
}

[CreateAssetMenu(
    fileName = "InventoryItem",
    menuName = "Inventory/Item Data"
)]
public class InventoryItemData : ScriptableObject
{
    [Header("Định danh")]
    [SerializeField]
    private string itemId;

    [SerializeField]
    private string displayName;

    [TextArea(2, 5)]
    [SerializeField]
    private string description;

    [Header("Hiển thị")]
    [SerializeField]
    private Sprite icon;

    [Header("Phân loại")]
    [SerializeField]
    private InventoryItemType itemType =
        InventoryItemType.Other;

    [SerializeField]
    private InventoryStackMode stackMode =
        InventoryStackMode.Stackable;

    [SerializeField, Min(1)]
    private int maxStack = 99;

    [Tooltip(
        "Bật cho trang bị chỉ được sở hữu một lần."
    )]
    [SerializeField]
    private bool uniqueOwnership;

    [Header("Giá")]
    [SerializeField, Min(0)]
    private int buyPrice;

    [SerializeField, Min(0)]
    private int sellPrice;

    public string ItemId => itemId;
    public string DisplayName => displayName;
    public string Description => description;
    public Sprite Icon => icon;

    public InventoryItemType ItemType => itemType;
    public InventoryStackMode StackMode => stackMode;

    public int MaxStack =>
        IsStackable
            ? Mathf.Max(1, maxStack)
            : 1;

    public bool UniqueOwnership => uniqueOwnership;
    public int BuyPrice => Mathf.Max(0, buyPrice);
    public int SellPrice => Mathf.Max(0, sellPrice);

    public bool IsStackable =>
        stackMode == InventoryStackMode.Stackable &&
        !uniqueOwnership;

    public bool IsValid =>
        !string.IsNullOrWhiteSpace(itemId) &&
        !string.IsNullOrWhiteSpace(displayName);

    private void OnValidate()
    {
        itemId =
            string.IsNullOrWhiteSpace(itemId)
                ? string.Empty
                : itemId.Trim().ToLowerInvariant();

        displayName =
            string.IsNullOrWhiteSpace(displayName)
                ? name
                : displayName.Trim();

        buyPrice = Mathf.Max(0, buyPrice);
        sellPrice = Mathf.Max(0, sellPrice);

        if (uniqueOwnership ||
            stackMode == InventoryStackMode.Separate)
        {
            maxStack = 1;
        }
        else
        {
            maxStack = Mathf.Max(1, maxStack);
        }
    }
}
