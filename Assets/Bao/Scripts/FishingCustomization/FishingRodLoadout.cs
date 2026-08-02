using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class FishingRodLoadout : MonoBehaviour
{
    [Header("Túi đồ")]
    [SerializeField] private InventoryManager inventoryManager;

    [Header("Các bộ phận có thể sử dụng")]
    [SerializeField]
    private FishingRodPartDefinition[] partDefinitions;

    [Header("Cần câu cơ bản")]
    [SerializeField] private string rodDisplayName = "CẦN CÂU CƠ BẢN";
    [SerializeField] private Sprite rodIcon;
    [SerializeField, Min(0)] private int baseRequiredSkill;
    [SerializeField] private float baseStrength = 2f;
    [SerializeField] private float baseSpeed = 1f;
    [SerializeField] private float baseMaxDepth = 10f;

    [Header("Lưu trang bị")]
    [SerializeField] private bool saveWithPlayerPrefs = true;
    [SerializeField] private string saveKeyPrefix = "FishingRodLoadout";

    [Header("Đang trang bị")]
    [SerializeField] private FishingRodPartDefinition equippedReel;
    [SerializeField] private FishingRodPartDefinition equippedLine;
    [SerializeField] private FishingRodPartDefinition equippedHook;
    [SerializeField] private FishingRodPartDefinition equippedBait;

    public event Action OnLoadoutChanged;

    public InventoryManager Inventory => inventoryManager;

    public IReadOnlyList<FishingRodPartDefinition> PartDefinitions =>
        partDefinitions ?? Array.Empty<FishingRodPartDefinition>();

    public string RodDisplayName =>
        string.IsNullOrWhiteSpace(rodDisplayName)
            ? "CẦN CÂU CƠ BẢN"
            : rodDisplayName;

    public Sprite RodIcon => rodIcon;

    public int EquippedCount =>
        (equippedReel != null ? 1 : 0) +
        (equippedLine != null ? 1 : 0) +
        (equippedHook != null ? 1 : 0) +
        (equippedBait != null ? 1 : 0);

    public int RequiredSkill
    {
        get
        {
            int result = Mathf.Max(0, baseRequiredSkill);

            foreach (FishingRodPartDefinition part in GetEquippedParts())
            {
                if (part != null)
                    result = Mathf.Max(result, part.RequiredSkill);
            }

            return result;
        }
    }

    public float TotalStrength =>
        baseStrength + SumEquipped(part => part.StrengthBonus);

    public float Speed =>
        baseSpeed + SumEquipped(part => part.SpeedBonus);

    public float MaxDepth =>
        Mathf.Max(
            0f,
            baseMaxDepth +
            SumEquipped(part => part.MaxDepthBonus)
        );

    // Tên tương thích với các FishingManager cũ.
    public float RodPower => TotalStrength;
    public float ReelSpeed => Speed;
    public float LineStrength =>
        baseStrength +
        (equippedLine != null
            ? equippedLine.StrengthBonus
            : 0f);

    public bool HasBait =>
        equippedBait != null &&
        GetOwnedAmount(equippedBait) > 0;

    public string CurrentBaitName =>
        HasBait
            ? equippedBait.ItemName
            : string.Empty;

    public int BaitAmount =>
        equippedBait != null
            ? GetOwnedAmount(equippedBait)
            : 0;

    private void Awake()
    {
        ResolveInventory();
        LoadEquippedParts();
        ValidateEquippedParts();
    }

    private void OnEnable()
    {
        ResolveInventory();
        BindInventory();
        ValidateEquippedParts();
        OnLoadoutChanged?.Invoke();
    }

    private void Start()
    {
        ResolveInventory();
        BindInventory();
        ValidateEquippedParts();
        OnLoadoutChanged?.Invoke();
    }

    private void OnDisable()
    {
        if (inventoryManager != null)
        {
            inventoryManager.OnInventoryChanged -=
                HandleInventoryChanged;
        }
    }

    private void ResolveInventory()
    {
        if (inventoryManager != null)
            return;

        inventoryManager = InventoryManager.Instance;

        if (inventoryManager == null)
        {
            inventoryManager =
                FindFirstObjectByType<InventoryManager>();
        }
    }

    private void BindInventory()
    {
        if (inventoryManager == null)
            return;

        inventoryManager.OnInventoryChanged -=
            HandleInventoryChanged;

        inventoryManager.OnInventoryChanged +=
            HandleInventoryChanged;
    }

    public FishingRodPartDefinition GetEquipped(
        FishingRodPartSlotType slotType)
    {
        switch (slotType)
        {
            case FishingRodPartSlotType.Reel:
                return equippedReel;
            case FishingRodPartSlotType.Line:
                return equippedLine;
            case FishingRodPartSlotType.Hook:
                return equippedHook;
            case FishingRodPartSlotType.Bait:
                return equippedBait;
            default:
                return null;
        }
    }

    public bool IsEquipped(FishingRodPartDefinition definition)
    {
        return definition != null &&
               GetEquipped(definition.SlotType) == definition;
    }

    public int GetOwnedAmount(FishingRodPartDefinition definition)
    {
        if (definition == null)
            return 0;

        return GetOwnedAmount(definition.InventoryItem);
    }

    public int GetOwnedAmount(InventoryItemData item)
    {
        ResolveInventory();

        if (inventoryManager == null || item == null)
            return 0;

        return Mathf.Max(
            0,
            inventoryManager.GetItemAmount(item)
        );
    }

    public List<FishingRodPartDefinition> GetOwnedParts(
        FishingRodPartSlotType slotType)
    {
        List<FishingRodPartDefinition> result =
            new List<FishingRodPartDefinition>();

        if (partDefinitions == null)
            return result;

        foreach (FishingRodPartDefinition definition in partDefinitions)
        {
            if (definition == null ||
                definition.InventoryItem == null ||
                definition.SlotType != slotType ||
                GetOwnedAmount(definition) <= 0)
            {
                continue;
            }

            result.Add(definition);
        }

        result.Sort(
            (left, right) =>
            {
                int levelCompare =
                    left.RequiredSkill.CompareTo(right.RequiredSkill);

                if (levelCompare != 0)
                    return levelCompare;

                return string.Compare(
                    left.ItemName,
                    right.ItemName,
                    StringComparison.CurrentCultureIgnoreCase
                );
            }
        );

        return result;
    }

    public bool TogglePart(
        FishingRodPartDefinition definition,
        out string message)
    {
        message = string.Empty;

        if (definition == null ||
            definition.InventoryItem == null)
        {
            message = "Dữ liệu trang bị không hợp lệ.";
            return false;
        }

        ResolveInventory();

        if (inventoryManager == null)
        {
            message = "Không tìm thấy túi đồ.";
            return false;
        }

        if (IsEquipped(definition))
        {
            SetEquipped(definition.SlotType, null);
            message = "Đã gỡ " + definition.ItemName + ".";
            NotifyChanged();
            return true;
        }

        if (GetOwnedAmount(definition) <= 0)
        {
            message =
                "Bạn không sở hữu " +
                definition.ItemName +
                ".";

            return false;
        }

        SetEquipped(
            definition.SlotType,
            definition
        );

        message =
            "Đã trang bị " +
            definition.ItemName +
            ".";

        NotifyChanged();
        return true;
    }

    public bool TryConsumeBait()
    {
        ResolveInventory();

        if (!HasBait ||
            inventoryManager == null ||
            equippedBait == null ||
            equippedBait.InventoryItem == null)
        {
            return false;
        }

        bool removed =
            inventoryManager.RemoveItem(
                equippedBait.InventoryItem,
                1,
                out _
            );

        if (!removed)
            return false;

        if (GetOwnedAmount(equippedBait) <= 0)
            equippedBait = null;

        NotifyChanged();
        return true;
    }

    public void RefreshNow()
    {
        ResolveInventory();
        ValidateEquippedParts();
        OnLoadoutChanged?.Invoke();
    }

    private void HandleInventoryChanged()
    {
        bool changed = ValidateEquippedParts();

        if (changed)
            SaveEquippedParts();

        OnLoadoutChanged?.Invoke();
    }

    private bool ValidateEquippedParts()
    {
        bool changed = false;

        changed |= ValidateSlot(FishingRodPartSlotType.Reel);
        changed |= ValidateSlot(FishingRodPartSlotType.Line);
        changed |= ValidateSlot(FishingRodPartSlotType.Hook);
        changed |= ValidateSlot(FishingRodPartSlotType.Bait);

        return changed;
    }

    private bool ValidateSlot(FishingRodPartSlotType slotType)
    {
        FishingRodPartDefinition definition =
            GetEquipped(slotType);

        if (definition == null)
            return false;

        if (definition.SlotType != slotType ||
            definition.InventoryItem == null ||
            GetOwnedAmount(definition) <= 0)
        {
            SetEquipped(slotType, null);
            return true;
        }

        return false;
    }

    private void SetEquipped(
        FishingRodPartSlotType slotType,
        FishingRodPartDefinition definition)
    {
        switch (slotType)
        {
            case FishingRodPartSlotType.Reel:
                equippedReel = definition;
                break;
            case FishingRodPartSlotType.Line:
                equippedLine = definition;
                break;
            case FishingRodPartSlotType.Hook:
                equippedHook = definition;
                break;
            case FishingRodPartSlotType.Bait:
                equippedBait = definition;
                break;
        }
    }

    private IEnumerable<FishingRodPartDefinition> GetEquippedParts()
    {
        yield return equippedReel;
        yield return equippedLine;
        yield return equippedHook;
        yield return equippedBait;
    }

    private float SumEquipped(
        Func<FishingRodPartDefinition, float> selector)
    {
        float result = 0f;

        foreach (FishingRodPartDefinition part in GetEquippedParts())
        {
            if (part != null)
                result += selector(part);
        }

        return result;
    }

    private void NotifyChanged()
    {
        SaveEquippedParts();
        OnLoadoutChanged?.Invoke();
    }

    private void SaveEquippedParts()
    {
        if (!saveWithPlayerPrefs)
            return;

        SavePart(FishingRodPartSlotType.Reel, equippedReel);
        SavePart(FishingRodPartSlotType.Line, equippedLine);
        SavePart(FishingRodPartSlotType.Hook, equippedHook);
        SavePart(FishingRodPartSlotType.Bait, equippedBait);

        PlayerPrefs.Save();
    }

    private void SavePart(
        FishingRodPartSlotType slotType,
        FishingRodPartDefinition definition)
    {
        PlayerPrefs.SetString(
            GetSaveKey(slotType),
            definition != null
                ? definition.ItemId
                : string.Empty
        );
    }

    private void LoadEquippedParts()
    {
        if (!saveWithPlayerPrefs ||
            partDefinitions == null)
        {
            return;
        }

        equippedReel = LoadPart(FishingRodPartSlotType.Reel);
        equippedLine = LoadPart(FishingRodPartSlotType.Line);
        equippedHook = LoadPart(FishingRodPartSlotType.Hook);
        equippedBait = LoadPart(FishingRodPartSlotType.Bait);
    }

    private FishingRodPartDefinition LoadPart(
        FishingRodPartSlotType slotType)
    {
        string savedId =
            PlayerPrefs.GetString(
                GetSaveKey(slotType),
                string.Empty
            );

        if (string.IsNullOrWhiteSpace(savedId))
            return null;

        foreach (FishingRodPartDefinition definition in partDefinitions)
        {
            if (definition == null ||
                definition.SlotType != slotType)
            {
                continue;
            }

            if (string.Equals(
                    definition.ItemId,
                    savedId,
                    StringComparison.OrdinalIgnoreCase))
            {
                return definition;
            }
        }

        return null;
    }

    private string GetSaveKey(
        FishingRodPartSlotType slotType)
    {
        string prefix =
            string.IsNullOrWhiteSpace(saveKeyPrefix)
                ? "FishingRodLoadout"
                : saveKeyPrefix.Trim();

        return prefix + "_" + slotType;
    }
}
