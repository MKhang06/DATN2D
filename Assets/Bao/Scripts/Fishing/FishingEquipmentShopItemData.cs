using UnityEngine;

[CreateAssetMenu(
    fileName = "EquipmentShopItem",
    menuName = "Fishing/Equipment Shop Item"
)]
public class FishingEquipmentShopItemData : ScriptableObject
{
    [Header("Trang bị được bán")]
    [SerializeField] public FishingEquipmentData equipmentData;

    [Header("Thông tin cửa hàng")]
    [Min(0)]
    public int price = 100;

    [Min(1)]
    public int amountPerPurchase = 1;

    [Min(0)]
    public int requiredPlayerLevel = 0;

    public string ItemName
    {
        get
        {
            if (equipmentData == null)
                return name;

            if (string.IsNullOrWhiteSpace(equipmentData.itemName))
                return name;

            return equipmentData.itemName;
        }
    }

    public Sprite Icon
    {
        get
        {
            if (equipmentData == null)
                return null;

            return equipmentData.icon;
        }
    }
}