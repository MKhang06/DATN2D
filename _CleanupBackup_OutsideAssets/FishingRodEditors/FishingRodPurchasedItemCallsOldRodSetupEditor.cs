#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class FishingRodPurchasedItemCallsOldRodSetupEditor
{
    private const string Prefix =
        "[PURCHASED ITEM CALLS OLD ROD] ";

    [MenuItem(
        "Tools/Fishing/Restore Old Rod Behaviour"
    )]
    public static void Setup()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog(
                "Restore Old Rod",
                "Hãy thoát Play Mode trước khi chạy.",
                "OK"
            );

            return;
        }

        GameObject player =
            FindExactObject(
                "Player"
            );

        FishingGearStats gear =
            UnityEngine.Object
                .FindFirstObjectByType<
                    FishingGearStats
                >(
                    FindObjectsInactive.Include
                );

        if (player == null &&
            gear != null)
        {
            player =
                gear.gameObject;
        }

        if (player == null)
        {
            Debug.LogError(
                Prefix +
                "Không tìm thấy Player."
            );

            return;
        }

        Transform toolHolder =
            FindChildByName(
                player.transform,
                "ToolHolder"
            );

        Transform oldRod =
            FindChildByName(
                player.transform,
                "Fishing Rod"
            );

        Transform heldItem =
            FindChildByName(
                player.transform,
                "HeldItem"
            );

        if (toolHolder == null ||
            oldRod == null)
        {
            Debug.LogError(
                Prefix +
                "Không tìm thấy ToolHolder hoặc Fishing Rod.",
                player
            );

            return;
        }

        /*
         * Đưa Fishing Rod cũ trở lại đúng chỗ ban đầu.
         * Giữ world transform để không làm nó nhảy vị trí.
         */
        if (oldRod.parent != toolHolder)
        {
            Undo.SetTransformParent(
                oldRod,
                toolHolder,
                "Restore Fishing Rod To ToolHolder"
            );
        }

        FishingRodPurchasedItemCallsOldRod
            bridge =
                player.GetComponent<
                    FishingRodPurchasedItemCallsOldRod
                >();

        if (bridge == null)
        {
            bridge =
                Undo.AddComponent<
                    FishingRodPurchasedItemCallsOldRod
                >(player);
        }

        SerializedObject serialized =
            new SerializedObject(
                bridge
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
            "oldFishingRod",
            oldRod.gameObject
        );

        SetReference(
            serialized,
            "heldItemRoot",
            heldItem
        );

        serialized.ApplyModifiedProperties();

        int disabledDisplays =
            DisableDisplayControllers(
                player.transform,
                bridge,
                oldRod
            );

        int disabledExperimentalAim =
            DisableExperimentalAimScripts(
                player.transform,
                oldRod
            );

        int enabledOldBehaviours =
            EnableOldRodBehaviours(
                oldRod
            );

        oldRod.gameObject.SetActive(
            false
        );

        EditorUtility.SetDirty(
            bridge
        );

        EditorUtility.SetDirty(
            oldRod.gameObject
        );

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkAllScenesDirty();

        Selection.activeGameObject =
            oldRod.gameObject;

        Debug.Log(
            Prefix +
            "DONE | OldRod=" +
            oldRod.name +
            " | DisabledDisplays=" +
            disabledDisplays +
            " | DisabledExperimentalAim=" +
            disabledExperimentalAim +
            " | EnabledOldBehaviours=" +
            enabledOldBehaviours,
            bridge
        );

        EditorUtility.DisplayDialog(
            "Restore Old Rod",
            "Đã sửa:\n\n" +
            "Item cần mua chỉ dùng để kiểm tra Hotbar.\n" +
            "Khi chọn item sẽ bật Fishing Rod cũ.\n" +
            "Bridge không còn ghi đè Rotation.\n" +
            "Component cũ trên Fishing Rod được giữ nguyên.\n\n" +
            "Đã tắt display controller cũ: " +
            disabledDisplays +
            "\nĐã tắt aim thử nghiệm: " +
            disabledExperimentalAim +
            "\nComponent cũ được bật lại: " +
            enabledOldBehaviours +
            "\n\nNhấn Ctrl+S rồi Play.",
            "OK"
        );
    }

    private static int DisableDisplayControllers(
        Transform root,
        ishingRodPurchasedItemCallsOldRod keep,
        Transform oldRod)
    {
        int count = 0;

        string[] names =
        {
            "FishingRodHotbarController",
            "FishingRodVisualReplacer",
            "FishingRodUseOldVisual",
            "FishingRodSingleVisualController",
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

            if (behaviour.transform ==
                    oldRod ||
                behaviour.transform
                    .IsChildOf(oldRod))
            {
                continue;
            }

            string typeName =
                behaviour.GetType().Name;

            foreach (string name
                     in names)
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
                    "Disable Old Rod Display Controller"
                );

                behaviour.enabled =
                    false;

                EditorUtility.SetDirty(
                    behaviour
                );

                count++;
                break;
            }
        }

        return count;
    }

    private static int DisableExperimentalAimScripts(
        Transform root,
        Transform oldRod)
    {
        int count = 0;

        string[] experimentalNames =
        {
            "FishingRodMouseAimStable",
            "FishingRodMouseAimFinal",
            "FishingRodMouseAimPivot",
            "FishingRodAutoRotate",
            "FishingRodAimByTip",
            "FishingRodAimExact"
        };

        MonoBehaviour[] behaviours =
            root.GetComponentsInChildren<
                MonoBehaviour
            >(true);

        foreach (
            MonoBehaviour behaviour
            in behaviours)
        {
            if (behaviour == null)
                continue;

            string typeName =
                behaviour.GetType().Name;

            foreach (
                string experimentalName
                in experimentalNames)
            {
                if (!string.Equals(
                        typeName,
                        experimentalName,
                        StringComparison.Ordinal))
                {
                    continue;
                }

                Undo.RecordObject(
                    behaviour,
                    "Disable Experimental Rod Aim"
                );

                behaviour.enabled =
                    false;

                EditorUtility.SetDirty(
                    behaviour
                );

                count++;
                break;
            }
        }

        return count;
    }

    private static int EnableOldRodBehaviours(
        Transform oldRod)
    {
        int count = 0;

        MonoBehaviour[] behaviours =
            oldRod.GetComponentsInChildren<
                MonoBehaviour
            >(true);

        foreach (
            MonoBehaviour behaviour
            in behaviours)
        {
            if (behaviour == null)
                continue;

            string typeName =
                behaviour.GetType().Name;

            /*
             * Giữ/bật lại script cũ thường dùng trên Fishing Rod.
             * Không bật các bản thử nghiệm đã loại ở trên.
             */
            bool looksLikeOldRodBehaviour =
                typeName ==
                    "FishingRodMouseAim" ||
                typeName.Contains(
                    "FishingRod"
                ) ||
                typeName.Contains(
                    "RodController"
                );

            bool experimental =
                typeName ==
                    "FishingRodMouseAimStable" ||
                typeName ==
                    "FishingRodMouseAimFinal" ||
                typeName ==
                    "FishingRodMouseAimPivot" ||
                typeName ==
                    "FishingRodAutoRotate" ||
                typeName ==
                    "FishingRodAimByTip" ||
                typeName ==
                    "FishingRodAimExact";

            if (!looksLikeOldRodBehaviour ||
                experimental)
            {
                continue;
            }

            Undo.RecordObject(
                behaviour,
                "Enable Old Fishing Rod Behaviour"
            );

            behaviour.enabled =
                true;

            EditorUtility.SetDirty(
                behaviour
            );

            count++;
        }

        return count;
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

        foreach (
            Transform candidate
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

        foreach (
            Transform child
            in children)
        {
            if (child != null &&
                string.Equals(
                    child.name,
                    objectName,
                    StringComparison
                        .OrdinalIgnoreCase))
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
