using UnityEngine;

public enum FishingBaitType
{
    General,        // Mồi thông thường
    FreshWater,     // Nước ngọt
    SaltWater,      // Nước mặn
    Predator,       // Cá săn mồi
    DeepWater,      // Cá nước sâu
    Night,          // Cá hoạt động ban đêm
    Legendary       // Cá huyền thoại
}

[CreateAssetMenu(
    fileName = "FishingBait",
    menuName = "Fishing/Bait Data"
)]
public class FishingBaitData : ScriptableObject
{
    [Header("Thông tin")]
    public string baitName;
    public Sprite icon;

    [TextArea(2, 5)]
    public string description;

    [Header("Phân loại")]
    public FishingBaitType baitType = FishingBaitType.General;

    [Header("Cửa hàng")]
    [Min(0)]
    public int requiredLevel;

    [Min(0)]
    public int price = 2;

    [Min(1)]
    public int amountPerPurchase = 1;

    [Header("Hiệu quả câu cá")]
    [Range(0f, 100f)]
    public float fishChanceBonus = 5f;

    [Range(0f, 100f)]
    public float rareFishChanceBonus;

    [Header("Độ sâu hoạt động tốt")]
    [Min(0f)]
    public float preferredMinDepth;

    [Min(0f)]
    public float preferredMaxDepth = 10f;

    [Header("Sử dụng")]
    public bool consumeOnCast = true;
}