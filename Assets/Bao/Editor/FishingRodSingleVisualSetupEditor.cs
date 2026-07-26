#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class FishingRodSingleVisualSetupEditor
{
    private const string Prefix =
        "[SINGLE OLD FISHING ROD] ";

    [MenuItem(
        "Tools/Fishing/Fix Two Rods - Use Old Fishing Rod"
    )]
    public static void Setup()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog(
                "Fix Two Rods",
                "Hãy thoát Play Mode trước khi chạy.",
                "OK"
            );

            return;
        }

        GameObject player = FindExactObject("Player");

        FishingGearStats gear =
            UnityEngine.Object.FindFirstObjectByType<FishingGearStats>(
                FindObjectsInactive.Include
            );

        if (player == null && gear != null)
            player = gear.gameObject;

        if (player == null)
        {
            Debug.LogError(Prefix + "Không tìm thấy Player.");
            return;
        }

        Transform toolHolder =
            FindChildByName(player.transform, "ToolHolder");

        Transform heldItem =
            FindChildByName(player.transform, "HeldItem");

        Transform oldRod =
            FindChildByName(player.transform, "Fishing Rod");

        if (toolHolder == null || oldRod == null)
        {
            Debug.LogError(
                Prefix +
                "Không tìm thấy ToolHolder hoặc Fishing Rod.",
                player
            );
            return;
        }

        /*
         * Đưa Fishing Rod cũ trở về trực tiếp dưới ToolHolder,
         * bỏ mọi Pivot thử nghiệm trước đó.
         */
        if (oldRod.parent != toolHolder)
        {
            Undo.SetTransformParent(
                oldRod,
                toolHolder,
                "Restore Old Fishing Rod Parent"
            );
        }

        SpriteRenderer oldRenderer =
            oldRod.GetComponentInChildren<SpriteRenderer>(true);

        FishingRodSingleVisualController controller =
            player.GetComponent<FishingRodSingleVisualController>();

        if (controller == null)
        {
            controller =
                Undo.AddComponent<FishingRodSingleVisualController>(player);
        }

        SerializedObject serialized =
            new SerializedObject(controller);

        SetReference(
            serialized,
            "inventoryManager",
            UnityEngine.Object.FindFirstObjectByType<InventoryManager>(
                FindObjectsInactive.Include
            )
        );

        SetReference(serialized, "oldRodObject", oldRod.gameObject);
        SetReference(serialized, "oldRodRenderer", oldRenderer);
        SetReference(
            serialized,
            "oldRodSprite",
            oldRenderer != null ? oldRenderer.sprite : null
        );
        SetReference(serialized, "heldItemRoot", heldItem);
        SetReference(serialized, "toolHolder", toolHolder);

        SetVector3(
            serialized,
            "oldLocalPosition",
            oldRod.localPosition
        );
        SetVector3(
            serialized,
            "oldLocalEulerAngles",
            oldRod.localEulerAngles
        );
        SetVector3(
            serialized,
            "oldLocalScale",
            oldRod.localScale
        );

        serialized.ApplyModifiedProperties();

        int disabled =
            DisableConflictingComponents(
                player.transform,
                controller
            );

        int hiddenDuplicates =
            HideCurrentDuplicateRodRenderers(
                player.transform,
                oldRenderer,
                heldItem
            );

        oldRod.gameObject.SetActive(false);

        EditorUtility.SetDirty(oldRod.gameObject);
        EditorUtility.SetDirty(controller);

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkAllScenesDirty();

        Selection.activeGameObject = player;

        Debug.Log(
            Prefix +
            "DONE | OldRod=" +
            oldRod.name +
            " | DisabledScripts=" +
            disabled +
            " | HiddenDuplicateRenderers=" +
            hiddenDuplicates,
            controller
        );

        EditorUtility.DisplayDialog(
            "Fix Two Rods",
            "Đã sửa về một cây cần duy nhất.\n\n" +
            "Item mua vẫn nằm trong Hotbar.\n" +
            "Khi chọn item đó sẽ gọi Fishing Rod cũ.\n" +
            "Đã tắt script xung đột: " +
            disabled +
            "\nĐã ẩn renderer trùng: " +
            hiddenDuplicates +
            "\n\nNhấn Ctrl+S rồi Play.",
            "OK"
        );
    }

    private static int DisableConflictingComponents(
        Transform root,
        FishingRodSingleVisualController keep)
    {
        int count = 0;

        string[] names =
        {
            "FishingRodHotbarController",
            "FishingRodVisualReplacer",
            "FishingRodUseOldVisual",
            "FishingRodMouseAim",
            "FishingRodMouseAimStable",
            "FishingRodMouseAimFinal",
            "FishingRodMouseAimPivot",
            "FishingRodAutoRotate",
            "FishingRodAimByTip",
            "FishingRodAimExact",
            "HideHeldItemWhenFishingRodSelected"
        };

        MonoBehaviour[] behaviours =
            root.GetComponentsInChildren<MonoBehaviour>(true);

        foreach (MonoBehaviour behaviour in behaviours)
        {
            if (behaviour == null || behaviour == keep)
                continue;

            string typeName = behaviour.GetType().Name;

            foreach (string name in names)
            {
                if (!string.Equals(
                        typeName,
                        name,
                        StringComparison.Ordinal))
                {
                    continue;
                }

                Undo.RecordObject(
                    behaviour,
                    "Disable Duplicate Rod Script"
                );

                behaviour.enabled = false;
                EditorUtility.SetDirty(behaviour);
                count++;
                break;
            }
        }

        return count;
    }

    private static int HideCurrentDuplicateRodRenderers(
        Transform root,
        SpriteRenderer keep,
        Transform heldItem)
    {
        int count = 0;

        SpriteRenderer[] renderers =
            root.GetComponentsInChildren<SpriteRenderer>(true);

        foreach (SpriteRenderer renderer in renderers)
        {
            if (renderer == null || renderer == keep)
                continue;

            bool isHeldItem =
                heldItem != null &&
                renderer.transform.IsChildOf(heldItem);

            string lowerName =
                renderer.gameObject.name.ToLowerInvariant();

            bool isKnownDuplicate =
                lowerName.Contains("rodpreview") ||
                lowerName.Contains("feather") ||
                lowerName.Contains("purchasedrod");

            if (!isHeldItem && !isKnownDuplicate)
                continue;

            Undo.RecordObject(
                renderer,
                "Hide Duplicate Fishing Rod"
            );

            renderer.enabled = false;
            EditorUtility.SetDirty(renderer);
            count++;
        }

        return count;
    }

    private static GameObject FindExactObject(string objectName)
    {
        Transform[] transforms =
            UnityEngine.Object.FindObjectsByType<Transform>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        foreach (Transform transform in transforms)
        {
            if (transform != null &&
                string.Equals(
                    transform.name,
                    objectName,
                    StringComparison.OrdinalIgnoreCase))
            {
                return transform.gameObject;
            }
        }

        return null;
    }

    private static Transform FindChildByName(
        Transform root,
        string objectName)
    {
        if (root == null)
            return null;

        Transform[] children =
            root.GetComponentsInChildren<Transform>(true);

        foreach (Transform child in children)
        {
            if (child != null &&
                string.Equals(
                    child.name,
                    objectName,
                    StringComparison.OrdinalIgnoreCase))
            {
                return child;
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
            serialized.FindProperty(fieldName);

        if (property != null &&
            property.propertyType ==
                SerializedPropertyType.ObjectReference)
        {
            property.objectReferenceValue = value;
        }
    }

    private static void SetVector3(
        SerializedObject serialized,
        string fieldName,
        Vector3 value)
    {
        SerializedProperty property =
            serialized.FindProperty(fieldName);

        if (property != null &&
            property.propertyType ==
                SerializedPropertyType.Vector3)
        {
            property.vector3Value = value;
        }
    }
}
#endif
