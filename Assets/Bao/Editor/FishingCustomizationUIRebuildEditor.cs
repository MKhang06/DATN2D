#if UNITY_EDITOR
using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class FishingCustomizationUIRebuildEditor
{
    private const string Prefix =
        "[FISHING CUSTOM REBUILD] ";

    private const string GeneratedFolder =
        "Assets/Bao/Generated/FishingCustomization";

    private const string PrefabPath =
        GeneratedFolder +
        "/RodPartOptionButton_Auto.prefab";

    [MenuItem(
        "Tools/Fishing/Rebuild Custom Selection UI"
    )]
    public static void Rebuild()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog(
                "Fishing Customization",
                "Hãy thoát Play Mode trước khi chạy.",
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

        GameObject selectionPanel =
            GetObjectReference(
                serialized,
                "selectionPanel"
            ) as GameObject;

        if (selectionPanel == null)
        {
            Debug.LogError(
                Prefix +
                "FishingCustomizationUI.SelectionPanel đang None.",
                customUI
            );

            return;
        }

        EnsureFolder(
            GeneratedFolder
        );

        GameObject prefab =
            BuildOptionPrefab();

        if (prefab == null)
        {
            Debug.LogError(
                Prefix +
                "Không tạo được Option Prefab."
            );

            return;
        }

        Undo.RegisterFullObjectHierarchyUndo(
            selectionPanel,
            "Rebuild Fishing Custom Selection UI"
        );

        Transform oldAutoScroll =
            FindDirectChild(
                selectionPanel.transform,
                "AutoScrollView"
            );

        if (oldAutoScroll != null)
        {
            Undo.DestroyObjectImmediate(
                oldAutoScroll.gameObject
            );
        }

        GameObject scrollObject =
            CreateUIObject(
                "AutoScrollView",
                selectionPanel.transform
            );

        RectTransform scrollRectTransform =
            scrollObject.GetComponent<
                RectTransform
            >();

        /*
         * Chừa phần tiêu đề phía trên và nút gỡ phía dưới.
         */
        SetStretch(
            scrollRectTransform,
            new Vector2(0.035f, 0.15f),
            new Vector2(0.965f, 0.84f),
            Vector2.zero,
            Vector2.zero
        );

        Image scrollBackground =
            scrollObject.AddComponent<
                Image
            >();

        scrollBackground.color =
            new Color(
                0.90f,
                0.92f,
                0.92f,
                1f
            );

        ScrollRect scrollRect =
            scrollObject.AddComponent<
                ScrollRect
            >();

        scrollRect.horizontal =
            false;

        scrollRect.vertical =
            true;

        scrollRect.movementType =
            ScrollRect.MovementType
                .Clamped;

        scrollRect.scrollSensitivity =
            25f;

        GameObject viewportObject =
            CreateUIObject(
                "Viewport",
                scrollObject.transform
            );

        RectTransform viewportRect =
            viewportObject.GetComponent<
                RectTransform
            >();

        SetStretch(
            viewportRect,
            Vector2.zero,
            Vector2.one,
            new Vector2(8f, 8f),
            new Vector2(-8f, -8f)
        );

        Image viewportImage =
            viewportObject.AddComponent<
                Image
            >();

        viewportImage.color =
            new Color(
                1f,
                1f,
                1f,
                0.001f
            );

        viewportImage.raycastTarget =
            true;

        viewportObject.AddComponent<
            RectMask2D
        >();

        GameObject contentObject =
            CreateUIObject(
                "Content",
                viewportObject.transform
            );

        RectTransform contentRect =
            contentObject.GetComponent<
                RectTransform
            >();

        contentRect.anchorMin =
            new Vector2(
                0f,
                1f
            );

        contentRect.anchorMax =
            new Vector2(
                1f,
                1f
            );

        contentRect.pivot =
            new Vector2(
                0.5f,
                1f
            );

        contentRect.anchoredPosition =
            Vector2.zero;

        contentRect.sizeDelta =
            new Vector2(
                0f,
                0f
            );

        VerticalLayoutGroup layout =
            contentObject.AddComponent<
                VerticalLayoutGroup
            >();

        layout.padding =
            new RectOffset(
                10,
                10,
                10,
                10
            );

        layout.spacing =
            10f;

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

        ContentSizeFitter fitter =
            contentObject.AddComponent<
                ContentSizeFitter
            >();

        fitter.horizontalFit =
            ContentSizeFitter.FitMode
                .Unconstrained;

        fitter.verticalFit =
            ContentSizeFitter.FitMode
                .PreferredSize;

        GameObject emptyObject =
            CreateUIObject(
                "EmptyListText",
                viewportObject.transform
            );

        RectTransform emptyRect =
            emptyObject.GetComponent<
                RectTransform
            >();

        SetStretch(
            emptyRect,
            new Vector2(0.08f, 0.20f),
            new Vector2(0.92f, 0.80f),
            Vector2.zero,
            Vector2.zero
        );

        TextMeshProUGUI emptyText =
            emptyObject.AddComponent<
                TextMeshProUGUI
            >();

        ConfigureText(
            emptyText,
            "Bạn chưa sở hữu trang bị phù hợp.",
            22f,
            TextAlignmentOptions.Center,
            new Color(
                0.20f,
                0.22f,
                0.24f,
                1f
            )
        );

        emptyText.raycastTarget =
            false;

        emptyObject.SetActive(
            false
        );

        scrollRect.viewport =
            viewportRect;

        scrollRect.content =
            contentRect;

        SetObjectReference(
            serialized,
            "optionContent",
            contentRect
        );

        SetObjectReference(
            serialized,
            "optionButtonPrefab",
            prefab
        );

        SetObjectReference(
            serialized,
            "emptyListText",
            emptyText
        );

        serialized
            .ApplyModifiedProperties();

        EditorUtility.SetDirty(
            customUI
        );

        selectionPanel.SetActive(
            false
        );

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkAllScenesDirty();

        Debug.Log(
            Prefix +
            "DONE | OptionContent=" +
            GetPath(contentRect) +
            " | OptionPrefab=" +
            prefab.name +
            " | EmptyListText=" +
            emptyText.name,
            customUI
        );

        EditorUtility.DisplayDialog(
            "Fishing Customization",
            "Đã dựng lại hoàn toàn vùng danh sách Custom.\n\n" +
            "Option Content: Content\n" +
            "Option Prefab: RodPartOptionButton_Auto\n" +
            "Empty Text: EmptyListText\n\n" +
            "Nhấn Ctrl+S rồi Play để kiểm tra.",
            "OK"
        );
    }

    private static GameObject
        BuildOptionPrefab()
    {
        GameObject root =
            CreateUIObject(
                "RodPartOptionButton_Auto",
                null
            );

        RectTransform rootRect =
            root.GetComponent<
                RectTransform
            >();

        rootRect.anchorMin =
            new Vector2(
                0f,
                1f
            );

        rootRect.anchorMax =
            new Vector2(
                1f,
                1f
            );

        rootRect.pivot =
            new Vector2(
                0.5f,
                1f
            );

        rootRect.sizeDelta =
            new Vector2(
                0f,
                116f
            );

        Image background =
            root.AddComponent<
                Image
            >();

        background.color =
            new Color(
                0.16f,
                0.22f,
                0.24f,
                1f
            );

        Button button =
            root.AddComponent<
                Button
            >();

        button.targetGraphic =
            background;

        ColorBlock colors =
            button.colors;

        colors.normalColor =
            Color.white;

        colors.highlightedColor =
            new Color(
                0.90f,
                0.95f,
                1f,
                1f
            );

        colors.pressedColor =
            new Color(
                0.75f,
                0.85f,
                0.90f,
                1f
            );

        colors.disabledColor =
            new Color(
                0.55f,
                0.55f,
                0.55f,
                0.75f
            );

        button.colors =
            colors;

        LayoutElement layoutElement =
            root.AddComponent<
                LayoutElement
            >();

        layoutElement.minHeight =
            100f;

        layoutElement.preferredHeight =
            116f;

        layoutElement.flexibleHeight =
            0f;

        GameObject iconObject =
            CreateUIObject(
                "Icon",
                root.transform
            );

        RectTransform iconRect =
            iconObject.GetComponent<
                RectTransform
            >();

        SetStretch(
            iconRect,
            new Vector2(0.02f, 0.13f),
            new Vector2(0.15f, 0.87f),
            Vector2.zero,
            Vector2.zero
        );

        Image icon =
            iconObject.AddComponent<
                Image
            >();

        icon.preserveAspect =
            true;

        icon.raycastTarget =
            false;

        GameObject itemNameObject =
            CreateUIObject(
                "ItemNameText",
                root.transform
            );

        RectTransform itemNameRect =
            itemNameObject.GetComponent<
                RectTransform
            >();

        SetStretch(
            itemNameRect,
            new Vector2(0.18f, 0.52f),
            new Vector2(0.69f, 0.90f),
            Vector2.zero,
            Vector2.zero
        );

        TextMeshProUGUI itemName =
            itemNameObject.AddComponent<
                TextMeshProUGUI
            >();

        ConfigureText(
            itemName,
            "TÊN TRANG BỊ",
            23f,
            TextAlignmentOptions.MidlineLeft,
            Color.white
        );

        itemName.fontStyle =
            FontStyles.Bold;

        GameObject statsObject =
            CreateUIObject(
                "StatsText",
                root.transform
            );

        RectTransform statsRect =
            statsObject.GetComponent<
                RectTransform
            >();

        SetStretch(
            statsRect,
            new Vector2(0.18f, 0.10f),
            new Vector2(0.70f, 0.50f),
            Vector2.zero,
            Vector2.zero
        );

        TextMeshProUGUI stats =
            statsObject.AddComponent<
                TextMeshProUGUI
            >();

        ConfigureText(
            stats,
            "Yêu cầu: 0 | Sức mạnh: +0 | Tốc độ: +0 | Độ sâu: +0m",
            15f,
            TextAlignmentOptions.MidlineLeft,
            new Color(
                0.82f,
                0.86f,
                0.88f,
                1f
            )
        );

        GameObject actionBackgroundObject =
            CreateUIObject(
                "ActionBackground",
                root.transform
            );

        RectTransform actionBackgroundRect =
            actionBackgroundObject.GetComponent<
                RectTransform
            >();

        SetStretch(
            actionBackgroundRect,
            new Vector2(0.73f, 0.22f),
            new Vector2(0.97f, 0.78f),
            Vector2.zero,
            Vector2.zero
        );

        Image actionBackground =
            actionBackgroundObject
                .AddComponent<
                    Image
                >();

        actionBackground.color =
            new Color(
                0.06f,
                0.48f,
                0.38f,
                1f
            );

        actionBackground.raycastTarget =
            false;

        GameObject actionTextObject =
            CreateUIObject(
                "ActionText",
                actionBackgroundObject
                    .transform
            );

        RectTransform actionTextRect =
            actionTextObject.GetComponent<
                RectTransform
            >();

        SetStretch(
            actionTextRect,
            Vector2.zero,
            Vector2.one,
            new Vector2(4f, 2f),
            new Vector2(-4f, -2f)
        );

        TextMeshProUGUI actionText =
            actionTextObject.AddComponent<
                TextMeshProUGUI
            >();

        ConfigureText(
            actionText,
            "TRANG BỊ",
            18f,
            TextAlignmentOptions.Center,
            Color.white
        );

        actionText.fontStyle =
            FontStyles.Bold;

        GameObject equippedMark =
            CreateUIObject(
                "EquippedMark",
                root.transform
            );

        RectTransform equippedRect =
            equippedMark.GetComponent<
                RectTransform
            >();

        SetStretch(
            equippedRect,
            new Vector2(0.86f, 0.79f),
            new Vector2(0.97f, 0.96f),
            Vector2.zero,
            Vector2.zero
        );

        Image equippedImage =
            equippedMark.AddComponent<
                Image
            >();

        equippedImage.color =
            new Color(
                0.20f,
                0.75f,
                0.45f,
                1f
            );

        equippedImage.raycastTarget =
            false;

        GameObject equippedTextObject =
            CreateUIObject(
                "EquippedText",
                equippedMark.transform
            );

        RectTransform equippedTextRect =
            equippedTextObject.GetComponent<
                RectTransform
            >();

        SetStretch(
            equippedTextRect,
            Vector2.zero,
            Vector2.one,
            Vector2.zero,
            Vector2.zero
        );

        TextMeshProUGUI equippedText =
            equippedTextObject.AddComponent<
                TextMeshProUGUI
            >();

        ConfigureText(
            equippedText,
            "ĐÃ GẮN",
            12f,
            TextAlignmentOptions.Center,
            Color.white
        );

        equippedText.fontStyle =
            FontStyles.Bold;

        equippedMark.SetActive(
            false
        );

        GameObject lockedObject =
            CreateUIObject(
                "LockedObject",
                root.transform
            );

        RectTransform lockedRect =
            lockedObject.GetComponent<
                RectTransform
            >();

        SetStretch(
            lockedRect,
            Vector2.zero,
            Vector2.one,
            Vector2.zero,
            Vector2.zero
        );

        Image lockedImage =
            lockedObject.AddComponent<
                Image
            >();

        lockedImage.color =
            new Color(
                0f,
                0f,
                0f,
                0.55f
            );

        lockedImage.raycastTarget =
            false;

        lockedObject.SetActive(
            false
        );

        GameObject savedPrefab =
            PrefabUtility
                .SaveAsPrefabAsset(
                    root,
                    PrefabPath
                );

        UnityEngine.Object
            .DestroyImmediate(
                root
            );

        return savedPrefab;
    }

    private static GameObject
        CreateUIObject(
            string objectName,
            Transform parent)
    {
        GameObject gameObject =
            new GameObject(
                objectName,
                typeof(RectTransform)
            );

        if (parent != null)
        {
            gameObject.transform
                .SetParent(
                    parent,
                    false
                );
        }

        return gameObject;
    }

    private static void ConfigureText(
        TextMeshProUGUI text,
        string content,
        float fontSize,
        TextAlignmentOptions alignment,
        Color color)
    {
        text.text =
            content;

        text.fontSize =
            fontSize;

        text.alignment =
            alignment;

        text.color =
            color;

        text.enableWordWrapping =
            false;

        text.overflowMode =
            TextOverflowModes.Ellipsis;

        text.raycastTarget =
            false;

        if (TMP_Settings
                .defaultFontAsset != null)
        {
            text.font =
                TMP_Settings
                    .defaultFontAsset;
        }
    }

    private static void SetStretch(
        RectTransform rect,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 offsetMin,
        Vector2 offsetMax)
    {
        rect.anchorMin =
            anchorMin;

        rect.anchorMax =
            anchorMax;

        rect.pivot =
            new Vector2(
                0.5f,
                0.5f
            );

        rect.offsetMin =
            offsetMin;

        rect.offsetMax =
            offsetMax;

        rect.localScale =
            Vector3.one;
    }

    private static UnityEngine.Object
        GetObjectReference(
            SerializedObject serialized,
            string fieldName)
    {
        SerializedProperty property =
            serialized.FindProperty(
                fieldName
            );

        if (property == null ||
            property.propertyType !=
                SerializedPropertyType
                    .ObjectReference)
        {
            return null;
        }

        return property
            .objectReferenceValue;
    }

    private static void SetObjectReference(
        SerializedObject serialized,
        string fieldName,
        UnityEngine.Object value)
    {
        SerializedProperty property =
            serialized.FindProperty(
                fieldName
            );

        if (property == null ||
            property.propertyType !=
                SerializedPropertyType
                    .ObjectReference)
        {
            return;
        }

        property.objectReferenceValue =
            value;
    }

    private static Transform FindDirectChild(
        Transform parent,
        string childName)
    {
        if (parent == null)
            return null;

        for (int index = 0;
             index < parent.childCount;
             index++)
        {
            Transform child =
                parent.GetChild(index);

            if (string.Equals(
                    child.name,
                    childName,
                    StringComparison
                        .OrdinalIgnoreCase))
            {
                return child;
            }
        }

        return null;
    }

    private static void EnsureFolder(
        string fullPath)
    {
        string[] parts =
            fullPath.Split('/');

        string current =
            parts[0];

        for (int index = 1;
             index < parts.Length;
             index++)
        {
            string next =
                current +
                "/" +
                parts[index];

            if (!AssetDatabase
                    .IsValidFolder(next))
            {
                AssetDatabase
                    .CreateFolder(
                        current,
                        parts[index]
                    );
            }

            current = next;
        }
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