using UnityEngine;

public enum FishingPartType
{
    Reel,
    Line,
    Hook,
    Bait
}

[CreateAssetMenu(
    fileName = "FishingEquipment",
    menuName = "Fishing/Equipment Data"
)]
public class FishingEquipmentData : ScriptableObject
{
    [Header("Thông tin")]
    public string itemName;

    [TextArea(2, 5)]
    public string description;

    public Sprite icon;
    public FishingPartType partType;

    [Header("Hình xem trước cây cần câu")]
    public Sprite rodPreviewSprite;

    [Header("Yêu cầu")]
    [Min(0)]
    public int requiredSkill;

    [Header("Chỉ số cộng thêm")]
    public float power;
    public float speed;
    public float maxDepth;
    public float fishChanceBonus;

    [Header("Mồi câu")]
    public string baitName;
    public float baitChanceBonus;
}