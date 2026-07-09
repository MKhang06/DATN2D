using System;
using System.Reflection;
using UnityEngine;

public class FishingGearStats : MonoBehaviour
{
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

    [Header("Bait / Mồi câu")]
    [SerializeField] private string currentBaitName = "Giun Đất";
    [SerializeField] private float baitFishChanceBonus = 15f;

    public string CurrentBaitName => currentBaitName;

    public float RodPower =>
        baseRodPower + (rodLevel - 1) * rodPowerPerLevel;

    public float MaxDepth =>
        baseMaxDepth + (lineLevel - 1) * depthPerLineLevel;

    public float LineStrength =>
        baseLineStrength + (lineLevel - 1) * lineStrengthPerLevel;

    public float ReelSpeed =>
        baseReelSpeed + (reelLevel - 1) * reelSpeedPerLevel;

    public float HookFishChanceBonus => hookFishChanceBonus;

    public bool HasBait =>
        !string.IsNullOrEmpty(currentBaitName);

    public float BaitFishChanceBonus =>
        HasBait ? baitFishChanceBonus : 0f;

    public bool TryConsumeBait()
    {
        if (!HasBait)
            return false;

        if (InventoryManager.Instance == null)
        {
            Debug.LogWarning("Không có InventoryManager, tạm cho dùng mồi.");
            return true;
        }

        object inventory = InventoryManager.Instance;
        Type type = inventory.GetType();

        MethodInfo removeMethod = type.GetMethod(
            "RemoveItem",
            new Type[] { typeof(string), typeof(int) }
        );

        if (removeMethod == null)
        {
            Debug.LogWarning("InventoryManager chưa có RemoveItem(string, int). Tạm cho dùng mồi.");
            return true;
        }

        object result = removeMethod.Invoke(
            inventory,
            new object[] { currentBaitName, 1 }
        );

        if (result is bool removed)
        {
            if (removed)
                Debug.Log("Đã trừ 1 mồi câu: " + currentBaitName);
            else
                Debug.Log("Không còn mồi câu: " + currentBaitName);

            return removed;
        }

        return true;
    }

    public void EquipBait(string baitName, float bonus)
    {
        currentBaitName = baitName;
        baitFishChanceBonus = bonus;
    }

    public void UpgradeRod()
    {
        rodLevel++;
    }

    public void UpgradeLine()
    {
        lineLevel++;
    }

    public void UpgradeReel()
    {
        reelLevel++;
    }

    public void UpgradeHook(float bonus)
    {
        hookLevel++;
        hookFishChanceBonus += bonus;
    }
}