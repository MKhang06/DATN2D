#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class FishingRodUseOldVisualSetupEditor
{
    private const string Prefix =
        "[USE OLD ROD VISUAL] ";

    [MenuItem(
        "Tools/Fishing/Use Old Rod Visual"
    )]
    public static void Setup()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog(
                "Use Old Rod Visual",
                "Hãy thoát Play Mode trước khi chạy.",
                "OK"
            );

            return;
        }

        GameObject rod =
            FindExactObject(
                "Fishing Rod"
            );

        if (rod == null)
        {
            Debug.LogError(
                Prefix +
                "Không tìm thấy object Fishing Rod."
            );

            return;
        }

        GameObject player =
            FindPlayerFromRod(
                rod.transform
            );

        if (player == null)
        {
            Debug.LogError(
                Prefix +
                "Không tìm thấy Player.",
                rod
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

        SpriteRenderer rodRenderer =
            rod.GetComponentInChildren<
                SpriteRenderer
            >(true);

        SpriteRenderer heldRenderer =
            FindHeldItemRenderer(
                player.transform
            );

        FishingRodUseOldVisual controller =
            player.GetComponent<
                FishingRodUseOldVisual
            >();

        if (controller == null)
        {
            controller =
                Undo.AddComponent<
                    FishingRodUseOldVisual
                >(player);
        }

        SerializedObject serialized =
            new SerializedObject(
                controller
            );

        SetReference(
            serialized,
            "inventoryManager",
            inventory
        );

        SetReference(
            serialized,
            "oldRodObject",
            rod
        );

        SetReference(
            serialized,
            "oldRodRenderer",
            rodRenderer
        );

        SetReference(
            serialized,
            "oldRodSprite",
            rodRenderer != null
                ? rodRenderer.sprite
                : null
        );

        SetReference(
            serialized,
            "heldItemRenderer",
            heldRenderer
        );

        SetVector3(
            serialized,
            "oldLocalPosition",
            rod.transform.localPosition
        );

        SetVector3(
            serialized,
            "oldLocalEulerAngles",
            rod.transform.localEulerAngles
        );

        SetVector3(
            serialized,
            "oldLocalScale",
            rod.transform.localScale
        );

        serialized.ApplyModifiedProperties();

        int disabled =
            DisableVisualAndAimScripts(
                player.transform,
                controller
            );

        rod.SetActive(false);

        EditorUtility.SetDirty(
            rod
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
            "DONE | Player=" +
            player.name +
            " | Rod=" +
            rod.name +
            " | DisabledOldScripts=" +
            disabled,
            controller
        );

        EditorUtility.DisplayDialog(
            "Use Old Rod Visual",
            "Đã chuyển về dùng cần câu cũ.\n\n" +
            "Vật phẩm cần câu mua vẫn dùng để kiểm tra sở hữu/Hotbar.\n" +
            "Hình ngoài thế giới luôn là Fishing Rod cũ.\n" +
            "Đã tắt script thay sprite/xoay cũ: " +
            disabled +
            "\n\nNhấn Ctrl+S rồi Play.",
            "OK"
        );
    }

    private static int
        DisableVisualAndAimScripts(
            Transform root,
            FishingRodUseOldVisual keep)
    {
        int count = 0;

        string[] disabledTypes =
        {
            "FishingRodHotbarController",
            "FishingRodVisualReplacer",
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
            root.GetComponentsInChildren<
                MonoBehaviour
            >(true);

        foreach (
            MonoBehaviour behaviour
            in behaviours)
        {
            if (behaviour == null ||
                behaviour == keep)
            {
                continue;
            }

            string typeName =
                behaviour.GetType().Name;

            foreach (string disabledType
                     in disabledTypes)
            {
                if (!string.Equals(
                        typeName,
                        disabledType,
                        StringComparison.Ordinal))
                {
                    continue;
                }

                Undo.RecordObject(
                    behaviour,
                    "Disable Old Rod Visual Script"
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
        FindPlayerFromRod(
            Transform rod)
    {
        Transform current = rod;

        while (current != null)
        {
            if (current.CompareTag("Player") ||
                string.Equals(
                    current.name,
                    "Player",
                    StringComparison
                        .OrdinalIgnoreCase))
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

    private static SpriteRenderer
        FindHeldItemRenderer(
            Transform player)
    {
        Transform[] children =
            player.GetComponentsInChildren<
                Transform
            >(true);

        foreach (Transform child
                 in children)
        {
            if (child != null &&
                string.Equals(
                    child.name,
                    "HeldItem",
                    StringComparison
                        .OrdinalIgnoreCase))
            {
                return child
                    .GetComponentInChildren<
                        SpriteRenderer
                    >(true);
            }
        }

        return null;
    }

    private static GameObject FindExactObject(
        string objectName)
    {
        Transform[] transforms =
            UnityEngine.Object
                .FindObjectsByType<
                    Transform
                >(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None
                );

        foreach (Transform candidate
                 in transforms)
        {
            if (candidate != null &&
                string.Equals(
                    candidate.name,
                    objectName,
                    StringComparison
                        .OrdinalIgnoreCase))
            {
                return candidate.gameObject;
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

        if (property != null &&
            property.propertyType ==
                SerializedPropertyType
                    .ObjectReference)
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
            serialized.FindProperty(
                fieldName
            );

        if (property != null &&
            property.propertyType ==
                SerializedPropertyType
                    .Vector3)
        {
            property.vector3Value =
                value;
        }
    }
}
#endif
