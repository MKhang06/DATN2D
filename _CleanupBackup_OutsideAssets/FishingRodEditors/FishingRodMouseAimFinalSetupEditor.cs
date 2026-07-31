#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class FishingRodMouseAimFinalSetupEditor
{
    [MenuItem(
        "Tools/Fishing/Fix Rod Mouse Aim Completely"
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
            FindObjectByExactName(
                "Fishing Rod"
            );

        if (rod == null)
        {
            Debug.LogError(
                "[FishingRodMouseAimFinalSetup] " +
                "Không tìm thấy object tên Fishing Rod."
            );

            return;
        }

        SpriteRenderer renderer =
            rod.GetComponentInChildren<
                SpriteRenderer
            >(true);

        FishingRodMouseAimFinal finalAim =
            rod.GetComponent<
                FishingRodMouseAimFinal
            >();

        if (finalAim == null)
        {
            finalAim =
                Undo.AddComponent<
                    FishingRodMouseAimFinal
                >(rod);
        }

        SerializedObject serialized =
            new SerializedObject(
                finalAim
            );

        SetReference(
            serialized,
            "rodPivot",
            rod.transform
        );

        SetReference(
            serialized,
            "rodVisual",
            renderer != null
                ? renderer.transform
                : rod.transform
        );

        SetReference(
            serialized,
            "rodRenderer",
            renderer
        );

        SetReference(
            serialized,
            "worldCamera",
            Camera.main
        );

        SerializedProperty forward =
            serialized.FindProperty(
                "spriteForwardLocal"
            );

        if (forward != null)
        {
            forward.vector2Value =
                new Vector2(-1f, 1f);
        }

        SetBool(
            serialized,
            "reverseForward",
            false
        );

        SetBool(
            serialized,
            "disableKnownOldAimScripts",
            true
        );

        SetBool(
            serialized,
            "forceDisableSpriteFlip",
            true
        );

        serialized.ApplyModifiedProperties();

        int disabledOldScripts =
            DisableOldAimComponents(
                rod.transform.root
            );

        bool disabledHotbarRotation =
            DisableHotbarDirectionRotation(
                rod.transform.root
            );

        EditorUtility.SetDirty(
            finalAim
        );

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkAllScenesDirty();

        Selection.activeGameObject =
            rod;

        Debug.Log(
            "[FishingRodMouseAimFinalSetup] DONE | " +
            "Rod=" +
            rod.name +
            " | Renderer=" +
            (
                renderer != null
                    ? renderer.name
                    : "NULL"
            ) +
            " | DisabledOldAim=" +
            disabledOldScripts +
            " | DisabledHotbarRotation=" +
            disabledHotbarRotation,
            finalAim
        );

        EditorUtility.DisplayDialog(
            "Fishing Rod Aim",
            "Đã sửa hệ thống xoay cần câu.\n\n" +
            "Sprite Forward Local = (-1, 1)\n" +
            "Script xoay cũ đã tắt: " +
            disabledOldScripts +
            "\nHotbar Rotate With Player Direction đã tắt: " +
            disabledHotbarRotation +
            "\n\nNhấn Ctrl+S rồi Play.",
            "OK"
        );
    }

    private static int DisableOldAimComponents(
        Transform root)
    {
        int count = 0;

        MonoBehaviour[] behaviours =
            root.GetComponentsInChildren<
                MonoBehaviour
            >(true);

        string[] oldTypes =
        {
            "FishingRodMouseAim",
            "FishingRodMouseAimStable",
            "FishingRodAutoRotate",
            "FishingRodAimByTip"
        };

        foreach (
            MonoBehaviour behaviour
            in behaviours)
        {
            if (behaviour == null ||
                behaviour is
                    FishingRodMouseAimFinal)
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
                    "Disable Old Rod Aim"
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
        DisableHotbarDirectionRotation(
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
                "Disable Rod Direction Rotation"
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

    private static GameObject
        FindObjectByExactName(
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

    private static void SetBool(
        SerializedObject serialized,
        string fieldName,
        bool value)
    {
        SerializedProperty property =
            serialized.FindProperty(
                fieldName
            );

        if (property != null &&
            property.propertyType ==
                SerializedPropertyType.Boolean)
        {
            property.boolValue =
                value;
        }
    }
}
#endif
