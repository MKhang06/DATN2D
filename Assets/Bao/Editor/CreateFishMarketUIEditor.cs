#if UNITY_EDITOR
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class RebuildFishMarketUIFixEditor
{
    private const string CanvasName =
        "FishMarketCanvas";

    private const string ControllerName =
        "FishMarketUIController";

    [MenuItem(
        "Tools/Fishing/Rebuild Fish Market UI - Fix White"
    )]
    public static void Rebuild()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog(
                "Fish Market UI",
                "Hãy thoát Play Mode trước khi tạo lại UI.",
                "OK"
            );

            return;
        }

        EnsureTextMeshProReady();

        Canvas canvas =
            FindOrCreateDedicatedCanvas();

        DeleteOldControllers();

        GameObject controller =
            CreateRect(
                ControllerName,
                canvas.transform
            );

        Stretch(controller);

        FishMarketUI marketUI =
            controller.AddComponent<
                FishMarketUI
            >();

        GameObject marketPanel =
            CreateImage(
                "MarketPanel",
                controller.transform,
                Hex("#07111D", 0.98f)
            );

        Stretch(marketPanel);

        CanvasGroup panelGroup =
            marketPanel.AddComponent<
                CanvasGroup
            >();

        GameObject dim =
            CreateImage(
                "DimBackground",
                marketPanel.transform,
                new Color(
                    0f,
                    0f,
                    0f,
                    0.70f
                )
            );

        Stretch(dim);

        GameObject window =
            CreateImage(
                "Window",
                marketPanel.transform,
                Hex("#101C2A", 1f)
            );

        RectTransform windowRect =
            window.GetComponent<
                RectTransform
            >();

        windowRect.anchorMin =
            new Vector2(0.035f, 0.055f);

        windowRect.anchorMax =
            new Vector2(0.965f, 0.945f);

        windowRect.offsetMin =
            Vector2.zero;

        windowRect.offsetMax =
            Vector2.zero;

        Outline windowOutline =
            window.AddComponent<Outline>();

        windowOutline.effectColor =
            Hex("#315B87", 0.65f);

        windowOutline.effectDistance =
            new Vector2(2f, -2f);

        VerticalLayoutGroup windowLayout =
            window.AddComponent<
                VerticalLayoutGroup
            >();

        windowLayout.padding =
            new RectOffset(
                28,
                28,
                22,
                24
            );

        windowLayout.spacing = 16f;
        windowLayout.childControlWidth = true;
        windowLayout.childControlHeight = true;
        windowLayout.childForceExpandWidth = true;
        windowLayout.childForceExpandHeight = false;

        GameObject header =
            CreateRect(
                "Header",
                window.transform
            );

        LayoutElement headerElement =
            header.AddComponent<
                LayoutElement
            >();

        headerElement.preferredHeight = 82f;

        HorizontalLayoutGroup headerLayout =
            header.AddComponent<
                HorizontalLayoutGroup
            >();

        headerLayout.spacing = 18f;
        headerLayout.padding =
            new RectOffset(
                6,
                6,
                4,
                4
            );

        headerLayout.childAlignment =
            TextAnchor.MiddleLeft;

        headerLayout.childControlWidth = true;
        headerLayout.childControlHeight = true;
        headerLayout.childForceExpandWidth = false;
        headerLayout.childForceExpandHeight = true;

        TMP_Text title =
            CreateText(
                "Title",
                header.transform,
                "CHỢ THU MUA CÁ",
                36,
                FontStyles.Bold,
                TextAlignmentOptions.MidlineLeft,
                Color.white
            );

        LayoutElement titleElement =
            title.gameObject.AddComponent<
                LayoutElement
            >();

        titleElement.flexibleWidth = 1f;

        TMP_Text nextUpdate =
            CreateText(
                "NextUpdateText",
                header.transform,
                "GIÁ MỚI SAU 59:59",
                19,
                FontStyles.Bold,
                TextAlignmentOptions.Center,
                Hex("#AFC7DF", 1f)
            );

        LayoutElement updateElement =
            nextUpdate.gameObject
                .AddComponent<
                    LayoutElement
                >();

        updateElement.preferredWidth = 250f;

        GameObject moneyPanel =
            CreateImage(
                "MoneyPanel",
                header.transform,
                Hex("#0B1723", 1f)
            );

        LayoutElement moneyPanelElement =
            moneyPanel.AddComponent<
                LayoutElement
            >();

        moneyPanelElement.preferredWidth = 290f;
        moneyPanelElement.preferredHeight = 54f;

        TMP_Text money =
            CreateText(
                "MoneyText",
                moneyPanel.transform,
                "TIỀN MẶT  <color=#55F6A9>$0</color>",
                21,
                FontStyles.Bold,
                TextAlignmentOptions.Center,
                Color.white
            );

        Stretch(money.gameObject);

        Button closeButton =
            CreateButton(
                "CloseButton",
                header.transform,
                "×",
                Hex("#1C2A39", 1f),
                Hex("#D8E8F8", 1f)
            );

        LayoutElement closeElement =
            closeButton.gameObject
                .AddComponent<
                    LayoutElement
                >();

        closeElement.preferredWidth = 58f;
        closeElement.preferredHeight = 54f;

        GameObject divider =
            CreateImage(
                "Divider",
                window.transform,
                Hex("#3691DC", 0.8f)
            );

        LayoutElement dividerElement =
            divider.AddComponent<
                LayoutElement
            >();

        dividerElement.preferredHeight = 2f;

        GameObject scrollRoot =
            CreateImage(
                "FishScrollView",
                window.transform,
                Hex("#08121D", 0.68f)
            );

        LayoutElement scrollElement =
            scrollRoot.AddComponent<
                LayoutElement
            >();

        scrollElement.flexibleHeight = 1f;
        scrollElement.minHeight = 360f;

        ScrollRect scrollRect =
            scrollRoot.AddComponent<
                ScrollRect
            >();

        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType =
            ScrollRect.MovementType.Clamped;

        scrollRect.scrollSensitivity = 42f;

        GameObject viewport =
            CreateImage(
                "Viewport",
                scrollRoot.transform,
                new Color(
                    1f,
                    1f,
                    1f,
                    0.001f
                )
            );

        Stretch(viewport);

        viewport.AddComponent<
            RectMask2D
        >();

        GameObject content =
            CreateRect(
                "Content",
                viewport.transform
            );

        RectTransform contentRect =
            content.GetComponent<
                RectTransform
            >();

        contentRect.anchorMin =
            new Vector2(0f, 1f);

        contentRect.anchorMax =
            new Vector2(1f, 1f);

        contentRect.pivot =
            new Vector2(0.5f, 1f);

        contentRect.anchoredPosition =
            Vector2.zero;

        contentRect.sizeDelta =
            Vector2.zero;

        VerticalLayoutGroup contentLayout =
            content.AddComponent<
                VerticalLayoutGroup
            >();

        contentLayout.padding =
            new RectOffset(
                12,
                12,
                12,
                12
            );

        contentLayout.spacing = 12f;
        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = true;
        contentLayout.childForceExpandWidth = true;
        contentLayout.childForceExpandHeight = false;

        ContentSizeFitter contentFitter =
            content.AddComponent<
                ContentSizeFitter
            >();

        contentFitter.verticalFit =
            ContentSizeFitter.FitMode
                .PreferredSize;

        scrollRect.viewport =
            viewport.GetComponent<
                RectTransform
            >();

        scrollRect.content = contentRect;

        GameObject templates =
            CreateRect(
                "Templates",
                controller.transform
            );

        FishMarketRowUI rowTemplate =
            CreateRowTemplate(
                templates.transform
            );

        templates.SetActive(false);

        AssignMarketReferences(
            marketUI,
            marketPanel,
            panelGroup,
            content.transform,
            rowTemplate,
            money,
            nextUpdate,
            closeButton
        );

        AssignAllFishItems(marketUI);

        /*
         * Controller phải luôn Active.
         * Chỉ tắt MarketPanel để script vẫn nhận OpenShop().
         */
        controller.SetActive(true);
        marketPanel.SetActive(false);

        EnsureEventSystem();

        EditorUtility.SetDirty(marketUI);

        EditorSceneManager.MarkSceneDirty(
            controller.scene
        );

        Selection.activeGameObject =
            controller;

        EditorUtility.DisplayDialog(
            "Fish Market UI",
            "Đã xóa UI cũ và tạo lại giao diện chợ cá.\n\n" +
            "Canvas: FishMarketCanvas\n" +
            "Render Mode: Screen Space Overlay\n" +
            "Sorting Order: 2000\n\n" +
            "Bây giờ kéo FishMarketUIController vào Market UI của NPC.",
            "OK"
        );
    }

    [MenuItem(
        "Tools/Fishing/Open Fish Market Panel In Editor"
    )]
    public static void ShowPanelInEditor()
    {
        GameObject controller =
            GameObject.Find(ControllerName);

        if (controller == null)
        {
            EditorUtility.DisplayDialog(
                "Fish Market UI",
                "Không tìm thấy FishMarketUIController.",
                "OK"
            );

            return;
        }

        Transform panel =
            controller.transform.Find(
                "MarketPanel"
            );

        if (panel != null)
        {
            panel.gameObject.SetActive(true);
            Selection.activeGameObject =
                panel.gameObject;

            EditorSceneManager.MarkSceneDirty(
                panel.gameObject.scene
            );
        }
    }

    [MenuItem(
        "Tools/Fishing/Hide Fish Market Panel In Editor"
    )]
    public static void HidePanelInEditor()
    {
        GameObject controller =
            GameObject.Find(ControllerName);

        if (controller == null)
            return;

        Transform panel =
            controller.transform.Find(
                "MarketPanel"
            );

        if (panel != null)
        {
            panel.gameObject.SetActive(false);

            EditorSceneManager.MarkSceneDirty(
                panel.gameObject.scene
            );
        }
    }

    private static Canvas
        FindOrCreateDedicatedCanvas()
    {
        GameObject canvasObject =
            GameObject.Find(CanvasName);

        if (canvasObject == null)
        {
            canvasObject =
                new GameObject(
                    CanvasName,
                    typeof(RectTransform),
                    typeof(Canvas),
                    typeof(CanvasScaler),
                    typeof(GraphicRaycaster)
                );

            Undo.RegisterCreatedObjectUndo(
                canvasObject,
                "Create Fish Market Canvas"
            );
        }

        Canvas canvas =
            canvasObject.GetComponent<
                Canvas
            >();

        if (canvas == null)
        {
            canvas =
                canvasObject.AddComponent<
                    Canvas
                >();
        }

        canvas.renderMode =
            RenderMode.ScreenSpaceOverlay;

        canvas.sortingOrder = 2000;
        canvas.pixelPerfect = false;

        CanvasScaler scaler =
            canvasObject.GetComponent<
                CanvasScaler
            >();

        if (scaler == null)
        {
            scaler =
                canvasObject.AddComponent<
                    CanvasScaler
                >();
        }

        scaler.uiScaleMode =
            CanvasScaler.ScaleMode
                .ScaleWithScreenSize;

        scaler.referenceResolution =
            new Vector2(1920f, 1080f);

        scaler.screenMatchMode =
            CanvasScaler.ScreenMatchMode
                .MatchWidthOrHeight;

        scaler.matchWidthOrHeight = 0.5f;

        if (canvasObject.GetComponent<
                GraphicRaycaster>() == null)
        {
            canvasObject.AddComponent<
                GraphicRaycaster
            >();
        }

        canvasObject.SetActive(true);

        return canvas;
    }

    private static void DeleteOldControllers()
    {
        FishMarketUI[] markets =
            Resources.FindObjectsOfTypeAll<
                FishMarketUI
            >();

        foreach (FishMarketUI market in markets)
        {
            if (market == null)
                continue;

            if (!market.gameObject
                    .scene.IsValid())
            {
                continue;
            }

            Undo.DestroyObjectImmediate(
                market.gameObject
            );
        }

        GameObject named =
            GameObject.Find(ControllerName);

        if (named != null)
        {
            Undo.DestroyObjectImmediate(
                named
            );
        }
    }

    private static FishMarketRowUI
        CreateRowTemplate(
            Transform parent)
    {
        GameObject row =
            CreateImage(
                "FishMarketRowTemplate",
                parent,
                Hex("#101B28", 1f)
            );

        Outline outline =
            row.AddComponent<Outline>();

        outline.effectColor =
            Hex("#213C58", 0.75f);

        outline.effectDistance =
            new Vector2(1f, -1f);

        LayoutElement rowElement =
            row.AddComponent<
                LayoutElement
            >();

        rowElement.preferredHeight = 124f;

        HorizontalLayoutGroup rowLayout =
            row.AddComponent<
                HorizontalLayoutGroup
            >();

        rowLayout.padding =
            new RectOffset(
                16,
                16,
                12,
                12
            );

        rowLayout.spacing = 14f;
        rowLayout.childAlignment =
            TextAnchor.MiddleLeft;

        rowLayout.childControlWidth = true;
        rowLayout.childControlHeight = true;
        rowLayout.childForceExpandWidth = false;
        rowLayout.childForceExpandHeight = true;

        TMP_Text[] historyPrices =
            new TMP_Text[3];

        TMP_Text[] historyTimes =
            new TMP_Text[3];

        for (int i = 0; i < 3; i++)
        {
            GameObject historyPanel =
                CreateImage(
                    "History_" + i,
                    row.transform,
                    Hex("#0A121C", 1f)
                );

            LayoutElement historyElement =
                historyPanel.AddComponent<
                    LayoutElement
                >();

            historyElement.preferredWidth = 145f;

            VerticalLayoutGroup historyLayout =
                historyPanel.AddComponent<
                    VerticalLayoutGroup
                >();

            historyLayout.padding =
                new RectOffset(
                    8,
                    8,
                    10,
                    8
                );

            historyLayout.spacing = 4f;
            historyLayout.childAlignment =
                TextAnchor.MiddleCenter;

            historyPrices[i] =
                CreateText(
                    "Price",
                    historyPanel.transform,
                    "$0.00",
                    23,
                    FontStyles.Bold,
                    TextAlignmentOptions.Center,
                    Color.white
                );

            historyTimes[i] =
                CreateText(
                    "Time",
                    historyPanel.transform,
                    (3 - i) +
                    " GIỜ TRƯỚC",
                    13,
                    FontStyles.Bold,
                    TextAlignmentOptions.Center,
                    Hex("#8CA5BC", 1f)
                );
        }

        GameObject currentPanel =
            CreateImage(
                "CurrentPrice",
                row.transform,
                Hex("#16304A", 1f)
            );

        LayoutElement currentElement =
            currentPanel.AddComponent<
                LayoutElement
            >();

        currentElement.preferredWidth = 170f;

        VerticalLayoutGroup currentLayout =
            currentPanel.AddComponent<
                VerticalLayoutGroup
            >();

        currentLayout.padding =
            new RectOffset(
                10,
                10,
                10,
                8
            );

        currentLayout.spacing = 3f;
        currentLayout.childAlignment =
            TextAnchor.MiddleCenter;

        TMP_Text currentPrice =
            CreateText(
                "CurrentPriceText",
                currentPanel.transform,
                "$0.00",
                31,
                FontStyles.Bold,
                TextAlignmentOptions.Center,
                Color.white
            );

        TMP_Text priceUnit =
            CreateText(
                "PriceUnitText",
                currentPanel.transform,
                "MỖI CON",
                13,
                FontStyles.Bold,
                TextAlignmentOptions.Center,
                Hex("#8EB8DD", 1f)
            );

        GameObject fishInfo =
            CreateRect(
                "FishInfo",
                row.transform
            );

        LayoutElement fishInfoElement =
            fishInfo.AddComponent<
                LayoutElement
            >();

        fishInfoElement.flexibleWidth = 1f;
        fishInfoElement.minWidth = 275f;

        HorizontalLayoutGroup fishInfoLayout =
            fishInfo.AddComponent<
                HorizontalLayoutGroup
            >();

        fishInfoLayout.spacing = 14f;
        fishInfoLayout.childAlignment =
            TextAnchor.MiddleLeft;

        Image fishIcon =
            CreateImage(
                "FishIcon",
                fishInfo.transform,
                new Color(
                    1f,
                    1f,
                    1f,
                    0.08f
                )
            ).GetComponent<Image>();

        LayoutElement iconElement =
            fishIcon.gameObject
                .AddComponent<
                    LayoutElement
                >();

        iconElement.preferredWidth = 78f;
        iconElement.preferredHeight = 78f;

        fishIcon.preserveAspect = true;
        fishIcon.raycastTarget = false;

        GameObject infoTextRoot =
            CreateRect(
                "InfoTexts",
                fishInfo.transform
            );

        LayoutElement infoTextElement =
            infoTextRoot.AddComponent<
                LayoutElement
            >();

        infoTextElement.flexibleWidth = 1f;

        VerticalLayoutGroup infoTextLayout =
            infoTextRoot.AddComponent<
                VerticalLayoutGroup
            >();

        infoTextLayout.spacing = 4f;
        infoTextLayout.childAlignment =
            TextAnchor.MiddleLeft;

        TMP_Text fishName =
            CreateText(
                "FishNameText",
                infoTextRoot.transform,
                "CÁ",
                29,
                FontStyles.Bold,
                TextAlignmentOptions.MidlineLeft,
                Color.white
            );

        TMP_Text ownedAmount =
            CreateText(
                "OwnedAmountText",
                infoTextRoot.transform,
                "ĐANG CÓ 0 CON",
                15,
                FontStyles.Bold,
                TextAlignmentOptions.MidlineLeft,
                Hex("#95AEC4", 1f)
            );

        Button sellButton =
            CreateButton(
                "SellButton",
                row.transform,
                string.Empty,
                Hex("#16304A", 1f),
                Color.white
            );

        LayoutElement sellElement =
            sellButton.gameObject
                .AddComponent<
                    LayoutElement
                >();

        sellElement.preferredWidth = 315f;

        HorizontalLayoutGroup sellLayout =
            sellButton.gameObject
                .AddComponent<
                    HorizontalLayoutGroup
                >();

        sellLayout.padding =
            new RectOffset(
                14,
                14,
                10,
                10
            );

        sellLayout.spacing = 12f;
        sellLayout.childAlignment =
            TextAnchor.MiddleCenter;

        TMP_Text sellText =
            CreateText(
                "SellButtonText",
                sellButton.transform,
                "BÁN 0 CON",
                19,
                FontStyles.Bold,
                TextAlignmentOptions.Center,
                Color.white
            );

        LayoutElement sellTextElement =
            sellText.gameObject
                .AddComponent<
                    LayoutElement
                >();

        sellTextElement.flexibleWidth = 1f;

        GameObject payoutPanel =
            CreateImage(
                "PayoutPanel",
                sellButton.transform,
                Hex("#080D13", 1f)
            );

        LayoutElement payoutElement =
            payoutPanel.AddComponent<
                LayoutElement
            >();

        payoutElement.preferredWidth = 132f;

        TMP_Text payout =
            CreateText(
                "PayoutText",
                payoutPanel.transform,
                "+ $0",
                21,
                FontStyles.Bold,
                TextAlignmentOptions.Center,
                Hex("#55F6A9", 1f)
            );

        Stretch(payout.gameObject);

        FishMarketRowUI rowUI =
            row.AddComponent<
                FishMarketRowUI
            >();

        SerializedObject serialized =
            new SerializedObject(rowUI);

        SerializedProperty prices =
            serialized.FindProperty(
                "historyPriceTexts"
            );

        prices.arraySize = 3;

        SerializedProperty times =
            serialized.FindProperty(
                "historyTimeTexts"
            );

        times.arraySize = 3;

        for (int i = 0; i < 3; i++)
        {
            prices
                .GetArrayElementAtIndex(i)
                .objectReferenceValue =
                    historyPrices[i];

            times
                .GetArrayElementAtIndex(i)
                .objectReferenceValue =
                    historyTimes[i];
        }

        serialized
            .FindProperty("currentPriceText")
            .objectReferenceValue =
                currentPrice;

        serialized
            .FindProperty("priceUnitText")
            .objectReferenceValue =
                priceUnit;

        serialized
            .FindProperty("fishIcon")
            .objectReferenceValue =
                fishIcon;

        serialized
            .FindProperty("fishNameText")
            .objectReferenceValue =
                fishName;

        serialized
            .FindProperty("ownedAmountText")
            .objectReferenceValue =
                ownedAmount;

        serialized
            .FindProperty("sellButton")
            .objectReferenceValue =
                sellButton;

        serialized
            .FindProperty("sellButtonText")
            .objectReferenceValue =
                sellText;

        serialized
            .FindProperty("payoutText")
            .objectReferenceValue =
                payout;

        serialized
            .ApplyModifiedPropertiesWithoutUndo();

        row.SetActive(true);

        return rowUI;
    }

    private static void AssignMarketReferences(
        FishMarketUI marketUI,
        GameObject root,
        CanvasGroup rootCanvasGroup,
        Transform content,
        FishMarketRowUI rowPrefab,
        TMP_Text moneyText,
        TMP_Text nextUpdateText,
        Button closeButton)
    {
        SerializedObject serialized =
            new SerializedObject(marketUI);

        serialized
            .FindProperty("root")
            .objectReferenceValue = root;

        serialized
            .FindProperty("rootCanvasGroup")
            .objectReferenceValue =
                rootCanvasGroup;

        serialized
            .FindProperty("content")
            .objectReferenceValue =
                content;

        serialized
            .FindProperty("rowPrefab")
            .objectReferenceValue =
                rowPrefab;

        serialized
            .FindProperty("moneyText")
            .objectReferenceValue =
                moneyText;

        serialized
            .FindProperty("nextUpdateText")
            .objectReferenceValue =
                nextUpdateText;

        serialized
            .FindProperty("closeButton")
            .objectReferenceValue =
                closeButton;

        serialized
            .ApplyModifiedPropertiesWithoutUndo();
    }

    private static void AssignAllFishItems(
        FishMarketUI marketUI)
    {
        string[] guids =
            AssetDatabase.FindAssets(
                "t:InventoryItemData"
            );

        List<InventoryItemData> fishItems =
            new List<InventoryItemData>();

        foreach (string guid in guids)
        {
            string path =
                AssetDatabase.GUIDToAssetPath(
                    guid
                );

            InventoryItemData item =
                AssetDatabase.LoadAssetAtPath<
                    InventoryItemData
                >(path);

            if (item == null ||
                item.ItemType !=
                    InventoryItemType.Fish)
            {
                continue;
            }

            if (!fishItems.Contains(item))
                fishItems.Add(item);
        }

        fishItems.Sort(
            (left, right) =>
                string.Compare(
                    left.DisplayName,
                    right.DisplayName,
                    System.StringComparison
                        .CurrentCulture
                )
        );

        SerializedObject serialized =
            new SerializedObject(marketUI);

        SerializedProperty property =
            serialized.FindProperty(
                "fishItems"
            );

        property.arraySize =
            fishItems.Count;

        for (int i = 0;
             i < fishItems.Count;
             i++)
        {
            property
                .GetArrayElementAtIndex(i)
                .objectReferenceValue =
                    fishItems[i];
        }

        serialized
            .ApplyModifiedPropertiesWithoutUndo();

        if (fishItems.Count == 0)
        {
            Debug.LogWarning(
                "[Fish Market UI] Không tìm thấy " +
                "InventoryItemData có ItemType = Fish.",
                marketUI
            );
        }
    }

    private static GameObject CreateRect(
        string name,
        Transform parent)
    {
        GameObject gameObject =
            new GameObject(
                name,
                typeof(RectTransform)
            );

        gameObject.transform.SetParent(
            parent,
            false
        );

        return gameObject;
    }

    private static GameObject CreateImage(
        string name,
        Transform parent,
        Color color)
    {
        GameObject gameObject =
            CreateRect(name, parent);

        Image image =
            gameObject.AddComponent<Image>();

        image.sprite = null;
        image.type = Image.Type.Simple;
        image.color = color;

        return gameObject;
    }

    private static TMP_Text CreateText(
        string name,
        Transform parent,
        string value,
        float fontSize,
        FontStyles fontStyle,
        TextAlignmentOptions alignment,
        Color color)
    {
        GameObject gameObject =
            CreateRect(name, parent);

        TextMeshProUGUI text =
            gameObject.AddComponent<
                TextMeshProUGUI
            >();

        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.alignment = alignment;
        text.color = color;
        text.enableWordWrapping = false;
        text.raycastTarget = false;
        text.richText = true;

        if (TMP_Settings.defaultFontAsset !=
            null)
        {
            text.font =
                TMP_Settings
                    .defaultFontAsset;
        }

        return text;
    }

    private static Button CreateButton(
        string name,
        Transform parent,
        string label,
        Color background,
        Color labelColor)
    {
        GameObject gameObject =
            CreateImage(
                name,
                parent,
                background
            );

        Button button =
            gameObject.AddComponent<Button>();

        ColorBlock colors =
            button.colors;

        colors.normalColor = Color.white;
        colors.highlightedColor =
            new Color(
                1.08f,
                1.08f,
                1.08f,
                1f
            );

        colors.pressedColor =
            new Color(
                0.82f,
                0.82f,
                0.82f,
                1f
            );

        colors.disabledColor =
            new Color(
                0.45f,
                0.45f,
                0.45f,
                0.55f
            );

        button.colors = colors;

        if (!string.IsNullOrWhiteSpace(
                label))
        {
            TMP_Text text =
                CreateText(
                    "Text",
                    gameObject.transform,
                    label,
                    28,
                    FontStyles.Bold,
                    TextAlignmentOptions.Center,
                    labelColor
                );

            Stretch(text.gameObject);
        }

        return button;
    }

    private static void Stretch(
        GameObject gameObject)
    {
        RectTransform rect =
            gameObject.GetComponent<
                RectTransform
            >();

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static Color Hex(
        string html,
        float alpha)
    {
        Color color = Color.white;

        ColorUtility.TryParseHtmlString(
            html,
            out color
        );

        color.a = alpha;
        return color;
    }

    private static void EnsureTextMeshProReady()
    {
        if (TMP_Settings.defaultFontAsset !=
            null)
        {
            return;
        }

        Debug.LogWarning(
            "[Fish Market UI] TextMeshPro chưa có " +
            "Default Font Asset. Nếu chữ không hiện, mở " +
            "Window > TextMeshPro > Import TMP Essential Resources."
        );
    }

    private static void EnsureEventSystem()
    {
        EventSystem eventSystem =
            Object.FindObjectOfType<
                EventSystem
            >();

        if (eventSystem != null)
            return;

        GameObject objectEventSystem =
            new GameObject(
                "EventSystem",
                typeof(EventSystem),
                typeof(StandaloneInputModule)
            );

        Undo.RegisterCreatedObjectUndo(
            objectEventSystem,
            "Create EventSystem"
        );
    }
}
#endif