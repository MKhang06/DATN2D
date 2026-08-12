#if UNITY_EDITOR
using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class FishingRodPartOptionPrefabFixEditor
{
    private const string Prefix =
        "[FISHING OPTION PREFAB FIX] ";

    [MenuItem(
        "Tools/Fishing/Fix Rod Part Option Prefab"
    )]
    public static void FixPrefab()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog(
                "Fishing Option Prefab",
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

        SerializedProperty prefabProperty =
            serialized.FindProperty(
                "optionButtonPrefab"
            );

        GameObject prefabReference =
            prefabProperty != null
                ? prefabProperty
                    .objectReferenceValue
                    as GameObject
                : null;

        if (prefabReference == null)
        {
            Debug.LogError(
                Prefix +
                "FishingCustomizationUI.OptionButtonPrefab đang None.",
                customUI
            );

            return;
        }

        string prefabPath =
            AssetDatabase.GetAssetPath(
                prefabReference
            );

        if (string.IsNullOrWhiteSpace(
                prefabPath))
        {
            Debug.LogError(
                Prefix +
                "OptionButtonPrefab không phải Prefab Asset.",
                prefabReference
            );

            return;
        }

        GameObject root =
            PrefabUtility
                .LoadPrefabContents(
                    prefabPath
                );

        int created = 0;
        int fixedCount = 0;

        try
        {
            RectTransform rootRect =
                EnsureRectTransform(
                    root
                );

            root.SetActive(true);
            root.transform.localScale =
                Vector3.one;

            if (rootRect.sizeDelta.x <
                100f)
            {
                rootRect.sizeDelta =
                    new Vector2(
                        620f,
                        Mathf.Max(
                            110f,
                            rootRect
                                .sizeDelta.y
                        )
                    );

                fixedCount++;
            }

            if (rootRect.sizeDelta.y <
                70f)
            {
                rootRect.sizeDelta =
                    new Vector2(
                        rootRect
                            .sizeDelta.x,
                        110f
                    );

                fixedCount++;
            }

            LayoutElement layout =
                root.GetComponent<
                    LayoutElement
                >();

            if (layout == null)
            {
                layout =
                    root.AddComponent<
                        LayoutElement
                    >();

                created++;
            }

            layout.minHeight = 90f;
            layout.preferredHeight =
                110f;

            layout.flexibleHeight = 0f;

            Button button =
                root.GetComponent<
                    Button
                >();

            if (button == null)
            {
                button =
                    root.GetComponentInChildren<
                        Button
                    >(true);
            }

            if (button == null)
            {
                button =
                    root.AddComponent<
                        Button
                    >();

                Image targetImage =
                    root.GetComponent<
                        Image
                    >();

                if (targetImage != null)
                {
                    button.targetGraphic =
                        targetImage;
                }

                created++;

                Debug.Log(
                    Prefix +
                    "Đã thêm Button vào root prefab.",
                    root
                );
            }

            TextMeshProUGUI styleSource =
                FindText(
                    root.transform,
                    "ItemNameText"
                );

            if (styleSource == null)
            {
                styleSource =
                    root.GetComponentInChildren<
                        TextMeshProUGUI
                    >(true);
            }

            Image icon =
                FindImage(
                    root.transform,
                    "Icon"
                );

            if (icon == null)
            {
                GameObject iconObject =
                    new GameObject(
                        "Icon",
                        typeof(RectTransform),
                        typeof(CanvasRenderer),
                        typeof(Image)
                    );

                iconObject.transform
                    .SetParent(
                        root.transform,
                        false
                    );

                RectTransform iconRect =
                    iconObject
                        .GetComponent<
                            RectTransform
                        >();

                SetRect(
                    iconRect,
                    new Vector2(
                        0.02f,
                        0.15f
                    ),
                    new Vector2(
                        0.16f,
                        0.85f
                    ),
                    Vector2.zero,
                    Vector2.zero
                );

                icon =
                    iconObject
                        .GetComponent<
                            Image
                        >();

                icon.preserveAspect =
                    true;

                icon.raycastTarget =
                    false;

                created++;
            }

            TextMeshProUGUI itemName =
                FindText(
                    root.transform,
                    "ItemNameText"
                );

            if (itemName == null)
            {
                itemName =
                    CreateText(
                        root.transform,
                        "ItemNameText",
                        "TÊN TRANG BỊ",
                        styleSource,
                        new Vector2(
                            0.19f,
                            0.52f
                        ),
                        new Vector2(
                            0.69f,
                            0.90f
                        ),
                        TextAlignmentOptions
                            .MidlineLeft,
                        24f
                    );

                styleSource =
                    itemName;

                created++;
            }

            TextMeshProUGUI stats =
                FindText(
                    root.transform,
                    "StatsText"
                );

            if (stats == null)
            {
                stats =
                    CreateText(
                        root.transform,
                        "StatsText",
                        "Yêu cầu: 0 | Sức mạnh: +0 | Tốc độ: +0 | Độ sâu: +0m",
                        styleSource,
                        new Vector2(
                            0.19f,
                            0.10f
                        ),
                        new Vector2(
                            0.70f,
                            0.52f
                        ),
                        TextAlignmentOptions
                            .MidlineLeft,
                        16f
                    );

                created++;

                Debug.Log(
                    Prefix +
                    "Đã tạo StatsText.",
                    stats
                );
            }

            TextMeshProUGUI action =
                FindText(
                    root.transform,
                    "ActionText"
                );

            if (action == null)
            {
                action =
                    FindText(
                        root.transform,
                        "ButtonText"
                    );
            }

            if (action == null)
            {
                Transform actionParent =
                    button.transform;

                Vector2 anchorMin;
                Vector2 anchorMax;

                if (actionParent ==
                    root.transform)
                {
                    anchorMin =
                        new Vector2(
                            0.73f,
                            0.18f
                        );

                    anchorMax =
                        new Vector2(
                            0.98f,
                            0.82f
                        );
                }
                else
                {
                    anchorMin =
                        Vector2.zero;

                    anchorMax =
                        Vector2.one;
                }

                action =
                    CreateText(
                        actionParent,
                        "ActionText",
                        "TRANG BỊ",
                        styleSource,
                        anchorMin,
                        anchorMax,
                        TextAlignmentOptions
                            .Center,
                        20f
                    );

                created++;

                Debug.Log(
                    Prefix +
                    "Đã tạo ActionText.",
                    action
                );
            }

            action.raycastTarget =
                false;

            stats.raycastTarget =
                false;

            itemName.raycastTarget =
                false;

            icon.raycastTarget =
                false;

            CanvasGroup canvasGroup =
                root.GetComponent<
                    CanvasGroup
                >();

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable =
                    true;

                canvasGroup.blocksRaycasts =
                    true;

                fixedCount++;
            }

            PrefabUtility
                .SaveAsPrefabAsset(
                    root,
                    prefabPath
                );
        }
        finally
        {
            PrefabUtility
                .UnloadPrefabContents(
                    root
                );
        }

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkAllScenesDirty();

        Debug.Log(
            Prefix +
            "DONE | Created=" +
            created +
            " | Fixed=" +
            fixedCount +
            " | Prefab=" +
            prefabReference.name,
            prefabReference
        );

        EditorUtility.DisplayDialog(
            "Fishing Option Prefab",
            "Đã sửa prefab " +
            prefabReference.name +
            ".\n\nCreated: " +
            created +
            "\nFixed: " +
            fixedCount +
            "\n\nNhấn Ctrl+S rồi Play để kiểm tra.",
            "OK"
        );
    }

    private static RectTransform
        EnsureRectTransform(
            GameObject gameObject)
    {
        RectTransform rect =
            gameObject.GetComponent<
                RectTransform
            >();

        if (rect != null)
            return rect;

        return gameObject
            .AddComponent<
                RectTransform
            >();
    }

    private static TextMeshProUGUI
        CreateText(
            Transform parent,
            string objectName,
            string defaultText,
            TextMeshProUGUI styleSource,
            Vector2 anchorMin,
            Vector2 anchorMax,
            TextAlignmentOptions alignment,
            float defaultFontSize)
    {
        GameObject textObject =
            new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI)
            );

        textObject.transform.SetParent(
            parent,
            false
        );

        RectTransform rect =
            textObject.GetComponent<
                RectTransform
            >();

        SetRect(
            rect,
            anchorMin,
            anchorMax,
            Vector2.zero,
            Vector2.zero
        );

        TextMeshProUGUI text =
            textObject.GetComponent<
                TextMeshProUGUI
            >();

        text.text = defaultText;
        text.alignment = alignment;
        text.textWrappingMode =
            TextWrappingModes.NoWrap;

        text.overflowMode =
            TextOverflowModes
                .Ellipsis;

        text.raycastTarget = false;

        if (styleSource != null)
        {
            text.font =
                styleSource.font;

            text.fontSharedMaterial =
                styleSource
                    .fontSharedMaterial;

            text.color =
                styleSource.color;

            text.fontStyle =
                styleSource.fontStyle;

            text.fontSize =
                styleSource.fontSize > 0f
                    ? styleSource.fontSize
                    : defaultFontSize;
        }
        else
        {
            text.fontSize =
                defaultFontSize;

            text.color =
                Color.white;
        }

        return text;
    }

    private static void SetRect(
        RectTransform rect,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 offsetMin,
        Vector2 offsetMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot =
            new Vector2(
                0.5f,
                0.5f
            );

        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
        rect.localScale =
            Vector3.one;
    }

    private static TextMeshProUGUI
        FindText(
            Transform parent,
            string objectName)
    {
        Transform child =
            FindDeepChild(
                parent,
                objectName
            );

        return child != null
            ? child.GetComponent<
                TextMeshProUGUI
            >()
            : null;
    }

    private static Image FindImage(
        Transform parent,
        string objectName)
    {
        Transform child =
            FindDeepChild(
                parent,
                objectName
            );

        return child != null
            ? child.GetComponent<
                Image
            >()
            : null;
    }

    private static Transform FindDeepChild(
        Transform parent,
        string objectName)
    {
        if (parent == null)
            return null;

        foreach (Transform child
                 in parent)
        {
            if (string.Equals(
                    child.name,
                    objectName,
                    StringComparison
                        .OrdinalIgnoreCase))
            {
                return child;
            }

            Transform result =
                FindDeepChild(
                    child,
                    objectName
                );

            if (result != null)
                return result;
        }

        return null;
    }
}
#endif
