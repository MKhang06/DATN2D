#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class RepairFishingLootInventoryLinksEditor
{
    private const string FishFolder =
        "Assets/Bao/Data/InventoryItems/Fishes";

    private sealed class FishDefinition
    {
        public string fileName;
        public string itemId;
        public string displayName;
        public string[] legacyIds;
        public string[] legacyNames;
        public int sellPrice;

        public FishDefinition(
            string fileName,
            string itemId,
            string displayName,
            string[] legacyIds,
            string[] legacyNames,
            int sellPrice)
        {
            this.fileName = fileName;
            this.itemId = itemId;
            this.displayName = displayName;
            this.legacyIds =
                legacyIds ?? Array.Empty<string>();
            this.legacyNames =
                legacyNames ?? Array.Empty<string>();
            this.sellPrice = Mathf.Max(0, sellPrice);
        }
    }

    private static readonly FishDefinition[] Fishes =
    {
        new FishDefinition(
            "SmallSalmon",
            "fish_small_salmon",
            "Cá Hồi Nhỏ",
            Array.Empty<string>(),
            Array.Empty<string>(),
            80
        ),

        new FishDefinition(
            "SmallPinkFish",
            "fish_small_pink",
            "Cá Hồng Nhỏ",
            new[]
            {
                "fish_small_hong"
            },
            new[]
            {
                "Cá hồng nhỏ",
                "Cá Hổng Nhỏ",
                "Cá hổng nhỏ"
            },
            60
        ),

        new FishDefinition(
            "SmallMackerel",
            "fish_small_mackerel",
            "Cá Thu Nhỏ",
            Array.Empty<string>(),
            Array.Empty<string>(),
            150
        ),

        new FishDefinition(
            "SeaBass",
            "fish_sea_bass",
            "Cá Vược",
            Array.Empty<string>(),
            Array.Empty<string>(),
            300
        )
    };

    [MenuItem(
        "Tools/Fishing Database/Repair Fish Loot Inventory Links"
    )]
    public static void Repair()
    {
        EnsureFolder(FishFolder);

        Dictionary<
            string,
            InventoryItemData
        > itemsByCanonicalId =
            new Dictionary<
                string,
                InventoryItemData
            >();

        foreach (FishDefinition definition
                 in Fishes)
        {
            InventoryItemData item =
                FindOrCreateFishItem(
                    definition
                );

            if (item == null)
            {
                Debug.LogError(
                    "Không thể tạo InventoryItemData cho " +
                    definition.displayName +
                    "."
                );

                continue;
            }

            ConfigureFishItem(
                item,
                definition
            );

            itemsByCanonicalId[
                definition.itemId
            ] = item;
        }

        int sceneManagers =
            RepairSceneFishingManagers(
                itemsByCanonicalId
            );

        int prefabManagers =
            RepairPrefabFishingManagers(
                itemsByCanonicalId
            );

        int databases =
            RepairItemDatabases(
                itemsByCanonicalId.Values
            );

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Repair Fish Loot",
            "Đã sửa liên kết InventoryItemData.\n\n" +
            "FishingManager trong Scene: " +
            sceneManagers +
            "\nFishingManager trong Prefab: " +
            prefabManagers +
            "\nItemDatabase cập nhật: " +
            databases +
            "\n\nChạy Validate Everything lại.",
            "OK"
        );
    }

    private static InventoryItemData
        FindOrCreateFishItem(
            FishDefinition definition)
    {
        InventoryItemData[] allItems =
            LoadAllInventoryItems();

        HashSet<string> acceptedIds =
            new HashSet<string>
            {
                NormalizeId(
                    definition.itemId
                )
            };

        foreach (string legacyId
                 in definition.legacyIds)
        {
            acceptedIds.Add(
                NormalizeId(legacyId)
            );
        }

        HashSet<string> acceptedNames =
            new HashSet<string>
            {
                NormalizeName(
                    definition.displayName
                )
            };

        foreach (string legacyName
                 in definition.legacyNames)
        {
            acceptedNames.Add(
                NormalizeName(legacyName)
            );
        }

        foreach (InventoryItemData item
                 in allItems)
        {
            if (item == null)
                continue;

            if (acceptedIds.Contains(
                    NormalizeId(item.ItemId)))
            {
                return item;
            }

            if (acceptedNames.Contains(
                    NormalizeName(
                        item.DisplayName)))
            {
                return item;
            }
        }

        string path =
            FishFolder +
            "/Item_Fish_" +
            definition.fileName +
            ".asset";

        InventoryItemData existing =
            AssetDatabase.LoadAssetAtPath<
                InventoryItemData
            >(path);

        if (existing != null)
            return existing;

        InventoryItemData created =
            ScriptableObject.CreateInstance<
                InventoryItemData
            >();

        AssetDatabase.CreateAsset(
            created,
            path
        );

        return created;
    }

    private static void ConfigureFishItem(
        InventoryItemData item,
        FishDefinition definition)
    {
        SerializedObject serialized =
            new SerializedObject(item);

        SetString(
            serialized,
            "itemId",
            definition.itemId
        );

        SetString(
            serialized,
            "displayName",
            definition.displayName
        );

        /*
         * InventoryItemType.Fish = 1
         */
        SetEnum(
            serialized,
            "itemType",
            1
        );

        /*
         * InventoryStackMode.Separate = 1
         */
        SetEnum(
            serialized,
            "stackMode",
            1
        );

        SetInt(
            serialized,
            "maxStack",
            1
        );

        SetBool(
            serialized,
            "uniqueOwnership",
            false
        );

        SetInt(
            serialized,
            "buyPrice",
            0
        );

        SetInt(
            serialized,
            "sellPrice",
            definition.sellPrice
        );

        serialized
            .ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(item);
    }

    private static int
        RepairSceneFishingManagers(
            Dictionary<
                string,
                InventoryItemData
            > itemsByCanonicalId)
    {
        FishingManager[] managers =
            Resources.FindObjectsOfTypeAll<
                FishingManager
            >();

        int repaired = 0;

        foreach (FishingManager manager
                 in managers)
        {
            if (manager == null)
                continue;

            if (!manager.gameObject.scene.IsValid())
                continue;

            if (RepairManager(
                    manager,
                    itemsByCanonicalId))
            {
                EditorUtility.SetDirty(manager);

                EditorSceneManager.MarkSceneDirty(
                    manager.gameObject.scene
                );

                repaired++;
            }
        }

        return repaired;
    }

    private static int
        RepairPrefabFishingManagers(
            Dictionary<
                string,
                InventoryItemData
            > itemsByCanonicalId)
    {
        string[] prefabGuids =
            AssetDatabase.FindAssets(
                "t:Prefab",
                new[] { "Assets" }
            );

        int repaired = 0;

        foreach (string guid in prefabGuids)
        {
            string path =
                AssetDatabase.GUIDToAssetPath(
                    guid
                );

            GameObject root = null;

            try
            {
                root =
                    PrefabUtility.LoadPrefabContents(
                        path
                    );

                FishingManager[] managers =
                    root.GetComponentsInChildren<
                        FishingManager
                    >(true);

                bool prefabChanged = false;

                foreach (FishingManager manager
                         in managers)
                {
                    if (RepairManager(
                            manager,
                            itemsByCanonicalId))
                    {
                        EditorUtility.SetDirty(
                            manager
                        );

                        repaired++;
                        prefabChanged = true;
                    }
                }

                if (prefabChanged)
                {
                    PrefabUtility
                        .SaveAsPrefabAsset(
                            root,
                            path
                        );
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    "Không thể kiểm tra Prefab: " +
                    path +
                    "\n" +
                    exception.Message
                );
            }
            finally
            {
                if (root != null)
                {
                    PrefabUtility
                        .UnloadPrefabContents(
                            root
                        );
                }
            }
        }

        return repaired;
    }

    private static bool RepairManager(
        FishingManager manager,
        Dictionary<
            string,
            InventoryItemData
        > itemsByCanonicalId)
    {
        SerializedObject serializedManager =
            new SerializedObject(manager);

        SerializedProperty fishLoots =
            serializedManager.FindProperty(
                "fishLoots"
            );

        if (fishLoots == null ||
            !fishLoots.isArray)
        {
            Debug.LogError(
                "FishingManager không có field fishLoots.",
                manager
            );

            return false;
        }

        bool changed = false;

        /*
         * Bảo đảm đủ 4 phần tử.
         */
        if (fishLoots.arraySize <
            Fishes.Length)
        {
            fishLoots.arraySize =
                Fishes.Length;

            changed = true;
        }

        for (int i = 0;
             i < Fishes.Length;
             i++)
        {
            FishDefinition definition =
                FindDefinitionForLoot(
                    fishLoots,
                    i
                );

            if (definition == null)
                definition = Fishes[i];

            if (!itemsByCanonicalId.TryGetValue(
                    definition.itemId,
                    out InventoryItemData item))
            {
                Debug.LogError(
                    "Không tìm thấy InventoryItemData cho " +
                    definition.displayName +
                    ".",
                    manager
                );

                continue;
            }

            SerializedProperty loot =
                fishLoots
                    .GetArrayElementAtIndex(i);

            SerializedProperty inventoryItem =
                loot.FindPropertyRelative(
                    "inventoryItem"
                );

            SerializedProperty itemName =
                loot.FindPropertyRelative(
                    "itemName"
                );

            SerializedProperty icon =
                loot.FindPropertyRelative(
                    "icon"
                );

            if (inventoryItem == null)
            {
                Debug.LogError(
                    "FishingLoot không có field inventoryItem.",
                    manager
                );

                continue;
            }

            if (inventoryItem
                    .objectReferenceValue !=
                item)
            {
                inventoryItem
                    .objectReferenceValue =
                        item;

                changed = true;
            }

            if (itemName != null &&
                itemName.stringValue !=
                    definition.displayName)
            {
                itemName.stringValue =
                    definition.displayName;

                changed = true;
            }

            if (icon != null)
            {
                Sprite currentLootIcon =
                    icon.objectReferenceValue
                        as Sprite;

                if (item.Icon == null &&
                    currentLootIcon != null)
                {
                    AssignItemIcon(
                        item,
                        currentLootIcon
                    );
                }
                else if (item.Icon != null &&
                         currentLootIcon !=
                            item.Icon)
                {
                    icon.objectReferenceValue =
                        item.Icon;

                    changed = true;
                }
            }
        }

        if (!changed)
            return false;

        serializedManager
            .ApplyModifiedPropertiesWithoutUndo();

        return true;
    }

    private static FishDefinition
        FindDefinitionForLoot(
            SerializedProperty fishLoots,
            int index)
    {
        if (index < 0 ||
            index >= fishLoots.arraySize)
        {
            return null;
        }

        SerializedProperty loot =
            fishLoots
                .GetArrayElementAtIndex(index);

        SerializedProperty itemName =
            loot.FindPropertyRelative(
                "itemName"
            );

        SerializedProperty inventoryItem =
            loot.FindPropertyRelative(
                "inventoryItem"
            );

        InventoryItemData existingItem =
            inventoryItem != null
                ? inventoryItem
                    .objectReferenceValue
                    as InventoryItemData
                : null;

        string existingId =
            existingItem != null
                ? NormalizeId(
                    existingItem.ItemId
                )
                : string.Empty;

        string existingName =
            itemName != null
                ? NormalizeName(
                    itemName.stringValue
                )
                : string.Empty;

        foreach (FishDefinition definition
                 in Fishes)
        {
            if (NormalizeId(
                    definition.itemId) ==
                existingId)
            {
                return definition;
            }

            if (definition.legacyIds.Any(
                    legacyId =>
                        NormalizeId(legacyId) ==
                        existingId))
            {
                return definition;
            }

            if (NormalizeName(
                    definition.displayName) ==
                existingName)
            {
                return definition;
            }

            if (definition.legacyNames.Any(
                    legacyName =>
                        NormalizeName(legacyName) ==
                        existingName))
            {
                return definition;
            }
        }

        return null;
    }

    private static int RepairItemDatabases(
        IEnumerable<InventoryItemData>
            requiredFishItems)
    {
        ItemDatabase[] databases =
            Resources.FindObjectsOfTypeAll<
                ItemDatabase
            >();

        int repaired = 0;

        foreach (ItemDatabase database
                 in databases)
        {
            if (database == null)
                continue;

            if (!database.gameObject.scene.IsValid())
                continue;

            SerializedObject serializedDatabase =
                new SerializedObject(database);

            SerializedProperty items =
                serializedDatabase.FindProperty(
                    "items"
                );

            if (items == null ||
                !items.isArray)
            {
                Debug.LogError(
                    "ItemDatabase không có field items.",
                    database
                );

                continue;
            }

            List<InventoryItemData> merged =
                new List<InventoryItemData>();

            for (int i = 0;
                 i < items.arraySize;
                 i++)
            {
                InventoryItemData existing =
                    items.GetArrayElementAtIndex(i)
                        .objectReferenceValue
                        as InventoryItemData;

                if (existing != null &&
                    !merged.Contains(existing))
                {
                    merged.Add(existing);
                }
            }

            foreach (InventoryItemData fishItem
                     in requiredFishItems)
            {
                if (fishItem != null &&
                    !merged.Contains(fishItem))
                {
                    merged.Add(fishItem);
                }
            }

            items.arraySize = merged.Count;

            for (int i = 0;
                 i < merged.Count;
                 i++)
            {
                items.GetArrayElementAtIndex(i)
                    .objectReferenceValue =
                        merged[i];
            }

            serializedDatabase
                .ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(database);
            EditorSceneManager.MarkSceneDirty(
                database.gameObject.scene
            );

            database.RebuildLookup();

            repaired++;
        }

        /*
         * Scene chưa có database thì tạo mới.
         */
        if (repaired == 0)
        {
            GameObject systems =
                GameObject.Find("GameSystems");

            if (systems == null)
            {
                systems =
                    new GameObject(
                        "GameSystems"
                    );
            }

            GameObject databaseObject =
                new GameObject(
                    "ItemDatabase"
                );

            databaseObject.transform.SetParent(
                systems.transform
            );

            ItemDatabase database =
                databaseObject.AddComponent<
                    ItemDatabase
                >();

            SerializedObject serializedDatabase =
                new SerializedObject(database);

            SerializedProperty items =
                serializedDatabase.FindProperty(
                    "items"
                );

            List<InventoryItemData> allItems =
                LoadAllInventoryItems()
                    .Where(item => item != null)
                    .Distinct()
                    .ToList();

            items.arraySize = allItems.Count;

            for (int i = 0;
                 i < allItems.Count;
                 i++)
            {
                items.GetArrayElementAtIndex(i)
                    .objectReferenceValue =
                        allItems[i];
            }

            serializedDatabase
                .ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(database);
            EditorSceneManager.MarkSceneDirty(
                databaseObject.scene
            );

            repaired = 1;
        }

        return repaired;
    }

    private static InventoryItemData[]
        LoadAllInventoryItems()
    {
        string[] guids =
            AssetDatabase.FindAssets(
                "t:InventoryItemData"
            );

        List<InventoryItemData> result =
            new List<InventoryItemData>();

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

            if (item != null)
                result.Add(item);
        }

        return result.ToArray();
    }

    private static void AssignItemIcon(
        InventoryItemData item,
        Sprite icon)
    {
        if (item == null ||
            icon == null)
        {
            return;
        }

        SerializedObject serializedItem =
            new SerializedObject(item);

        SerializedProperty iconProperty =
            serializedItem.FindProperty(
                "icon"
            );

        if (iconProperty == null)
            return;

        iconProperty.objectReferenceValue =
            icon;

        serializedItem
            .ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(item);
    }

    private static void EnsureFolder(
        string fullPath)
    {
        string[] parts =
            fullPath.Split('/');

        string current = parts[0];

        for (int i = 1;
             i < parts.Length;
             i++)
        {
            string next =
                current +
                "/" +
                parts[i];

            if (!AssetDatabase.IsValidFolder(
                    next))
            {
                AssetDatabase.CreateFolder(
                    current,
                    parts[i]
                );
            }

            current = next;
        }
    }

    private static string NormalizeId(
        string value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Trim().ToLowerInvariant();
    }

    private static string NormalizeName(
        string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        string decomposed =
            value.Trim()
                .Normalize(
                    NormalizationForm.FormD
                );

        StringBuilder builder =
            new StringBuilder();

        foreach (char character in decomposed)
        {
            UnicodeCategory category =
                CharUnicodeInfo.GetUnicodeCategory(
                    character
                );

            if (category ==
                UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetterOrDigit(character))
            {
                builder.Append(
                    char.ToLowerInvariant(character)
                );
            }
        }

        return builder
            .ToString()
            .Normalize(
                NormalizationForm.FormC
            );
    }

    private static void SetString(
        SerializedObject target,
        string name,
        string value)
    {
        SerializedProperty property =
            target.FindProperty(name);

        if (property != null)
            property.stringValue = value;
    }

    private static void SetInt(
        SerializedObject target,
        string name,
        int value)
    {
        SerializedProperty property =
            target.FindProperty(name);

        if (property != null)
            property.intValue = value;
    }

    private static void SetBool(
        SerializedObject target,
        string name,
        bool value)
    {
        SerializedProperty property =
            target.FindProperty(name);

        if (property != null)
            property.boolValue = value;
    }

    private static void SetEnum(
        SerializedObject target,
        string name,
        int index)
    {
        SerializedProperty property =
            target.FindProperty(name);

        if (property == null ||
            property.propertyType !=
                SerializedPropertyType.Enum)
        {
            return;
        }

        property.enumValueIndex =
            Mathf.Clamp(
                index,
                0,
                property.enumNames.Length - 1
            );
    }
}
#endif