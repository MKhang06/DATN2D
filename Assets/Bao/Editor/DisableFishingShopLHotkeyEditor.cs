#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class DisableFishingShopLHotkeyEditor
{
    [MenuItem(
        "Tools/Fishing/Disable L Shop Hotkey"
    )]
    public static void DisableSerializedHotkeys()
    {
        int sceneChanges =
            DisableInLoadedScenes();

        int prefabChanges =
            DisableInPrefabs();

        List<string> hardcodedResults =
            FindHardcodedLHotkeys();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        string message =
            "Đã tắt hotkey L dạng field trong:\n" +
            "- Scene: " +
            sceneChanges +
            "\n- Prefab: " +
            prefabChanges +
            "\n\n";

        if (hardcodedResults.Count == 0)
        {
            message +=
                "Không tìm thấy KeyCode.L viết cứng " +
                "trong source.";
        }
        else
        {
            message +=
                "Còn " +
                hardcodedResults.Count +
                " file có KeyCode.L viết cứng. " +
                "Xem Console và xóa đoạn Update tương ứng.";
        }

        EditorUtility.DisplayDialog(
            "Disable L Shop Hotkey",
            message,
            "OK"
        );
    }

    private static int
        DisableInLoadedScenes()
    {
        MonoBehaviour[] behaviours =
            Resources.FindObjectsOfTypeAll<
                MonoBehaviour
            >();

        int changedCount = 0;

        foreach (MonoBehaviour behaviour
                 in behaviours)
        {
            if (behaviour == null ||
                !behaviour.gameObject
                    .scene.IsValid())
            {
                continue;
            }

            if (!IsFishingShopType(
                    behaviour.GetType()))
            {
                continue;
            }

            if (!DisableFields(behaviour))
                continue;

            EditorUtility.SetDirty(behaviour);

            EditorSceneManager.MarkSceneDirty(
                behaviour.gameObject.scene
            );

            changedCount++;
        }

        return changedCount;
    }

    private static int
        DisableInPrefabs()
    {
        string[] prefabGuids =
            AssetDatabase.FindAssets(
                "t:Prefab",
                new[] { "Assets" }
            );

        int changedCount = 0;

        foreach (string guid
                 in prefabGuids)
        {
            string path =
                AssetDatabase.GUIDToAssetPath(
                    guid
                );

            GameObject root = null;

            try
            {
                root =
                    PrefabUtility
                        .LoadPrefabContents(
                            path
                        );

                bool prefabChanged = false;

                MonoBehaviour[] behaviours =
                    root.GetComponentsInChildren<
                        MonoBehaviour
                    >(true);

                foreach (MonoBehaviour behaviour
                         in behaviours)
                {
                    if (behaviour == null ||
                        !IsFishingShopType(
                            behaviour.GetType()))
                    {
                        continue;
                    }

                    if (!DisableFields(
                            behaviour))
                    {
                        continue;
                    }

                    EditorUtility.SetDirty(
                        behaviour
                    );

                    changedCount++;
                    prefabChanged = true;
                }

                if (prefabChanged)
                {
                    PrefabUtility
                        .SaveAsPrefabAsset(
                            root,
                            path
                        );
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    "Không thể kiểm tra prefab " +
                    path +
                    ": " +
                    exception.Message
                );
            }
            finally
            {
                if (root != null)
                {
                    PrefabUtility
                        .UnloadPrefabContents(
                            root
                        );
                }
            }
        }

        return changedCount;
    }

    private static bool DisableFields(
        MonoBehaviour behaviour)
    {
        SerializedObject serialized =
            new SerializedObject(
                behaviour
            );

        SerializedProperty iterator =
            serialized.GetIterator();

        bool enterChildren = true;
        bool changed = false;

        while (iterator.NextVisible(
                   enterChildren))
        {
            enterChildren = false;

            string lowerName =
                iterator.name
                    .ToLowerInvariant();

            bool keyField =
                lowerName.Contains("key") ||
                lowerName.Contains("hotkey") ||
                lowerName.Contains("shortcut");

            if (!keyField)
                continue;

            if (iterator.propertyType ==
                SerializedPropertyType.Enum)
            {
                string selectedName =
                    iterator.enumDisplayNames[
                        iterator.enumValueIndex
                    ];

                if (!string.Equals(
                        selectedName,
                        "L",
                        StringComparison
                            .OrdinalIgnoreCase))
                {
                    continue;
                }

                int noneIndex =
                    FindEnumIndex(
                        iterator.enumDisplayNames,
                        "None"
                    );

                if (noneIndex < 0)
                    continue;

                iterator.enumValueIndex =
                    noneIndex;

                changed = true;

                Debug.Log(
                    "Đã tắt phím L: " +
                    behaviour.GetType().Name +
                    "." +
                    iterator.name,
                    behaviour
                );
            }
            else if (
                iterator.propertyType ==
                    SerializedPropertyType.Boolean &&
                (lowerName.Contains("hotkey") ||
                 lowerName.Contains("shortcut")))
            {
                if (!iterator.boolValue)
                    continue;

                iterator.boolValue = false;
                changed = true;
            }
        }

        if (changed)
        {
            serialized
                .ApplyModifiedPropertiesWithoutUndo();
        }

        return changed;
    }

    private static List<string>
        FindHardcodedLHotkeys()
    {
        string[] scriptGuids =
            AssetDatabase.FindAssets(
                "t:MonoScript",
                new[] { "Assets" }
            );

        List<string> results =
            new List<string>();

        foreach (string guid
                 in scriptGuids)
        {
            string path =
                AssetDatabase.GUIDToAssetPath(
                    guid
                );

            if (!File.Exists(path))
                continue;

            string text;

            try
            {
                text =
                    File.ReadAllText(path);
            }
            catch
            {
                continue;
            }

            bool containsHardcodedL =
                text.Contains("KeyCode.L") ||
                text.Contains(".lKey") ||
                text.Contains(
                    "GetKeyDown(\"l\")"
                ) ||
                text.Contains(
                    "GetKeyDown(\"L\")"
                );

            if (!containsHardcodedL)
                continue;

            bool looksLikeFishingShop =
                text.Contains("Fishing") ||
                text.Contains("Shop") ||
                text.Contains("Bait") ||
                text.Contains("Equipment");

            if (!looksLikeFishingShop)
                continue;

            results.Add(path);

            Debug.LogWarning(
                "[L Shop Hotkey] Còn phím L " +
                "viết cứng trong file: " +
                path +
                "\nMở file và xóa block " +
                "Input.GetKeyDown(KeyCode.L)."
            );
        }

        return results;
    }

    private static bool IsFishingShopType(
        Type type)
    {
        if (type == null)
            return false;

        string name =
            type.Name.ToLowerInvariant();

        bool fishingRelated =
            name.Contains("fishing") ||
            name.Contains("bait") ||
            name.Contains("equipment");

        bool shopRelated =
            name.Contains("shop") ||
            name.Contains("store") ||
            name.Contains("vendor");

        return fishingRelated &&
               shopRelated;
    }

    private static int FindEnumIndex(
        string[] names,
        string target)
    {
        if (names == null)
            return -1;

        for (int i = 0;
             i < names.Length;
             i++)
        {
            if (string.Equals(
                    names[i],
                    target,
                    StringComparison
                        .OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return -1;
    }
}
#endif
