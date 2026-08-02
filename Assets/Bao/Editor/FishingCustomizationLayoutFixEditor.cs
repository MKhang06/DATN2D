#if UNITY_EDITOR
using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class FishingCustomizationLayoutFixEditor
{
    private const string Prefix =
        "[FISHING CUSTOM LAYOUT] ";

    [MenuItem(
        "Tools/Fishing/Fix Customization Card Layout"
    )]
    public static void FixLayout()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog(
                "Fishing Customization",
                "Hãy thoát Play Mode trước khi chạy sửa layout.",
                "OK"
            );

            return;
        }

        FishingCustomizationUI customUI =
            UnityEngine.Object
                .FindFirstObjectByType<
                    FishingCustomizationUI
                >(
                    FindObjectsInactive.Include
                );

        if (customUI == null)
        {
            Debug.LogError(
                Prefix +
                "Không tìm thấy FishingCustomizationUI trong Scene."
            );

            return;
        }

        SerializedObject serialized =
            new SerializedObject(
                customUI
            );

        SerializedProperty contentProperty =
            serialized.FindProperty(
                "optionContent"
            );

        SerializedProperty prefabProperty =
            serialized.FindProperty(
                "optionButtonPrefab"
            );

        Transform content =
            contentProperty != null
                ? contentProperty
                    .objectReferenceValue
                    as Transform
                : null;

        GameObject optionPrefab =
            prefabProperty != null
                ? prefabProperty
                    .objectReferenceValue
                    as GameObject
                : null;

        int fixes = 0;
        int warnings = 0;

        if (content == null)
        {
            Debug.LogError(
                Prefix +
                "FishingCustomizationUI.OptionContent đang None.",
                customUI
            );

            return;
        }

        fixes += FixContent(
            content,
            ref warnings
        );

        if (optionPrefab == null)
        {
            warnings++;

            Debug.LogError(
                Prefix +
                "FishingCustomizationUI.OptionButtonPrefab đang None.",
                customUI
            );
        }
        else
        {
            fixes += FixPrefab(
                optionPrefab,
                ref warnings
            );
        }

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkAllScenesDirty();

        Debug.Log(
            Prefix +
            "DONE | Fixed=" +
            fixes +
            " | Warnings=" +
            warnings +
            " | Content=" +
            content.name +
            " | Prefab=" +
            (
                optionPrefab != null
                    ? optionPrefab.name
                    : "NULL"
            ),
            customUI
        );

        EditorUtility.DisplayDialog(
            "Fishing Customization",
            "Đã sửa layout card.\n\n" +
            "Fixed: " +
            fixes +
            "\nWarnings: " +
            warnings +
            "\n\nNhấn Ctrl+S rồi Play để kiểm tra.",
            "OK"
        );
    }

    [MenuItem(
        "Tools/Fishing/Inspect Runtime Custom Cards"
    )]
    public static void InspectRuntimeCards()
    {
        if (!EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog(
                "Fishing Customization",
                "Hãy vào Play Mode, mở bảng Custom rồi chạy lại.",
                "OK"
            );

            return;
        }

        FishingCustomizationUI customUI =
            UnityEngine.Object
                .FindFirstObjectByType<
                    FishingCustomizationUI
                >(
                    FindObjectsInactive.Include
                );

        if (customUI == null)
        {
            Debug.LogError(
                Prefix +
                "Không tìm thấy FishingCustomizationUI."
            );

            return;
        }

        SerializedObject serialized =
            new SerializedObject(
                customUI
            );

        SerializedProperty contentProperty =
            serialized.FindProperty(
                "optionContent"
            );

        Transform content =
            contentProperty != null
                ? contentProperty
                    .objectReferenceValue
                    as Transform
                : null;

        if (content == null)
        {
            Debug.LogError(
                Prefix +
                "OptionContent đang None.",
                customUI
            );

            return;
        }

        RectTransform contentRect =
            content as RectTransform;

        Debug.Log(
            Prefix +
            "Content=" +
            GetPath(content) +
            " | Active=" +
            content.gameObject.activeInHierarchy +
            " | ChildCount=" +
            content.childCount +
            " | Rect=" +
            (
                contentRect != null
                    ? contentRect.rect.size
                        .ToString()
                    : "NO RECT"
            ),
            content
        );

        for (int index = 0;
             index < content.childCount;
             index++)
        {
            Transform child =
                content.GetChild(index);

            RectTransform rect =
                child as RectTransform;

            CanvasGroup canvasGroup =
                child.GetComponent<
                    CanvasGroup
                >();

            Button button =
                child.GetComponentInChildren<
                    Button
                >(true);

            TMP_Text[] texts =
                child.GetComponentsInChildren<
                    TMP_Text
                >(true);

            Graphic[] graphics =
                child.GetComponentsInChildren<
                    Graphic
                >(true);

            float minimumGraphicAlpha =
                graphics.Length > 0
                    ? graphics.Min(
                        item =>
                            item != null
                                ? item.color.a
                                : 1f
                    )
                    : 1f;

            Debug.Log(
                Prefix +
                "Card[" +
                index +
                "] " +
                child.name +
                " | ActiveSelf=" +
                child.gameObject.activeSelf +
                " | ActiveHierarchy=" +
                child.gameObject.activeInHierarchy +
                " | Scale=" +
                child.localScale +
                " | Rect=" +
                (
                    rect != null
                        ? rect.rect.size
                            .ToString()
                        : "NO RECT"
                ) +
                " | AnchoredPosition=" +
                (
                    rect != null
                        ? rect.anchoredPosition
                            .ToString()
                        : "N/A"
                ) +
                " | CanvasAlpha=" +
                (
                    canvasGroup != null
                        ? canvasGroup.alpha
                            .ToString("0.##")
                        : "NONE"
                ) +
                " | MinGraphicAlpha=" +
                minimumGraphicAlpha
                    .ToString("0.##") +
                " | Button=" +
                (
                    button != null
                        ? "YES/" +
                          button.interactable
                        : "NO"
                ) +
                " | TextCount=" +
                texts.Length,
                child
            );
        }
    }

    private static int FixContent(
        Transform content,
        ref int warnings)
    {
        int fixes = 0;

        RectTransform rect =
            content as RectTransform;

        if (rect == null)
        {
            warnings++;

            Debug.LogError(
                Prefix +
                "OptionContent không có RectTransform.",
                content
            );

            return fixes;
        }

        Undo.RecordObject(
            rect,
            "Fix Fishing Content Rect"
        );

        if (rect.anchorMin !=
            new Vector2(0f, 1f))
        {
            rect.anchorMin =
                new Vector2(0f, 1f);

            fixes++;
        }

        if (rect.anchorMax !=
            new Vector2(1f, 1f))
        {
            rect.anchorMax =
                new Vector2(1f, 1f);

            fixes++;
        }

        if (rect.pivot !=
            new Vector2(0.5f, 1f))
        {
            rect.pivot =
                new Vector2(0.5f, 1f);

            fixes++;
        }

        if (rect.localScale !=
            Vector3.one)
        {
            rect.localScale =
                Vector3.one;

            fixes++;
        }

        Vector2 size =
            rect.sizeDelta;

        if (Mathf.Abs(size.x) >
            0.01f)
        {
            size.x = 0f;
            rect.sizeDelta = size;
            fixes++;
        }

        LayoutGroup existingLayout =
            content.GetComponent<
                LayoutGroup
            >();

        if (existingLayout == null)
        {
            VerticalLayoutGroup layout =
                Undo.AddComponent<
                    VerticalLayoutGroup
                >(
                    content.gameObject
                );

            layout.padding =
                new RectOffset(
                    10,
                    10,
                    10,
                    10
                );

            layout.spacing = 10f;
            layout.childAlignment =
                TextAnchor.UpperCenter;

            layout.childControlWidth =
                true;

            layout.childControlHeight =
                true;

            layout.childForceExpandWidth =
                true;

            layout.childForceExpandHeight =
                false;

            fixes++;

            Debug.Log(
                Prefix +
                "Đã thêm VerticalLayoutGroup vào Content.",
                content
            );
        }
        else if (
            existingLayout is
            VerticalLayoutGroup vertical)
        {
            Undo.RecordObject(
                vertical,
                "Fix Fishing Vertical Layout"
            );

            vertical.childControlWidth =
                true;

            vertical.childControlHeight =
                true;

            vertical.childForceExpandWidth =
                true;

            vertical.childForceExpandHeight =
                false;

            if (vertical.spacing <= 0f)
                vertical.spacing = 10f;

            fixes++;
        }
        else if (
            existingLayout is
            GridLayoutGroup grid)
        {
            Undo.RecordObject(
                grid,
                "Fix Fishing Grid Layout"
            );

            if (grid.cellSize.x <= 1f)
                grid.cellSize =
                    new Vector2(
                        600f,
                        Mathf.Max(
                            110f,
                            grid.cellSize.y
                        )
                    );

            if (grid.cellSize.y <= 1f)
                grid.cellSize =
                    new Vector2(
                        grid.cellSize.x,
                        110f
                    );

            if (grid.spacing.y <= 0f)
                grid.spacing =
                    new Vector2(
                        grid.spacing.x,
                        10f
                    );

            fixes++;
        }

        ContentSizeFitter fitter =
            content.GetComponent<
                ContentSizeFitter
            >();

        if (fitter == null)
        {
            fitter =
                Undo.AddComponent<
                    ContentSizeFitter
                >(
                    content.gameObject
                );

            fixes++;
        }

        Undo.RecordObject(
            fitter,
            "Fix Fishing Content Size"
        );

        fitter.horizontalFit =
            ContentSizeFitter.FitMode
                .Unconstrained;

        fitter.verticalFit =
            ContentSizeFitter.FitMode
                .PreferredSize;

        ScrollRect scrollRect =
            content.GetComponentInParent<
                ScrollRect
            >(true);

        if (scrollRect != null)
        {
            Undo.RecordObject(
                scrollRect,
                "Fix Fishing Scroll Rect"
            );

            if (scrollRect.content !=
                rect)
            {
                scrollRect.content =
                    rect;

                fixes++;
            }

            scrollRect.horizontal =
                false;

            scrollRect.vertical =
                true;

            RectTransform viewport =
                FindViewport(content);

            if (viewport != null &&
                scrollRect.viewport !=
                    viewport)
            {
                scrollRect.viewport =
                    viewport;

                fixes++;
            }
        }
        else
        {
            warnings++;

            Debug.LogWarning(
                Prefix +
                "Không tìm thấy ScrollRect cha của Content.",
                content
            );
        }

        EditorUtility.SetDirty(
            content.gameObject
        );

        return fixes;
    }

    private static int FixPrefab(
        GameObject prefabReference,
        ref int warnings)
    {
        string assetPath =
            AssetDatabase.GetAssetPath(
                prefabReference
            );

        if (!string.IsNullOrWhiteSpace(
                assetPath) &&
            PrefabUtility.GetPrefabAssetType(
                prefabReference) !=
                PrefabAssetType.NotAPrefab)
        {
            GameObject prefabRoot =
                PrefabUtility
                    .LoadPrefabContents(
                        assetPath
                    );

            try
            {
                int fixes =
                    FixCardObject(
                        prefabRoot,
                        ref warnings
                    );

                PrefabUtility
                    .SaveAsPrefabAsset(
                        prefabRoot,
                        assetPath
                    );

                return fixes;
            }
            finally
            {
                PrefabUtility
                    .UnloadPrefabContents(
                        prefabRoot
                    );
            }
        }

        Undo.RecordObject(
            prefabReference,
            "Fix Fishing Option Card"
        );

        return FixCardObject(
            prefabReference,
            ref warnings
        );
    }

    private static int FixCardObject(
        GameObject card,
        ref int warnings)
    {
        int fixes = 0;

        if (!card.activeSelf)
        {
            card.SetActive(true);
            fixes++;
        }

        RectTransform rect =
            card.GetComponent<
                RectTransform
            >();

        if (rect == null)
        {
            warnings++;

            Debug.LogError(
                Prefix +
                card.name +
                " không có RectTransform.",
                card
            );

            return fixes;
        }

        rect.localScale =
            Vector3.one;

        Vector2 size =
            rect.sizeDelta;

        if (size.x <= 1f)
            size.x = 600f;

        if (size.y <= 1f)
            size.y = 110f;

        rect.sizeDelta = size;

        LayoutElement layout =
            card.GetComponent<
                LayoutElement
            >();

        if (layout == null)
        {
            layout =
                card.AddComponent<
                    LayoutElement
                >();

            fixes++;
        }

        layout.minHeight =
            Mathf.Max(
                layout.minHeight,
                90f
            );

        layout.preferredHeight =
            Mathf.Max(
                layout.preferredHeight,
                110f
            );

        layout.flexibleHeight = 0f;

        CanvasGroup canvasGroup =
            card.GetComponent<
                CanvasGroup
            >();

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            fixes++;
        }

        Graphic[] graphics =
            card.GetComponentsInChildren<
                Graphic
            >(true);

        foreach (Graphic graphic
                 in graphics)
        {
            if (graphic == null ||
                graphic.color.a > 0.01f)
            {
                continue;
            }

            Color color =
                graphic.color;

            color.a = 1f;
            graphic.color = color;
            fixes++;
        }

        Button button =
            card.GetComponentInChildren<
                Button
            >(true);

        if (button == null)
        {
            warnings++;

            Debug.LogError(
                Prefix +
                card.name +
                " không có Button.",
                card
            );
        }

        WarnMissingChild(
            card.transform,
            "Icon",
            ref warnings
        );

        WarnMissingChild(
            card.transform,
            "ItemNameText",
            ref warnings
        );

        WarnMissingChild(
            card.transform,
            "StatsText",
            ref warnings
        );

        bool hasActionText =
            FindDeepChild(
                card.transform,
                "ActionText"
            ) != null ||
            FindDeepChild(
                card.transform,
                "ButtonText"
            ) != null;

        if (!hasActionText)
        {
            warnings++;

            Debug.LogWarning(
                Prefix +
                card.name +
                " thiếu ActionText hoặc ButtonText.",
                card
            );
        }

        EditorUtility.SetDirty(card);

        return fixes;
    }

    private static RectTransform FindViewport(
        Transform content)
    {
        Transform current =
            content.parent;

        while (current != null)
        {
            if (string.Equals(
                    current.name,
                    "Viewport",
                    StringComparison
                        .OrdinalIgnoreCase))
            {
                return current as
                    RectTransform;
            }

            current = current.parent;
        }

        return null;
    }

    private static void WarnMissingChild(
        Transform root,
        string childName,
        ref int warnings)
    {
        if (FindDeepChild(
                root,
                childName) != null)
        {
            return;
        }

        warnings++;

        Debug.LogWarning(
            Prefix +
            root.name +
            " thiếu child tên " +
            childName +
            ".",
            root
        );
    }

    private static Transform FindDeepChild(
        Transform root,
        string name)
    {
        if (root == null)
            return null;

        foreach (Transform child
                 in root)
        {
            if (string.Equals(
                    child.name,
                    name,
                    StringComparison
                        .OrdinalIgnoreCase))
            {
                return child;
            }

            Transform found =
                FindDeepChild(
                    child,
                    name
                );

            if (found != null)
                return found;
        }

        return null;
    }

    private static string GetPath(
        Transform transform)
    {
        if (transform == null)
            return "NULL";

        string path =
            transform.name;

        Transform current =
            transform.parent;

        while (current != null)
        {
            path =
                current.name +
                "/" +
                path;

            current =
                current.parent;
        }

        return path;
    }
}
#endif