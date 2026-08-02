#if UNITY_EDITOR
using System;
using System.Globalization;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class FishingRodOwnershipSetupEditor
{
    private const string Prefix =
        "[FISHING ROD SETUP] ";

    [MenuItem(
        "Tools/Fishing/Require Purchased Rod"
    )]
    public static void Setup()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog(
                "Fishing Rod Setup",
                "Hãy thoát Play Mode trước khi chạy.",
                "OK"
            );

            return;
        }

        FishingGearStats gearStats =
            UnityEngine.Object
                .FindFirstObjectByType<
                    FishingGearStats
                >(
                    FindObjectsInactive.Include
                );

        GameObject target =
            gearStats != null
                ? gearStats.gameObject
                : FindPlayerObject();

        if (target == null)
        {
            Debug.LogError(
                Prefix +
                "Không tìm thấy Player/FishingGearStats."
            );

            return;
        }

        InventoryManager inventory =
            UnityEngine.Object
                .FindFirstObjectByType<
                    InventoryManager
                >(
                    FindObjectsInactive.Include
                );

        FishingCustomizationUI customUI =
            UnityEngine.Object
                .FindFirstObjectByType<
                    FishingCustomizationUI
                >(
                    FindObjectsInactive.Include
                );

        InventoryItemData rodItem =
            FindRodInventoryItem();

        GameObject rodVisual =
            FindRodVisual(
                target.transform
            );

        FishingRodOwnershipGate gate =
            target.GetComponent<
                FishingRodOwnershipGate
            >();

        if (gate == null)
        {
            gate =
                Undo.AddComponent<
                    FishingRodOwnershipGate
                >(target);
        }

        Undo.RecordObject(
            gate,
            "Setup Purchased Fishing Rod"
        );

        SerializedObject serialized =
            new SerializedObject(
                gate
            );

        SetReference(
            serialized,
            "inventoryManager",
            inventory
        );

        SetReference(
            serialized,
            "customizationUI",
            customUI
        );

        SetReference(
            serialized,
            "requiredRodItem",
            rodItem
        );

        SetReference(
            serialized,
            "hierarchyRodVisual",
            rodVisual
        );

        SetBool(
            serialized,
            "requireSelectedRod",
            true
        );

        SetBool(
            serialized,
            "closeCustomizationWhenRodPutAway",
            true
        );

        SetBool(
            serialized,
            "forceCloseWhileNotHoldingRod",
            true
        );

        serialized
            .ApplyModifiedProperties();

        EditorUtility.SetDirty(
            gate
        );

        int disabledStarterControllers =
            DisableStarterControllers();

        if (rodVisual != null)
        {
            Undo.RecordObject(
                rodVisual,
                "Disable Default Fishing Rod"
            );

            rodVisual.SetActive(
                false
            );

            EditorUtility.SetDirty(
                rodVisual
            );
        }

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkAllScenesDirty();

        string summary =
            "Đã thiết lập bắt buộc mua cần câu.\n\n" +
            "Gate object: " +
            target.name +
            "\nInventory: " +
            (
                inventory != null
                    ? inventory.name
                    : "NULL"
            ) +
            "\nCustom UI: " +
            (
                customUI != null
                    ? customUI.name
                    : "NULL"
            ) +
            "\nRod Item: " +
            (
                rodItem != null
                    ? rodItem.DisplayName
                    : "CHƯA TÌM THẤY"
            ) +
            "\nRod Visual: " +
            (
                rodVisual != null
                    ? rodVisual.name
                    : "CHƯA TÌM THẤY"
            ) +
            "\nStarter Controller đã tắt: " +
            disabledStarterControllers +
            "\n\nKiểm tra Inspector rồi nhấn Ctrl+S.";

        Debug.Log(
            Prefix +
            "DONE | Target=" +
            target.name +
            " | Inventory=" +
            (
                inventory != null
                    ? inventory.name
                    : "NULL"
            ) +
            " | Custom=" +
            (
                customUI != null
                    ? customUI.name
                    : "NULL"
            ) +
            " | RodItem=" +
            (
                rodItem != null
                    ? rodItem.DisplayName
                    : "NULL"
            ) +
            " | RodVisual=" +
            (
                rodVisual != null
                    ? rodVisual.name
                    : "NULL"
            ) +
            " | DisabledStarter=" +
            disabledStarterControllers,
            gate
        );

        EditorUtility.DisplayDialog(
            "Fishing Rod Setup",
            summary,
            "OK"
        );

        Selection.activeGameObject =
            target;
    }

    private static int
        DisableStarterControllers()
    {
        int count = 0;

        MonoBehaviour[] components =
            UnityEngine.Object
                .FindObjectsByType<
                    MonoBehaviour
                >(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None
                );

        foreach (
            MonoBehaviour component
            in components)
        {
            if (component == null ||
                component.GetType().Name !=
                    "FishingStarterGearController")
            {
                continue;
            }

            Undo.RecordObject(
                component,
                "Disable Starter Fishing Gear"
            );

            SerializedObject serialized =
                new SerializedObject(
                    component
                );

            SetBool(
                serialized,
                "grantStarterItemsToInventory",
                false
            );

            SetBool(
                serialized,
                "keepStarterGearAsFallback",
                false
            );

            serialized
                .ApplyModifiedProperties();

            component.enabled =
                false;

            EditorUtility.SetDirty(
                component
            );

            count++;

            Debug.Log(
                Prefix +
                "Đã tắt FishingStarterGearController trên " +
                component.name +
                ".",
                component
            );
        }

        return count;
    }

    private static InventoryItemData
        FindRodInventoryItem()
    {
        string[] guids =
            AssetDatabase.FindAssets(
                "t:InventoryItemData",
                new[]
                {
                    "Assets"
                }
            );

        InventoryItemData best =
            null;

        int bestScore =
            int.MinValue;

        foreach (string guid
                 in guids)
        {
            string path =
                AssetDatabase
                    .GUIDToAssetPath(
                        guid
                    );

            InventoryItemData item =
                AssetDatabase
                    .LoadAssetAtPath<
                        InventoryItemData
                    >(path);

            if (item == null)
                continue;

            string combined =
                item.name +
                " " +
                item.DisplayName +
                " " +
                ReadSerializedString(
                    item,
                    "itemId"
                );

            string normalized =
                Normalize(combined);

            int score = 0;

            if (normalized.Contains(
                    "equipmentrodfeatherlight"))
            {
                score += 100;
            }

            if (normalized.Contains(
                    "featherlight"))
            {
                score += 60;
            }

            if (normalized.Contains(
                    "fishingrod"))
            {
                score += 40;
            }

            if (normalized.Contains(
                    "cancau"))
            {
                score += 30;
            }

            if (score > bestScore)
            {
                bestScore =
                    score;

                best =
                    item;
            }
        }

        return bestScore > 0
            ? best
            : null;
    }

    private static GameObject FindRodVisual(
        Transform player)
    {
        if (player == null)
            return null;

        Transform[] children =
            player.GetComponentsInChildren<
                Transform
            >(true);

        GameObject best =
            null;

        int bestScore =
            int.MinValue;

        foreach (Transform child
                 in children)
        {
            if (child == null ||
                child == player)
            {
                continue;
            }

            if (child.GetComponentInParent<
                    Canvas
                >() != null)
            {
                continue;
            }

            string normalized =
                Normalize(
                    child.name
                );

            int score = 0;

            if (normalized.Contains(
                    "featherlight"))
            {
                score += 100;
            }

            if (normalized.Contains(
                    "fishingrod"))
            {
                score += 80;
            }

            if (normalized.Contains(
                    "rodvisual"))
            {
                score += 70;
            }

            if (normalized.Contains(
                    "cancau"))
            {
                score += 60;
            }

            if (normalized == "rod")
            {
                score += 50;
            }

            if (child.GetComponent<
                    SpriteRenderer
                >() != null)
            {
                score += 10;
            }

            if (score > bestScore)
            {
                bestScore =
                    score;

                best =
                    child.gameObject;
            }
        }

        return bestScore > 0
            ? best
            : null;
    }

    private static GameObject
        FindPlayerObject()
    {
        GameObject tagged =
            GameObject.FindGameObjectWithTag(
                "Player"
            );

        if (tagged != null)
            return tagged;

        MonoBehaviour[] behaviours =
            UnityEngine.Object
                .FindObjectsByType<
                    MonoBehaviour
                >(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None
                );

        foreach (
            MonoBehaviour behaviour
            in behaviours)
        {
            if (behaviour == null)
                continue;

            string typeName =
                behaviour.GetType().Name;

            if (typeName == "PlayerController" ||
                typeName ==
                    "PlayerMovement")
            {
                return behaviour.gameObject;
            }
        }

        return null;
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

    private static void SetBool(
        SerializedObject serialized,
        string fieldName,
        bool value)
    {
        SerializedProperty property =
            serialized.FindProperty(
                fieldName
            );

        if (property == null ||
            property.propertyType !=
                SerializedPropertyType
                    .Boolean)
        {
            return;
        }

        property.boolValue =
            value;
    }

    private static string
        ReadSerializedString(
            UnityEngine.Object target,
            string fieldName)
    {
        SerializedObject serialized =
            new SerializedObject(
                target
            );

        SerializedProperty property =
            serialized.FindProperty(
                fieldName
            );

        return property != null &&
               property.propertyType ==
                   SerializedPropertyType.String
            ? property.stringValue
            : string.Empty;
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
