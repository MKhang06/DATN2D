#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class FishingCustomizationSourceSyncEditor
{
    private const string Prefix =
        "[FISHING CUSTOM SYNC] ";

    [MenuItem(
        "Tools/Fishing/Fix Customization Source"
    )]
    public static void FixCustomizationSource()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog(
                "Fishing Customization",
                "Hãy thoát Play Mode trước khi chạy.",
                "OK"
            );

            return;
        }

        FishingEquipmentShopUI shop =
            UnityEngine.Object
                .FindFirstObjectByType<
                    FishingEquipmentShopUI
                >(
                    FindObjectsInactive.Include
                );

        FishingCustomizationUI[] customUIs =
            UnityEngine.Object
                .FindObjectsByType<
                    FishingCustomizationUI
                >(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None
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

        if (shop == null)
        {
            Debug.LogError(
                Prefix +
                "Không tìm thấy FishingEquipmentShopUI trong Scene."
            );

            return;
        }

        if (customUIs == null ||
            customUIs.Length == 0)
        {
            Debug.LogError(
                Prefix +
                "Không tìm thấy FishingCustomizationUI trong Scene."
            );

            return;
        }

        FishingEquipmentShopItemData[]
            shopSource =
                shop.AvailableEquipment;

        if (shopSource == null ||
            shopSource.Length == 0)
        {
            Debug.LogError(
                Prefix +
                "FishingEquipmentShopUI.AvailableEquipment đang trống.",
                shop
            );

            return;
        }

        List<
            FishingEquipmentShopItemData
        > validShopItems =
            new List<
                FishingEquipmentShopItemData
            >();

        List<
            FishingEquipmentData
        > validEquipment =
            new List<
                FishingEquipmentData
            >();

        int fixedPartTypes = 0;
        int skippedRod = 0;
        int missingEquipment = 0;

        foreach (
            FishingEquipmentShopItemData shopItem
            in shopSource)
        {
            if (shopItem == null)
                continue;

            FishingEquipmentData equipment =
                shopItem.equipmentData;

            if (equipment == null)
            {
                missingEquipment++;

                Debug.LogError(
                    Prefix +
                    shopItem.name +
                    " chưa gắn Equipment Data.",
                    shopItem
                );

                continue;
            }

            string combinedName =
                shopItem.ItemName +
                " " +
                shopItem.name +
                " " +
                equipment.itemName +
                " " +
                equipment.name;

            if (IsRodName(combinedName))
            {
                skippedRod++;

                Debug.Log(
                    Prefix +
                    "Bỏ qua cần câu khỏi Custom phụ kiện: " +
                    shopItem.name,
                    shopItem
                );

                continue;
            }

            if (TryResolvePartType(
                    combinedName,
                    out FishingPartType resolvedType) &&
                equipment.partType !=
                    resolvedType)
            {
                Undo.RecordObject(
                    equipment,
                    "Fix Fishing Part Type"
                );

                FishingPartType oldType =
                    equipment.partType;

                equipment.partType =
                    resolvedType;

                EditorUtility.SetDirty(
                    equipment
                );

                fixedPartTypes++;

                Debug.Log(
                    Prefix +
                    "Đã sửa PartType: " +
                    equipment.name +
                    " | " +
                    oldType +
                    " -> " +
                    resolvedType,
                    equipment
                );
            }

            validShopItems.Add(
                shopItem
            );

            if (!validEquipment.Contains(
                    equipment))
            {
                validEquipment.Add(
                    equipment
                );
            }
        }

        int fixedCustomCount = 0;

        foreach (
            FishingCustomizationUI customUI
            in customUIs)
        {
            if (customUI == null)
                continue;

            Undo.RecordObject(
                customUI,
                "Fix Fishing Customization Source"
            );

            SerializedObject serialized =
                new SerializedObject(
                    customUI
                );

            SetObjectReference(
                serialized,
                "equipmentShopUI",
                shop
            );

            SetObjectReference(
                serialized,
                "inventoryManager",
                inventory
            );

            SetObjectReference(
                serialized,
                "gearStats",
                gearStats
            );

            SetArray(
                serialized,
                "shopEquipmentItems",
                validShopItems
                    .Cast<
                        UnityEngine.Object
                    >()
                    .ToList()
            );

            SetArray(
                serialized,
                "availableEquipment",
                validEquipment
                    .Cast<
                        UnityEngine.Object
                    >()
                    .ToList()
            );

            serialized
                .ApplyModifiedProperties();

            EditorUtility.SetDirty(
                customUI
            );

            fixedCustomCount++;

            Debug.Log(
                Prefix +
                "Đã đồng bộ " +
                customUI.name +
                " | ShopItems=" +
                validShopItems.Count +
                " | Equipment=" +
                validEquipment.Count +
                " | Inventory=" +
                (
                    inventory != null
                        ? inventory.name
                        : "NULL"
                ),
                customUI
            );
        }

        if (gearStats != null &&
            inventory != null)
        {
            SerializedObject gearSerialized =
                new SerializedObject(
                    gearStats
                );

            SetObjectReference(
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

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkAllScenesDirty();

        string result =
            "Đã sửa xong.\n\n" +
            "Customization UI: " +
            fixedCustomCount +
            "\nShop Items đã gắn: " +
            validShopItems.Count +
            "\nEquipment đã gắn: " +
            validEquipment.Count +
            "\nPartType đã sửa: " +
            fixedPartTypes +
            "\nCần câu đã bỏ qua: " +
            skippedRod +
            "\nShop Item thiếu EquipmentData: " +
            missingEquipment +
            "\n\nNhấn Ctrl+S rồi Play để kiểm tra.";

        Debug.Log(
            Prefix +
            "DONE | CustomUI=" +
            fixedCustomCount +
            " | ShopItems=" +
            validShopItems.Count +
            " | Equipment=" +
            validEquipment.Count +
            " | FixedPartTypes=" +
            fixedPartTypes +
            " | SkippedRod=" +
            skippedRod +
            " | MissingEquipment=" +
            missingEquipment
        );

        EditorUtility.DisplayDialog(
            "Fishing Customization",
            result,
            "OK"
        );
    }

    private static void SetObjectReference(
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

    private static bool TryResolvePartType(
        string value,
        out FishingPartType partType)
    {
        string normalized =
            Normalize(value);

        partType =
            FishingPartType.Reel;

        if (normalized.Contains(
                "callistoxsr") ||
            normalized.Contains(
                "maycau") ||
            normalized.Contains(
                "reel"))
        {
            partType =
                FishingPartType.Reel;

            return true;
        }

        if (normalized.Contains(
                "daycau") ||
            normalized.Contains(
                "daydon") ||
            normalized.Contains(
                "monoline") ||
            normalized.Contains(
                "braidedline") ||
            normalized.Contains(
                "fishingline") ||
            normalized.Contains(
                "line"))
        {
            partType =
                FishingPartType.Line;

            return true;
        }

        if (normalized.Contains(
                "moccau") ||
            normalized.Contains(
                "luoicau") ||
            normalized.Contains(
                "hook"))
        {
            partType =
                FishingPartType.Hook;

            return true;
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
            partType =
                FishingPartType.Bait;

            return true;
        }

        return false;
    }

    private static bool IsRodName(
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