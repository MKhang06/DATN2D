#if UNITY_EDITOR
using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class ToolSupplyShopCompleteFixEditor
{
    private const string ProductFolder =
        "Assets/Bao/ToolShop/Products";

    private const string CardPrefabPath =
        "Assets/Bao/ToolShop/Prefabs/ToolShopItemCard.prefab";

    [MenuItem(
        "Tools/Tool Shop/FIX ALL - Items + Buttons"
    )]
    public static void FixEverything()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog(
                "Tool Shop Fix",
                "Hãy thoát Play Mode rồi chạy lại.",
                "OK"
            );
            return;
        }

        ToolSupplyShopUI shop =
            UnityEngine.Object
                .FindFirstObjectByType<
                    ToolSupplyShopUI
                >(
                    FindObjectsInactive.Include
                );

        if (shop == null)
        {
            EditorUtility.DisplayDialog(
                "Tool Shop Fix",
                "Không tìm thấy ToolSupplyShopUI trong Scene.",
                "OK"
            );
            return;
        }

        GameObject shopObject =
            shop.gameObject;

        Canvas canvas =
            shopObject.GetComponent<
                Canvas
            >();

        if (canvas == null)
        {
            canvas =
                Undo.AddComponent<
                    Canvas
                >(shopObject);
        }

        canvas.renderMode =
            RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 32760;
        canvas.enabled = true;

        CanvasScaler scaler =
            shopObject.GetComponent<
                CanvasScaler
            >();

        if (scaler == null)
        {
            scaler =
                Undo.AddComponent<
                    CanvasScaler
                >(shopObject);
        }

        scaler.uiScaleMode =
            CanvasScaler.ScaleMode
                .ScaleWithScreenSize;
        scaler.referenceResolution =
            new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GraphicRaycaster raycaster =
            shopObject.GetComponent<
                GraphicRaycaster
            >();

        if (raycaster == null)
        {
            raycaster =
                Undo.AddComponent<
                    GraphicRaycaster
                >(shopObject);
        }

        raycaster.enabled = true;

        RectTransform shopRoot =
            FindRect(
                shop.transform,
                "ShopRoot"
            );

        RectTransform content =
            FindRect(
                shop.transform,
                "Content"
            );

        TMP_InputField searchInput =
            FindComponent<TMP_InputField>(
                shop.transform,
                "SearchInput"
            );

        TMP_Text moneyText =
            FindComponent<TMP_Text>(
                shop.transform,
                "MoneyText"
            );

        TMP_Text messageText =
            FindComponent<TMP_Text>(
                shop.transform,
                "MessageText"
            );

        Button closeButton =
            FindComponent<Button>(
                shop.transform,
                "CloseButton"
            );

        ToolShopPurchasePopupUI popup =
            shopObject.GetComponent<
                ToolShopPurchasePopupUI
            >();

        if (popup == null)
        {
            popup =
                Undo.AddComponent<
                    ToolShopPurchasePopupUI
                >(shopObject);
        }

        ToolShopItemCardUI cardPrefab =
            AssetDatabase
                .LoadAssetAtPath<
                    ToolShopItemCardUI
                >(CardPrefabPath);

        ToolShopItemData[] products =
            AssetDatabase.FindAssets(
                "t:ToolShopItemData",
                new[] { ProductFolder }
            )
            .Select(
                guid =>
                    AssetDatabase
                        .LoadAssetAtPath<
                            ToolShopItemData
                        >(
                            AssetDatabase
                                .GUIDToAssetPath(guid)
                        )
            )
            .Where(
                item =>
                    item != null &&
                    item.inventoryItem != null
            )
            .OrderBy(
                item => item.DisplayName
            )
            .ToArray();

        SerializedObject serialized =
            new SerializedObject(shop);

        SetObject(
            serialized,
            "shopRoot",
            shopRoot != null
                ? shopRoot.gameObject
                : null
        );

        SetObject(
            serialized,
            "content",
            content
        );

        SetObject(
            serialized,
            "itemCardPrefab",
            cardPrefab
        );

        SetObject(
            serialized,
            "purchasePopup",
            popup
        );

        SetObject(
            serialized,
            "moneyText",
            moneyText
        );

        SetObject(
            serialized,
            "messageText",
            messageText
        );

        SetObject(
            serialized,
            "searchInput",
            searchInput
        );

        SetObject(
            serialized,
            "closeButton",
            closeButton
        );

        SerializedProperty availableItems =
            serialized.FindProperty(
                "availableItems"
            );

        if (availableItems != null)
        {
            availableItems.arraySize =
                products.Length;

            for (int index = 0;
                 index < products.Length;
                 index++)
            {
                availableItems
                    .GetArrayElementAtIndex(
                        index
                    )
                    .objectReferenceValue =
                        products[index];
            }
        }

        SerializedProperty sortingOrder =
            serialized.FindProperty(
                "canvasSortingOrder"
            );

        if (sortingOrder != null)
            sortingOrder.intValue = 32760;

        serialized.ApplyModifiedProperties();

        FixShopRoot(shopRoot);
        FixContent(content);
        FixRaycastTargets(shopRoot);
        FixCardPrefab();
        FixEventSystem();

        EditorUtility.SetDirty(shop);
        EditorUtility.SetDirty(shopObject);
        EditorSceneManager.MarkAllScenesDirty();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        string report =
            "Canvas: OK, Sorting Order 32760" +
            "\nShopRoot: " +
            (shopRoot != null ? "OK" : "THIẾU") +
            "\nContent: " +
            (content != null ? "OK" : "THIẾU") +
            "\nCard Prefab: " +
            (cardPrefab != null ? "OK" : "THIẾU") +
            "\nProducts hợp lệ: " +
            products.Length +
            "\nEventSystem: " +
            (
                EventSystem.current != null ||
                UnityEngine.Object
                    .FindFirstObjectByType<
                        EventSystem
                    >(
                        FindObjectsInactive.Include
                    ) != null
                    ? "OK"
                    : "THIẾU"
            );

        Debug.Log(
            "[Tool Shop Complete Fix]\n" +
            report,
            shop
        );

        Selection.activeGameObject =
            shopObject;

        EditorUtility.DisplayDialog(
            "Tool Shop Fix hoàn tất",
            report +
            "\n\nNhấn Ctrl+S, Play rồi F8.",
            "OK"
        );
    }

    private static void FixShopRoot(
        RectTransform root)
    {
        if (root == null)
            return;

        Undo.RecordObject(
            root,
            "Fix Tool Shop Root"
        );

        root.anchorMin =
            Vector2.zero;
        root.anchorMax =
            Vector2.one;
        root.pivot =
            new Vector2(0.5f, 0.5f);
        root.offsetMin =
            Vector2.zero;
        root.offsetMax =
            Vector2.zero;
        root.localScale =
            Vector3.one;
        root.localRotation =
            Quaternion.identity;
        root.SetAsLastSibling();

        CanvasGroup group =
            root.GetComponent<
                CanvasGroup
            >();

        if (group == null)
        {
            group =
                Undo.AddComponent<
                    CanvasGroup
                >(root.gameObject);
        }

        group.alpha = 1f;
        group.interactable = true;
        group.blocksRaycasts = true;

        Image background =
            root.GetComponent<Image>();

        if (background != null)
        {
            background.raycastTarget = true;
            background.color =
                new Color(
                    0.055f,
                    0.055f,
                    0.055f,
                    0.985f
                );
        }
    }

    private static void FixContent(
        RectTransform content)
    {
        if (content == null)
            return;

        Undo.RecordObject(
            content,
            "Fix Tool Shop Content"
        );

        content.gameObject
            .SetActive(true);
        content.anchorMin =
            new Vector2(0f, 1f);
        content.anchorMax =
            new Vector2(1f, 1f);
        content.pivot =
            new Vector2(0.5f, 1f);
        content.anchoredPosition =
            Vector2.zero;
        content.sizeDelta =
            new Vector2(0f, 800f);
        content.localScale =
            Vector3.one;

        GridLayoutGroup grid =
            content.GetComponent<
                GridLayoutGroup
            >();

        if (grid == null)
        {
            grid =
                Undo.AddComponent<
                    GridLayoutGroup
                >(content.gameObject);
        }

        grid.cellSize =
            new Vector2(245f, 325f);
        grid.spacing =
            new Vector2(24f, 24f);
        grid.padding =
            new RectOffset(0, 0, 0, 24);
        grid.constraint =
            GridLayoutGroup.Constraint
                .FixedColumnCount;
        grid.constraintCount = 4;
        grid.childAlignment =
            TextAnchor.UpperLeft;

        ContentSizeFitter fitter =
            content.GetComponent<
                ContentSizeFitter
            >();

        if (fitter == null)
        {
            fitter =
                Undo.AddComponent<
                    ContentSizeFitter
                >(content.gameObject);
        }

        fitter.horizontalFit =
            ContentSizeFitter.FitMode
                .Unconstrained;
        fitter.verticalFit =
            ContentSizeFitter.FitMode
                .PreferredSize;
    }

    private static void FixRaycastTargets(
        RectTransform shopRoot)
    {
        if (shopRoot == null)
            return;

        Graphic[] graphics =
            shopRoot.GetComponentsInChildren<
                Graphic
            >(true);

        foreach (Graphic graphic
                 in graphics)
        {
            if (graphic == null)
                continue;

            graphic.raycastTarget = false;
            EditorUtility.SetDirty(graphic);
        }

        Button[] buttons =
            shopRoot.GetComponentsInChildren<
                Button
            >(true);

        foreach (Button button
                 in buttons)
        {
            if (button == null)
                continue;

            button.interactable = true;

            if (button.targetGraphic != null)
                button.targetGraphic
                    .raycastTarget = true;

            EditorUtility.SetDirty(button);
        }

        TMP_InputField[] inputs =
            shopRoot.GetComponentsInChildren<
                TMP_InputField
            >(true);

        foreach (TMP_InputField input
                 in inputs)
        {
            if (input == null)
                continue;

            input.interactable = true;

            Graphic graphic =
                input.GetComponent<
                    Graphic
                >();

            if (graphic != null)
                graphic.raycastTarget = true;

            if (input.textComponent != null)
            {
                input.textComponent
                    .raycastTarget = true;
            }

            EditorUtility.SetDirty(input);
        }

        ScrollRect[] scrollRects =
            shopRoot.GetComponentsInChildren<
                ScrollRect
            >(true);

        foreach (ScrollRect scroll
                 in scrollRects)
        {
            Image image =
                scroll.GetComponent<Image>();

            if (image != null)
                image.raycastTarget = true;
        }

        Image rootBackground =
            shopRoot.GetComponent<Image>();

        if (rootBackground != null)
            rootBackground.raycastTarget = true;
    }

    private static void FixCardPrefab()
    {
        if (!AssetDatabase.LoadAssetAtPath<
                GameObject
            >(CardPrefabPath))
        {
            return;
        }

        GameObject root =
            PrefabUtility
                .LoadPrefabContents(
                    CardPrefabPath
                );

        try
        {
            root.SetActive(true);

            RectTransform rect =
                root.GetComponent<
                    RectTransform
                >();

            if (rect != null)
            {
                rect.sizeDelta =
                    new Vector2(245f, 325f);
                rect.localScale =
                    Vector3.one;
            }

            Image rootImage =
                root.GetComponent<Image>();

            if (rootImage != null)
                rootImage.raycastTarget = true;

            Button[] buttons =
                root.GetComponentsInChildren<
                    Button
                >(true);

            foreach (Button button
                     in buttons)
            {
                button.interactable = true;

                if (button.targetGraphic != null)
                {
                    button.targetGraphic
                        .raycastTarget = true;
                }
            }

            PrefabUtility.SaveAsPrefabAsset(
                root,
                CardPrefabPath
            );
        }
        finally
        {
            PrefabUtility
                .UnloadPrefabContents(root);
        }
    }

    private static void FixEventSystem()
    {
        EventSystem eventSystem =
            UnityEngine.Object
                .FindFirstObjectByType<
                    EventSystem
                >(
                    FindObjectsInactive.Include
                );

        if (eventSystem == null)
        {
            GameObject eventObject =
                new GameObject(
                    "EventSystem",
                    typeof(EventSystem)
                );

            Undo.RegisterCreatedObjectUndo(
                eventObject,
                "Create EventSystem"
            );

            eventSystem =
                eventObject.GetComponent<
                    EventSystem
                >();
        }

        eventSystem.gameObject
            .SetActive(true);

        Type inputSystemModule =
            Type.GetType(
                "UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem"
            );

        if (inputSystemModule != null &&
            typeof(BaseInputModule)
                .IsAssignableFrom(
                    inputSystemModule
                ))
        {
            Component inputModule =
                eventSystem.gameObject
                    .GetComponent(
                        inputSystemModule
                    );

            if (inputModule == null)
            {
                inputModule =
                    Undo.AddComponent(
                        eventSystem.gameObject,
                        inputSystemModule
                    );
            }

            if (inputModule is Behaviour behaviour)
                behaviour.enabled = true;

            StandaloneInputModule oldModule =
                eventSystem.GetComponent<
                    StandaloneInputModule
                >();

            if (oldModule != null)
                oldModule.enabled = false;

            return;
        }

        StandaloneInputModule standalone =
            eventSystem.GetComponent<
                StandaloneInputModule
            >();

        if (standalone == null)
        {
            standalone =
                Undo.AddComponent<
                    StandaloneInputModule
                >(eventSystem.gameObject);
        }

        standalone.enabled = true;
    }

    private static RectTransform FindRect(
        Transform root,
        string name)
    {
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
                    name,
                    StringComparison.OrdinalIgnoreCase
                ))
            {
                return child as RectTransform;
            }
        }

        return null;
    }

    private static T FindComponent<T>(
        Transform root,
        string name)
        where T : Component
    {
        T[] components =
            root.GetComponentsInChildren<T>(
                true
            );

        foreach (T component
                 in components)
        {
            if (component != null &&
                string.Equals(
                    component.gameObject.name,
                    name,
                    StringComparison.OrdinalIgnoreCase
                ))
            {
                return component;
            }
        }

        return null;
    }

    private static void SetObject(
        SerializedObject serialized,
        string propertyName,
        UnityEngine.Object value)
    {
        SerializedProperty property =
            serialized.FindProperty(
                propertyName
            );

        if (property != null)
        {
            property.objectReferenceValue =
                value;
        }
    }
}
#endif
