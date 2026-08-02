using System;
using System.Reflection;
using UnityEngine;

[CreateAssetMenu(
    fileName = "ToolShopProduct",
    menuName = "Shop/Tool Shop Product"
)]
public class ToolShopItemData : ScriptableObject
{
    [Header("Inventory Item")]
    public InventoryItemData inventoryItem;

    [Header("Shop")]
    public string category = "Dụng cụ";

    [Tooltip("-1: dùng Buy Price trong InventoryItemData.")]
    public int priceOverride = -1;

    [Min(1)]
    public int maxPurchaseQuantity = 1;

    public string DisplayName =>
        InventoryItemDataAccess.GetDisplayName(inventoryItem);

    public string Description =>
        InventoryItemDataAccess.GetDescription(inventoryItem);

    public Sprite Icon =>
        InventoryItemDataAccess.GetIcon(inventoryItem);

    public string ItemId =>
        InventoryItemDataAccess.GetItemId(inventoryItem);

    public bool UniqueOwnership =>
        InventoryItemDataAccess.GetUniqueOwnership(inventoryItem);

    public int MaxStack =>
        InventoryItemDataAccess.GetMaxStack(inventoryItem);

    public int Price =>
        priceOverride >= 0
            ? priceOverride
            : InventoryItemDataAccess.GetBuyPrice(inventoryItem);
}

public static class InventoryItemDataAccess
{
    private static readonly BindingFlags Flags =
        BindingFlags.Instance |
        BindingFlags.Public |
        BindingFlags.NonPublic |
        BindingFlags.IgnoreCase;

    public static string GetItemId(InventoryItemData item) =>
        GetValue(item, string.Empty, "itemId", "id", "ItemId");

    public static string GetDisplayName(InventoryItemData item)
    {
        string value = GetValue(
            item,
            string.Empty,
            "displayName",
            "itemName",
            "DisplayName"
        );

        return string.IsNullOrWhiteSpace(value) && item != null
            ? item.name
            : value;
    }

    public static string GetDescription(InventoryItemData item) =>
        GetValue(item, string.Empty, "description", "Description");

    public static Sprite GetIcon(InventoryItemData item) =>
        GetValue<Sprite>(item, null, "icon", "Icon", "itemIcon");

    public static bool GetUniqueOwnership(InventoryItemData item) =>
        GetValue(item, false, "uniqueOwnership", "isUnique", "UniqueOwnership");

    public static int GetMaxStack(InventoryItemData item) =>
        Mathf.Max(1, GetValue(item, 1, "maxStack", "MaxStack", "stackLimit"));

    public static int GetBuyPrice(InventoryItemData item) =>
        Mathf.Max(0, GetValue(item, 0, "buyPrice", "BuyPrice", "price"));

    private static T GetValue<T>(
        object source,
        T fallback,
        params string[] names)
    {
        if (source == null)
            return fallback;

        Type type = source.GetType();

        foreach (string name in names)
        {
            FieldInfo field = type.GetField(name, Flags);

            if (field != null)
            {
                object value = field.GetValue(source);

                if (value is T typed)
                    return typed;
            }

            PropertyInfo property = type.GetProperty(name, Flags);

            if (property != null && property.CanRead)
            {
                object value = property.GetValue(source);

                if (value is T typed)
                    return typed;
            }
        }

        return fallback;
    }
}
