using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
internal static class MapMainHierarchyOrganizerEditor
{
    private const string MapMainPath =
        "Assets/Khang/SceneMK/MAPMAIN.unity";

    private static readonly string[] GroupNames =
    {
        "_01_WORLD",
        "_02_CHARACTERS",
        "_03_SYSTEMS",
        "_04_INTERACTIONS",
        "_05_UI",
        "_06_VFX"
    };

    private static readonly HashSet<string> SystemNames =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Main Camera",
            "Grid_PlanterManager",
            "Audio",
            "CursorManager",
            "GameManager",
            "GameSystems",
            "CinemachineCamera",
            "WorldSceneOrganizer2D"
        };

    private static readonly HashSet<string> InteractionNames =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "WoodChopMinigame",
            "FishingObjects"
        };

    private static readonly HashSet<string> VisualEffectNames =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Rain",
            "Snow"
        };

    static MapMainHierarchyOrganizerEditor()
    {
        EditorApplication.delayCall += OrganizeLoadedMapMain;
    }

    [MenuItem("Tools/DATN2D/Organize MAPMAIN Hierarchy")]
    private static void OrganizeFromMenu()
    {
        Scene scene = SceneManager.GetActiveScene();

        if (scene.path != MapMainPath)
        {
            Debug.LogWarning(
                "Open MAPMAIN before organizing its hierarchy."
            );
            return;
        }

        OrganizeScene(scene, true);
    }

    private static void OrganizeLoadedMapMain()
    {
        Scene scene = SceneManager.GetActiveScene();

        if (scene.IsValid() &&
            scene.isLoaded &&
            scene.path == MapMainPath)
        {
            OrganizeScene(scene, true);
        }
    }

    private static void OrganizeScene(Scene scene, bool saveScene)
    {
        GameObject[] groups = new GameObject[GroupNames.Length];
        bool changed = false;

        for (int i = 0; i < GroupNames.Length; i++)
        {
            groups[i] = FindRoot(scene, GroupNames[i]);

            if (groups[i] != null)
                continue;

            groups[i] = new GameObject(GroupNames[i]);
            SceneManager.MoveGameObjectToScene(groups[i], scene);
            Undo.RegisterCreatedObjectUndo(
                groups[i],
                "Create MAPMAIN hierarchy groups"
            );
            changed = true;
        }

        GameObject[] roots = scene.GetRootGameObjects();
        List<GameObject> candidates = new List<GameObject>();

        foreach (GameObject root in roots)
        {
            if (root == null)
                continue;

            if (!IsOrganizerGroup(root.name))
            {
                candidates.Add(root);
                continue;
            }

            Transform groupTransform = root.transform;

            for (int i = 0; i < groupTransform.childCount; i++)
                candidates.Add(groupTransform.GetChild(i).gameObject);
        }

        int movedCount = 0;

        foreach (GameObject root in candidates)
        {
            if (root == null)
                continue;

            int targetGroupIndex = GetTargetGroupIndex(root);
            Transform targetParent = groups[targetGroupIndex].transform;

            if (root.transform.parent == targetParent)
                continue;

            Undo.SetTransformParent(
                root.transform,
                targetParent,
                "Organize MAPMAIN hierarchy"
            );

            movedCount++;
            changed = true;
        }

        for (int i = 0; i < groups.Length; i++)
            groups[i].transform.SetSiblingIndex(i);

        if (!changed)
            return;

        EditorSceneManager.MarkSceneDirty(scene);

        if (saveScene)
            EditorSceneManager.SaveScene(scene);

        Debug.Log(
            $"MAPMAIN hierarchy organized: moved {movedCount} roots " +
            $"into {GroupNames.Length} groups."
        );
    }

    private static int GetTargetGroupIndex(GameObject root)
    {
        string objectName = root.name;

        if (SystemNames.Contains(objectName) ||
            objectName.EndsWith("Manager", StringComparison.OrdinalIgnoreCase) ||
            objectName.EndsWith("Systems", StringComparison.OrdinalIgnoreCase))
        {
            return 2;
        }

        if (InteractionNames.Contains(objectName))
            return 3;

        if (VisualEffectNames.Contains(objectName) ||
            root.GetComponent<ParticleSystem>() != null)
        {
            return 5;
        }

        if (IsUiRoot(root))
            return 4;

        if (IsCharacterRoot(root))
            return 1;

        return 0;
    }

    private static bool IsUiRoot(GameObject root)
    {
        string objectName = root.name;

        return root.GetComponent<Canvas>() != null ||
               root.GetComponent<RectTransform>() != null ||
               objectName.Equals(
                   "EventSystem",
                   StringComparison.OrdinalIgnoreCase
               ) ||
               objectName.IndexOf(
                   "Canvas",
                   StringComparison.OrdinalIgnoreCase
               ) >= 0 ||
               objectName.IndexOf(
                   "Prompt",
                   StringComparison.OrdinalIgnoreCase
               ) >= 0;
    }

    private static bool IsCharacterRoot(GameObject root)
    {
        string objectName = root.name;

        return (root.GetComponent<Rigidbody2D>() != null &&
                root.GetComponent<SpriteRenderer>() != null) ||
               objectName.Equals(
                   "Player",
                   StringComparison.OrdinalIgnoreCase
               ) ||
               objectName.IndexOf(
                   "npc",
                   StringComparison.OrdinalIgnoreCase
               ) >= 0 ||
               objectName.IndexOf(
                   "Waypoint",
                   StringComparison.OrdinalIgnoreCase
               ) >= 0 ||
               objectName.StartsWith(
                   "Point_npc",
                   StringComparison.OrdinalIgnoreCase
               );
    }

    private static bool IsOrganizerGroup(string objectName)
    {
        foreach (string groupName in GroupNames)
        {
            if (objectName == groupName)
                return true;
        }

        return false;
    }

    private static GameObject FindRoot(Scene scene, string objectName)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root != null && root.name == objectName)
                return root;
        }

        return null;
    }
}
