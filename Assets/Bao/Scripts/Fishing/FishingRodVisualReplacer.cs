using System;
using System.Globalization;
using System.Text;
using UnityEngine;

[DisallowMultipleComponent]
public class FishingRodVisualReplacer : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private InventoryManager inventoryManager;

    [SerializeField]
    private FishingEquipmentShopUI equipmentShopUI;

    [Tooltip(
        "Kéo object cần câu đã đặt sẵn trong Hierarchy vào đây."
    )]
    [SerializeField]
    private GameObject hierarchyRodObject;

    [Tooltip(
        "SpriteRenderer nằm trên object cần câu hoặc object con của nó."
    )]
    [SerializeField]
    private SpriteRenderer rodSpriteRenderer;

    [Header("Display")]
    [Tooltip(
        "Chỉ hiện cần câu khi người chơi đang chọn đúng ô cần câu trong Hotbar."
    )]
    [SerializeField]
    private bool onlyShowWhenSelected = true;

    [Tooltip(
        "Tắt object cần câu khi không chọn cần câu."
    )]
    [SerializeField]
    private bool hideWhenNoRodSelected = true;

    [Tooltip(
        "Bật để ép kích thước cần câu ngoài thế giới."
    )]
    [SerializeField]
    private bool overrideLocalScale = true;

    [SerializeField]
    private Vector3 heldLocalScale =
        new Vector3(
            0.15f,
            0.15f,
            1f
        );

    [Header("Rod Detection")]
    [SerializeField]
    private string[] rodKeywords =
    {
        "Feather Light",
        "FeatherLight",
        "Fishing Rod",
        "Cần Câu",
        "equipment_rod"
    };

    [Header("Debug")]
    [SerializeField]
    private bool showDebugLogs = true;

    private string lastSelectedItemName =
        string.Empty;

    private Sprite lastAppliedSprite;

    private bool lastVisible;

    private void Awake()
    {
        ResolveReferences();

        if (hierarchyRodObject != null)
        {
            hierarchyRodObject.SetActive(
                false
            );
        }
    }

    private void OnEnable()
    {
        ResolveReferences();
        RefreshVisual(true);
    }

    private void Update()
    {
        RefreshVisual(false);
    }

    public void RefreshNow()
    {
        RefreshVisual(true);
    }

    private void RefreshVisual(
        bool force)
    {
        ResolveReferences();

        if (inventoryManager == null ||
            hierarchyRodObject == null ||
            rodSpriteRenderer == null)
        {
            return;
        }

        InventoryManager.InventorySlot selected =
            inventoryManager.SelectedSlot;

        string selectedName =
            selected != null &&
            !selected.IsEmpty
                ? selected.itemName
                : string.Empty;

        bool selectedIsRod =
            IsRodName(
                selectedName
            );

        bool shouldShow =
            onlyShowWhenSelected
                ? selectedIsRod
                : HasAnyRodInInventory();

        if (!force &&
            selectedName ==
                lastSelectedItemName &&
            shouldShow ==
                lastVisible)
        {
            return;
        }

        lastSelectedItemName =
            selectedName;

        lastVisible =
            shouldShow;

        if (!shouldShow)
        {
            if (hideWhenNoRodSelected)
            {
                hierarchyRodObject
                    .SetActive(false);
            }

            return;
        }

        FishingEquipmentShopItemData
            rodShopItem =
                FindMatchingRodShopItem(
                    selectedName
                );

        Sprite newSprite =
            ResolveWorldSprite(
                rodShopItem,
                selected
            );

        if (newSprite != null &&
            newSprite != lastAppliedSprite)
        {
            rodSpriteRenderer.sprite =
                newSprite;

            lastAppliedSprite =
                newSprite;

            if (showDebugLogs)
            {
                Debug.Log(
                    "[FishingRodVisualReplacer] " +
                    "Đã thay sprite cần câu thành " +
                    newSprite.name +
                    " cho item " +
                    selectedName +
                    ".",
                    this
                );
            }
        }

        if (overrideLocalScale)
        {
            hierarchyRodObject
                .transform.localScale =
                    heldLocalScale;
        }

        hierarchyRodObject
            .SetActive(true);
    }

    private FishingEquipmentShopItemData
        FindMatchingRodShopItem(
            string selectedName)
    {
        if (equipmentShopUI == null)
            return null;

        FishingEquipmentShopItemData[]
            items =
                equipmentShopUI
                    .AvailableEquipment;

        if (items == null)
            return null;

        string normalizedSelected =
            NormalizeName(
                selectedName
            );

        FishingEquipmentShopItemData
            firstRod =
                null;

        foreach (
            FishingEquipmentShopItemData item
            in items)
        {
            if (item == null)
                continue;

            FishingEquipmentData equipment =
                item.equipmentData;

            string combined =
                item.ItemName +
                " " +
                item.name +
                " " +
                (
                    equipment != null
                        ? equipment.itemName
                        : string.Empty
                ) +
                " " +
                (
                    equipment != null
                        ? equipment.name
                        : string.Empty
                );

            if (!IsRodName(combined))
                continue;

            if (firstRod == null)
                firstRod = item;

            if (NameMatches(
                    normalizedSelected,
                    item.ItemName) ||
                NameMatches(
                    normalizedSelected,
                    item.name) ||
                (
                    equipment != null &&
                    NameMatches(
                        normalizedSelected,
                        equipment.itemName)
                ) ||
                (
                    equipment != null &&
                    NameMatches(
                        normalizedSelected,
                        equipment.name)
                ))
            {
                return item;
            }
        }

        /*
         * Project hiện chỉ có một loại cần câu.
         * Khi Inventory lưu tên hơi khác, vẫn dùng đúng rod item đầu tiên.
         */
        return firstRod;
    }

    private Sprite ResolveWorldSprite(
        FishingEquipmentShopItemData shopItem,
        InventoryManager.InventorySlot slot)
    {
        if (shopItem != null)
        {
            FishingEquipmentData equipment =
                shopItem.equipmentData;

            if (equipment != null)
            {
                if (equipment
                        .rodPreviewSprite != null)
                {
                    return equipment
                        .rodPreviewSprite;
                }

                if (equipment.icon != null)
                {
                    return equipment.icon;
                }
            }

            if (shopItem.Icon != null)
            {
                return shopItem.Icon;
            }
        }

        if (slot != null &&
            slot.icon != null)
        {
            return slot.icon;
        }

        return rodSpriteRenderer != null
            ? rodSpriteRenderer.sprite
            : null;
    }

    private bool HasAnyRodInInventory()
    {
        if (inventoryManager == null)
            return false;

        return HasRodInSlots(
                   inventoryManager
                       .HotbarSlots
               ) ||
               HasRodInSlots(
                   inventoryManager
                       .BagSlots
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
            if (slot == null ||
                slot.IsEmpty)
            {
                continue;
            }

            if (IsRodName(
                    slot.itemName))
            {
                return true;
            }
        }

        return false;
    }

    private bool IsRodName(
        string value)
    {
        if (string.IsNullOrWhiteSpace(
                value))
        {
            return false;
        }

        string normalizedValue =
            NormalizeName(value);

        if (normalizedValue.Contains(
                "featherlight") ||
            normalizedValue.Contains(
                "fishingrod") ||
            normalizedValue.Contains(
                "cancau") ||
            normalizedValue.Contains(
                "equipmentrod"))
        {
            return true;
        }

        if (rodKeywords == null)
            return false;

        foreach (string keyword
                 in rodKeywords)
        {
            string normalizedKeyword =
                NormalizeName(
                    keyword
                );

            if (string.IsNullOrWhiteSpace(
                    normalizedKeyword))
            {
                continue;
            }

            if (normalizedValue.Contains(
                    normalizedKeyword) ||
                normalizedKeyword.Contains(
                    normalizedValue))
            {
                return true;
            }
        }

        return false;
    }

    private static bool NameMatches(
        string normalizedSelected,
        string candidate)
    {
        if (string.IsNullOrWhiteSpace(
                normalizedSelected) ||
            string.IsNullOrWhiteSpace(
                candidate))
        {
            return false;
        }

        string normalizedCandidate =
            NormalizeName(candidate);

        return normalizedSelected ==
                   normalizedCandidate ||
               normalizedSelected.Contains(
                   normalizedCandidate) ||
               normalizedCandidate.Contains(
                   normalizedSelected);
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
                >(
                    FindObjectsInactive.Include
                );
        }

        if (equipmentShopUI == null)
        {
            equipmentShopUI =
                FishingEquipmentShopUI
                    .Instance;
        }

        if (equipmentShopUI == null)
        {
            equipmentShopUI =
                FindFirstObjectByType<
                    FishingEquipmentShopUI
                >(
                    FindObjectsInactive.Include
                );
        }

        if (hierarchyRodObject != null &&
            rodSpriteRenderer == null)
        {
            rodSpriteRenderer =
                hierarchyRodObject
                    .GetComponentInChildren<
                        SpriteRenderer
                    >(true);
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
        "TEST - Refresh Rod Visual"
    )]
    private void TestRefresh()
    {
        RefreshVisual(true);
    }
}
