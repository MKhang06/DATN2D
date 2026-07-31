#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Dọn hệ thống Tool/Hotbar đã thử nghiệm trước đó.
///
/// Menu:
/// Tools/Project Cleanup/1. Preview Tool Cleanup
/// Tools/Project Cleanup/2. Apply Safe Cleanup
/// Tools/Project Cleanup/3. Delete Obsolete Scripts Permanently
///
/// Safe Cleanup:
/// - Chuẩn hóa ToolHolder.
/// - Chỉ giữ một object cho mỗi dụng cụ.
/// - Tắt toàn bộ dụng cụ lúc Edit/khởi động.
/// - Đưa Fishing Rod cũ về trực tiếp dưới ToolHolder.
/// - Khôi phục Position + Scale chuẩn của Fishing Rod.
/// - Gỡ các component thử nghiệm/xung đột.
/// - Chuyển file code cũ vào Assets/_CleanupBackup thay vì xóa.
///
/// Permanent Cleanup:
/// - Làm mọi bước trên.
/// - Xóa vĩnh viễn các file code cũ đã xác định.
/// </summary>
public static class ToolSystemCleanupEditor
{
    private const string MenuRoot =
        "Tools/Project Cleanup/";

    private const string BackupRoot =
        "Assets/_CleanupBackup";

    private static readonly string[]
        StandardToolNames =
        {
            "HoeSprite",
            "CanSprite",
            "icon_Chop",
            "icon_Pickaxe",
            "icon_Reap",
            "Fishing Rod"
        };

    private static readonly string[]
        HelperObjectNames =
        {
            "FishingRodAimPivot",
            "RodPivot",
            "RodHandlePoint",
            "RodTipPoint"
        };

    /*
     * Chỉ xóa/gỡ các class đã được tạo trong những lần thử nghiệm trước.
     * Không xóa:
     * - PlayerToolController
     * - PlayerToolAnimation
     * - FishingRodOwnershipGate
     * - FishingManager
     * - InventoryManager
     */
    private static readonly HashSet<string>
        ObsoleteClassNames =
        new HashSet<string>(
            StringComparer.Ordinal
        )
        {
            "FishingRodPurchasedHotbarBridge",
            "FishingRodCallExactOldObject",
            "FishingRodPurchasedItemCallsOldRod",
            "FishingRodSingleVisualController",
            "FishingRodUseOldVisual",
            "FishingRodHotbarController",
            "FishingRodVisualReplacer",
            "HideHeldItemWhenFishingRodSelected",
            "FishingRodMouseAim",
            "FishingRodMouseAimStable",
            "FishingRodMouseAimFinal",
            "FishingRodMouseAimPivot",
            "FishingRodAutoRotate",
            "FishingRodAimByTip",
            "FishingRodAimExact",
            "ToolHolderStartupHide"
        };

    [MenuItem(
        MenuRoot +
        "1. Preview Tool Cleanup"
    )]
    public static void PreviewCleanup()
    {
        CleanupReport report =
            AnalyzeProject();

        Debug.Log(
            report.BuildDetailedMessage(
                "PREVIEW - CHƯA THAY ĐỔI GÌ"
            )
        );

        EditorUtility.DisplayDialog(
            "Tool Cleanup Preview",
            report.BuildSummaryMessage(
                "Chưa thay đổi project."
            ),
            "OK"
        );
    }

    [MenuItem(
        MenuRoot +
        "2. Apply Safe Cleanup"
    )]
    public static void ApplySafeCleanup()
    {
        bool confirmed =
            EditorUtility.DisplayDialog(
                "Apply Safe Cleanup",
                "Tool sẽ chuẩn hóa Hierarchy, gỡ component xung đột " +
                "và chuyển code cũ vào thư mục backup.\n\n" +
                "Không xóa vĩnh viễn file code.\n\n" +
                "Tiếp tục?",
                "Dọn an toàn",
                "Hủy"
            );

        if (!confirmed)
            return;

        ExecuteCleanup(
            permanentlyDeleteScripts: false
        );
    }

    [MenuItem(
        MenuRoot +
        "3. Delete Obsolete Scripts Permanently"
    )]
    public static void ApplyPermanentCleanup()
    {
        bool confirmed =
            EditorUtility.DisplayDialog(
                "Xóa code cũ vĩnh viễn",
                "CẢNH BÁO:\n\n" +
                "Các file code thử nghiệm được nhận diện sẽ bị xóa khỏi Assets.\n" +
                "Hãy commit Git hoặc backup project trước.\n\n" +
                "Hierarchy và component xung đột cũng được dọn.\n\n" +
                "Bạn chắc chắn muốn tiếp tục?",
                "Xóa vĩnh viễn",
                "Hủy"
            );

        if (!confirmed)
            return;

        ExecuteCleanup(
            permanentlyDeleteScripts: true
        );
    }

    private static void ExecuteCleanup(
        bool permanentlyDeleteScripts)
    {
        CleanupReport report =
            new CleanupReport();

        GameObject player =
            FindPlayer();

        if (player == null)
        {
            EditorUtility.DisplayDialog(
                "Tool Cleanup",
                "Không tìm thấy Player hoặc PlayerToolController trong Scene.",
                "OK"
            );

            return;
        }

        Undo.IncrementCurrentGroup();

        int undoGroup =
            Undo.GetCurrentGroup();

        Undo.SetCurrentGroupName(
            "Cleanup Tool System"
        );

        try
        {
            Transform toolHolder =
                FindOrCreateToolHolder(
                    player.transform,
                    report
                );

            NormalizeToolHierarchy(
                player.transform,
                toolHolder,
                report
            );

            RemoveObsoleteComponents(
                report
            );

            RemoveMissingScripts(
                player.transform,
                report
            );

            CleanupHelperObjects(
                player.transform,
                report
            );

            ProcessObsoleteScriptAssets(
                permanentlyDeleteScripts,
                report
            );

            EditorUtility.SetDirty(player);

            EditorSceneManager
                .MarkAllScenesDirty();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Undo.CollapseUndoOperations(
                undoGroup
            );

            Debug.Log(
                report.BuildDetailedMessage(
                    permanentlyDeleteScripts
                        ? "PERMANENT CLEANUP DONE"
                        : "SAFE CLEANUP DONE"
                ),
                player
            );

            Selection.activeGameObject =
                player;

            EditorUtility.DisplayDialog(
                "Tool Cleanup hoàn tất",
                report.BuildSummaryMessage(
                    permanentlyDeleteScripts
                        ? "Code cũ đã bị xóa vĩnh viễn."
                        : "Code cũ đã được chuyển vào Assets/_CleanupBackup."
                ),
                "OK"
            );
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);

            EditorUtility.DisplayDialog(
                "Tool Cleanup lỗi",
                exception.Message,
                "OK"
            );
        }
    }

    private static CleanupReport AnalyzeProject()
    {
        CleanupReport report =
            new CleanupReport();

        GameObject player =
            FindPlayer();

        if (player == null)
        {
            report.warnings.Add(
                "Không tìm thấy Player."
            );

            return report;
        }

        Transform toolHolder =
            FindChildByExactName(
                player.transform,
                "ToolHolder"
            );

        if (toolHolder == null)
        {
            report.warnings.Add(
                "Player chưa có ToolHolder."
            );
        }
        else
        {
            foreach (string toolName
                     in StandardToolNames)
            {
                List<Transform> matches =
                    FindChildrenByExactName(
                        player.transform,
                        toolName
                    );

                if (matches.Count == 0)
                {
                    report.warnings.Add(
                        "Thiếu object: " +
                        toolName
                    );
                }
                else if (matches.Count > 1)
                {
                    report.duplicateToolObjects +=
                        matches.Count - 1;
                }

                if (matches.Any(
                        item =>
                            item.parent !=
                            toolHolder))
                {
                    report.toolsToReparent++;
                }
            }
        }

        MonoBehaviour[] behaviours =
            UnityEngine.Object
                .FindObjectsByType<
                    MonoBehaviour
                >(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None
                );

        foreach (MonoBehaviour behaviour
                 in behaviours)
        {
            if (behaviour == null)
                continue;

            if (ObsoleteClassNames.Contains(
                    behaviour
                        .GetType()
                        .Name))
            {
                report.obsoleteComponents++;
            }
        }

        report.obsoleteScriptFiles =
            FindObsoleteScriptAssets()
                .Count;

        return report;
    }

    private static GameObject FindPlayer()
    {
        PlayerToolController controller =
            UnityEngine.Object
                .FindFirstObjectByType<
                    PlayerToolController
                >(
                    FindObjectsInactive.Include
                );

        if (controller != null)
        {
            Transform current =
                controller.transform;

            while (current.parent != null)
            {
                if (string.Equals(
                        current.name,
                        "Player",
                        StringComparison
                            .OrdinalIgnoreCase))
                {
                    return current.gameObject;
                }

                current = current.parent;
            }

            return controller.gameObject;
        }

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
                    "Player",
                    StringComparison
                        .OrdinalIgnoreCase))
            {
                return transform.gameObject;
            }
        }

        return null;
    }

    private static Transform
        FindOrCreateToolHolder(
            Transform player,
            CleanupReport report)
    {
        Transform toolHolder =
            FindChildByExactName(
                player,
                "ToolHolder"
            );

        if (toolHolder != null)
            return toolHolder;

        GameObject holderObject =
            new GameObject(
                "ToolHolder"
            );

        Undo.RegisterCreatedObjectUndo(
            holderObject,
            "Create ToolHolder"
        );

        holderObject.transform.SetParent(
            player,
            false
        );

        report.createdObjects++;

        return holderObject.transform;
    }

    private static void NormalizeToolHierarchy(
        Transform player,
        Transform toolHolder,
        CleanupReport report)
    {
        Transform orphanRoot =
            null;

        for (int order = 0;
             order <
                 StandardToolNames.Length;
             order++)
        {
            string toolName =
                StandardToolNames[order];

            List<Transform> matches =
                FindChildrenByExactName(
                    player,
                    toolName
                );

            if (matches.Count == 0)
            {
                report.warnings.Add(
                    "Không tìm thấy " +
                    toolName +
                    ", không tự tạo vì thiếu Sprite/Animator gốc."
                );

                continue;
            }

            Transform keep =
                ChooseToolToKeep(
                    matches,
                    toolHolder
                );

            if (keep.parent != toolHolder)
            {
                Undo.SetTransformParent(
                    keep,
                    toolHolder,
                    "Move Tool To ToolHolder"
                );

                report.toolsReparented++;
            }

            Undo.RecordObject(
                keep,
                "Reorder Tool"
            );

            keep.SetSiblingIndex(order);

            /*
             * Cần câu cũ dùng đúng Transform đã xác nhận.
             * Không ghi đè Rotation vì PlayerToolAnimation cần xoay nó.
             */
            if (string.Equals(
                    toolName,
                    "Fishing Rod",
                    StringComparison.Ordinal))
            {
                Undo.RecordObject(
                    keep,
                    "Restore Fishing Rod Transform"
                );

                keep.localPosition =
                    new Vector3(
                        0.4570001f,
                        1.059f,
                        0f
                    );

                keep.localScale =
                    new Vector3(
                        0.2622464f,
                        0.2622464f,
                        0.2622464f
                    );
            }

            if (keep.gameObject.activeSelf)
            {
                Undo.RecordObject(
                    keep.gameObject,
                    "Disable Tool By Default"
                );

                keep.gameObject.SetActive(
                    false
                );

                report.toolsDisabled++;
            }

            foreach (Transform duplicate
                     in matches)
            {
                if (duplicate == keep)
                    continue;

                if (orphanRoot == null)
                {
                    orphanRoot =
                        FindOrCreateOrphanRoot(
                            player,
                            report
                        );
                }

                Undo.SetTransformParent(
                    duplicate,
                    orphanRoot,
                    "Move Duplicate Tool To Cleanup Orphans"
                );

                Undo.RecordObject(
                    duplicate.gameObject,
                    "Disable Duplicate Tool"
                );

                duplicate.gameObject
                    .SetActive(false);

                report.duplicateToolObjects++;
            }
        }

        if (orphanRoot != null)
        {
            orphanRoot.gameObject
                .SetActive(false);
        }
    }

    private static Transform ChooseToolToKeep(
        List<Transform> matches,
        Transform toolHolder)
    {
        Transform directChild =
            matches.FirstOrDefault(
                item =>
                    item.parent ==
                    toolHolder
            );

        if (directChild != null)
            return directChild;

        Transform withRenderer =
            matches.FirstOrDefault(
                item =>
                    item.GetComponentInChildren<
                        SpriteRenderer
                    >(true) != null
            );

        return withRenderer ??
               matches[0];
    }

    private static Transform
        FindOrCreateOrphanRoot(
            Transform player,
            CleanupReport report)
    {
        const string orphanName =
            "__Cleanup_Orphans";

        Transform existing =
            FindChildByExactName(
                player,
                orphanName
            );

        if (existing != null)
            return existing;

        GameObject root =
            new GameObject(
                orphanName
            );

        Undo.RegisterCreatedObjectUndo(
            root,
            "Create Cleanup Orphan Root"
        );

        root.transform.SetParent(
            player,
            false
        );

        report.createdObjects++;

        return root.transform;
    }

    private static void
        RemoveObsoleteComponents(
            CleanupReport report)
    {
        MonoBehaviour[] behaviours =
            UnityEngine.Object
                .FindObjectsByType<
                    MonoBehaviour
                >(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None
                );

        foreach (MonoBehaviour behaviour
                 in behaviours)
        {
            if (behaviour == null)
                continue;

            string className =
                behaviour
                    .GetType()
                    .Name;

            if (!ObsoleteClassNames.Contains(
                    className))
            {
                continue;
            }

            report.removedComponentNames
                .Add(
                    className +
                    " @ " +
                    GetHierarchyPath(
                        behaviour.transform
                    )
                );

            Undo.DestroyObjectImmediate(
                behaviour
            );

            report.obsoleteComponents++;
        }
    }

    private static void RemoveMissingScripts(
        Transform root,
        CleanupReport report)
    {
        Transform[] objects =
            root.GetComponentsInChildren<
                Transform
            >(true);

        foreach (Transform item
                 in objects)
        {
            if (item == null)
                continue;

            int removed =
                GameObjectUtility
                    .RemoveMonoBehavioursWithMissingScript(
                        item.gameObject
                    );

            report.missingScriptsRemoved +=
                removed;
        }
    }

    private static void CleanupHelperObjects(
        Transform player,
        CleanupReport report)
    {
        foreach (string helperName
                 in HelperObjectNames)
        {
            List<Transform> helpers =
                FindChildrenByExactName(
                    player,
                    helperName
                );

            foreach (Transform helper
                     in helpers)
            {
                if (helper == null)
                    continue;

                /*
                 * Fishing Rod đã được đưa về ToolHolder trước bước này.
                 * Nếu helper còn child khác thì chuyển chúng sang orphan root,
                 * tránh xóa nhầm asset có nội dung quan trọng.
                 */
                if (helper.childCount > 0)
                {
                    Transform orphanRoot =
                        FindOrCreateOrphanRoot(
                            player,
                            report
                        );

                    List<Transform> children =
                        new List<Transform>();

                    for (int index = 0;
                         index <
                             helper.childCount;
                         index++)
                    {
                        children.Add(
                            helper.GetChild(index)
                        );
                    }

                    foreach (Transform child
                             in children)
                    {
                        Undo.SetTransformParent(
                            child,
                            orphanRoot,
                            "Preserve Helper Child"
                        );

                        child.gameObject
                            .SetActive(false);
                    }
                }

                report.removedObjectNames
                    .Add(
                        GetHierarchyPath(
                            helper
                        )
                    );

                Undo.DestroyObjectImmediate(
                    helper.gameObject
                );

                report.helperObjectsRemoved++;
            }
        }
    }

    private static void
        ProcessObsoleteScriptAssets(
            bool permanentlyDelete,
            CleanupReport report)
    {
        List<ScriptAssetInfo> scripts =
            FindObsoleteScriptAssets();

        if (scripts.Count == 0)
            return;

        string backupFolder =
            string.Empty;

        if (!permanentlyDelete)
        {
            backupFolder =
                CreateTimestampedBackupFolder();
        }

        foreach (ScriptAssetInfo script
                 in scripts)
        {
            if (permanentlyDelete)
            {
                bool deleted =
                    AssetDatabase.DeleteAsset(
                        script.path
                    );

                if (deleted)
                {
                    report.deletedScriptPaths
                        .Add(script.path);

                    report.obsoleteScriptFiles++;
                }

                continue;
            }

            string fileName =
                Path.GetFileName(
                    script.path
                );

            string destination =
                AssetDatabase
                    .GenerateUniqueAssetPath(
                        backupFolder +
                        "/" +
                        fileName
                    );

            string moveError =
                AssetDatabase.MoveAsset(
                    script.path,
                    destination
                );

            if (string.IsNullOrWhiteSpace(
                    moveError))
            {
                report.backedUpScriptPaths
                    .Add(
                        script.path +
                        " -> " +
                        destination
                    );

                report.obsoleteScriptFiles++;
            }
            else
            {
                report.warnings.Add(
                    "Không chuyển được " +
                    script.path +
                    ": " +
                    moveError
                );
            }
        }
    }

    private static List<ScriptAssetInfo>
        FindObsoleteScriptAssets()
    {
        List<ScriptAssetInfo> results =
            new List<ScriptAssetInfo>();

        string[] guids =
            AssetDatabase.FindAssets(
                "t:MonoScript",
                new[]
                {
                    "Assets"
                }
            );

        foreach (string guid
                 in guids)
        {
            string path =
                AssetDatabase
                    .GUIDToAssetPath(
                        guid
                    );

            MonoScript monoScript =
                AssetDatabase
                    .LoadAssetAtPath<
                        MonoScript
                    >(path);

            if (monoScript == null)
                continue;

            Type scriptClass =
                monoScript.GetClass();

            string className =
                scriptClass != null
                    ? scriptClass.Name
                    : Path.GetFileNameWithoutExtension(
                        path
                      );

            if (!ObsoleteClassNames.Contains(
                    className))
            {
                continue;
            }

            results.Add(
                new ScriptAssetInfo
                {
                    className =
                        className,
                    path = path
                }
            );
        }

        return results
            .OrderBy(
                item =>
                    item.path,
                StringComparer.Ordinal
            )
            .ToList();
    }

    private static string
        CreateTimestampedBackupFolder()
    {
        EnsureFolder(
            BackupRoot
        );

        string timestamp =
            DateTime.Now.ToString(
                "yyyyMMdd_HHmmss"
            );

        string folder =
            BackupRoot +
            "/ToolCleanup_" +
            timestamp;

        EnsureFolder(folder);

        return folder;
    }

    private static void EnsureFolder(
        string folderPath)
    {
        if (AssetDatabase.IsValidFolder(
                folderPath))
        {
            return;
        }

        string normalized =
            folderPath.Replace(
                "\\",
                "/"
            );

        string[] parts =
            normalized.Split('/');

        string current =
            parts[0];

        for (int index = 1;
             index <
                 parts.Length;
             index++)
        {
            string next =
                current +
                "/" +
                parts[index];

            if (!AssetDatabase
                    .IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(
                    current,
                    parts[index]
                );
            }

            current = next;
        }
    }

    private static Transform
        FindChildByExactName(
            Transform root,
            string exactName)
    {
        if (root == null)
            return null;

        Transform[] children =
            root.GetComponentsInChildren<
                Transform
            >(true);

        return children
            .FirstOrDefault(
                item =>
                    item != null &&
                    string.Equals(
                        item.name,
                        exactName,
                        StringComparison
                            .OrdinalIgnoreCase
                    )
            );
    }

    private static List<Transform>
        FindChildrenByExactName(
            Transform root,
            string exactName)
    {
        if (root == null)
            return new List<Transform>();

        return root
            .GetComponentsInChildren<
                Transform
            >(true)
            .Where(
                item =>
                    item != null &&
                    string.Equals(
                        item.name,
                        exactName,
                        StringComparison
                            .OrdinalIgnoreCase
                    )
            )
            .ToList();
    }

    private static string GetHierarchyPath(
        Transform transform)
    {
        if (transform == null)
            return "<null>";

        List<string> names =
            new List<string>();

        Transform current =
            transform;

        while (current != null)
        {
            names.Add(current.name);
            current = current.parent;
        }

        names.Reverse();

        return string.Join(
            "/",
            names
        );
    }

    private sealed class ScriptAssetInfo
    {
        public string className;
        public string path;
    }

    private sealed class CleanupReport
    {
        public int toolsToReparent;
        public int toolsReparented;
        public int toolsDisabled;
        public int duplicateToolObjects;
        public int obsoleteComponents;
        public int obsoleteScriptFiles;
        public int missingScriptsRemoved;
        public int helperObjectsRemoved;
        public int createdObjects;

        public readonly List<string>
            warnings =
            new List<string>();

        public readonly List<string>
            removedComponentNames =
            new List<string>();

        public readonly List<string>
            removedObjectNames =
            new List<string>();

        public readonly List<string>
            deletedScriptPaths =
            new List<string>();

        public readonly List<string>
            backedUpScriptPaths =
            new List<string>();

        public string BuildSummaryMessage(
            string footer)
        {
            return
                "Tool chuyển về ToolHolder: " +
                toolsReparented +
                "\nTool tắt mặc định: " +
                toolsDisabled +
                "\nObject tool trùng được cách ly: " +
                duplicateToolObjects +
                "\nComponent cũ được gỡ: " +
                obsoleteComponents +
                "\nMissing Script được gỡ: " +
                missingScriptsRemoved +
                "\nHelper object được xóa: " +
                helperObjectsRemoved +
                "\nFile code cũ xử lý: " +
                obsoleteScriptFiles +
                "\nCảnh báo: " +
                warnings.Count +
                "\n\n" +
                footer;
        }

        public string BuildDetailedMessage(
            string title)
        {
            List<string> lines =
                new List<string>
                {
                    "========== " +
                    title +
                    " ==========",
                    BuildSummaryMessage(
                        string.Empty
                    )
                };

            AppendSection(
                lines,
                "REMOVED COMPONENTS",
                removedComponentNames
            );

            AppendSection(
                lines,
                "REMOVED OBJECTS",
                removedObjectNames
            );

            AppendSection(
                lines,
                "BACKED UP SCRIPTS",
                backedUpScriptPaths
            );

            AppendSection(
                lines,
                "DELETED SCRIPTS",
                deletedScriptPaths
            );

            AppendSection(
                lines,
                "WARNINGS",
                warnings
            );

            return string.Join(
                "\n",
                lines
            );
        }

        private static void AppendSection(
            List<string> lines,
            string title,
            List<string> entries)
        {
            if (entries.Count == 0)
                return;

            lines.Add(
                "\n--- " +
                title +
                " ---"
            );

            lines.AddRange(entries);
        }
    }
}
#endif