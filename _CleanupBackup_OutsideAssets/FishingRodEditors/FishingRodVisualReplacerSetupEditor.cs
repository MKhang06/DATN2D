#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class FishingRodVisualReplacerSetupEditor
{
    [MenuItem(
        "Tools/Fishing/Setup Hierarchy Rod Replacer"
    )]
    public static void Setup()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog(
                "Fishing Rod Visual",
                "Hãy thoát Play Mode trước khi chạy.",
                "OK"
            );

            return;
        }

        FishingRodOwnershipGate gate =
            UnityEngine.Object
                .FindFirstObjectByType<
                    FishingRodOwnershipGate
                >(
                    FindObjectsInactive.Include
                );

        GameObject target =
            gate != null
                ? gate.gameObject
                : FindPlayer();

        if (target == null)
        {
            Debug.LogError(
                "[FishingRodVisualReplacerSetup] " +
                "Không tìm thấy Player."
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

        FishingEquipmentShopUI shop =
            UnityEngine.Object
                .FindFirstObjectByType<
                    FishingEquipmentShopUI
                >(
                    FindObjectsInactive.Include
                );

        GameObject rodVisual =
            gate != null
                ? ReadReference(
                      gate,
                      "hierarchyRodVisual"
                  ) as GameObject
                : null;

        if (rodVisual == null)
        {
            rodVisual =
                FindRodVisual(
                    target.transform
                );
        }

        FishingRodVisualReplacer replacer =
            target.GetComponent<
                FishingRodVisualReplacer
            >();

        if (replacer == null)
        {
            replacer =
                Undo.AddComponent<
                    FishingRodVisualReplacer
                >(target);
        }

        SerializedObject serialized =
            new SerializedObject(
                replacer
            );

        SetReference(
            serialized,
            "inventoryManager",
            inventory
        );

        SetReference(
            serialized,
            "equipmentShopUI",
            shop
        );

        SetReference(
            serialized,
            "hierarchyRodObject",
            rodVisual
        );

        SpriteRenderer spriteRenderer =
            rodVisual != null
                ? rodVisual
                    .GetComponentInChildren<
                        SpriteRenderer
                    >(true)
                : null;

        SetReference(
            serialized,
            "rodSpriteRenderer",
            spriteRenderer
        );

        serialized
            .ApplyModifiedProperties();

        EditorUtility.SetDirty(
            replacer
        );

        if (rodVisual != null)
        {
            Undo.RecordObject(
                rodVisual,
                "Disable Default Rod Visual"
            );

            rodVisual.SetActive(false);

            EditorUtility.SetDirty(
                rodVisual
            );
        }

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkAllScenesDirty();

        Debug.Log(
            "[FishingRodVisualReplacerSetup] DONE | " +
            "Target=" +
            target.name +
            " | Inventory=" +
            (
                inventory != null
                    ? inventory.name
                    : "NULL"
            ) +
            " | Shop=" +
            (
                shop != null
                    ? shop.name
                    : "NULL"
            ) +
            " | RodVisual=" +
            (
                rodVisual != null
                    ? rodVisual.name
                    : "NULL"
            ),
            replacer
        );

        EditorUtility.DisplayDialog(
            "Fishing Rod Visual",
            "Đã gắn FishingRodVisualReplacer.\n\n" +
            "Rod Visual: " +
            (
                rodVisual != null
                    ? rodVisual.name
                    : "CHƯA TÌM THẤY"
            ) +
            "\nSpriteRenderer: " +
            (
                spriteRenderer != null
                    ? spriteRenderer.name
                    : "CHƯA TÌM THẤY"
            ) +
            "\n\nKiểm tra Inspector rồi nhấn Ctrl+S.",
            "OK"
        );

        Selection.activeGameObject =
            target;
    }

    private static UnityEngine.Object
        ReadReference(
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
                   SerializedPropertyType
                       .ObjectReference
            ? property.objectReferenceValue
            : null;
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

    private static GameObject FindPlayer()
    {
        GameObject tagged =
            GameObject.FindGameObjectWithTag(
                "Player"
            );

        if (tagged != null)
            return tagged;

        FishingGearStats gear =
            UnityEngine.Object
                .FindFirstObjectByType<
                    FishingGearStats
                >(
                    FindObjectsInactive.Include
                );

        return gear != null
            ? gear.gameObject
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

        foreach (Transform child
                 in children)
        {
            if (child == null ||
                child == player)
            {
                continue;
            }

            string name =
                child.name.ToLowerInvariant();

            if (name.Contains(
                    "fishingrod") ||
                name.Contains(
                    "featherlight") ||
                name.Contains(
                    "rodvisual") ||
                name.Contains(
                    "cần câu") ||
                name == "rod")
            {
                return child.gameObject;
            }
        }

        return null;
    }
}
#endif
