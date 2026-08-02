using UnityEngine;

public enum FishingRodPartSlotType
{
    Reel = 0,
    Line = 1,
    Hook = 2,
    Bait = 3
}

[CreateAssetMenu(
    fileName = "RodPart_",
    menuName = "Fishing/Rod Part Definition"
)]
public class FishingRodPartDefinition : ScriptableObject
{
    [Header("Vật phẩm trong balo")]
    [SerializeField] private InventoryItemData inventoryItem;

    [Header("Loại ô")]
    [SerializeField] private FishingRodPartSlotType slotType;

    [Header("Chỉ số")]
    [SerializeField, Min(0)] private int requiredSkill;
    [SerializeField] private float strengthBonus;
    [SerializeField] private float speedBonus;
    [SerializeField] private float maxDepthBonus;

    public InventoryItemData InventoryItem => inventoryItem;
    public FishingRodPartSlotType SlotType => slotType;
    public int RequiredSkill => Mathf.Max(0, requiredSkill);
    public float StrengthBonus => strengthBonus;
    public float SpeedBonus => speedBonus;
    public float MaxDepthBonus => maxDepthBonus;

    public string ItemName =>
        inventoryItem != null
            ? inventoryItem.DisplayName
            : name;

    public Sprite Icon =>
        inventoryItem != null
            ? inventoryItem.Icon
            : null;

    public string ItemId =>
        inventoryItem != null
            ? inventoryItem.ItemId
            : string.Empty;
}
