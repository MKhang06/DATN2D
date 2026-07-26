using System;
using System.Globalization;
using System.Text;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
[DefaultExecutionOrder(-50)]
public class FishingRodOwnershipGate : MonoBehaviour
{
    public static FishingRodOwnershipGate Instance
    {
        get;
        private set;
    }

    [Header("References")]
    [SerializeField]
    private InventoryManager inventoryManager;

    [SerializeField]
    private FishingCustomizationUI customizationUI;

    [Tooltip(
        "Kéo đúng InventoryItemData mà Shop bán cho cần câu vào đây. " +
        "Không kéo FishingEquipmentData."
    )]
    [SerializeField]
    private InventoryItemData requiredRodItem;

    [Tooltip(
        "Object cần câu đang được đặt sẵn trong Hierarchy. " +
        "Object này sẽ bị tắt khi không cầm cần."
    )]
    [SerializeField]
    private GameObject hierarchyRodVisual;

    [Header("Rules")]
    [Tooltip(
        "Chỉ mở Custom khi ô Hotbar đang chọn chứa đúng cần câu."
    )]
    [SerializeField]
    private bool requireSelectedRod = true;

    [Tooltip(
        "Tự đóng Custom ngay khi đổi sang ô Hotbar khác."
    )]
    [SerializeField]
    private bool closeCustomizationWhenRodPutAway = true;

    [Tooltip(
        "Bảo vệ trường hợp script cũ gọi FishingCustomizationUI.Open() trực tiếp."
    )]
    [SerializeField]
    private bool forceCloseWhileNotHoldingRod = true;

    [Header("Fallback names")]
    [Tooltip(
        "Chỉ dùng khi chưa gắn Required Rod Item hoặc dữ liệu cũ lưu bằng tên."
    )]
    [SerializeField]
    private string[] acceptedRodNames =
    {
        "Feather Light",
        "EQ_FeatherLight",
        "equipment_rod_feather_light",
        "Fishing Rod",
        "Cần Câu"
    };

    [Header("Events")]
    [SerializeField]
    private UnityEvent onCustomizationDenied;

    [SerializeField]
    private UnityEvent onRodHeld;

    [SerializeField]
    private UnityEvent onRodPutAway;

    [Header("Debug")]
    [SerializeField]
    private bool showDebugLogs = true;

    private bool lastHoldingRod;
    private bool initialized;

    public InventoryManager Inventory =>
        inventoryManager;

    public InventoryItemData RequiredRodItem =>
        requiredRodItem;

    public bool OwnsRequiredRod =>
        HasRodInInventory();

    public bool IsHoldingRequiredRod =>
        IsSelectedHotbarRod();

    public string LastBlockReason
    {
        get;
        private set;
    }

    private void Awake()
    {
        if (Instance != null &&
            Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        ResolveReferences();
    }

    private void OnEnable()
    {
        ResolveReferences();
        RefreshNow();
    }

    private void Start()
    {
        RefreshNow();
    }

    private void Update()
    {
        ResolveReferences();

        bool holding =
            IsSelectedHotbarRod();

        if (!initialized ||
            holding != lastHoldingRod)
        {
            ApplyHoldingState(
                holding
            );
        }

        /*
         * Script khác có thể gọi Custom.Open() trực tiếp.
         * Việc đóng cưỡng chế bảo đảm không thể mở Custom khi không cầm cần.
         */
        if (!holding &&
            forceCloseWhileNotHoldingRod &&
            customizationUI != null)
        {
            customizationUI.Close();
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void RefreshNow()
    {
        ResolveReferences();

        ApplyHoldingState(
            IsSelectedHotbarRod()
        );
    }

    public bool CanOpenCustomization()
    {
        LastBlockReason =
            string.Empty;

        if (inventoryManager == null)
        {
            LastBlockReason =
                "Không tìm thấy InventoryManager.";

            return false;
        }

        if (requiredRodItem == null &&
            !HasAnyFallbackRodName())
        {
            LastBlockReason =
                "Chưa cấu hình InventoryItemData của cần câu.";

            return false;
        }

        if (!HasRodInInventory())
        {
            LastBlockReason =
                "Bạn chưa mua cần câu.";

            return false;
        }

        if (requireSelectedRod &&
            !IsSelectedHotbarRod())
        {
            LastBlockReason =
                "Hãy đặt cần câu vào Hotbar và chọn đúng ô cần câu.";

            return false;
        }

        return true;
    }

    public void OpenCustomization()
    {
        ResolveReferences();

        if (!CanOpenCustomization())
        {
            DenyCustomization();
            return;
        }

        if (customizationUI == null)
        {
            LastBlockReason =
                "Không tìm thấy FishingCustomizationUI.";

            DenyCustomization();
            return;
        }

        customizationUI.Open();
    }

    public void ToggleCustomization()
    {
        ResolveReferences();

        if (!CanOpenCustomization())
        {
            if (customizationUI != null)
                customizationUI.Close();

            DenyCustomization();
            return;
        }

        if (customizationUI == null)
        {
            LastBlockReason =
                "Không tìm thấy FishingCustomizationUI.";

            DenyCustomization();
            return;
        }

        customizationUI.Toggle();
    }

    public void CloseCustomization()
    {
        if (customizationUI != null)
            customizationUI.Close();
    }

    public bool IsRodItem(
        InventoryItemData item)
    {
        if (item == null)
            return false;

        if (requiredRodItem != null &&
            item == requiredRodItem)
        {
            return true;
        }

        if (MatchesAcceptedName(
                item.DisplayName))
        {
            return true;
        }

        return MatchesAcceptedName(
            item.name
        );
    }

    private void ApplyHoldingState(
        bool holding)
    {
        initialized = true;

        bool changed =
            holding != lastHoldingRod;

        lastHoldingRod =
            holding;

        if (hierarchyRodVisual != null &&
            hierarchyRodVisual != gameObject)
        {
            hierarchyRodVisual.SetActive(
                holding
            );
        }

        if (!changed)
            return;

        if (holding)
        {
            onRodHeld?.Invoke();

            if (showDebugLogs)
            {
                Debug.Log(
                    "[FishingRodOwnershipGate] Đang cầm cần câu. " +
                    "Cho phép mở Custom.",
                    this
                );
            }
        }
        else
        {
            if (closeCustomizationWhenRodPutAway &&
                customizationUI != null)
            {
                customizationUI.Close();
            }

            onRodPutAway?.Invoke();

            if (showDebugLogs)
            {
                Debug.Log(
                    "[FishingRodOwnershipGate] Đã cất cần câu. " +
                    "Custom bị khóa.",
                    this
                );
            }
        }
    }

    private void DenyCustomization()
    {
        onCustomizationDenied?.Invoke();

        Debug.LogWarning(
            "[FishingRodOwnershipGate] Không thể mở Custom: " +
            LastBlockReason,
            this
        );
    }

    private bool HasRodInInventory()
    {
        if (inventoryManager == null)
            return false;

        return HasRodInSlots(
                   inventoryManager.HotbarSlots
               ) ||
               HasRodInSlots(
                   inventoryManager.BagSlots
               );
    }

    private bool HasRodInSlots(
        InventoryManager.InventorySlot[] slots)
    {
        if (slots == null)
            return false;

        foreach (
            InventoryManager.InventorySlot slot
            in slots)
        {
            if (SlotMatchesRod(slot))
                return true;
        }

        return false;
    }

    private bool IsSelectedHotbarRod()
    {
        if (inventoryManager == null)
            return false;

        InventoryManager.InventorySlot slot =
            inventoryManager.SelectedSlot;

        return SlotMatchesRod(slot);
    }

    private bool SlotMatchesRod(
        InventoryManager.InventorySlot slot)
    {
        if (slot == null ||
            slot.IsEmpty)
        {
            return false;
        }

        /*
         * InventorySlot của project hiện chỉ bảo đảm có itemName.
         * Không truy cập slot.item hoặc slot.itemData để tránh
         * phụ thuộc phiên bản InventoryManager.
         */
        if (requiredRodItem != null)
        {
            if (NamesMatch(
                    slot.itemName,
                    requiredRodItem.DisplayName) ||
                NamesMatch(
                    slot.itemName,
                    requiredRodItem.name))
            {
                return true;
            }
        }

        return MatchesAcceptedName(
            slot.itemName
        );
    }

    private static bool NamesMatch(
        string first,
        string second)
    {
        if (string.IsNullOrWhiteSpace(first) ||
            string.IsNullOrWhiteSpace(second))
        {
            return false;
        }

        string normalizedFirst =
            NormalizeName(first);

        string normalizedSecond =
            NormalizeName(second);

        return normalizedFirst ==
            normalizedSecond;
    }

    private bool HasAnyFallbackRodName()
    {
        if (acceptedRodNames == null)
            return false;

        foreach (string name
                 in acceptedRodNames)
        {
            if (!string.IsNullOrWhiteSpace(
                    name))
            {
                return true;
            }
        }

        return false;
    }

    private bool MatchesAcceptedName(
        string value)
    {
        if (string.IsNullOrWhiteSpace(
                value) ||
            acceptedRodNames == null)
        {
            return false;
        }

        string normalizedValue =
            NormalizeName(value);

        foreach (string acceptedName
                 in acceptedRodNames)
        {
            string normalizedAccepted =
                NormalizeName(
                    acceptedName
                );

            if (string.IsNullOrWhiteSpace(
                    normalizedAccepted))
            {
                continue;
            }

            if (normalizedValue ==
                    normalizedAccepted ||
                normalizedValue.Contains(
                    normalizedAccepted) ||
                normalizedAccepted.Contains(
                    normalizedValue))
            {
                return true;
            }
        }

        return false;
    }

    private void ResolveReferences()
    {
        if (inventoryManager == null)
        {
            inventoryManager =
                InventoryManager.Instance;
        }

        if (inventoryManager == null)
        {
            inventoryManager =
                FindFirstObjectByType<
                    InventoryManager
                >();
        }

        if (customizationUI == null)
        {
            customizationUI =
                FindFirstObjectByType<
                    FishingCustomizationUI
                >(
                    FindObjectsInactive.Include
                );
        }
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

    [ContextMenu(
        "TEST - Print Rod Ownership"
    )]
    private void PrintRodOwnership()
    {
        ResolveReferences();

        InventoryManager.InventorySlot selected =
            inventoryManager != null
                ? inventoryManager.SelectedSlot
                : null;

        Debug.Log(
            "[FishingRodOwnershipGate TEST] " +
            "Inventory=" +
            (
                inventoryManager != null
                    ? inventoryManager.name
                    : "NULL"
            ) +
            " | RequiredRod=" +
            (
                requiredRodItem != null
                    ? requiredRodItem.DisplayName
                    : "NULL"
            ) +
            " | OwnsRod=" +
            HasRodInInventory() +
            " | HoldingRod=" +
            IsSelectedHotbarRod() +
            " | SelectedName=" +
            (
                selected != null &&
                !selected.IsEmpty
                    ? selected.itemName
                    : "EMPTY"
            ),
            this
        );
    }
}