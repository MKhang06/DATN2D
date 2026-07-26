#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class FishingRodMouseAimPivotSetupEditor
{
    private const string Prefix =
        "[ROD PIVOT AIM SETUP] ";

    [MenuItem(
        "Tools/Fishing/Rebuild Rod Mouse Aim"
    )]
    public static void Rebuild()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog(
                "Rod Mouse Aim",
                "Hãy thoát Play Mode trước khi chạy.",
                "OK"
            );

            return;
        }

        GameObject rodVisualObject =
            FindExactObject(
                "Fishing Rod"
            );

        if (rodVisualObject == null)
        {
            Debug.LogError(
                Prefix +
                "Không tìm thấy object Fishing Rod."
            );

            return;
        }

        SpriteRenderer renderer =
            rodVisualObject
                .GetComponentInChildren<
                    SpriteRenderer
                >(true);

        if (renderer == null ||
            renderer.sprite == null)
        {
            Debug.LogError(
                Prefix +
                "Fishing Rod chưa có SpriteRenderer/Sprite.",
                rodVisualObject
            );

            return;
        }

        Transform oldParent =
            rodVisualObject.transform.parent;

        /*
         * Tạo Pivot riêng. Không xoay trực tiếp Sprite object nữa.
         */
        Transform pivot =
            oldParent != null
                ? oldParent.Find(
                    "FishingRodAimPivot"
                  )
                : null;

        if (pivot == null)
        {
            GameObject pivotObject =
                new GameObject(
                    "FishingRodAimPivot"
                );

            Undo.RegisterCreatedObjectUndo(
                pivotObject,
                "Create Fishing Rod Aim Pivot"
            );

            pivot =
                pivotObject.transform;

            pivot.SetParent(
                oldParent,
                false
            );
        }

        Transform handle =
            renderer.transform.Find(
                "RodHandlePoint"
            );

        if (handle == null)
        {
            handle =
                CreatePoint(
                    renderer.transform,
                    "RodHandlePoint"
                );
        }

        Transform tip =
            renderer.transform.Find(
                "RodTipPoint"
            );

        if (tip == null)
        {
            tip =
                CreatePoint(
                    renderer.transform,
                    "RodTipPoint"
                );
        }

        Bounds bounds =
            renderer.sprite.bounds;

        /*
         * Asset trong ảnh:
         * cán dưới-phải, đầu trên-trái.
         * Đây chỉ là vị trí ban đầu; có thể kéo marker chính xác hơn.
         */
        handle.localPosition =
            new Vector3(
                Mathf.Lerp(
                    bounds.min.x,
                    bounds.max.x,
                    0.86f
                ),
                Mathf.Lerp(
                    bounds.min.y,
                    bounds.max.y,
                    0.14f
                ),
                0f
            );

        tip.localPosition =
            new Vector3(
                Mathf.Lerp(
                    bounds.min.x,
                    bounds.max.x,
                    0.06f
                ),
                Mathf.Lerp(
                    bounds.min.y,
                    bounds.max.y,
                    0.94f
                ),
                0f
            );

        handle.localRotation =
            Quaternion.identity;

        tip.localRotation =
            Quaternion.identity;

        handle.localScale =
            Vector3.one;

        tip.localScale =
            Vector3.one;

        Vector3 handleWorld =
            handle.position;

        /*
         * Pivot nằm đúng tại cán.
         */
        pivot.position =
            handleWorld;

        pivot.rotation =
            Quaternion.identity;

        pivot.localScale =
            Vector3.one;

        Undo.SetTransformParent(
            rodVisualObject.transform,
            pivot,
            "Parent Fishing Rod Under Aim Pivot"
        );

        /*
         * Giữ nguyên world transform sau khi reparent,
         * rồi dịch visual để Handle trùng đúng gốc Pivot.
         */
        Vector3 correction =
            pivot.position -
            handle.position;

        rodVisualObject.transform.position +=
            correction;

        FishingRodMouseAimPivot aim =
            pivot.GetComponent<
                FishingRodMouseAimPivot
            >();

        if (aim == null)
        {
            aim =
                Undo.AddComponent<
                    FishingRodMouseAimPivot
                >(pivot.gameObject);
        }

        SerializedObject serialized =
            new SerializedObject(
                aim
            );

        SetReference(
            serialized,
            "rodPivot",
            pivot
        );

        SetReference(
            serialized,
            "rodVisual",
            renderer.transform
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
            DisableAllOtherRodAimScripts(
                pivot.root,
                aim
            );

        bool hotbarDisabled =
            DisableHotbarRotation(
                pivot.root
            );

        renderer.flipX = false;
        renderer.flipY = false;

        EditorUtility.SetDirty(
            renderer
        );

        EditorUtility.SetDirty(
            pivot
        );

        EditorUtility.SetDirty(
            rodVisualObject
        );

        EditorUtility.SetDirty(
            aim
        );

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkAllScenesDirty();

        Selection.activeGameObject =
            pivot.gameObject;

        Debug.Log(
            Prefix +
            "DONE | Pivot=" +
            pivot.name +
            " | Visual=" +
            renderer.name +
            " | DisabledOld=" +
            disabled +
            " | HotbarRotationOff=" +
            hotbarDisabled,
            aim
        );

        EditorUtility.DisplayDialog(
            "Rod Mouse Aim",
            "Đã dựng lại đúng cấu trúc:\n\n" +
            "FishingRodAimPivot\n" +
            "└── Fishing Rod\n" +
            "    ├── RodHandlePoint\n" +
            "    └── RodTipPoint\n\n" +
            "Đã tắt script xoay cũ: " +
            disabled +
            "\nHotbar rotation đã tắt: " +
            hotbarDisabled +
            "\n\nNhấn Ctrl+S rồi Play.",
            "OK"
        );
    }

    private static Transform CreatePoint(
        Transform parent,
        string pointName)
    {
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
        DisableAllOtherRodAimScripts(
            Transform root,
            FishingRodMouseAimPivot keep)
    {
        int count = 0;

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

            string name =
                behaviour.GetType().Name;

            bool isRodAim =
                name == "FishingRodMouseAim" ||
                name ==
                    "FishingRodMouseAimStable" ||
                name ==
                    "FishingRodMouseAimFinal" ||
                name ==
                    "FishingRodAutoRotate" ||
                name ==
                    "FishingRodAimByTip" ||
                name ==
                    "FishingRodAimExact";

            if (!isRodAim)
                continue;

            Undo.RecordObject(
                behaviour,
                "Disable Old Rod Aim"
            );

            behaviour.enabled = false;

            EditorUtility.SetDirty(
                behaviour
            );

            count++;
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
                "Disable Hotbar Rotation"
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
}
#endif
