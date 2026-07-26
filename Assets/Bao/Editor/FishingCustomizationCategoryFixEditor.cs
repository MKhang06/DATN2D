#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class FishingCustomizationCategoryFixEditor
{
    private const string Prefix =
        "[FISHING CATEGORY FIX] ";

    [MenuItem(
        "Tools/Fishing/Fix Line And Hook Categories"
    )]
    public static void FixCategories()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog(
                "Fishing Category Fix",
                "Hãy thoát Play Mode trước khi chạy.",
                "OK"
            );

            return;
        }

        FishingCustomizationUI customUI =
            UnityEngine.Object
                .FindFirstObjectByType<
                    FishingCustomizationUI
                >(
                    FindObjectsInactive.Include
                );

        FishingEquipmentShopUI shop =
            UnityEngine.Object
                .FindFirstObjectByType<
                    FishingEquipmentShopUI
                >(
                    FindObjectsInactive.Include
                );

        FishingGearStats gearStats =
            UnityEngine.Object
                .FindFirstObjectByType<
                    FishingGearStats
                >(
                    FindObjectsInactive.Include
                );

        InventoryManager inventory =
            shop != null
                ? shop.Inventory
                : null;

        if (inventory == null)
        {
            inventory =
                UnityEngine.Object
                    .FindFirstObjectByType<
                        InventoryManager
                    >(
                        FindObjectsInactive.Include
                    );
        }

        if (customUI == null)
        {
            Debug.LogError(
                Prefix +
                "Không tìm thấy FishingCustomizationUI."
            );

            return;
        }

        if (shop == null)
        {
            Debug.LogError(
                Prefix +
                "Không tìm thấy FishingEquipmentShopUI."
            );

            return;
        }

        FishingEquipmentShopItemData[]
            source =
                shop.AvailableEquipment;

        if (source == null ||
            source.Length == 0)
        {
            Debug.LogError(
                Prefix +
                "Shop.AvailableEquipment đang trống.",
                shop
            );

            return;
        }

        int fixedTypes = 0;
        int reelCount = 0;
        int lineCount = 0;
        int hookCount = 0;
        int baitCount = 0;
        int rodCount = 0;
        int invalidCount = 0;

        List<
            FishingEquipmentShopItemData
        > accessoryShopItems =
            new List<
                FishingEquipmentShopItemData
            >();

        List<
            FishingEquipmentData
        > accessoryEquipment =
            new List<
                FishingEquipmentData
            >();

        foreach (
            FishingEquipmentShopItemData shopItem
            in source)
        {
            if (shopItem == null ||
                shopItem.equipmentData == null)
            {
                invalidCount++;

                Debug.LogError(
                    Prefix +
                    (
                        shopItem != null
                            ? shopItem.name
                            : "NULL ShopItem"
                    ) +
                    " thiếu EquipmentData.",
                    shopItem
                );

                continue;
            }

            FishingEquipmentData equipment =
                shopItem.equipmentData;

            string combined =
                shopItem.ItemName +
                " " +
                shopItem.name +
                " " +
                equipment.itemName +
                " " +
                equipment.name;

            if (IsRod(combined))
            {
                rodCount++;

                Debug.Log(
                    Prefix +
                    "Bỏ qua cần câu: " +
                    shopItem.ItemName,
                    shopItem
                );

                continue;
            }

            FishingPartType resolved =
                ResolveType(
                    combined,
                    equipment.partType
                );

            if (equipment.partType !=
                resolved)
            {
                Undo.RecordObject(
                    equipment,
                    "Fix Fishing Equipment Type"
                );

                FishingPartType old =
                    equipment.partType;

                equipment.partType =
                    resolved;

                EditorUtility.SetDirty(
                    equipment
                );

                fixedTypes++;

                Debug.Log(
                    Prefix +
                    "Sửa " +
                    equipment.itemName +
                    ": " +
                    old +
                    " -> " +
                    resolved,
                    equipment
                );
            }

            switch (resolved)
            {
                case FishingPartType.Reel:
                    reelCount++;
                    break;

                case FishingPartType.Line:
                    lineCount++;
                    break;

                case FishingPartType.Hook:
                    hookCount++;
                    break;

                case FishingPartType.Bait:
                    baitCount++;
                    break;
            }

            accessoryShopItems.Add(
                shopItem
            );

            if (!accessoryEquipment.Contains(
                    equipment))
            {
                accessoryEquipment.Add(
                    equipment
                );
            }

            Debug.Log(
                Prefix +
                "DATA | " +
                shopItem.ItemName +
                " | Type=" +
                resolved +
                " | Equipment=" +
                equipment.name,
                shopItem
            );
        }

        Undo.RecordObject(
            customUI,
            "Fix Fishing Category Slots"
        );

        SerializedObject serialized =
            new SerializedObject(
                customUI
            );

        SetReference(
            serialized,
            "equipmentShopUI",
            shop
        );

        SetReference(
            serialized,
            "inventoryManager",
            inventory
        );

        SetReference(
            serialized,
            "gearStats",
            gearStats
        );

        SetArray(
            serialized,
            "shopEquipmentItems",
            accessoryShopItems
                .Cast<
                    UnityEngine.Object
                >()
                .ToList()
        );

        SetArray(
            serialized,
            "availableEquipment",
            accessoryEquipment
                .Cast<
                    UnityEngine.Object
                >()
                .ToList()
        );

        int fixedSlots = 0;

        fixedSlots += FixSlot(
            customUI,
            serialized,
            "reelSlot",
            "ReelSlot",
            "Reel"
        );

        fixedSlots += FixSlot(
            customUI,
            serialized,
            "lineSlot",
            "LineSlot",
            "Line"
        );

        fixedSlots += FixSlot(
            customUI,
            serialized,
            "hookSlot",
            "HookSlot",
            "Hook"
        );

        fixedSlots += FixSlot(
            customUI,
            serialized,
            "baitSlot",
            "BaitSlot",
            "Bait"
        );

        serialized
            .ApplyModifiedProperties();

        EditorUtility.SetDirty(
            customUI
        );

        if (gearStats != null &&
            inventory != null)
        {
            SerializedObject gearSerialized =
                new SerializedObject(
                    gearStats
                );

            SetReference(
                gearSerialized,
                "inventoryManager",
                inventory
            );

            gearSerialized
                .ApplyModifiedProperties();

            EditorUtility.SetDirty(
                gearStats
            );
        }

        ValidateUniqueButtons(
            serialized,
            customUI
        );

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkAllScenesDirty();

        string summary =
            "Đã sửa xong.\n\n" +
            "Reel: " +
            reelCount +
            "\nLine: " +
            lineCount +
            "\nHook: " +
            hookCount +
            "\nBait: " +
            baitCount +
            "\nRod bỏ qua: " +
            rodCount +
            "\nInvalid: " +
            invalidCount +
            "\nPartType đã sửa: " +
            fixedTypes +
            "\nSlot references đã sửa: " +
            fixedSlots +
            "\n\nNhấn Ctrl+S rồi Play để kiểm tra.";

        Debug.Log(
            Prefix +
            "DONE | Reel=" +
            reelCount +
            " | Line=" +
            lineCount +
            " | Hook=" +
            hookCount +
            " | Bait=" +
            baitCount +
            " | Rod=" +
            rodCount +
            " | Invalid=" +
            invalidCount +
            " | FixedTypes=" +
            fixedTypes +
            " | FixedSlots=" +
            fixedSlots,
            customUI
        );

        EditorUtility.DisplayDialog(
            "Fishing Category Fix",
            summary,
            "OK"
        );
    }

    [MenuItem(
        "Tools/Fishing/Print Line And Hook Runtime Status"
    )]
    public static void PrintRuntimeStatus()
    {
        if (!EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog(
                "Fishing Runtime Status",
                "Hãy vào Play Mode rồi chạy lại.",
                "OK"
            );

            return;
        }

        FishingCustomizationUI customUI =
            UnityEngine.Object
                .FindFirstObjectByType<
                    FishingCustomizationUI
                >(
                    FindObjectsInactive.Include
                );

        FishingEquipmentShopUI shop =
            UnityEngine.Object
                .FindFirstObjectByType<
                    FishingEquipmentShopUI
                >(
                    FindObjectsInactive.Include
                );

        InventoryManager inventory =
            shop != null
                ? shop.Inventory
                : null;

        if (customUI == null ||
            shop == null)
        {
            Debug.LogError(
                Prefix +
                "Thiếu CustomUI hoặc Shop."
            );

            return;
        }

        FishingEquipmentShopItemData[]
            source =
                shop.AvailableEquipment;

        PrintCategory(
            "LINE",
            FishingPartType.Line,
            source,
            inventory
        );

        PrintCategory(
            "HOOK",
            FishingPartType.Hook,
            source,
            inventory
        );
    }

    private static int FixSlot(
        FishingCustomizationUI owner,
        SerializedObject serialized,
        string slotFieldName,
        string hierarchyName,
        string enumName)
    {
        SerializedProperty slot =
            serialized.FindProperty(
                slotFieldName
            );

        if (slot == null)
        {
            Debug.LogError(
                Prefix +
                "Không tìm thấy field " +
                slotFieldName +
                ".",
                owner
            );

            return 0;
        }

        Transform slotRoot =
            FindChild(
                owner.transform,
                hierarchyName
            );

        if (slotRoot == null)
        {
            Debug.LogError(
                Prefix +
                "Không tìm thấy object " +
                hierarchyName +
                " trong hierarchy.",
                owner
            );

            return 0;
        }

        int fixedCount = 0;

        SerializedProperty partType =
            slot.FindPropertyRelative(
                "partType"
            );

        if (partType != null &&
            partType.propertyType ==
                SerializedPropertyType.Enum)
        {
            int expected =
                Array.IndexOf(
                    partType.enumNames,
                    enumName
                );

            if (expected >= 0 &&
                partType.enumValueIndex !=
                    expected)
            {
                partType.enumValueIndex =
                    expected;

                fixedCount++;
            }
        }

        Button button =
            slotRoot.GetComponent<
                Button
            >();

        if (button == null)
        {
            button =
                slotRoot.GetComponentInChildren<
                    Button
                >(true);
        }

        fixedCount += SetNestedReference(
            slot,
            "button",
            button
        );

        Image icon =
            FindNamedComponent<
                Image
            >(
                slotRoot,
                new string[]
                {
                    "Icon",
                    "IconImage",
                    "EquipmentIcon"
                }
            );

        fixedCount += SetNestedReference(
            slot,
            "iconImage",
            icon
        );

        TMP_Text title =
            FindNamedComponent<
                TMP_Text
            >(
                slotRoot,
                new string[]
                {
                    "SlotTitleText",
                    "TitleText",
                    "SlotTitle"
                }
            );

        fixedCount += SetNestedReference(
            slot,
            "slotTitleText",
            title
        );

        TMP_Text equippedName =
            FindNamedComponent<
                TMP_Text
            >(
                slotRoot,
                new string[]
                {
                    "EquippedNameText",
                    "ItemNameText",
                    "NameText"
                }
            );

        fixedCount += SetNestedReference(
            slot,
            "equippedNameText",
            equippedName
        );

        Transform selected =
            FindChild(
                slotRoot,
                "SelectedFrame"
            );

        fixedCount += SetNestedReference(
            slot,
            "selectedFrame",
            selected != null
                ? selected.gameObject
                : null
        );

        Debug.Log(
            Prefix +
            slotFieldName +
            " -> Object=" +
            slotRoot.name +
            " | Button=" +
            (
                button != null
                    ? button.name
                    : "NULL"
            ) +
            " | Type=" +
            enumName,
            owner
        );

        return fixedCount;
    }

    private static int SetNestedReference(
        SerializedProperty parent,
        string fieldName,
        UnityEngine.Object value)
    {
        SerializedProperty property =
            parent.FindPropertyRelative(
                fieldName
            );

        if (property == null ||
            property.propertyType !=
                SerializedPropertyType
                    .ObjectReference ||
            value == null)
        {
            return 0;
        }

        if (property.objectReferenceValue ==
            value)
        {
            return 0;
        }

        property.objectReferenceValue =
            value;

        return 1;
    }

    private static void ValidateUniqueButtons(
        SerializedObject serialized,
        UnityEngine.Object context)
    {
        Dictionary<
            UnityEngine.Object,
            string
        > used =
            new Dictionary<
                UnityEngine.Object,
                string
            >();

        foreach (
            string slotName
            in new string[]
            {
                "reelSlot",
                "lineSlot",
                "hookSlot",
                "baitSlot"
            })
        {
            SerializedProperty slot =
                serialized.FindProperty(
                    slotName
                );

            SerializedProperty button =
                slot != null
                    ? slot.FindPropertyRelative(
                        "button"
                    )
                    : null;

            UnityEngine.Object value =
                button != null
                    ? button
                        .objectReferenceValue
                    : null;

            if (value == null)
            {
                Debug.LogError(
                    Prefix +
                    slotName +
                    ".button đang NULL.",
                    context
                );

                continue;
            }

            if (used.TryGetValue(
                    value,
                    out string previous))
            {
                Debug.LogError(
                    Prefix +
                    slotName +
                    " và " +
                    previous +
                    " đang dùng chung Button " +
                    value.name +
                    ".",
                    context
                );
            }
            else
            {
                used[value] =
                    slotName;
            }
        }
    }

    private static void PrintCategory(
        string label,
        FishingPartType type,
        FishingEquipmentShopItemData[] source,
        InventoryManager inventory)
    {
        int compatible = 0;
        int ownedByName = 0;
        int ownedByIcon = 0;

        if (source == null)
        {
            Debug.LogError(
                Prefix +
                label +
                " source=NULL."
            );

            return;
        }

        foreach (
            FishingEquipmentShopItemData shopItem
            in source)
        {
            if (shopItem == null ||
                shopItem.equipmentData == null)
            {
                continue;
            }

            FishingEquipmentData equipment =
                shopItem.equipmentData;

            string combined =
                shopItem.ItemName +
                " " +
                shopItem.name +
                " " +
                equipment.itemName +
                " " +
                equipment.name;

            FishingPartType resolved =
                ResolveType(
                    combined,
                    equipment.partType
                );

            if (resolved != type)
                continue;

            compatible++;

            bool nameOwned =
                HasInventoryName(
                    inventory,
                    shopItem.ItemName
                ) ||
                HasInventoryName(
                    inventory,
                    equipment.itemName
                );

            bool iconOwned =
                HasInventoryIcon(
                    inventory,
                    shopItem.Icon
                ) ||
                HasInventoryIcon(
                    inventory,
                    equipment.icon
                );

            if (nameOwned)
                ownedByName++;

            if (iconOwned)
                ownedByIcon++;

            Debug.Log(
                Prefix +
                label +
                " | " +
                shopItem.ItemName +
                " | Equipment=" +
                equipment.itemName +
                " | Type=" +
                resolved +
                " | NameOwned=" +
                nameOwned +
                " | IconOwned=" +
                iconOwned,
                shopItem
            );
        }

        Debug.Log(
            Prefix +
            label +
            " SUMMARY | Compatible=" +
            compatible +
            " | NameOwned=" +
            ownedByName +
            " | IconOwned=" +
            ownedByIcon +
            " | Inventory=" +
            (
                inventory != null
                    ? inventory.name
                    : "NULL"
            )
        );
    }

    private static bool HasInventoryName(
        InventoryManager inventory,
        string itemName)
    {
        if (inventory == null ||
            string.IsNullOrWhiteSpace(
                itemName))
        {
            return false;
        }

        return HasName(
                   inventory.HotbarSlots,
                   itemName
               ) ||
               HasName(
                   inventory.BagSlots,
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
            Normalize(itemName);

        foreach (
            InventoryManager.InventorySlot slot
            in slots)
        {
            if (slot == null ||
                slot.IsEmpty)
            {
                continue;
            }

            if (Normalize(
                    slot.itemName) ==
                expected)
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasInventoryIcon(
        InventoryManager inventory,
        Sprite icon)
    {
        if (inventory == null ||
            icon == null)
        {
            return false;
        }

        return HasIcon(
                   inventory.HotbarSlots,
                   icon
               ) ||
               HasIcon(
                   inventory.BagSlots,
                   icon
               );
    }

    private static bool HasIcon(
        InventoryManager.InventorySlot[] slots,
        Sprite icon)
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

            if (slot.icon == icon)
                return true;
        }

        return false;
    }

    private static void SetReference(
        SerializedObject serialized,
        string fieldName,
        UnityEngine.Object value)
    {
        SerializedProperty property =
            serialized.FindProperty(
                fieldName
            );

        if (property == null ||
            property.propertyType !=
                SerializedPropertyType
                    .ObjectReference)
        {
            return;
        }

        property.objectReferenceValue =
            value;
    }

    private static void SetArray(
        SerializedObject serialized,
        string fieldName,
        List<UnityEngine.Object> values)
    {
        SerializedProperty property =
            serialized.FindProperty(
                fieldName
            );

        if (property == null ||
            !property.isArray)
        {
            return;
        }

        property.arraySize =
            values.Count;

        for (int index = 0;
             index < values.Count;
             index++)
        {
            property
                .GetArrayElementAtIndex(
                    index
                )
                .objectReferenceValue =
                    values[index];
        }
    }

    private static Transform FindChild(
        Transform root,
        string childName)
    {
        if (root == null)
            return null;

        Transform[] children =
            root.GetComponentsInChildren<
                Transform
            >(true);

        foreach (Transform child
                 in children)
        {
            if (child == null)
                continue;

            if (string.Equals(
                    child.name,
                    childName,
                    StringComparison
                        .OrdinalIgnoreCase))
            {
                return child;
            }
        }

        return null;
    }

    private static T FindNamedComponent<T>(
        Transform root,
        string[] names)
        where T : Component
    {
        if (root == null)
            return null;

        Transform[] children =
            root.GetComponentsInChildren<
                Transform
            >(true);

        foreach (string wantedName
                 in names)
        {
            foreach (Transform child
                     in children)
            {
                if (child == null ||
                    !string.Equals(
                        child.name,
                        wantedName,
                        StringComparison
                            .OrdinalIgnoreCase))
                {
                    continue;
                }

                T component =
                    child.GetComponent<T>();

                if (component != null)
                    return component;
            }
        }

        return root
            .GetComponentInChildren<
                T
            >(true);
    }

    private static FishingPartType ResolveType(
        string value,
        FishingPartType fallback)
    {
        string normalized =
            Normalize(value);

        if (normalized.Contains(
                "callistoxsr") ||
            normalized.Contains(
                "maycau") ||
            normalized.Contains(
                "reel"))
        {
            return FishingPartType.Reel;
        }

        if (normalized.Contains(
                "daycau") ||
            normalized.Contains(
                "daydon") ||
            normalized.Contains(
                "daydu") ||
            normalized.Contains(
                "monoline") ||
            normalized.Contains(
                "braidedline") ||
            normalized.Contains(
                "fishingline") ||
            normalized.Contains(
                "line") ||
            normalized.Contains(
                "noodle"))
        {
            return FishingPartType.Line;
        }

        if (normalized.Contains(
                "moccau") ||
            normalized.Contains(
                "luoicau") ||
            normalized.Contains(
                "heavyhook") ||
            normalized.Contains(
                "hook"))
        {
            return FishingPartType.Hook;
        }

        if (normalized.Contains(
                "moicau") ||
            normalized.Contains(
                "bait") ||
            normalized.Contains(
                "giundat") ||
            normalized.Contains(
                "worm"))
        {
            return FishingPartType.Bait;
        }

        return fallback;
    }

    private static bool IsRod(
        string value)
    {
        string normalized =
            Normalize(value);

        return normalized.Contains(
                   "featherlight") ||
               normalized.Contains(
                   "equipmentrod"
               );
    }

    private static string Normalize(
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
}
#endif