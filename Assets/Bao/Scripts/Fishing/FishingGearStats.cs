using System;
using System.Reflection;
using UnityEngine;

public class FishingGearStats : MonoBehaviour
{
    [Header("Fishing Skill / Kỹ năng câu")]
    [SerializeField] private int fishingSkillLevel = 0;

    [Header("Equipped Equipment / Phụ kiện đang trang bị")]
    [SerializeField] private FishingEquipmentData equippedReel;
    [SerializeField] private FishingEquipmentData equippedLine;
    [SerializeField] private FishingEquipmentData equippedHook;
    [SerializeField] private FishingEquipmentData equippedBait;

    [Header("Rod / Cần câu")]
    [SerializeField] private int rodLevel = 1;
    [SerializeField] private float baseRodPower = 1f;
    [SerializeField] private float rodPowerPerLevel = 0.25f;

    [Header("Line / Dây câu")]
    [SerializeField] private int lineLevel = 1;
    [SerializeField] private float baseMaxDepth = 10f;
    [SerializeField] private float depthPerLineLevel = 5f;
    [SerializeField] private float baseLineStrength = 1f;
    [SerializeField] private float lineStrengthPerLevel = 0.25f;

    [Header("Reel / Máy câu")]
    [SerializeField] private int reelLevel = 1;
    [SerializeField] private float baseReelSpeed = 1f;
    [SerializeField] private float reelSpeedPerLevel = 0.2f;

    [Header("Hook / Lưỡi câu")]
    [SerializeField] private int hookLevel = 1;
    [SerializeField] private float hookFishChanceBonus = 0f;

    [Header("Legacy Bait / Mồi mặc định")]
    [SerializeField] private string currentBaitName = "Giun Đất";
    [SerializeField] private float baitFishChanceBonus = 15f;

    public event Action OnGearChanged;

    public int FishingSkillLevel => fishingSkillLevel;
    public int RodLevel => rodLevel;
    public int LineLevel => lineLevel;
    public int ReelLevel => reelLevel;
    public int HookLevel => hookLevel;

    public string CurrentBaitName
    {
        get
        {
            if (equippedBait != null)
            {
                if (!string.IsNullOrEmpty(equippedBait.baitName))
                    return equippedBait.baitName;

                return equippedBait.itemName;
            }

            return currentBaitName;
        }
    }

    public float RodPower
    {
        get
        {
            float levelPower =
                baseRodPower +
                (rodLevel - 1) * rodPowerPerLevel;

            return levelPower +
                   GetPower(equippedReel) +
                   GetPower(equippedHook);
        }
    }

    public float MaxDepth
    {
        get
        {
            float levelDepth =
                baseMaxDepth +
                (lineLevel - 1) * depthPerLineLevel;

            return Mathf.Max(
                0f,
                levelDepth +
                GetMaxDepth(equippedReel) +
                GetMaxDepth(equippedLine) +
                GetMaxDepth(equippedHook) +
                GetMaxDepth(equippedBait)
            );
        }
    }

    public float LineStrength
    {
        get
        {
            float levelStrength =
                baseLineStrength +
                (lineLevel - 1) * lineStrengthPerLevel;

            return levelStrength + GetPower(equippedLine);
        }
    }

    public float ReelSpeed
    {
        get
        {
            float levelSpeed =
                baseReelSpeed +
                (reelLevel - 1) * reelSpeedPerLevel;

            return levelSpeed +
                   GetSpeed(equippedReel) +
                   GetSpeed(equippedLine) +
                   GetSpeed(equippedHook) +
                   GetSpeed(equippedBait);
        }
    }

    public float HookFishChanceBonus
    {
        get
        {
            return hookFishChanceBonus +
                   GetFishChance(equippedReel) +
                   GetFishChance(equippedLine) +
                   GetFishChance(equippedHook);
        }
    }

    public bool HasBait =>
        !string.IsNullOrEmpty(CurrentBaitName);

    public float BaitFishChanceBonus
    {
        get
        {
            if (!HasBait)
                return 0f;

            if (equippedBait != null)
            {
                return equippedBait.baitChanceBonus +
                       equippedBait.fishChanceBonus;
            }

            return baitFishChanceBonus;
        }
    }

    public float TotalPower =>
        RodPower + LineStrength;

    public float TotalSpeed =>
        ReelSpeed;

    public int RequiredSkill
    {
        get
        {
            int required = 0;

            required = Mathf.Max(
                required,
                GetRequiredSkill(equippedReel)
            );

            required = Mathf.Max(
                required,
                GetRequiredSkill(equippedLine)
            );

            required = Mathf.Max(
                required,
                GetRequiredSkill(equippedHook)
            );

            required = Mathf.Max(
                required,
                GetRequiredSkill(equippedBait)
            );

            return required;
        }
    }

    public FishingEquipmentData GetEquipped(
        FishingPartType type)
    {
        switch (type)
        {
            case FishingPartType.Reel:
                return equippedReel;

            case FishingPartType.Line:
                return equippedLine;

            case FishingPartType.Hook:
                return equippedHook;

            case FishingPartType.Bait:
                return equippedBait;

            default:
                return null;
        }
    }

    public bool CanEquip(FishingEquipmentData equipment)
    {
        if (equipment == null)
            return false;

        return fishingSkillLevel >= equipment.requiredSkill;
    }

    public bool TryEquip(FishingEquipmentData equipment)
    {
        if (equipment == null)
        {
            Debug.LogWarning("Equipment bị null.");
            return false;
        }

        if (!CanEquip(equipment))
        {
            Debug.LogWarning(
                "Chưa đủ kỹ năng để trang bị " +
                equipment.itemName +
                ". Yêu cầu cấp " +
                equipment.requiredSkill
            );

            return false;
        }

        switch (equipment.partType)
        {
            case FishingPartType.Reel:
                equippedReel = equipment;
                break;

            case FishingPartType.Line:
                equippedLine = equipment;
                break;

            case FishingPartType.Hook:
                equippedHook = equipment;
                break;

            case FishingPartType.Bait:
                equippedBait = equipment;

                currentBaitName =
                    !string.IsNullOrEmpty(equipment.baitName)
                        ? equipment.baitName
                        : equipment.itemName;

                baitFishChanceBonus =
                    equipment.baitChanceBonus;
                break;

            default:
                return false;
        }

        Debug.Log("Đã trang bị: " + equipment.itemName);

        NotifyGearChanged();
        return true;
    }

    public void Unequip(FishingPartType type)
    {
        switch (type)
        {
            case FishingPartType.Reel:
                equippedReel = null;
                break;

            case FishingPartType.Line:
                equippedLine = null;
                break;

            case FishingPartType.Hook:
                equippedHook = null;
                break;

            case FishingPartType.Bait:
                equippedBait = null;
                currentBaitName = "";
                baitFishChanceBonus = 0f;
                break;
        }

        NotifyGearChanged();
    }

    public bool TryConsumeBait()
    {
        if (!HasBait)
        {
            Debug.Log("Chưa trang bị mồi câu.");
            return false;
        }

        if (InventoryManager.Instance == null)
        {
            Debug.LogWarning(
                "Không có InventoryManager, tạm cho dùng mồi."
            );

            return true;
        }

        object inventory = InventoryManager.Instance;
        Type inventoryType = inventory.GetType();

        MethodInfo removeMethod =
            inventoryType.GetMethod(
                "RemoveItem",
                new Type[]
                {
                    typeof(string),
                    typeof(int)
                }
            );

        if (removeMethod == null)
        {
            Debug.LogWarning(
                "InventoryManager chưa có " +
                "RemoveItem(string, int). Tạm cho dùng mồi."
            );

            return true;
        }

        object result = removeMethod.Invoke(
            inventory,
            new object[]
            {
                CurrentBaitName,
                1
            }
        );

        if (result is bool removed)
        {
            if (removed)
            {
                Debug.Log(
                    "Đã trừ 1 mồi câu: " +
                    CurrentBaitName
                );
            }
            else
            {
                Debug.Log(
                    "Không còn mồi câu: " +
                    CurrentBaitName
                );
            }

            return removed;
        }

        return true;
    }

    public void EquipBait(string baitName, float bonus)
    {
        equippedBait = null;

        currentBaitName = baitName;
        baitFishChanceBonus = bonus;

        NotifyGearChanged();
    }

    public void SetFishingSkillLevel(int level)
    {
        fishingSkillLevel = Mathf.Max(0, level);
        NotifyGearChanged();
    }

    public void AddFishingSkillLevel(int amount)
    {
        if (amount <= 0)
            return;

        fishingSkillLevel += amount;
        NotifyGearChanged();
    }

    public void UpgradeRod()
    {
        rodLevel++;
        NotifyGearChanged();
    }

    public void UpgradeLine()
    {
        lineLevel++;
        NotifyGearChanged();
    }

    public void UpgradeReel()
    {
        reelLevel++;
        NotifyGearChanged();
    }

    public void UpgradeHook(float bonus)
    {
        hookLevel++;
        hookFishChanceBonus += bonus;

        NotifyGearChanged();
    }

    private void NotifyGearChanged()
    {
        OnGearChanged?.Invoke();
    }

    private float GetPower(FishingEquipmentData equipment)
    {
        return equipment != null
            ? equipment.power
            : 0f;
    }

    private float GetSpeed(FishingEquipmentData equipment)
    {
        return equipment != null
            ? equipment.speed
            : 0f;
    }

    private float GetMaxDepth(
        FishingEquipmentData equipment)
    {
        return equipment != null
            ? equipment.maxDepth
            : 0f;
    }

    private float GetFishChance(
        FishingEquipmentData equipment)
    {
        return equipment != null
            ? equipment.fishChanceBonus
            : 0f;
    }

    private int GetRequiredSkill(
        FishingEquipmentData equipment)
    {
        return equipment != null
            ? equipment.requiredSkill
            : 0;
    }
}