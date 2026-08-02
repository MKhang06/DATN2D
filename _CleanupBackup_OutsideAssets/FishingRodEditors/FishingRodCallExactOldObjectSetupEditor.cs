#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class FishingRodCallExactOldObjectSetupEditor
{
    private const string Prefix =
        "[CALL EXACT OLD ROD] ";

    [MenuItem(
        "Tools/Fishing/Use Selected Old Fishing Rod"
    )]
    public static void Setup()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog(
                "Use Old Fishing Rod",
                "Hãy thoát Play Mode trước khi chạy.",
                "OK"
            );

            return;
        }

        GameObject selected =
            Selection.activeGameObject;

        if (selected == null)
        {
            EditorUtility.DisplayDialog(
                "Use Old Fishing Rod",
                "Hãy chọn đúng object Fishing Rod cũ trong Hierarchy trước.",
                "OK"
            );

            return;
        }

        GameObject player =
            FindPlayerFromSelected(
                selected.transform
            );

        if (player == null)
        {
            Debug.LogError(
                Prefix +
                "Object được chọn không nằm trong Player.",
                selected
            );

            return;
        }

        Transform heldItem =
            FindChildByName(
                player.transform,
                "HeldItem"
            );

        FishingRodCallExactOldObject
            controller =
                player.GetComponent<
                    FishingRodCallExactOldObject
                >();

        if (controller == null)
        {
            controller =
                Undo.AddComponent<
                    FishingRodCallExactOldObject
                >(player);
        }

        SerializedObject serialized =
            new SerializedObject(
                controller
            );

        SetReference(
            serialized,
            "inventoryManager",
            UnityEngine.Object
                .FindFirstObjectByType<
                    InventoryManager
                >(
                    FindObjectsInactive.Include
                )
        );

        SetReference(
            serialized,
            "exactOldRodObject",
            selected
        );

        SetReference(
            serialized,
            "originalParent",
            selected.transform.parent
        );

        SetReference(
            serialized,
            "heldItemRoot",
            heldItem
        );

        SetVector3(
            serialized,
            "originalLocalPosition",
            selected.transform.localPosition
        );

        SetVector3(
            serialized,
            "originalLocalScale",
            selected.transform.localScale
        );

        serialized.ApplyModifiedProperties();

        int disabled =
            DisableConflictingDisplayControllers(
                player.transform,
                controller,
                selected.transform
            );

        selected.SetActive(false);

        EditorUtility.SetDirty(
            selected
        );

        EditorUtility.SetDirty(
            controller
        );

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkAllScenesDirty();

        Selection.activeGameObject =
            player;

        Debug.Log(
            Prefix +
            "DONE | ExactRod=" +
            selected.name +
            " | Parent=" +
            (
                selected.transform.parent != null
                    ? selected.transform.parent.name
                    : "NULL"
            ) +
            " | LocalPosition=" +
            selected.transform.localPosition +
            " | LocalScale=" +
            selected.transform.localScale +
            " | DisabledControllers=" +
            disabled,
            controller
        );

        EditorUtility.DisplayDialog(
            "Use Old Fishing Rod",
            "Đã lưu đúng object cần cũ:\n\n" +
            selected.name +
            "\nPosition: " +
            selected.transform.localPosition +
            "\nScale: " +
            selected.transform.localScale +
            "\n\nKhi chọn item cần mua, script sẽ SetActive chính object này.\n" +
            "Rotation không bị ghi đè nên hệ thống xoay cũ vẫn chạy.\n\n" +
            "Nhấn Ctrl+S rồi Play.",
            "OK"
        );
    }

    private static int
        DisableConflictingDisplayControllers(
            Transform root,
            ishingRodCallExactOldObject keep,
            Transform selectedRod)
    {
        int count = 0;

        string[] typeNames =
        {
            "FishingRodHotbarController",
            "FishingRodVisualReplacer",
            "FishingRodUseOldVisual",
            "FishingRodSingleVisualController",
            "FishingRodPurchasedItemCallsOldRod",
            "HideHeldItemWhenFishingRodSelected"
        };

        MonoBehaviour[] behaviours =
            root.GetComponentsInChildren<
                MonoBehaviour
            >(true);

        foreach (MonoBehaviour behaviour
                 in behaviours)
        {
            if (behaviour == null ||
                behaviour == keep)
            {
                continue;
            }

            /*
             * Không tắt script nằm trên chính cây cần cũ.
             * Các script xoay/animation gốc của nó được giữ.
             */
            if (behaviour.transform ==
                    selectedRod ||
                behaviour.transform.IsChildOf(
                    selectedRod))
            {
                continue;
            }

            string currentType =
                behaviour.GetType().Name;

            foreach (string typeName
                     in typeNames)
            {
                if (!string.Equals(
                        currentType,
                        typeName,
                        StringComparison.Ordinal))
                {
                    continue;
                }

                Undo.RecordObject(
                    behaviour,
                    "Disable Old Rod Display Controller"
                );

                behaviour.enabled = false;
                EditorUtility.SetDirty(
                    behaviour
                );

                count++;
                break;
            }
        }

        return count;
    }

    private static GameObject
        FindPlayerFromSelected(
            Transform selected)
    {
        Transform current = selected;

        while (current != null)
        {
            if (string.Equals(
                    current.name,
                    "Player",
                    StringComparison.OrdinalIgnoreCase))
            {
                return current.gameObject;
            }

            current = current.parent;
        }

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

    private static Transform FindChildByName(
        Transform root,
        string objectName)
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
            property.objectReferenceValue =
                value;
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
