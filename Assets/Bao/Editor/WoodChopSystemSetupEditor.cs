#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class WoodChopSystemSetupEditor
{
    [MenuItem(
        "Tools/Woodcutting/Create Minigame Manager"
    )]
    public static void CreateManager()
    {
        WoodChopMinigameUI existing =
            Object.FindFirstObjectByType<
                WoodChopMinigameUI
            >(
                FindObjectsInactive.Include
            );

        if (existing != null)
        {
            Selection.activeGameObject =
                existing.gameObject;

            EditorUtility.DisplayDialog(
                "Woodcutting",
                "Scene đã có WoodChopMinigameUI.",
                "OK"
            );

            return;
        }

        GameObject manager =
            new GameObject(
                "WoodChopMinigame"
            );

        Undo.RegisterCreatedObjectUndo(
            manager,
            "Create Wood Chop Minigame"
        );

        manager.AddComponent<
            WoodChopMinigameUI
        >();

        Selection.activeGameObject =
            manager;

        EditorSceneManager
            .MarkAllScenesDirty();

        EditorUtility.DisplayDialog(
            "Woodcutting",
            "Đã tạo WoodChopMinigame.\n" +
            "UI sẽ tự dựng khi Play.",
            "OK"
        );
    }

    [MenuItem(
        "Tools/Woodcutting/Setup Selected Tree"
    )]
    public static void SetupSelectedTree()
    {
        GameObject selected =
            Selection.activeGameObject;

        if (selected == null)
        {
            EditorUtility.DisplayDialog(
                "Woodcutting",
                "Hãy chọn object cây trong Hierarchy.",
                "OK"
            );

            return;
        }

        ChoppableTree tree =
            selected.GetComponent<
                ChoppableTree
            >();

        if (tree == null)
        {
            tree =
                Undo.AddComponent<
                    ChoppableTree
                >(selected);
        }

        Collider2D[] colliders =
            selected.GetComponents<
                Collider2D
            >();

        bool hasTrigger = false;

        foreach (Collider2D collider
                 in colliders)
        {
            if (collider != null &&
                collider.isTrigger)
            {
                hasTrigger = true;
                break;
            }
        }

        if (!hasTrigger)
        {
            CircleCollider2D trigger =
                Undo.AddComponent<
                    CircleCollider2D
                >(selected);

            trigger.isTrigger = true;
            trigger.radius = 1.35f;
            trigger.offset =
                new Vector2(
                    0f,
                    0.25f
                );
        }

        WoodChopMinigameUI manager =
            Object.FindFirstObjectByType<
                WoodChopMinigameUI
            >(
                FindObjectsInactive.Include
            );

        if (manager == null)
            CreateManager();

        EditorUtility.SetDirty(
            selected
        );

        EditorSceneManager
            .MarkAllScenesDirty();

        Selection.activeGameObject =
            selected;

        EditorUtility.DisplayDialog(
            "Woodcutting",
            "Đã setup cây.\n\n" +
            "Kéo InventoryItemData Gỗ vào Wood Item.\n" +
            "Player đến gần và nhấn E để bắt đầu.",
            "OK"
        );
    }
}
#endif
