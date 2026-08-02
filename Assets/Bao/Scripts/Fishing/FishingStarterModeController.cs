using System;
using System.Globalization;
using System.Text;
using UnityEngine;

[DisallowMultipleComponent]
public class FishingStarterModeController : MonoBehaviour
{
    [Header("Cần câu hiện tại")]
    [Tooltip(
        "Bật: hiện tại người chơi có sẵn một cây cần để sử dụng.\n" +
        "Tắt: sau này bắt buộc phải mua cây cần trong Shop."
    )]
    [SerializeField]
    private bool rodAvailableByDefault = true;

    [Tooltip(
        "Tên cây cần sẽ phải mua khi Rod Available By Default bị tắt."
    )]
    [SerializeField]
    private string requiredRodName =
        "Feather Light";

    [Header("Bắt buộc tự mua và tự trang bị")]
    [SerializeField]
    private bool requireReel = true;

    [SerializeField]
    private bool requireLine = true;

    [SerializeField]
    private bool requireHook = true;

    [SerializeField]
    private bool requireBait;

    [Header("References")]
    [SerializeField]
    private FishingGearStats gearStats;

    [SerializeField]
    private InventoryManager inventoryManager;

    [Header("Debug")]
    [SerializeField]
    private bool showDebugLogs = true;

    public static FishingStarterModeController Instance
    {
        get;
        private set;
    }

    public bool RodAvailableByDefault =>
        rodAvailableByDefault;

    private void Awake()
    {
        if (Instance != null &&
            Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        ResolveReferences();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void ResolveReferences()
    {
        if (gearStats == null)
        {
            gearStats =
                FindFirstObjectByType<
                    FishingGearStats
                >();
        }

        if (inventoryManager == null)
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

    /*
     * Script này KHÔNG:
     * - Cấp miễn phí Callisto XSR
     * - Cấp miễn phí dây câu
     * - Cấp miễn phí móc câu
     * - Tự trang bị bất kỳ phụ kiện nào
     *
     * Người chơi phải mua trong Shop và bấm TRANG BỊ trong UI.
     */

    public bool CanStartFishing(
        out string reason)
    {
        ResolveReferences();

        reason = string.Empty;

        if (!HasUsableRod())
        {
            reason =
                "Bạn cần mua cần câu " +
                requiredRodName +
                " trước.";

            return false;
        }

        if (gearStats == null)
        {
            reason =
                "Không tìm thấy FishingGearStats.";

            return false;
        }

        if (requireReel &&
            !ValidateEquippedPart(
                FishingPartType.Reel,
                "máy câu",
                out reason))
        {
            return false;
        }

        if (requireLine &&
            !ValidateEquippedPart(
                FishingPartType.Line,
                "dây câu",
                out reason))
        {
            return false;
        }

        if (requireHook &&
            !ValidateEquippedPart(
                FishingPartType.Hook,
                "móc câu",
                out reason))
        {
            return false;
        }

        if (requireBait &&
            !ValidateEquippedPart(
                FishingPartType.Bait,
                "mồi câu",
                out reason))
        {
            return false;
        }

        return true;
    }

    public bool HasUsableRod()
    {
        if (rodAvailableByDefault)
            return true;

        return HasInventoryItem(
            requiredRodName
        );
    }

    private bool ValidateEquippedPart(
        FishingPartType partType,
        string displayName,
        out string reason)
    {
        reason = string.Empty;

        FishingEquipmentData equipped =
            gearStats.GetEquipped(
                partType
            );

        if (equipped == null)
        {
            reason =
                "Bạn cần mua và trang bị " +
                displayName +
                ".";

            return false;
        }

        /*
         * Không chỉ kiểm tra đã gắn trong GearStats,
         * còn phải thật sự sở hữu trong Inventory.
         */
        if (!HasInventoryItem(
                equipped.itemName))
        {
            reason =
                "Bạn không còn sở hữu " +
                equipped.itemName +
                " trong túi đồ.";

            return false;
        }

        return true;
    }

    public bool HasInventoryItem(
        string itemName)
    {
        ResolveReferences();

        if (inventoryManager == null ||
            string.IsNullOrWhiteSpace(
                itemName))
        {
            return false;
        }

        return HasItemInSlots(
                   inventoryManager.HotbarSlots,
                   itemName
               ) ||
               HasItemInSlots(
                   inventoryManager.BagSlots,
                   itemName
               );
    }

    private static bool HasItemInSlots(
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
                UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetterOrDigit(
                    character))
            {
                builder.Append(character);
            }
        }

        return builder
            .ToString()
            .Normalize(
                NormalizationForm.FormC
            );
    }

    public void SetRodAvailableByDefault(
        bool value)
    {
        rodAvailableByDefault = value;
    }

    [ContextMenu(
        "MODE - Có sẵn cần, tự mua phụ kiện"
    )]
    private void UseBuiltInRodMode()
    {
        rodAvailableByDefault = true;

        if (showDebugLogs)
        {
            Debug.Log(
                "[Fishing Equipment] Có sẵn cần câu. " +
                "Người chơi phải tự mua và tự trang bị phụ kiện.",
                this
            );
        }
    }

    [ContextMenu(
        "MODE - Phải mua cả cần và phụ kiện"
    )]
    private void UseBuyEverythingMode()
    {
        rodAvailableByDefault = false;

        if (showDebugLogs)
        {
            Debug.Log(
                "[Fishing Equipment] Người chơi phải tự mua " +
                "cần câu và toàn bộ phụ kiện.",
                this
            );
        }
    }

    [ContextMenu(
        "TEST - Kiểm tra điều kiện câu"
    )]
    private void TestFishingRequirements()
    {
        bool allowed =
            CanStartFishing(
                out string reason
            );

        if (allowed)
        {
            Debug.Log(
                "[Fishing Equipment] Đủ điều kiện câu cá.",
                this
            );
        }
        else
        {
            Debug.LogWarning(
                "[Fishing Equipment] " +
                reason,
                this
            );
        }
    }
}