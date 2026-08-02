#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class CreateFishingRodPartDefinitionsEditor
{
    private const string OutputFolder =
        "Assets/Bao/Data/Fishing/RodParts";

    private sealed class DefaultStats
    {
        public int requiredSkill;
        public float strength;
        public float speed;
        public float depth;

        public DefaultStats(
            int requiredSkill,
            float strength,
            float speed,
            float depth)
        {
            this.requiredSkill = Mathf.Max(0, requiredSkill);
            this.strength = strength;
            this.speed = speed;
            this.depth = depth;
        }
    }

    private static readonly Dictionary<string, DefaultStats>
        KnownStats =
            new Dictionary<string, DefaultStats>(
                StringComparer.OrdinalIgnoreCase)
            {
                {
                    "equipment_reel_callisto_xsr",
                    new DefaultStats(7, 2f, 4f, 1f)
                },
                {
                    "equipment_line_mono_cheap",
                    new DefaultStats(1, 1f, 0f, 1f)
                },
                {
                    "equipment_line_mono_medium",
                    new DefaultStats(2, 2f, 0.25f, 2f)
                },
                {
                    "equipment_line_mobey_mono",
                    new DefaultStats(3, 2.5f, 0.5f, 2.5f)
                },
                {
                    "equipment_line_braided_noodle",
                    new DefaultStats(4, 3f, 0.75f, 3f)
                },
                {
                    "equipment_line_braided_lightning",
                    new DefaultStats(6, 4f, 1.5f, 4f)
                },
                {
                    "equipment_line_braided_king",
                    new DefaultStats(8, 5f, 1f, 5f)
                },
                {
                    "equipment_hook_6",
                    new DefaultStats(1, 1f, 0.5f, 0f)
                },
                {
                    "equipment_hook_1",
                    new DefaultStats(3, 2f, 0.25f, 0.5f)
                },
                {
                    "equipment_hook_heavy",
                    new DefaultStats(6, 4f, -0.5f, 1f)
                }
            };

    [MenuItem(
        "Tools/Fishing/Rod Customization/Rebuild Part Definitions"
    )]
    public static void RebuildDefinitions()
    {
        EnsureFolder(OutputFolder);

        string[] itemGuids =
            AssetDatabase.FindAssets(
                "t:InventoryItemData",
                new[] { "Assets" }
            );

        List<FishingRodPartDefinition> definitions =
            new List<FishingRodPartDefinition>();

        foreach (string guid in itemGuids)
        {
            string path =
                AssetDatabase.GUIDToAssetPath(guid);

            InventoryItemData item =
                AssetDatabase.LoadAssetAtPath<
                    InventoryItemData
                >(path);

            if (item == null ||
                string.IsNullOrWhiteSpace(item.ItemId))
            {
                continue;
            }

            if (!TryResolveSlotType(
                    item.ItemId,
                    out FishingRodPartSlotType slotType))
            {
                continue;
            }

            FishingRodPartDefinition definition =
                CreateOrUpdateDefinition(
                    item,
                    slotType
                );

            if (definition != null)
                definitions.Add(definition);
        }

        definitions.Sort(
            (left, right) =>
            {
                int typeCompare =
                    left.SlotType.CompareTo(right.SlotType);

                if (typeCompare != 0)
                    return typeCompare;

                return string.Compare(
                    left.ItemName,
                    right.ItemName,
                    StringComparison.CurrentCultureIgnoreCase
                );
            }
        );

        int assignedLoadouts =
            AssignToSceneLoadouts(definitions);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Rod Customization",
            "Đã tạo/cập nhật " +
            definitions.Count +
            " bộ phận cần câu.\n\n" +
            "FishingRodLoadout đã được gán: " +
            assignedLoadouts +
            "\n\n" +
            "Chỉ số được tạo là giá trị mặc định. " +
            "Có thể mở từng asset RodPart_ để cân chỉnh.",
            "OK"
        );

        Debug.Log(
            "[Rod Customization] Đã tạo/cập nhật " +
            definitions.Count +
            " FishingRodPartDefinition."
        );
    }

    private static FishingRodPartDefinition
        CreateOrUpdateDefinition(
            InventoryItemData item,
            FishingRodPartSlotType slotType)
    {
        string path =
            OutputFolder +
            "/RodPart_" +
            MakeSafeFileName(item.ItemId) +
            ".asset";

        FishingRodPartDefinition definition =
            AssetDatabase.LoadAssetAtPath<
                FishingRodPartDefinition
            >(path);

        if (definition == null)
        {
            definition =
                ScriptableObject.CreateInstance<
                    FishingRodPartDefinition
                >();

            AssetDatabase.CreateAsset(
                definition,
                path
            );
        }

        DefaultStats stats =
            ResolveStats(item, slotType);

        SerializedObject serialized =
            new SerializedObject(definition);

        SetObject(serialized, "inventoryItem", item);
        SetEnum(serialized, "slotType", (int)slotType);
        SetInt(
            serialized,
            "requiredSkill",
            stats.requiredSkill
        );
        SetFloat(
            serialized,
            "strengthBonus",
            stats.strength
        );
        SetFloat(
            serialized,
            "speedBonus",
            stats.speed
        );
        SetFloat(
            serialized,
            "maxDepthBonus",
            stats.depth
        );

        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(definition);

        return definition;
    }

    private static DefaultStats ResolveStats(
        InventoryItemData item,
        FishingRodPartSlotType slotType)
    {
        if (KnownStats.TryGetValue(
                item.ItemId,
                out DefaultStats known))
        {
            return known;
        }

        switch (slotType)
        {
            case FishingRodPartSlotType.Reel:
                return new DefaultStats(1, 1f, 1f, 0f);

            case FishingRodPartSlotType.Line:
                return new DefaultStats(1, 1f, 0f, 1f);

            case FishingRodPartSlotType.Hook:
                return new DefaultStats(1, 1f, 0f, 0f);

            case FishingRodPartSlotType.Bait:
                return new DefaultStats(0, 0f, 0f, 0f);

            default:
                return new DefaultStats(0, 0f, 0f, 0f);
        }
    }

    private static bool TryResolveSlotType(
        string itemId,
        out FishingRodPartSlotType slotType)
    {
        string id = itemId.Trim().ToLowerInvariant();

        if (id.StartsWith("equipment_reel_"))
        {
            slotType = FishingRodPartSlotType.Reel;
            return true;
        }

        if (id.StartsWith("equipment_line_"))
        {
            slotType = FishingRodPartSlotType.Line;
            return true;
        }

        if (id.StartsWith("equipment_hook_"))
        {
            slotType = FishingRodPartSlotType.Hook;
            return true;
        }

        if (id.StartsWith("bait_"))
        {
            slotType = FishingRodPartSlotType.Bait;
            return true;
        }

        slotType = FishingRodPartSlotType.Reel;
        return false;
    }

    private static int AssignToSceneLoadouts(
        List<FishingRodPartDefinition> definitions)
    {
        FishingRodLoadout[] loadouts =
            UnityEngine.Object.FindObjectsOfType<
                FishingRodLoadout
            >(true);

        int changedCount = 0;

        foreach (FishingRodLoadout loadout in loadouts)
        {
            if (loadout == null)
                continue;

            SerializedObject serialized =
                new SerializedObject(loadout);

            SerializedProperty property =
                serialized.FindProperty("partDefinitions");

            if (property == null || !property.isArray)
                continue;

            property.arraySize = definitions.Count;

            for (int i = 0; i < definitions.Count; i++)
            {
                property
                    .GetArrayElementAtIndex(i)
                    .objectReferenceValue =
                        definitions[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(loadout);

            if (loadout.gameObject.scene.IsValid())
            {
                EditorSceneManager.MarkSceneDirty(
                    loadout.gameObject.scene
                );
            }

            changedCount++;
        }

        return changedCount;
    }

    private static void EnsureFolder(string fullPath)
    {
        string[] parts = fullPath.Split('/');
        string current = parts[0];

        for (int i = 1; i < parts.Length; i++)
        {
            string next =
                current + "/" + parts[i];

            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(
                    current,
                    parts[i]
                );
            }

            current = next;
        }
    }

    private static string MakeSafeFileName(string value)
    {
        string result =
            string.IsNullOrWhiteSpace(value)
                ? "Unknown"
                : value.Trim();

        foreach (char invalid in Path.GetInvalidFileNameChars())
            result = result.Replace(invalid, '_');

        return result;
    }

    private static void SetObject(
        SerializedObject serialized,
        string propertyName,
        UnityEngine.Object value)
    {
        SerializedProperty property =
            serialized.FindProperty(propertyName);

        if (property != null)
            property.objectReferenceValue = value;
    }

    private static void SetEnum(
        SerializedObject serialized,
        string propertyName,
        int value)
    {
        SerializedProperty property =
            serialized.FindProperty(propertyName);

        if (property != null)
            property.enumValueIndex = value;
    }

    private static void SetInt(
        SerializedObject serialized,
        string propertyName,
        int value)
    {
        SerializedProperty property =
            serialized.FindProperty(propertyName);

        if (property != null)
            property.intValue = value;
    }

    private static void SetFloat(
        SerializedObject serialized,
        string propertyName,
        float value)
    {
        SerializedProperty property =
            serialized.FindProperty(propertyName);

        if (property != null)
            property.floatValue = value;
    }
}
#endif
