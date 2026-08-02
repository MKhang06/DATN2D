#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class FishingRodPurchasedHotbarBridgeSetupEditor
{
    [MenuItem(
        "Tools/Fishing/Fix Purchased Rod And Disable F"
    )]
    public static void Setup()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog(
                "Fishing Rod",
                "Hãy thoát Play Mode trước khi chạy.",
                "OK"
            );

            return;
        }

        FishingGearStats gear =
            UnityEngine.Object
                .FindFirstObjectByType<
                    FishingGearStats
                >(
                    FindObjectsInactive.Include
                );

        GameObject player =
            gear != null
                ? gear.gameObject
                : FindExactObject("Player");

        if (player == null)
        {
            Debug.LogError(
                "[Fishing Rod Bridge] Không tìm thấy Player."
            );

            return;
        }

        Transform toolHolder =
            FindChildByName(
                player.transform,
                "ToolHolder"
            );

        Transform rod =
            toolHolder != null
                ? FindChildByName(
                    toolHolder,
                    "Fishing Rod"
                  )
                : null;

        Transform heldItem =
            FindChildByName(
                player.transform,
                "HeldItem"
            );

        PlayerToolAnimation toolAnimation =
            player.GetComponent<
                PlayerToolAnimation
            >();

        if (toolAnimation == null)
        {
            toolAnimation =
                player.GetComponentInChildren<
                    PlayerToolAnimation
                >(true);
        }

        FishingRodPurchasedHotbarBridge bridge =
            player.GetComponent<
                FishingRodPurchasedHotbarBridge
            >();

        if (bridge == null)
        {
            bridge =
                Undo.AddComponent<
                    FishingRodPurchasedHotbarBridge
                >(player);
        }

        SerializedObject serialized =
            new SerializedObject(bridge);

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
            "ownershipGate",
            UnityEngine.Object
                .FindFirstObjectByType<
                    FishingRodOwnershipGate
                >(
                    FindObjectsInactive.Include
                )
        );

        SetReference(
            serialized,
            "playerToolAnimation",
            toolAnimation
        );

        SetReference(
            serialized,
            "oldFishingRod",
            rod != null
                ? rod.gameObject
                : null
        );

        SetReference(
            serialized,
            "heldItemRoot",
            heldItem
        );

        SetBool(
            serialized,
            "autoEquipFromHotbar",
            true
        );

        SetBool(
            serialized,
            "blockLegacyFWithoutPurchasedRod",
            true
        );

        SetBool(
            serialized,
            "forceEnableMouseRotation",
            true
        );

        serialized.ApplyModifiedProperties();

        int disabledOldBridges =
            DisableOldDisplayBridges(
                player.transform,
                bridge
            );

        if (rod != null)
            rod.gameObject.SetActive(false);

        EditorUtility.SetDirty(bridge);

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkAllScenesDirty();

        Selection.activeGameObject =
            player;

        Debug.Log(
            "[Fishing Rod Bridge] DONE | Player=" +
            player.name +
            " | Rod=" +
            (rod != null ? rod.name : "NULL") +
            " | PlayerToolAnimation=" +
            (
                toolAnimation != null
                    ? toolAnimation.name
                    : "NULL"
            ) +
            " | DisabledOldBridges=" +
            disabledOldBridges,
            bridge
        );

        EditorUtility.DisplayDialog(
            "Fishing Rod",
            "Đã sửa:\n\n" +
            "Chưa chọn cần đã mua → F không gọi được cần cũ.\n" +
            "Chọn cần đã mua → tự ShowTool(FishingRod).\n" +
            "Tự SetFishingLock(false) nên xoay ngay, không cần nhấn F lần 2.\n\n" +
            "Nhấn Ctrl+S rồi Play.",
            "OK"
        );
    }

    private static int DisableOldDisplayBridges(
        Transform root,
        FishingRodPurchasedHotbarBridge keep)
    {
        int count = 0;

        string[] names =
        {
            "FishingRodHotbarController",
            "FishingRodVisualReplacer",
            "FishingRodUseOldVisual",
            "FishingRodSingleVisualController",
            "FishingRodPurchasedItemCallsOldRod",
            "FishingRodCallExactOldObject",
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

            string typeName =
                behaviour.GetType().Name;

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
                    "Disable Old Fishing Rod Bridge"
                );

                behaviour.enabled = false;
                EditorUtility.SetDirty(behaviour);
                count++;
                break;
            }
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
            serialized.FindProperty(fieldName);

        if (property != null &&
            property.propertyType ==
                SerializedPropertyType.ObjectReference)
        {
            property.objectReferenceValue = value;
        }
    }

    private static void SetBool(
        SerializedObject serialized,
        string fieldName,
        bool value)
    {
        SerializedProperty property =
            serialized.FindProperty(fieldName);

        if (property != null &&
            property.propertyType ==
                SerializedPropertyType.Boolean)
        {
            property.boolValue = value;
        }
    }
}
#endif
