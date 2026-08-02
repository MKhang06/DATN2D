using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

public class FishingGearStats : MonoBehaviour
{
    [Header("Fishing Skill / Kỹ năng câu")]
    [SerializeField]
    private int fishingSkillLevel;

    [Header("Inventory")]
    [Tooltip(
        "Phải là InventoryManager mà Shop đang thêm vật phẩm vào."
    )]
    [SerializeField]
    private InventoryManager inventoryManager;

    [Header("Equipped Equipment / Phụ kiện đang trang bị")]
    [SerializeField]
    private FishingEquipmentData equippedReel;

    [SerializeField]
    private FishingEquipmentData equippedLine;

    [SerializeField]
    private FishingEquipmentData equippedHook;

    [SerializeField]
    private FishingEquipmentData equippedBait;

    [Header("Rod / Cần câu")]
    [SerializeField]
    private int rodLevel = 1;

    [SerializeField]
    private float baseRodPower = 1f;

    [SerializeField]
    private float rodPowerPerLevel = 0.25f;

    [Header("Line / Dây câu")]
    [SerializeField]
    private int lineLevel = 1;

    [SerializeField]
    private float baseMaxDepth = 10f;

    [SerializeField]
    private float depthPerLineLevel = 5f;

    [SerializeField]
    private float baseLineStrength = 1f;

    [SerializeField]
    private float lineStrengthPerLevel = 0.25f;

    [Header("Reel / Máy câu")]
    [SerializeField]
    private int reelLevel = 1;

    [SerializeField]
    private float baseReelSpeed = 1f;

    [SerializeField]
    private float reelSpeedPerLevel = 0.2f;

    [Header("Hook / Lưỡi câu")]
    [SerializeField]
    private int hookLevel = 1;

    [SerializeField]
    private float hookFishChanceBonus;

    [Header("Legacy Bait / Mồi mặc định")]
    [Tooltip(
        "Để trống để người chơi phải mua và trang bị mồi."
    )]
    [SerializeField]
    private string currentBaitName = "";

    [SerializeField]
    private float baitFishChanceBonus;

    [Header("Trang bị cơ bản")]
    [Tooltip(
        "Ba món cơ bản vẫn phải mua, nhưng không yêu cầu kỹ năng."
    )]
    [SerializeField]
    private bool basicEquipmentIgnoresSkill = true;

    [Header("Debug")]
    [SerializeField]
    private bool showDebugLogs = true;

    /*
     * FishingEquipmentData và Shop Item là hai asset khác nhau.
     * Inventory lưu Shop ItemName, nên cần map EquipmentData
     * sang đúng tên đã được Shop thêm vào Inventory.
     */
    private readonly Dictionary<
        FishingEquipmentData,
        string
    > ownershipNameMap =
        new Dictionary<
            FishingEquipmentData,
            string
        >();

    /*
     * Liên kết chính xác EquipmentData -> InventoryItemData.
     * Không còn phụ thuộc việc tên hoặc icon giống nhau.
     */
    private readonly Dictionary<
        FishingEquipmentData,
        InventoryItemData
    > ownershipItemMap =
        new Dictionary<
            FishingEquipmentData,
            InventoryItemData
        >();

    public event Action OnGearChanged;

    public int FishingSkillLevel =>
        fishingSkillLevel;

    public int RodLevel =>
        rodLevel;

    public int LineLevel =>
        lineLevel;

    public int ReelLevel =>
        reelLevel;

    public int HookLevel =>
        hookLevel;

    public string CurrentBaitName
    {
        get
        {
            if (equippedBait != null)
            {
                if (!string.IsNullOrEmpty(
                        equippedBait.baitName))
                {
                    return equippedBait.baitName;
                }

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
                (rodLevel - 1) *
                rodPowerPerLevel;

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
                (lineLevel - 1) *
                depthPerLineLevel;

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
                (lineLevel - 1) *
                lineStrengthPerLevel;

            return levelStrength +
                   GetPower(equippedLine);
        }
    }

    public float ReelSpeed
    {
        get
        {
            float levelSpeed =
                baseReelSpeed +
                (reelLevel - 1) *
                reelSpeedPerLevel;

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
        !string.IsNullOrEmpty(
            CurrentBaitName
        );

    public float BaitFishChanceBonus
    {
        get
        {
            if (!HasBait)
                return 0f;

            if (equippedBait != null)
            {
                return
                    equippedBait.baitChanceBonus +
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

            required =
                Mathf.Max(
                    required,
                    GetRequiredSkill(
                        equippedReel)
                );

            required =
                Mathf.Max(
                    required,
                    GetRequiredSkill(
                        equippedLine)
                );

            required =
                Mathf.Max(
                    required,
                    GetRequiredSkill(
                        equippedHook)
                );

            required =
                Mathf.Max(
                    required,
                    GetRequiredSkill(
                        equippedBait)
                );

            return required;
        }
    }

    private void Awake()
    {
        ResolveInventory();
        ValidateEquippedParts();
    }

    private void OnEnable()
    {
        ResolveInventory();

        if (inventoryManager != null)
        {
            inventoryManager
                .OnInventoryChanged -=
                    HandleInventoryChanged;

            inventoryManager
                .OnInventoryChanged +=
                    HandleInventoryChanged;
        }

        ValidateEquippedParts();
    }

    private void OnDisable()
    {
        if (inventoryManager != null)
        {
            inventoryManager
                .OnInventoryChanged -=
                    HandleInventoryChanged;
        }
    }

    private void ResolveInventory()
    {
        if (inventoryManager != null)
            return;

        inventoryManager =
            InventoryManager.Instance;

        if (inventoryManager == null)
        {
            inventoryManager =
                FindFirstObjectByType<
                    InventoryManager
                >();
        }
    }

    private void HandleInventoryChanged()
    {
        ValidateEquippedParts();
        NotifyGearChanged();
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

    public void BindInventory(
        InventoryManager manager)
    {
        if (manager == null)
            return;

        if (inventoryManager == manager)
            return;

        if (inventoryManager != null)
        {
            inventoryManager
                .OnInventoryChanged -=
                    HandleInventoryChanged;
        }

        inventoryManager = manager;

        inventoryManager
            .OnInventoryChanged -=
                HandleInventoryChanged;

        inventoryManager
            .OnInventoryChanged +=
                HandleInventoryChanged;

        ValidateEquippedParts();
    }

    public void RegisterOwnershipItem(
        FishingEquipmentData equipment,
        InventoryItemData inventoryItem)
    {
        if (equipment == null ||
            inventoryItem == null)
        {
            return;
        }

        ownershipItemMap[equipment] =
            inventoryItem;

        RegisterOwnershipName(
            equipment,
            inventoryItem.DisplayName
        );
    }

    public void RegisterOwnershipName(
        FishingEquipmentData equipment,
        string inventoryItemName)
    {
        if (equipment == null ||
            string.IsNullOrWhiteSpace(
                inventoryItemName))
        {
            return;
        }

        ownershipNameMap[equipment] =
            inventoryItemName.Trim();

        if (showDebugLogs)
        {
            Debug.Log(
                "[FishingGearStats] Ownership link: " +
                equipment.itemName +
                " -> " +
                inventoryItemName,
                equipment
            );
        }
    }

    public InventoryItemData GetOwnershipItem(
        FishingEquipmentData equipment)
    {
        if (equipment == null)
            return null;

        if (ownershipItemMap.TryGetValue(
                equipment,
                out InventoryItemData item))
        {
            return item;
        }

        return null;
    }

    public string GetOwnershipName(
        FishingEquipmentData equipment)
    {
        if (equipment == null)
            return string.Empty;

        if (ownershipNameMap.TryGetValue(
                equipment,
                out string mappedName) &&
            !string.IsNullOrWhiteSpace(
                mappedName))
        {
            return mappedName;
        }

        return equipment.itemName;
    }

    public bool CanEquip(
        FishingEquipmentData equipment)
    {
        return CanEquip(
            equipment,
            out _
        );
    }

    public bool CanEquip(
        FishingEquipmentData equipment,
        out string reason)
    {
        ResolveInventory();

        reason = string.Empty;

        if (equipment == null)
        {
            reason =
                "Dữ liệu trang bị bị null.";

            return false;
        }

        if (string.IsNullOrWhiteSpace(
                equipment.itemName))
        {
            reason =
                "Trang bị chưa có Item Name.";

            return false;
        }

        InventoryItemData ownershipItem =
            GetOwnershipItem(
                equipment
            );

        string ownershipName =
            GetOwnershipName(
                equipment
            );

        bool isOwned =
            ownershipItem != null
                ? inventoryManager != null &&
                  inventoryManager.HasItem(
                      ownershipItem
                  )
                : HasOwnedEquipment(
                    ownershipName
                  );

        if (!isOwned)
        {
            reason =
                "Bạn chưa sở hữu " +
                ownershipName +
                " trong túi đồ.";

            return false;
        }

        if (basicEquipmentIgnoresSkill &&
            IsBasicEquipment(equipment))
        {
            return true;
        }

        if (fishingSkillLevel <
            equipment.requiredSkill)
        {
            reason =
                "Chưa đủ kỹ năng để trang bị " +
                equipment.itemName +
                ". Yêu cầu cấp " +
                equipment.requiredSkill +
                ", hiện tại cấp " +
                fishingSkillLevel +
                ".";

            return false;
        }

        return true;
    }

    public bool TryEquip(
        FishingEquipmentData equipment)
    {
        if (!CanEquip(
                equipment,
                out string reason))
        {
            Debug.LogWarning(
                "[FishingGearStats] " +
                reason,
                this
            );

            return false;
        }

        FishingPartType resolvedType =
            ResolvePartType(equipment);

        /*
         * Sửa asset cũ bị gán tất cả thành Reel.
         */
        if (equipment.partType !=
            resolvedType)
        {
            if (showDebugLogs)
            {
                Debug.LogWarning(
                    "[FishingGearStats] Đã sửa Part Type của " +
                    equipment.itemName +
                    ": " +
                    equipment.partType +
                    " -> " +
                    resolvedType,
                    equipment
                );
            }

            equipment.partType =
                resolvedType;
        }

        switch (resolvedType)
        {
            case FishingPartType.Reel:
                equippedReel =
                    equipment;
                break;

            case FishingPartType.Line:
                equippedLine =
                    equipment;
                break;

            case FishingPartType.Hook:
                equippedHook =
                    equipment;
                break;

            case FishingPartType.Bait:
                equippedBait =
                    equipment;

                currentBaitName =
                    !string.IsNullOrEmpty(
                        equipment.baitName)
                        ? equipment.baitName
                        : equipment.itemName;

                baitFishChanceBonus =
                    equipment.baitChanceBonus;
                break;

            default:
                Debug.LogWarning(
                    "[FishingGearStats] Không xác định được loại của " +
                    equipment.itemName +
                    ".",
                    this
                );

                return false;
        }

        Debug.Log(
            "[FishingGearStats] Đã trang bị " +
            equipment.itemName +
            " vào ô " +
            resolvedType +
            ".",
            this
        );

        NotifyGearChanged();
        return true;
    }

    public void Unequip(
        FishingPartType type)
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
                currentBaitName =
                    string.Empty;

                baitFishChanceBonus = 0f;
                break;
        }

        NotifyGearChanged();
    }

    private void ValidateEquippedParts()
    {
        ResolveInventory();

        bool changed = false;

        changed |= ValidateEquipped(
            ref equippedReel,
            FishingPartType.Reel
        );

        changed |= ValidateEquipped(
            ref equippedLine,
            FishingPartType.Line
        );

        changed |= ValidateEquipped(
            ref equippedHook,
            FishingPartType.Hook
        );

        changed |= ValidateEquipped(
            ref equippedBait,
            FishingPartType.Bait
        );

        if (equippedBait == null)
        {
            currentBaitName =
                string.Empty;

            baitFishChanceBonus = 0f;
        }

        if (changed)
            NotifyGearChanged();
    }

    private bool ValidateEquipped(
        ref FishingEquipmentData equipment,
        FishingPartType expectedType)
    {
        if (equipment == null)
            return false;

        InventoryItemData ownershipItem =
            GetOwnershipItem(
                equipment
            );

        string ownershipName =
            GetOwnershipName(
                equipment
            );

        bool isOwned =
            ownershipItem != null
                ? inventoryManager != null &&
                  inventoryManager.HasItem(
                      ownershipItem
                  )
                : HasOwnedEquipment(
                    ownershipName
                  );

        if (!isOwned)
        {
            if (showDebugLogs)
            {
                Debug.LogWarning(
                    "[FishingGearStats] Đã tự gỡ " +
                    equipment.itemName +
                    " vì Inventory không còn " +
                    ownershipName +
                    ".",
                    this
                );
            }

            equipment = null;
            return true;
        }

        FishingPartType resolved =
            ResolvePartType(equipment);

        if (resolved != expectedType)
        {
            if (showDebugLogs)
            {
                Debug.LogWarning(
                    "[FishingGearStats] Đã tự gỡ " +
                    equipment.itemName +
                    " vì đang nằm sai ô. Đúng phải là " +
                    resolved +
                    ".",
                    this
                );
            }

            equipment = null;
            return true;
        }

        return false;
    }

    public bool HasOwnedEquipment(
        string itemName)
    {
        ResolveInventory();

        if (inventoryManager == null ||
            string.IsNullOrWhiteSpace(
                itemName))
        {
            return false;
        }

        return HasName(
                   inventoryManager
                       .HotbarSlots,
                   itemName
               ) ||
               HasName(
                   inventoryManager
                       .BagSlots,
                   itemName
               );
    }

    private static bool HasName(
        InventoryManager.InventorySlot[] slots,
        string itemName)
    {
        if (slots == null)
            return false;

        string expected =
            NormalizeName(itemName);

        foreach (
            InventoryManager.InventorySlot slot
            in slots)
        {
            if (slot == null ||
                slot.IsEmpty)
            {
                continue;
            }

            if (NormalizeName(
                    slot.itemName) ==
                expected)
            {
                return true;
            }
        }

        return false;
    }

    private static FishingPartType
        ResolvePartType(
            FishingEquipmentData equipment)
    {
        if (equipment == null)
            return FishingPartType.Reel;

        string normalized =
            NormalizeName(
                equipment.itemName
            );

        if (normalized.Contains(
                "callistoxsr") ||
            normalized.Contains(
                "maycau") ||
            normalized.Contains(
                "reel"))
        {
            return FishingPartType.Reel;
        }

        if (normalized.StartsWith(
                "day") ||
            normalized.Contains(
                "line") ||
            normalized.Contains(
                "mono") ||
            normalized.Contains(
                "noodle"))
        {
            return FishingPartType.Line;
        }

        if (normalized.StartsWith(
                "moccau") ||
            normalized.Contains(
                "hook"))
        {
            return FishingPartType.Hook;
        }

        if (normalized.Contains(
                "bait") ||
            normalized.Contains(
                "moicau") ||
            !string.IsNullOrWhiteSpace(
                equipment.baitName))
        {
            return FishingPartType.Bait;
        }

        /*
         * Không nhận diện được tên thì dùng PartType trong asset.
         */
        return equipment.partType;
    }

    private static bool IsBasicEquipment(
        FishingEquipmentData equipment)
    {
        if (equipment == null)
            return false;

        string normalized =
            NormalizeName(
                equipment.itemName
            );

        return
            normalized == "callistoxsr" ||
            normalized == "daydonretien" ||
            normalized == "moccau1";
    }

    private static string NormalizeName(
        string value)
    {
        if (string.IsNullOrWhiteSpace(
                value))
        {
            return string.Empty;
        }

        string decomposed =
            value.Trim()
                .ToLowerInvariant()
                .Normalize(
                    NormalizationForm.FormD
                );

        StringBuilder builder =
            new StringBuilder();

        foreach (char character
                 in decomposed)
        {
            UnicodeCategory category =
                CharUnicodeInfo
                    .GetUnicodeCategory(
                        character
                    );

            if (category ==
                UnicodeCategory
                    .NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetterOrDigit(
                    character))
            {
                builder.Append(
                    character
                );
            }
        }

        return builder
            .ToString()
            .Normalize(
                NormalizationForm.FormC
            );
    }

    public bool TryConsumeBait()
    {
        if (!HasBait)
        {
            Debug.Log(
                "Chưa trang bị mồi câu."
            );

            return false;
        }

        ResolveInventory();

        if (inventoryManager == null)
        {
            Debug.LogWarning(
                "Không có InventoryManager."
            );

            return false;
        }

        bool removed =
            RemoveOneByName(
                inventoryManager
                    .BagSlots,
                CurrentBaitName
            );

        if (!removed)
        {
            removed =
                RemoveOneByName(
                    inventoryManager
                        .HotbarSlots,
                    CurrentBaitName
                );
        }

        if (!removed)
        {
            Debug.Log(
                "Không còn mồi câu: " +
                CurrentBaitName
            );

            equippedBait = null;
            currentBaitName =
                string.Empty;

            baitFishChanceBonus = 0f;

            NotifyGearChanged();
            return false;
        }

        inventoryManager
            .RefreshInventoryUI();

        if (!HasOwnedEquipment(
                CurrentBaitName))
        {
            equippedBait = null;
            currentBaitName =
                string.Empty;

            baitFishChanceBonus = 0f;
        }

        NotifyGearChanged();
        return true;
    }

    private static bool RemoveOneByName(
        InventoryManager.InventorySlot[] slots,
        string itemName)
    {
        if (slots == null)
            return false;

        string expected =
            NormalizeName(itemName);

        for (int i =
                 slots.Length - 1;
             i >= 0;
             i--)
        {
            InventoryManager.InventorySlot slot =
                slots[i];

            if (slot == null ||
                slot.IsEmpty ||
                NormalizeName(
                    slot.itemName) !=
                expected)
            {
                continue;
            }

            slot.amount--;

            if (slot.amount <= 0)
                slot.Clear();

            return true;
        }

        return false;
    }

    public void EquipBait(
        string baitName,
        float bonus)
    {
        equippedBait = null;
        currentBaitName = baitName;
        baitFishChanceBonus = bonus;
        NotifyGearChanged();
    }

    public void SetFishingSkillLevel(
        int level)
    {
        fishingSkillLevel =
            Mathf.Max(0, level);

        NotifyGearChanged();
    }

    public void AddFishingSkillLevel(
        int amount)
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

    public void UpgradeHook(
        float bonus)
    {
        hookLevel++;
        hookFishChanceBonus += bonus;
        NotifyGearChanged();
    }

    private void NotifyGearChanged()
    {
        OnGearChanged?.Invoke();
    }

    private static float GetPower(
        FishingEquipmentData equipment)
    {
        return equipment != null
            ? equipment.power
            : 0f;
    }

    private static float GetSpeed(
        FishingEquipmentData equipment)
    {
        return equipment != null
            ? equipment.speed
            : 0f;
    }

    private static float GetMaxDepth(
        FishingEquipmentData equipment)
    {
        return equipment != null
            ? equipment.maxDepth
            : 0f;
    }

    private static float GetFishChance(
        FishingEquipmentData equipment)
    {
        return equipment != null
            ? equipment.fishChanceBonus
            : 0f;
    }

    private int GetRequiredSkill(
        FishingEquipmentData equipment)
    {
        if (equipment == null)
            return 0;

        if (basicEquipmentIgnoresSkill &&
            IsBasicEquipment(equipment))
        {
            return 0;
        }

        return equipment.requiredSkill;
    }

    [ContextMenu(
        "TEST - Print Equipped And Ownership"
    )]
    private void PrintEquippedAndOwnership()
    {
        ResolveInventory();

        PrintSlot(
            FishingPartType.Reel,
            equippedReel
        );

        PrintSlot(
            FishingPartType.Line,
            equippedLine
        );

        PrintSlot(
            FishingPartType.Hook,
            equippedHook
        );

        PrintSlot(
            FishingPartType.Bait,
            equippedBait
        );
    }

    private void PrintSlot(
        FishingPartType type,
        FishingEquipmentData equipment)
    {
        Debug.Log(
            "[FishingGearStats] " +
            type +
            " | Equipped=" +
            (equipment != null
                ? equipment.itemName
                : "NULL") +
            " | InventoryName=" +
            (equipment != null
                ? GetOwnershipName(
                    equipment)
                : "NULL") +
            " | Owned=" +
            (equipment != null &&
             HasOwnedEquipment(
                 GetOwnershipName(
                     equipment))) +
            " | Skill=" +
            fishingSkillLevel +
            " | Required=" +
            (equipment != null
                ? equipment.requiredSkill
                : 0),
            this
        );
    }
}
