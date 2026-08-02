#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class RepairFishingCustomizationOwnershipEditor
{
    private const BindingFlags MemberFlags =
        BindingFlags.Instance |
        BindingFlags.Public |
        BindingFlags.NonPublic;

    [MenuItem(
        "Tools/Fishing/Customization Ownership Fix/Repair All Links"
    )]
    public static void RepairAllLinks()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog(
                "Fishing Customization Fix",
                "Hãy thoát Play Mode trước khi sửa.",
                "OK"
            );

            return;
        }

        InventoryManager sceneInventory =
            FindSceneObject<
                InventoryManager
            >();

        Dictionary<string, InventoryItemData>
            itemLookup =
                BuildInventoryItemLookup();

        List<FishingRodPartDefinition>
            definitions =
                RepairRodPartDefinitions(
                    itemLookup
                );

        int loadoutCount =
            RepairLoadouts(
                definitions,
                sceneInventory
            );

        int shopCount =
            RepairShopItemLinks(
                itemLookup
            );

        int starterCount =
            RepairStarterControllers(
                definitions,
                itemLookup,
                sceneInventory
            );

        int syncCount =
            EnsureRuntimeSyncComponents();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Fishing Customization Fix",
            "Đã sửa liên kết sở hữu trang bị.\n\n" +
            "Rod Part Definitions: " +
            definitions.Count +
            "\nFishingRodLoadout: " +
            loadoutCount +
            "\nShop Item Assets: " +
            shopCount +
            "\nStarter Controllers: " +
            starterCount +
            "\nRuntime Sync Components: " +
            syncCount +
            "\n\n" +
            "Nhấn Ctrl + S, vào Play Mode rồi mua/thử lại.",
            "OK"
        );

        Debug.Log(
            "[Fishing Customization Fix] Hoàn tất.\n" +
            "Quan trọng: FishingRodLoadout đã được gán " +
            definitions.Count +
            " Part Definitions và cùng InventoryManager với Shop."
        );
    }

    [MenuItem(
        "Tools/Fishing/Customization Ownership Fix/Validate Links"
    )]
    public static void ValidateLinks()
    {
        FishingRodLoadout[] loadouts =
            FindSceneObjects<
                FishingRodLoadout
            >();

        int problems = 0;

        foreach (FishingRodLoadout loadout
                 in loadouts)
        {
            if (loadout == null)
                continue;

            int count =
                loadout.PartDefinitions != null
                    ? loadout.PartDefinitions.Count
                    : 0;

            if (count <= 0)
            {
                problems++;

                Debug.LogError(
                    "[Fishing Customization Fix] " +
                    loadout.name +
                    " có Part Definitions = 0.",
                    loadout
                );
            }

            if (loadout.Inventory == null)
            {
                problems++;

                Debug.LogError(
                    "[Fishing Customization Fix] " +
                    loadout.name +
                    " chưa gắn InventoryManager.",
                    loadout
                );
            }
        }

        string[] definitionGuids =
            AssetDatabase.FindAssets(
                "t:FishingRodPartDefinition",
                new[] { "Assets" }
            );

        foreach (string guid
                 in definitionGuids)
        {
            string path =
                AssetDatabase.GUIDToAssetPath(
                    guid
                );

            FishingRodPartDefinition definition =
                AssetDatabase.LoadAssetAtPath<
                    FishingRodPartDefinition
                >(path);

            if (definition == null)
                continue;

            if (definition.InventoryItem ==
                null)
            {
                problems++;

                Debug.LogError(
                    "[Fishing Customization Fix] " +
                    definition.name +
                    " chưa liên kết InventoryItemData.",
                    definition
                );
            }
        }

        if (problems == 0)
        {
            Debug.Log(
                "[Fishing Customization Fix] " +
                "Tất cả liên kết đều hợp lệ."
            );

            EditorUtility.DisplayDialog(
                "Fishing Customization Fix",
                "Tất cả liên kết đều hợp lệ.",
                "OK"
            );
        }
        else
        {
            EditorUtility.DisplayDialog(
                "Fishing Customization Fix",
                "Còn " +
                problems +
                " lỗi liên kết. Xem Console.",
                "OK"
            );
        }
    }

    private static Dictionary<
        string,
        InventoryItemData
    > BuildInventoryItemLookup()
    {
        Dictionary<string, InventoryItemData>
            result =
                new Dictionary<
                    string,
                    InventoryItemData
                >(
                    StringComparer.OrdinalIgnoreCase
                );

        string[] guids =
            AssetDatabase.FindAssets(
                "t:InventoryItemData",
                new[] { "Assets" }
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

            AddLookupKey(
                result,
                item.ItemId,
                item
            );

            AddLookupKey(
                result,
                item.DisplayName,
                item
            );

            AddLookupKey(
                result,
                item.name,
                item
            );
        }

        return result;
    }

    private static List<
        FishingRodPartDefinition
    > RepairRodPartDefinitions(
        Dictionary<string, InventoryItemData>
            itemLookup)
    {
        List<FishingRodPartDefinition>
            result =
                new List<
                    FishingRodPartDefinition
                >();

        string[] guids =
            AssetDatabase.FindAssets(
                "t:FishingRodPartDefinition",
                new[] { "Assets" }
            );

        foreach (string guid in guids)
        {
            string path =
                AssetDatabase.GUIDToAssetPath(
                    guid
                );

            FishingRodPartDefinition definition =
                AssetDatabase.LoadAssetAtPath<
                    FishingRodPartDefinition
                >(path);

            if (definition == null)
                continue;

            SerializedObject serialized =
                new SerializedObject(
                    definition
                );

            SerializedProperty itemProperty =
                serialized.FindProperty(
                    "inventoryItem"
                );

            InventoryItemData item =
                itemProperty != null
                    ? itemProperty
                        .objectReferenceValue
                        as InventoryItemData
                    : null;

            if (item == null)
            {
                string guessedKey =
                    GetDefinitionKey(
                        definition.name
                    );

                item =
                    FindItem(
                        itemLookup,
                        guessedKey
                    );

                if (item == null)
                {
                    item =
                        FindItem(
                            itemLookup,
                            definition.name
                        );
                }

                if (itemProperty != null &&
                    item != null)
                {
                    itemProperty
                        .objectReferenceValue =
                            item;
                }
            }

            if (item != null)
            {
                SerializedProperty slotProperty =
                    serialized.FindProperty(
                        "slotType"
                    );

                if (slotProperty != null)
                {
                    int slotIndex =
                        ResolveSlotIndex(
                            item.ItemId
                        );

                    if (slotIndex >= 0)
                    {
                        slotProperty
                            .enumValueIndex =
                                slotIndex;
                    }
                }
            }

            serialized
                .ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(
                definition
            );

            result.Add(definition);
        }

        result.Sort(
            (left, right) =>
            {
                int slotCompare =
                    left.SlotType.CompareTo(
                        right.SlotType
                    );

                if (slotCompare != 0)
                    return slotCompare;

                return string.Compare(
                    left.ItemName,
                    right.ItemName,
                    StringComparison
                        .CurrentCultureIgnoreCase
                );
            }
        );

        return result;
    }

    private static int RepairLoadouts(
        List<FishingRodPartDefinition>
            definitions,
        InventoryManager sceneInventory)
    {
        FishingRodLoadout[] loadouts =
            FindSceneObjects<
                FishingRodLoadout
            >();

        int changedCount = 0;

        foreach (FishingRodLoadout loadout
                 in loadouts)
        {
            if (loadout == null)
                continue;

            SerializedObject serialized =
                new SerializedObject(loadout);

            SerializedProperty inventoryProperty =
                serialized.FindProperty(
                    "inventoryManager"
                );

            if (inventoryProperty != null &&
                sceneInventory != null)
            {
                inventoryProperty
                    .objectReferenceValue =
                        sceneInventory;
            }

            SerializedProperty definitionsProperty =
                serialized.FindProperty(
                    "partDefinitions"
                );

            if (definitionsProperty != null &&
                definitionsProperty.isArray)
            {
                definitionsProperty.arraySize =
                    definitions.Count;

                for (int i = 0;
                     i < definitions.Count;
                     i++)
                {
                    definitionsProperty
                        .GetArrayElementAtIndex(i)
                        .objectReferenceValue =
                            definitions[i];
                }
            }

            serialized
                .ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(loadout);

            if (loadout.gameObject
                    .scene.IsValid())
            {
                EditorSceneManager.MarkSceneDirty(
                    loadout.gameObject.scene
                );
            }

            changedCount++;
        }

        return changedCount;
    }

    private static int RepairShopItemLinks(
        Dictionary<string, InventoryItemData>
            itemLookup)
    {
        Type shopType =
            FindType(
                "FishingEquipmentShopItemData"
            );

        if (shopType == null)
            return 0;

        string[] guids =
            AssetDatabase.FindAssets(
                "t:FishingEquipmentShopItemData",
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
                    shopType
                );

            if (asset == null)
                continue;

            SerializedObject serialized =
                new SerializedObject(asset);

            SerializedProperty itemProperty =
                serialized.FindProperty(
                    "inventoryItem"
                );

            InventoryItemData current =
                itemProperty != null
                    ? itemProperty
                        .objectReferenceValue
                        as InventoryItemData
                    : null;

            InventoryItemData resolved =
                current;

            if (resolved == null)
            {
                string[] candidateValues =
                {
                    ReadString(
                        serialized,
                        "itemId"
                    ),
                    ReadString(
                        serialized,
                        "equipmentId"
                    ),
                    ReadString(
                        serialized,
                        "itemName"
                    ),
                    ReadString(
                        serialized,
                        "displayName"
                    ),
                    asset.name
                };

                foreach (string candidate
                         in candidateValues)
                {
                    resolved =
                        FindItem(
                            itemLookup,
                            candidate
                        );

                    if (resolved != null)
                        break;
                }

                if (resolved == null)
                {
                    UnityEngine.Object equipmentData =
                        ReadObject(
                            serialized,
                            "equipmentData"
                        );

                    resolved =
                        ResolveItemFromObject(
                            equipmentData,
                            itemLookup
                        );
                }
            }

            if (itemProperty == null ||
                resolved == null ||
                current == resolved)
            {
                continue;
            }

            itemProperty.objectReferenceValue =
                resolved;

            serialized
                .ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(asset);
            changedCount++;
        }

        return changedCount;
    }

    private static int RepairStarterControllers(
        List<FishingRodPartDefinition>
            definitions,
        Dictionary<string, InventoryItemData>
            itemLookup,
        InventoryManager sceneInventory)
    {
        Type controllerType =
            FindType(
                "FishingStarterGearController"
            );

        if (controllerType == null)
            return 0;

        MonoBehaviour[] behaviours =
            Resources.FindObjectsOfTypeAll<
                MonoBehaviour
            >();

        int changedCount = 0;

        foreach (MonoBehaviour behaviour
                 in behaviours)
        {
            if (behaviour == null ||
                behaviour.GetType() !=
                    controllerType ||
                !behaviour.gameObject
                    .scene.IsValid())
            {
                continue;
            }

            SerializedObject serialized =
                new SerializedObject(
                    behaviour
                );

            SetObject(
                serialized,
                "inventoryManager",
                sceneInventory
            );

            SetObject(
                serialized,
                "featherLightRod",
                FindItem(
                    itemLookup,
                    "equipment_rod_feather_light"
                )
            );

            SetObject(
                serialized,
                "callistoXsrReel",
                FindDefinition(
                    definitions,
                    "equipment_reel_callisto_xsr"
                )
            );

            SetObject(
                serialized,
                "cheapMonoLine",
                FindDefinition(
                    definitions,
                    "equipment_line_mono_cheap"
                )
            );

            SetObject(
                serialized,
                "hookNumberOne",
                FindDefinition(
                    definitions,
                    "equipment_hook_1"
                )
            );

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

    private static int
        EnsureRuntimeSyncComponents()
    {
        FishingRodLoadout[] loadouts =
            FindSceneObjects<
                FishingRodLoadout
            >();

        int count = 0;

        foreach (FishingRodLoadout loadout
                 in loadouts)
        {
            if (loadout == null)
                continue;

            FishingCustomizationInventorySync
                sync =
                    loadout.GetComponent<
                        FishingCustomizationInventorySync
                    >();

            if (sync == null)
            {
                sync =
                    Undo.AddComponent<
                        FishingCustomizationInventorySync
                    >(
                        loadout.gameObject
                    );
            }

            EditorUtility.SetDirty(sync);

            EditorSceneManager.MarkSceneDirty(
                loadout.gameObject.scene
            );

            count++;
        }

        return count;
    }

    private static InventoryItemData
        ResolveItemFromObject(
            UnityEngine.Object source,
            Dictionary<string, InventoryItemData>
                itemLookup)
    {
        if (source == null)
            return null;

        SerializedObject serialized =
            new SerializedObject(source);

        SerializedProperty itemProperty =
            serialized.FindProperty(
                "inventoryItem"
            );

        if (itemProperty != null &&
            itemProperty.objectReferenceValue
                is InventoryItemData direct)
        {
            return direct;
        }

        string[] candidates =
        {
            ReadString(
                serialized,
                "itemId"
            ),
            ReadString(
                serialized,
                "equipmentId"
            ),
            ReadString(
                serialized,
                "itemName"
            ),
            ReadString(
                serialized,
                "displayName"
            ),
            source.name
        };

        foreach (string candidate
                 in candidates)
        {
            InventoryItemData item =
                FindItem(
                    itemLookup,
                    candidate
                );

            if (item != null)
                return item;
        }

        return null;
    }

    private static FishingRodPartDefinition
        FindDefinition(
            List<FishingRodPartDefinition>
                definitions,
            string itemId)
    {
        foreach (
            FishingRodPartDefinition definition
            in definitions)
        {
            if (definition == null)
                continue;

            if (string.Equals(
                    definition.ItemId,
                    itemId,
                    StringComparison
                        .OrdinalIgnoreCase))
            {
                return definition;
            }
        }

        return null;
    }

    private static InventoryItemData FindItem(
        Dictionary<string, InventoryItemData>
            lookup,
        string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return null;

        string normalized =
            Normalize(key);

        if (lookup.TryGetValue(
                normalized,
                out InventoryItemData item))
        {
            return item;
        }

        foreach (
            KeyValuePair<
                string,
                InventoryItemData
            > pair in lookup)
        {
            if (pair.Key.Contains(
                    normalized) ||
                normalized.Contains(
                    pair.Key))
            {
                return pair.Value;
            }
        }

        return null;
    }

    private static void AddLookupKey(
        Dictionary<string, InventoryItemData>
            lookup,
        string key,
        InventoryItemData item)
    {
        if (item == null ||
            string.IsNullOrWhiteSpace(key))
        {
            return;
        }

        string normalized =
            Normalize(key);

        if (!lookup.ContainsKey(
                normalized))
        {
            lookup.Add(
                normalized,
                item
            );
        }
    }

    private static string GetDefinitionKey(
        string assetName)
    {
        if (string.IsNullOrWhiteSpace(
                assetName))
        {
            return string.Empty;
        }

        string result =
            assetName.Trim();

        string[] prefixes =
        {
            "RodPart_",
            "RodPart-",
            "FishingRodPart_",
            "FishingRodPart-"
        };

        foreach (string prefix
                 in prefixes)
        {
            if (result.StartsWith(
                    prefix,
                    StringComparison
                        .OrdinalIgnoreCase))
            {
                result =
                    result.Substring(
                        prefix.Length
                    );

                break;
            }
        }

        return result;
    }

    private static int ResolveSlotIndex(
        string itemId)
    {
        if (string.IsNullOrWhiteSpace(
                itemId))
        {
            return -1;
        }

        string id =
            itemId.ToLowerInvariant();

        if (id.StartsWith(
                "equipment_reel_"))
        {
            return 0;
        }

        if (id.StartsWith(
                "equipment_line_"))
        {
            return 1;
        }

        if (id.StartsWith(
                "equipment_hook_"))
        {
            return 2;
        }

        if (id.StartsWith("bait_"))
            return 3;

        return -1;
    }

    private static string Normalize(
        string value)
    {
        if (string.IsNullOrWhiteSpace(
                value))
        {
            return string.Empty;
        }

        System.Text.StringBuilder builder =
            new System.Text.StringBuilder();

        foreach (char character
                 in value.ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(
                    character))
            {
                builder.Append(character);
            }
        }

        return builder.ToString();
    }

    private static void SetObject(
        SerializedObject serialized,
        string propertyName,
        UnityEngine.Object value)
    {
        if (value == null)
            return;

        SerializedProperty property =
            serialized.FindProperty(
                propertyName
            );

        if (property != null &&
            property.propertyType ==
                SerializedPropertyType
                    .ObjectReference)
        {
            property.objectReferenceValue =
                value;
        }
    }

    private static string ReadString(
        SerializedObject serialized,
        string propertyName)
    {
        SerializedProperty property =
            serialized.FindProperty(
                propertyName
            );

        return property != null &&
               property.propertyType ==
                   SerializedPropertyType
                       .String
            ? property.stringValue
            : string.Empty;
    }

    private static UnityEngine.Object
        ReadObject(
            SerializedObject serialized,
            string propertyName)
    {
        SerializedProperty property =
            serialized.FindProperty(
                propertyName
            );

        return property != null &&
               property.propertyType ==
                   SerializedPropertyType
                       .ObjectReference
            ? property.objectReferenceValue
            : null;
    }

    private static T FindSceneObject<T>()
        where T : Component
    {
        T[] values =
            FindSceneObjects<T>();

        return values.Length > 0
            ? values[0]
            : null;
    }

    private static T[] FindSceneObjects<T>()
        where T : Component
    {
        List<T> result =
            new List<T>();

        T[] values =
            Resources.FindObjectsOfTypeAll<T>();

        foreach (T value in values)
        {
            if (value == null ||
                !value.gameObject
                    .scene.IsValid())
            {
                continue;
            }

            result.Add(value);
        }

        return result.ToArray();
    }

    private static Type FindType(
        string typeName)
    {
        foreach (Assembly assembly
                 in AppDomain.CurrentDomain
                     .GetAssemblies())
        {
            try
            {
                foreach (Type type
                         in assembly.GetTypes())
                {
                    if (type.Name ==
                        typeName)
                    {
                        return type;
                    }
                }
            }
            catch
            {
                // Bỏ qua assembly không đọc được.
            }
        }

        return null;
    }
}
#endif
