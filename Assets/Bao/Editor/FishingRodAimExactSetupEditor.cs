#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class FishingRodAimExactSetupEditor
{
    private const string Prefix =
        "[FISHING ROD EXACT AIM] ";

    [MenuItem(
        "Tools/Fishing/Fix Exact Rod Aim"
    )]
    public static void Setup()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog(
                "Fishing Rod Aim",
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

        SpriteRenderer renderer =
            rod.GetComponentInChildren<
                SpriteRenderer
            >(true);

        if (renderer == null ||
            renderer.sprite == null)
        {
            Debug.LogError(
                Prefix +
                "Fishing Rod chưa có SpriteRenderer hoặc Sprite.",
                rod
            );

            return;
        }

        Transform handle =
            FindOrCreatePoint(
                renderer.transform,
                "RodHandlePoint"
            );

        Transform tip =
            FindOrCreatePoint(
                renderer.transform,
                "RodTipPoint"
            );

        Bounds bounds =
            renderer.sprite.bounds;

        /*
         * Asset người dùng:
         * - Cán ở phía dưới bên phải.
         * - Đầu cần ở phía trên bên trái.
         *
         * Các giá trị này lấy theo bounds của chính sprite,
         * không phụ thuộc độ phân giải hoặc Pixels Per Unit.
         */
        Vector3 handleLocal =
            new Vector3(
                Mathf.Lerp(
                    bounds.min.x,
                    bounds.max.x,
                    0.82f
                ),
                Mathf.Lerp(
                    bounds.min.y,
                    bounds.max.y,
                    0.18f
                ),
                0f
            );

        Vector3 tipLocal =
            new Vector3(
                Mathf.Lerp(
                    bounds.min.x,
                    bounds.max.x,
                    0.08f
                ),
                Mathf.Lerp(
                    bounds.min.y,
                    bounds.max.y,
                    0.92f
                ),
                0f
            );

        Undo.RecordObject(
            handle,
            "Place Rod Handle Point"
        );

        Undo.RecordObject(
            tip,
            "Place Rod Tip Point"
        );

        handle.localPosition =
            handleLocal;

        handle.localRotation =
            Quaternion.identity;

        handle.localScale =
            Vector3.one;

        tip.localPosition =
            tipLocal;

        tip.localRotation =
            Quaternion.identity;

        tip.localScale =
            Vector3.one;

        FishingRodAimExact exactAim =
            rod.GetComponent<
                FishingRodAimExact
            >();

        if (exactAim == null)
        {
            exactAim =
                Undo.AddComponent<
                    FishingRodAimExact
                >(rod);
        }

        SerializedObject serialized =
            new SerializedObject(
                exactAim
            );

        SetReference(
            serialized,
            "rodRoot",
            rod.transform
        );

        SetReference(
            serialized,
            "handlePoint",
            handle
        );

        SetReference(
            serialized,
            "tipPoint",
            tip
        );

        SetReference(
            serialized,
            "worldCamera",
            Camera.main
        );

        SetReference(
            serialized,
            "rodRenderer",
            renderer
        );

        serialized.ApplyModifiedProperties();

        int disabled =
            DisableOldRotationScripts(
                rod.transform.root,
                exactAim
            );

        bool hotbarDisabled =
            DisableHotbarRotation(
                rod.transform.root
            );

        renderer.flipX = false;
        renderer.flipY = false;

        EditorUtility.SetDirty(renderer);
        EditorUtility.SetDirty(handle);
        EditorUtility.SetDirty(tip);
        EditorUtility.SetDirty(exactAim);

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkAllScenesDirty();

        Selection.activeGameObject =
            rod;

        Debug.Log(
            Prefix +
            "DONE | Rod=" +
            rod.name +
            " | Handle=" +
            handle.localPosition +
            " | Tip=" +
            tip.localPosition +
            " | DisabledOld=" +
            disabled +
            " | HotbarRotationOff=" +
            hotbarDisabled,
            exactAim
        );

        EditorUtility.DisplayDialog(
            "Fishing Rod Aim",
            "Đã tạo hai điểm thật trên cây cần:\n\n" +
            "RodHandlePoint = cán dưới bên phải\n" +
            "RodTipPoint = đầu trên bên trái\n\n" +
            "Đã tắt script xoay cũ: " +
            disabled +
            "\nHotbar rotation đã tắt: " +
            hotbarDisabled +
            "\n\nNhấn Ctrl+S rồi Play.",
            "OK"
        );
    }

    private static Transform FindOrCreatePoint(
        Transform parent,
        string pointName)
    {
        Transform existing =
            parent.Find(pointName);

        if (existing != null)
            return existing;

        GameObject point =
            new GameObject(pointName);

        Undo.RegisterCreatedObjectUndo(
            point,
            "Create " + pointName
        );

        point.transform.SetParent(
            parent,
            false
        );

        return point.transform;
    }

    private static int
        DisableOldRotationScripts(
            Transform root,
            FishingRodAimExact keep)
    {
        int count = 0;

        string[] oldTypes =
        {
            "FishingRodMouseAim",
            "FishingRodMouseAimStable",
            "FishingRodMouseAimFinal",
            "FishingRodAutoRotate",
            "FishingRodAimByTip"
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

            foreach (string oldType
                     in oldTypes)
            {
                if (!string.Equals(
                        typeName,
                        oldType,
                        StringComparison.Ordinal))
                {
                    continue;
                }

                Undo.RecordObject(
                    behaviour,
                    "Disable Old Rod Rotation"
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

    private static bool
        DisableHotbarRotation(
            Transform root)
    {
        MonoBehaviour[] behaviours =
            root.GetComponentsInChildren<
                MonoBehaviour
            >(true);

        foreach (
            MonoBehaviour behaviour
            in behaviours)
        {
            if (behaviour == null ||
                behaviour.GetType().Name !=
                    "FishingRodHotbarController")
            {
                continue;
            }

            SerializedObject serialized =
                new SerializedObject(
                    behaviour
                );

            SerializedProperty property =
                serialized.FindProperty(
                    "rotateWithPlayerDirection"
                );

            if (property == null ||
                property.propertyType !=
                    SerializedPropertyType.Boolean)
            {
                return false;
            }

            Undo.RecordObject(
                behaviour,
                "Disable Hotbar Rod Rotation"
            );

            property.boolValue = false;
            serialized.ApplyModifiedProperties();

            EditorUtility.SetDirty(
                behaviour
            );

            return true;
        }

        return false;
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

        foreach (Transform transform
                 in transforms)
        {
            if (transform != null &&
                string.Equals(
                    transform.name,
                    objectName,
                    StringComparison
                        .OrdinalIgnoreCase))
            {
                return transform.gameObject;
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
}
#endif
