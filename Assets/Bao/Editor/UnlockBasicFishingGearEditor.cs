#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class UnlockBasicFishingGearEditor
{
    private sealed class StarterDefinition
    {
        public string itemId;
        public string displayName;
        public string[] aliases;

        public StarterDefinition(
            string itemId,
            string displayName,
            params string[] aliases)
        {
            this.itemId = itemId;
            this.displayName = displayName;
            this.aliases = aliases ?? Array.Empty<string>();
        }
    }

    private static readonly StarterDefinition[] Starters =
    {
        new StarterDefinition(
            "equipment_reel_callisto_xsr",
            "Callisto XSR",
            "callisto xsr",
            "callistoxsr"
        ),

        new StarterDefinition(
            "equipment_rod_feather_light",
            "Feather Light",
            "eq featherlight",
            "eq_featherlight",
            "feather light",
            "featherlight"
        ),

        new StarterDefinition(
            "equipment_line_mono_cheap",
            "Dây Đơn Rẻ Tiền",
            "day don re tien",
            "cheap mono line",
            "cheapmonoline"
        ),

        new StarterDefinition(
            "equipment_hook_1",
            "Móc Câu #1",
            "moc cau 1",
            "hook 1",
            "hook1"
        )
    };

    [MenuItem(
        "Tools/Fishing/Starter Gear/Unlock Basic Gear"
    )]
    public static void UnlockBasicGear()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog(
                "Starter Fishing Gear",
                "Hãy thoát Play Mode trước khi sửa dữ liệu.",
                "OK"
            );

            return;
        }

        int inventoryChanged =
            PatchInventoryItems();

        int equipmentDataChanged =
            PatchAssetsByType(
                "FishingEquipmentData"
            );

        int shopDataChanged =
            PatchAssetsByType(
                "FishingEquipmentShopItemData"
            );

        int rodPartChanged =
            PatchAssetsByType(
                "FishingRodPartDefinition"
            );

        int sourceFilesChanged =
            PatchEquipmentEditorSources();

        int sceneComponentsChanged =
            PatchSceneComponents();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Starter Fishing Gear",
            "Đã mở khóa bộ đồ câu cơ bản.\n\n" +
            "InventoryItemData: " +
            inventoryChanged +
            "\nFishingEquipmentData: " +
            equipmentDataChanged +
            "\nFishingEquipmentShopItemData: " +
            shopDataChanged +
            "\nFishingRodPartDefinition: " +
            rodPartChanged +
            "\nScene components: " +
            sceneComponentsChanged +
            "\nEditor source files: " +
            sourceFilesChanged +
            "\n\n" +
            "Đóng rồi mở lại Shop hoặc bấm Refresh.",
            "OK"
        );

        Debug.Log(
            "[Starter Fishing Gear] Đã mở khóa:\n" +
            "- Callisto XSR\n" +
            "- Feather Light\n" +
            "- Dây Đơn Rẻ Tiền\n" +
            "- Móc Câu #1\n\n" +
            "Shop Required Level = 1\n" +
            "Customization Required Skill = 0"
        );
    }

    [MenuItem(
        "Tools/Fishing/Starter Gear/Validate Basic Gear"
    )]
    public static void ValidateBasicGear()
    {
        int problems = 0;

        problems +=
            ValidateInventoryItems();

        problems +=
            ValidateAssetsByType(
                "FishingEquipmentData"
            );

        problems +=
            ValidateAssetsByType(
                "FishingEquipmentShopItemData"
            );

        problems +=
            ValidateAssetsByType(
                "FishingRodPartDefinition"
            );

        if (problems == 0)
        {
            Debug.Log(
                "[Starter Fishing Gear] Hợp lệ. " +
                "Bốn món cơ bản không còn bị khóa."
            );

            EditorUtility.DisplayDialog(
                "Starter Fishing Gear",
                "Bốn món cơ bản đã hợp lệ và không còn khóa.",
                "OK"
            );
        }
        else
        {
            Debug.LogError(
                "[Starter Fishing Gear] Còn " +
                problems +
                " dữ liệu chưa được mở khóa. " +
                "Xem các lỗi phía trên trong Console."
            );
        }
    }

    private static int PatchInventoryItems()
    {
        string[] guids =
            AssetDatabase.FindAssets(
                "t:InventoryItemData",
                new[] { "Assets" }
            );

        int changedCount = 0;

        foreach (string guid in guids)
        {
            string path =
                AssetDatabase.GUIDToAssetPath(
                    guid
                );

            InventoryItemData item =
                AssetDatabase.LoadAssetAtPath<
                    InventoryItemData
                >(path);

            if (item == null)
                continue;

            StarterDefinition starter =
                FindStarter(
                    item.ItemId,
                    item.DisplayName,
                    item.name
                );

            if (starter == null)
                continue;

            SerializedObject serialized =
                new SerializedObject(item);

            bool changed = false;

            changed |= SetString(
                serialized,
                "displayName",
                starter.displayName
            );

            changed |= SetInt(
                serialized,
                "buyPrice",
                0
            );

            changed |= SetInt(
                serialized,
                "sellPrice",
                0
            );

            changed |= SetInt(
                serialized,
                "maxStack",
                1
            );

            changed |= SetBool(
                serialized,
                "uniqueOwnership",
                true
            );

            changed |= SetFalse(
                serialized,
                "locked",
                "isLocked",
                "requiresUnlock",
                "levelLocked"
            );

            changed |= SetTrue(
                serialized,
                "unlocked",
                "isUnlocked",
                "availableByDefault",
                "starterItem"
            );

            if (!changed)
                continue;

            serialized
                .ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(item);
            changedCount++;

            Debug.Log(
                "[Starter Fishing Gear] Đã sửa InventoryItemData: " +
                starter.displayName,
                item
            );
        }

        return changedCount;
    }

    private static int PatchAssetsByType(
        string typeName)
    {
        Type type = FindType(typeName);

        if (type == null)
        {
            Debug.LogWarning(
                "[Starter Fishing Gear] Không tìm thấy type " +
                typeName +
                ". Bỏ qua."
            );

            return 0;
        }

        string[] guids =
            AssetDatabase.FindAssets(
                "t:" + typeName,
                new[] { "Assets" }
            );

        int changedCount = 0;

        foreach (string guid in guids)
        {
            string path =
                AssetDatabase.GUIDToAssetPath(
                    guid
                );

            UnityEngine.Object asset =
                AssetDatabase.LoadAssetAtPath(
                    path,
                    type
                );

            if (asset == null)
                continue;

            SerializedObject serialized =
                new SerializedObject(asset);

            StarterDefinition starter =
                ResolveStarter(
                    serialized,
                    asset
                );

            if (starter == null)
                continue;

            bool changed = false;

            changed |= SetFirstString(
                serialized,
                starter.displayName,
                "itemName",
                "equipmentName",
                "displayName",
                "shopName"
            );

            /*
             * Shop và Equipment Data dùng level 1,
             * vì PlayerStats bắt đầu từ Level 1.
             */
            changed |= SetFirstInt(
                serialized,
                1,
                "requiredPlayerLevel",
                "requiredLevel",
                "levelRequired",
                "unlockLevel",
                "minimumLevel"
            );

            /*
             * RodPartDefinition dùng kỹ năng 0 để đồ cơ bản
             * không bị khóa trong bảng tùy chỉnh.
             */
            changed |= SetFirstInt(
                serialized,
                0,
                "requiredSkill",
                "skillRequired",
                "requiredFishingSkill",
                "minimumSkill"
            );

            changed |= SetFirstInt(
                serialized,
                0,
                "price",
                "buyPrice",
                "shopPrice"
            );

            changed |= SetFalse(
                serialized,
                "locked",
                "isLocked",
                "requiresUnlock",
                "levelLocked",
                "skillLocked"
            );

            changed |= SetTrue(
                serialized,
                "unlocked",
                "isUnlocked",
                "availableByDefault",
                "starterItem",
                "isStarter"
            );

            if (!changed)
                continue;

            serialized
                .ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(asset);
            changedCount++;

            Debug.Log(
                "[Starter Fishing Gear] Đã sửa " +
                typeName +
                ": " +
                starter.displayName,
                asset
            );
        }

        return changedCount;
    }

    private static int PatchSceneComponents()
    {
        MonoBehaviour[] behaviours =
            Resources.FindObjectsOfTypeAll<
                MonoBehaviour
            >();

        int changedCount = 0;

        foreach (MonoBehaviour behaviour
                 in behaviours)
        {
            if (behaviour == null ||
                !behaviour.gameObject
                    .scene.IsValid())
            {
                continue;
            }

            SerializedObject serialized;

            try
            {
                serialized =
                    new SerializedObject(
                        behaviour
                    );
            }
            catch
            {
                continue;
            }

            StarterDefinition starter =
                ResolveStarter(
                    serialized,
                    behaviour
                );

            if (starter == null)
                continue;

            bool changed = false;

            changed |= SetFirstInt(
                serialized,
                1,
                "requiredPlayerLevel",
                "requiredLevel",
                "levelRequired",
                "unlockLevel"
            );

            changed |= SetFirstInt(
                serialized,
                0,
                "requiredSkill",
                "skillRequired",
                "minimumSkill"
            );

            changed |= SetFalse(
                serialized,
                "locked",
                "isLocked",
                "requiresUnlock"
            );

            if (!changed)
                continue;

            serialized
                .ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(
                behaviour
            );

            EditorSceneManager.MarkSceneDirty(
                behaviour.gameObject.scene
            );

            changedCount++;
        }

        return changedCount;
    }

    private static int PatchEquipmentEditorSources()
    {
        string[] guids =
            AssetDatabase.FindAssets(
                "CreateFishingEquipmentDataEditor t:MonoScript",
                new[] { "Assets" }
            );

        int changedCount = 0;

        foreach (string guid in guids)
        {
            string assetPath =
                AssetDatabase.GUIDToAssetPath(
                    guid
                );

            string absolutePath =
                Path.GetFullPath(assetPath);

            if (!File.Exists(absolutePath))
                continue;

            string text =
                File.ReadAllText(
                    absolutePath
                );

            string original = text;

            text = PatchDefinitionBlock(
                text,
                "equipment_reel_callisto_xsr",
                "Callisto XSR"
            );

            text = PatchDefinitionBlock(
                text,
                "equipment_rod_feather_light",
                "Feather Light"
            );

            text = PatchDefinitionBlock(
                text,
                "equipment_line_mono_cheap",
                "Dây Đơn Rẻ Tiền"
            );

            text = PatchDefinitionBlock(
                text,
                "equipment_hook_1",
                "Móc Câu #1"
            );

            if (text == original)
                continue;

            File.WriteAllText(
                absolutePath,
                text
            );

            changedCount++;

            Debug.Log(
                "[Starter Fishing Gear] Đã sửa nguồn để lần rebuild sau " +
                "không khóa lại: " +
                assetPath
            );
        }

        return changedCount;
    }

    private static string PatchDefinitionBlock(
        string source,
        string itemId,
        string displayName)
    {
        /*
         * Tìm constructor EquipmentDefinition chứa đúng itemId,
         * đổi display name và tham số required level cuối thành 1.
         */
        string pattern =
            @"new\s+EquipmentDefinition\s*\(\s*" +
            @"(?<file>""[^""]*"")\s*,\s*" +
            @"""" +
            Regex.Escape(itemId) +
            @"""\s*,\s*" +
            @"""[^""]*""\s*,\s*" +
            @"(?<category>""[^""]*"")\s*,\s*" +
            @"(?<price>\d+)\s*,\s*" +
            @"(?<level>\d+)\s*\)";

        return Regex.Replace(
            source,
            pattern,
            match =>
                "new EquipmentDefinition(\n" +
                "            " +
                match.Groups["file"].Value +
                ",\n" +
                "            \"" +
                itemId +
                "\",\n" +
                "            \"" +
                displayName +
                "\",\n" +
                "            " +
                match.Groups["category"].Value +
                ",\n" +
                "            " +
                match.Groups["price"].Value +
                ",\n" +
                "            1\n" +
                "        )",
            RegexOptions.Multiline
        );
    }

    private static int ValidateInventoryItems()
    {
        string[] guids =
            AssetDatabase.FindAssets(
                "t:InventoryItemData",
                new[] { "Assets" }
            );

        int problems = 0;
        HashSet<string> found =
            new HashSet<string>(
                StringComparer.OrdinalIgnoreCase
            );

        foreach (string guid in guids)
        {
            string path =
                AssetDatabase.GUIDToAssetPath(
                    guid
                );

            InventoryItemData item =
                AssetDatabase.LoadAssetAtPath<
                    InventoryItemData
                >(path);

            if (item == null)
                continue;

            StarterDefinition starter =
                FindStarter(
                    item.ItemId,
                    item.DisplayName,
                    item.name
                );

            if (starter == null)
                continue;

            found.Add(starter.itemId);
        }

        foreach (StarterDefinition starter
                 in Starters)
        {
            if (found.Contains(
                    starter.itemId))
            {
                continue;
            }

            problems++;

            Debug.LogError(
                "[Starter Fishing Gear] Không tìm thấy InventoryItemData: " +
                starter.itemId
            );
        }

        return problems;
    }

    private static int ValidateAssetsByType(
        string typeName)
    {
        Type type = FindType(typeName);

        if (type == null)
            return 0;

        string[] guids =
            AssetDatabase.FindAssets(
                "t:" + typeName,
                new[] { "Assets" }
            );

        int problems = 0;

        foreach (string guid in guids)
        {
            string path =
                AssetDatabase.GUIDToAssetPath(
                    guid
                );

            UnityEngine.Object asset =
                AssetDatabase.LoadAssetAtPath(
                    path,
                    type
                );

            if (asset == null)
                continue;

            SerializedObject serialized =
                new SerializedObject(asset);

            StarterDefinition starter =
                ResolveStarter(
                    serialized,
                    asset
                );

            if (starter == null)
                continue;

            int requiredLevel =
                ReadFirstInt(
                    serialized,
                    -1,
                    "requiredPlayerLevel",
                    "requiredLevel",
                    "levelRequired",
                    "unlockLevel",
                    "minimumLevel"
                );

            int requiredSkill =
                ReadFirstInt(
                    serialized,
                    -1,
                    "requiredSkill",
                    "skillRequired",
                    "requiredFishingSkill",
                    "minimumSkill"
                );

            bool locked =
                ReadFirstBool(
                    serialized,
                    false,
                    "locked",
                    "isLocked",
                    "requiresUnlock",
                    "levelLocked",
                    "skillLocked"
                );

            if (requiredLevel > 1 ||
                requiredSkill > 0 ||
                locked)
            {
                problems++;

                Debug.LogError(
                    "[Starter Fishing Gear] Vẫn khóa: " +
                    starter.displayName +
                    " | Type=" +
                    typeName +
                    " | RequiredLevel=" +
                    requiredLevel +
                    " | RequiredSkill=" +
                    requiredSkill +
                    " | Locked=" +
                    locked,
                    asset
                );
            }
        }

        return problems;
    }

    private static StarterDefinition
        ResolveStarter(
            SerializedObject serialized,
            UnityEngine.Object asset)
    {
        InventoryItemData item =
            ReadFirstObject<
                InventoryItemData
            >(
                serialized,
                "inventoryItem",
                "itemData",
                "inventoryItemData",
                "equipmentItem"
            );

        if (item != null)
        {
            StarterDefinition byItem =
                FindStarter(
                    item.ItemId,
                    item.DisplayName,
                    item.name
                );

            if (byItem != null)
                return byItem;
        }

        string itemId =
            ReadFirstString(
                serialized,
                "itemId",
                "equipmentId",
                "id"
            );

        string displayName =
            ReadFirstString(
                serialized,
                "itemName",
                "equipmentName",
                "displayName",
                "shopName"
            );

        return FindStarter(
            itemId,
            displayName,
            asset != null
                ? asset.name
                : string.Empty
        );
    }

    private static StarterDefinition FindStarter(
        params string[] values)
    {
        foreach (StarterDefinition starter
                 in Starters)
        {
            foreach (string value
                     in values)
            {
                if (MatchesStarter(
                        starter,
                        value))
                {
                    return starter;
                }
            }
        }

        return null;
    }

    private static bool MatchesStarter(
        StarterDefinition starter,
        string value)
    {
        if (starter == null ||
            string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        string normalized =
            Normalize(value);

        if (normalized ==
            Normalize(starter.itemId))
        {
            return true;
        }

        if (normalized ==
            Normalize(starter.displayName))
        {
            return true;
        }

        foreach (string alias
                 in starter.aliases)
        {
            string normalizedAlias =
                Normalize(alias);

            if (normalized ==
                normalizedAlias)
            {
                return true;
            }

            if (normalized.Contains(
                    normalizedAlias) ||
                normalizedAlias.Contains(
                    normalized))
            {
                return true;
            }
        }

        return false;
    }

    private static string Normalize(
        string value)
    {
        if (string.IsNullOrWhiteSpace(
                value))
        {
            return string.Empty;
        }

        char[] characters =
            value.ToLowerInvariant()
                .ToCharArray();

        System.Text.StringBuilder builder =
            new System.Text.StringBuilder();

        foreach (char character
                 in characters)
        {
            if (char.IsLetterOrDigit(
                    character))
            {
                builder.Append(character);
            }
        }

        return builder.ToString();
    }

    private static Type FindType(
        string typeName)
    {
        foreach (Assembly assembly
                 in AppDomain.CurrentDomain
                     .GetAssemblies())
        {
            Type type =
                assembly.GetType(typeName);

            if (type != null)
                return type;

            try
            {
                foreach (Type candidate
                         in assembly.GetTypes())
                {
                    if (candidate.Name ==
                        typeName)
                    {
                        return candidate;
                    }
                }
            }
            catch
            {
                // Một số assembly không cho đọc toàn bộ type.
            }
        }

        return null;
    }

    private static T ReadFirstObject<T>(
        SerializedObject serialized,
        params string[] names)
        where T : UnityEngine.Object
    {
        foreach (string name in names)
        {
            SerializedProperty property =
                serialized.FindProperty(name);

            if (property == null ||
                property.propertyType !=
                    SerializedPropertyType
                        .ObjectReference)
            {
                continue;
            }

            T value =
                property.objectReferenceValue
                as T;

            if (value != null)
                return value;
        }

        return null;
    }

    private static string ReadFirstString(
        SerializedObject serialized,
        params string[] names)
    {
        foreach (string name in names)
        {
            SerializedProperty property =
                serialized.FindProperty(name);

            if (property != null &&
                property.propertyType ==
                    SerializedPropertyType
                        .String)
            {
                return property.stringValue;
            }
        }

        return string.Empty;
    }

    private static int ReadFirstInt(
        SerializedObject serialized,
        int fallback,
        params string[] names)
    {
        foreach (string name in names)
        {
            SerializedProperty property =
                serialized.FindProperty(name);

            if (property != null &&
                property.propertyType ==
                    SerializedPropertyType
                        .Integer)
            {
                return property.intValue;
            }
        }

        return fallback;
    }

    private static bool ReadFirstBool(
        SerializedObject serialized,
        bool fallback,
        params string[] names)
    {
        foreach (string name in names)
        {
            SerializedProperty property =
                serialized.FindProperty(name);

            if (property != null &&
                property.propertyType ==
                    SerializedPropertyType
                        .Boolean)
            {
                return property.boolValue;
            }
        }

        return fallback;
    }

    private static bool SetString(
        SerializedObject serialized,
        string propertyName,
        string value)
    {
        SerializedProperty property =
            serialized.FindProperty(
                propertyName
            );

        if (property == null ||
            property.propertyType !=
                SerializedPropertyType.String ||
            property.stringValue == value)
        {
            return false;
        }

        property.stringValue = value;
        return true;
    }

    private static bool SetInt(
        SerializedObject serialized,
        string propertyName,
        int value)
    {
        SerializedProperty property =
            serialized.FindProperty(
                propertyName
            );

        if (property == null ||
            property.propertyType !=
                SerializedPropertyType.Integer ||
            property.intValue == value)
        {
            return false;
        }

        property.intValue = value;
        return true;
    }

    private static bool SetBool(
        SerializedObject serialized,
        string propertyName,
        bool value)
    {
        SerializedProperty property =
            serialized.FindProperty(
                propertyName
            );

        if (property == null ||
            property.propertyType !=
                SerializedPropertyType.Boolean ||
            property.boolValue == value)
        {
            return false;
        }

        property.boolValue = value;
        return true;
    }

    private static bool SetFirstString(
        SerializedObject serialized,
        string value,
        params string[] propertyNames)
    {
        foreach (string name in propertyNames)
        {
            if (SetString(
                    serialized,
                    name,
                    value))
            {
                return true;
            }

            SerializedProperty property =
                serialized.FindProperty(name);

            if (property != null &&
                property.propertyType ==
                    SerializedPropertyType.String &&
                property.stringValue == value)
            {
                return false;
            }
        }

        return false;
    }

    private static bool SetFirstInt(
        SerializedObject serialized,
        int value,
        params string[] propertyNames)
    {
        bool changed = false;

        foreach (string name in propertyNames)
        {
            changed |= SetInt(
                serialized,
                name,
                value
            );
        }

        return changed;
    }

    private static bool SetFalse(
        SerializedObject serialized,
        params string[] propertyNames)
    {
        bool changed = false;

        foreach (string name in propertyNames)
        {
            changed |= SetBool(
                serialized,
                name,
                false
            );
        }

        return changed;
    }

    private static bool SetTrue(
        SerializedObject serialized,
        params string[] propertyNames)
    {
        bool changed = false;

        foreach (string name in propertyNames)
        {
            changed |= SetBool(
                serialized,
                name,
                true
            );
        }

        return changed;
    }
}
#endif