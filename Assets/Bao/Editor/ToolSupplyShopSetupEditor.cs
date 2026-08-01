#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class ToolSupplyShopSetupEditor
{
    private const string RootFolder =
        "Assets/Bao/ToolShop";

    private const string ItemFolder =
        RootFolder + "/Items";

    private const string ProductFolder =
        RootFolder + "/Products";

    private const string PrefabFolder =
        RootFolder + "/Prefabs";

    private const string CanvasName =
        "ToolSupplyShopCanvas";

    private static readonly Color Background =
        new Color(0.055f, 0.055f, 0.055f, 0.985f);

    private static readonly Color CardColor =
        new Color(0.15f, 0.15f, 0.15f, 1f);

    private static readonly Color MutedText =
        new Color(0.52f, 0.52f, 0.52f, 1f);

    private static readonly Color White =
        new Color(0.96f, 0.96f, 0.96f, 1f);

    [MenuItem(
        "Tools/Tool Shop/Create Full Tool Shop"
    )]
    public static void CreateFullToolShop()
    {
        EnsureFolders();

        InventoryItemData wateringCan =
            CreateInventoryItem(
                "watering_can",
                "Bình tưới nước",
                "Dụng cụ dùng để tưới nước cho cây trồng.",
                150
            );

        InventoryItemData hoe =
            CreateInventoryItem(
                "hoe",
                "Cuốc đất",
                "Dụng cụ dùng để cuốc và chuẩn bị đất trồng.",
                120
            );

        InventoryItemData axe =
            CreateInventoryItem(
                "axe",
                "Rìu",
                "Dụng cụ dùng để chặt cây và lấy gỗ.",
                180
            );

        InventoryItemData pickaxe =
            CreateInventoryItem(
                "pickaxe",
                "Cuốc chim",
                "Dụng cụ dùng để phá đá và khai thác khoáng sản.",
                200
            );

        InventoryItemData sickle =
            CreateInventoryItem(
                "sickle",
                "Liềm",
                "Dụng cụ dùng để thu hoạch cây trồng.",
                140
            );

        ToolShopItemData[] products =
        {
            CreateProduct(
                "Product_WateringCan",
                wateringCan
            ),
            CreateProduct(
                "Product_Hoe",
                hoe
            ),
            CreateProduct(
                "Product_Axe",
                axe
            ),
            CreateProduct(
                "Product_Pickaxe",
                pickaxe
            ),
            CreateProduct(
                "Product_Sickle",
                sickle
            )
        };

        ToolShopItemCardUI cardPrefab =
            CreateOrReplaceCardPrefab();

        ToolSupplyShopUI shop =
            CreateOrReplaceShopCanvas(
                cardPrefab,
                products
            );

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorSceneManager.MarkAllScenesDirty();

        Selection.activeGameObject =
            shop.gameObject;

        EditorUtility.DisplayDialog(
            "Tool Shop",
            "Đã tạo cửa hàng dụng cụ hoàn chỉnh.\n\n" +
            "Nhấn F8 khi Play để mở thử shop.\n" +
            "Gắn icon vào 5 InventoryItemData trong:\n" +
            ItemFolder,
            "OK"
        );
    }

    [MenuItem(
        "Tools/Tool Shop/Setup Selected NPC"
    )]
    public static void SetupSelectedNPC()
    {
        GameObject selected =
            Selection.activeGameObject;

        if (selected == null)
        {
            EditorUtility.DisplayDialog(
                "Tool Shop",
                "Hãy chọn NPC trong Hierarchy.",
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
                "Tool Shop",
                "Chưa có ToolSupplyShopCanvas.\n" +
                "Hãy chạy Create Full Tool Shop trước.",
                "OK"
            );

            return;
        }

        ToolSupplyShopNPCInteraction interaction =
            selected.GetComponent<
                ToolSupplyShopNPCInteraction
            >();

        if (interaction == null)
        {
            interaction =
                Undo.AddComponent<
                    ToolSupplyShopNPCInteraction
                >(selected);
        }

        CircleCollider2D trigger =
            selected.GetComponents<
                CircleCollider2D
            >()
            .FirstOrDefault(
                collider =>
                    collider != null &&
                    collider.isTrigger
            );

        if (trigger == null)
        {
            trigger =
                Undo.AddComponent<
                    CircleCollider2D
                >(selected);

            trigger.isTrigger = true;
            trigger.radius = 1.5f;
            trigger.offset =
                new Vector2(0f, 0.3f);
        }

        GameObject prompt =
            FindDirectChild(
                selected.transform,
                "ToolShopPrompt"
            );

        TMP_Text promptText;

        if (prompt == null)
        {
            prompt =
                new GameObject(
                    "ToolShopPrompt"
                );

            Undo.RegisterCreatedObjectUndo(
                prompt,
                "Create Tool Shop Prompt"
            );

            prompt.transform.SetParent(
                selected.transform,
                false
            );

            prompt.transform.localPosition =
                new Vector3(0f, 1.4f, 0f);

            TextMeshPro text =
                prompt.AddComponent<
                    TextMeshPro
                >();

            text.text = "[E] MUA DỤNG CỤ";
            text.fontSize = 3f;
            text.alignment =
                TextAlignmentOptions.Center;
            text.color = Color.white;
            text.sortingOrder = 1000;

            promptText = text;
        }
        else
        {
            promptText =
                prompt.GetComponent<TMP_Text>();
        }

        SerializedObject serialized =
            new SerializedObject(interaction);

        serialized
            .FindProperty("shopUI")
            .objectReferenceValue = shop;

        serialized
            .FindProperty("promptRoot")
            .objectReferenceValue = prompt;

        serialized
            .FindProperty("promptText")
            .objectReferenceValue = promptText;

        serialized.ApplyModifiedProperties();

        prompt.SetActive(false);

        EditorUtility.SetDirty(selected);
        EditorSceneManager.MarkAllScenesDirty();

        EditorUtility.DisplayDialog(
            "Tool Shop",
            "Đã setup NPC.\n" +
            "Player đi vào trigger và nhấn E để mở shop.",
            "OK"
        );
    }

    private static void EnsureFolders()
    {
        EnsureFolder("Assets/Bao");
        EnsureFolder(RootFolder);
        EnsureFolder(ItemFolder);
        EnsureFolder(ProductFolder);
        EnsureFolder(PrefabFolder);
    }

    private static void EnsureFolder(
        string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        string[] parts =
            path.Split('/');

        string current =
            parts[0];

        for (int index = 1;
             index < parts.Length;
             index++)
        {
            string next =
                current + "/" + parts[index];

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

    private static InventoryItemData
        CreateInventoryItem(
            string itemId,
            string displayName,
            string description,
            int buyPrice)
    {
        string path =
            ItemFolder +
            "/" +
            itemId +
            ".asset";

        InventoryItemData item =
            AssetDatabase
                .LoadAssetAtPath<
                    InventoryItemData
                >(path);

        if (item == null)
        {
            item =
                ScriptableObject
                    .CreateInstance<
                        InventoryItemData
                    >();

            AssetDatabase.CreateAsset(
                item,
                path
            );
        }

        SerializedObject serialized =
            new SerializedObject(item);

        SetString(
            serialized,
            itemId,
            "itemId",
            "id"
        );

        SetString(
            serialized,
            displayName,
            "displayName",
            "itemName"
        );

        SetString(
            serialized,
            description,
            "description"
        );

        SetInteger(
            serialized,
            1,
            "maxStack"
        );

        SetBoolean(
            serialized,
            true,
            "uniqueOwnership",
            "isUnique"
        );

        SetInteger(
            serialized,
            buyPrice,
            "buyPrice"
        );

        SetInteger(
            serialized,
            Mathf.Max(1, buyPrice / 2),
            "sellPrice"
        );

        SetEnumByName(
            serialized,
            new[]
            {
                "Tool",
                "Other"
            },
            "itemType"
        );

        SetEnumByName(
            serialized,
            new[]
            {
                "Stackable",
                "Unique"
            },
            "stackMode"
        );

        serialized.ApplyModifiedProperties();
        EditorUtility.SetDirty(item);

        return item;
    }

    private static ToolShopItemData
        CreateProduct(
            string fileName,
            InventoryItemData item)
    {
        string path =
            ProductFolder +
            "/" +
            fileName +
            ".asset";

        ToolShopItemData product =
            AssetDatabase
                .LoadAssetAtPath<
                    ToolShopItemData
                >(path);

        if (product == null)
        {
            product =
                ScriptableObject
                    .CreateInstance<
                        ToolShopItemData
                    >();

            AssetDatabase.CreateAsset(
                product,
                path
            );
        }

        product.inventoryItem = item;
        product.category = "Dụng cụ";
        product.priceOverride = -1;
        product.maxPurchaseQuantity = 1;

        EditorUtility.SetDirty(product);

        return product;
    }

    private static ToolShopItemCardUI
        CreateOrReplaceCardPrefab()
    {
        string prefabPath =
            PrefabFolder +
            "/ToolShopItemCard.prefab";

        GameObject root =
            new GameObject(
                "ToolShopItemCard",
                typeof(RectTransform),
                typeof(Image),
                typeof(ToolShopItemCardUI)
            );

        RectTransform rect =
            root.GetComponent<
                RectTransform
            >();

        rect.sizeDelta =
            new Vector2(
                245f,
                325f
            );

        Image background =
            root.GetComponent<Image>();

        background.color =
            CardColor;

        background.sprite =
            GetUISprite();

        background.type =
            Image.Type.Sliced;

        TMP_Text nameText =
            CreateText(
                root.transform,
                "ItemNameText",
                "Tên dụng cụ",
                20f,
                FontStyles.Bold,
                TextAlignmentOptions.TopLeft,
                White
            );

        SetRect(
            nameText.rectTransform,
            new Vector2(18f, -18f),
            new Vector2(-70f, 30f),
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(0f, 1f)
        );

        TMP_Text categoryText =
            CreateText(
                root.transform,
                "CategoryText",
                "Dụng cụ",
                14f,
                FontStyles.Normal,
                TextAlignmentOptions.TopLeft,
                MutedText
            );

        SetRect(
            categoryText.rectTransform,
            new Vector2(18f, -48f),
            new Vector2(-36f, 24f),
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(0f, 1f)
        );

        TMP_Text priceText =
            CreateText(
                root.transform,
                "PriceText",
                "$100",
                18f,
                FontStyles.Bold,
                TextAlignmentOptions.TopRight,
                White
            );

        SetRect(
            priceText.rectTransform,
            new Vector2(-18f, -18f),
            new Vector2(70f, 30f),
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(1f, 1f)
        );

        Image icon =
            CreateImage(
                root.transform,
                "ItemIcon",
                Color.white
            );

        SetRect(
            icon.rectTransform,
            new Vector2(0f, -78f),
            new Vector2(-32f, 200f),
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(0.5f, 1f)
        );

        icon.preserveAspect = true;

        Button buyButton =
            CreateButton(
                root.transform,
                "BuyButton",
                "MUA",
                new Color(0.96f, 0.96f, 0.96f, 1f),
                new Color(0.08f, 0.08f, 0.08f, 1f)
            );

        SetRect(
            buyButton.GetComponent<
                RectTransform
            >(),
            new Vector2(18f, 16f),
            new Vector2(-36f, 46f),
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(0.5f, 0f)
        );

        buyButton.gameObject
            .SetActive(false);

        ToolShopItemCardUI card =
            root.GetComponent<
                ToolShopItemCardUI
            >();

        SerializedObject serialized =
            new SerializedObject(card);

        serialized
            .FindProperty("itemIcon")
            .objectReferenceValue = icon;

        serialized
            .FindProperty("itemNameText")
            .objectReferenceValue =
                nameText;

        serialized
            .FindProperty("categoryText")
            .objectReferenceValue =
                categoryText;

        serialized
            .FindProperty("priceText")
            .objectReferenceValue =
                priceText;

        serialized
            .FindProperty("buyButtonObject")
            .objectReferenceValue =
                buyButton.gameObject;

        serialized
            .FindProperty("buyButton")
            .objectReferenceValue =
                buyButton;

        serialized.ApplyModifiedProperties();

        GameObject prefab =
            PrefabUtility
                .SaveAsPrefabAsset(
                    root,
                    prefabPath
                );

        UnityEngine.Object
            .DestroyImmediate(root);

        return prefab.GetComponent<
            ToolShopItemCardUI
        >();
    }

    private static ToolSupplyShopUI
        CreateOrReplaceShopCanvas(
            ToolShopItemCardUI cardPrefab,
            ToolShopItemData[] products)
    {
        GameObject existing =
            GameObject.Find(CanvasName);

        if (existing != null)
        {
            Undo.DestroyObjectImmediate(
                existing
            );
        }

        EnsureEventSystem();

        GameObject canvasObject =
            new GameObject(
                CanvasName,
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster),
                typeof(ToolSupplyShopUI),
                typeof(ToolShopPurchasePopupUI)
            );

        Undo.RegisterCreatedObjectUndo(
            canvasObject,
            "Create Tool Supply Shop"
        );

        Canvas canvas =
            canvasObject.GetComponent<
                Canvas
            >();

        canvas.renderMode =
            RenderMode.ScreenSpaceOverlay;

        canvas.sortingOrder = 5000;

        CanvasScaler scaler =
            canvasObject.GetComponent<
                CanvasScaler
            >();

        scaler.uiScaleMode =
            CanvasScaler.ScaleMode
                .ScaleWithScreenSize;

        scaler.referenceResolution =
            new Vector2(
                1920f,
                1080f
            );

        scaler.matchWidthOrHeight =
            0.5f;

        RectTransform shopRoot =
            CreateRect(
                canvasObject.transform,
                "ShopRoot"
            );

        Stretch(shopRoot);

        Image shopBackground =
            shopRoot.gameObject
                .AddComponent<Image>();

        shopBackground.color =
            Background;

        TMP_Text title =
            CreateText(
                shopRoot,
                "TitleText",
                "CỬA HÀNG DỤNG CỤ",
                31f,
                FontStyles.Bold,
                TextAlignmentOptions.Left,
                White
            );

        SetRect(
            title.rectTransform,
            new Vector2(54f, -34f),
            new Vector2(480f, 54f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f)
        );

        TMP_InputField search =
            CreateInputField(
                shopRoot,
                "SearchInput",
                "Tìm kiếm dụng cụ..."
            );

        SetRect(
            search.GetComponent<
                RectTransform
            >(),
            new Vector2(54f, -104f),
            new Vector2(420f, 54f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f)
        );

        TMP_Text moneyText =
            CreateTextBox(
                shopRoot,
                "MoneyText",
                "TIỀN MẶT\n<b>$0</b>",
                new Vector2(-220f, -34f),
                new Vector2(170f, 64f)
            );

        Button closeButton =
            CreateButton(
                shopRoot,
                "CloseButton",
                "ESC  ĐÓNG",
                new Color(0.82f, 0.82f, 0.82f, 1f),
                new Color(0.08f, 0.08f, 0.08f, 1f)
            );

        SetRect(
            closeButton.GetComponent<
                RectTransform
            >(),
            new Vector2(-46f, -38f),
            new Vector2(132f, 46f),
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(1f, 1f)
        );

        ScrollRect scrollRect =
            CreateScrollView(
                shopRoot,
                out RectTransform content
            );

        SetRect(
            scrollRect.GetComponent<
                RectTransform
            >(),
            new Vector2(42f, 186f),
            new Vector2(-84f, -224f),
            new Vector2(0f, 0f),
            new Vector2(1f, 1f),
            new Vector2(0.5f, 0.5f)
        );

        TMP_Text messageText =
            CreateText(
                shopRoot,
                "MessageText",
                string.Empty,
                21f,
                FontStyles.Bold,
                TextAlignmentOptions.Center,
                Color.white
            );

        SetRect(
            messageText.rectTransform,
            new Vector2(0f, 28f),
            new Vector2(700f, 42f),
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f)
        );

        PopupReferences popup =
            CreatePurchasePopup(
                shopRoot
            );

        ToolShopPurchasePopupUI popupComponent =
            canvasObject.GetComponent<
                ToolShopPurchasePopupUI
            >();

        WirePopup(
            popupComponent,
            popup
        );

        ToolSupplyShopUI shop =
            canvasObject.GetComponent<
                ToolSupplyShopUI
            >();

        SerializedObject serialized =
            new SerializedObject(shop);

        serialized
            .FindProperty("shopRoot")
            .objectReferenceValue =
                shopRoot.gameObject;

        serialized
            .FindProperty("inventoryManager")
            .objectReferenceValue =
                UnityEngine.Object
                    .FindFirstObjectByType<
                        InventoryManager
                    >(
                        FindObjectsInactive.Include
                    );

        serialized
            .FindProperty("walletSource")
            .objectReferenceValue =
                FindPlayerStatsComponent();

        serialized
            .FindProperty("content")
            .objectReferenceValue =
                content;

        serialized
            .FindProperty("itemCardPrefab")
            .objectReferenceValue =
                cardPrefab;

        SerializedProperty items =
            serialized.FindProperty(
                "availableItems"
            );

        items.arraySize =
            products.Length;

        for (int index = 0;
             index < products.Length;
             index++)
        {
            items.GetArrayElementAtIndex(
                index
            ).objectReferenceValue =
                products[index];
        }

        serialized
            .FindProperty("purchasePopup")
            .objectReferenceValue =
                popupComponent;

        serialized
            .FindProperty("moneyText")
            .objectReferenceValue =
                moneyText;

        serialized
            .FindProperty("messageText")
            .objectReferenceValue =
                messageText;

        serialized
            .FindProperty("searchInput")
            .objectReferenceValue =
                search;

        serialized
            .FindProperty("closeButton")
            .objectReferenceValue =
                closeButton;

        serialized.ApplyModifiedProperties();

        popup.root.gameObject.SetActive(false);
        messageText.gameObject
            .SetActive(false);

        shopRoot.gameObject
            .SetActive(false);

        return shop;
    }

    private static PopupReferences
        CreatePurchasePopup(
            Transform parent)
    {
        PopupReferences refs =
            new PopupReferences();

        refs.root =
            CreateRect(
                parent,
                "PurchasePopup"
            );

        Stretch(refs.root);

        Image overlay =
            refs.root.gameObject
                .AddComponent<Image>();

        overlay.color =
            new Color(
                0f,
                0f,
                0f,
                0.72f
            );

        RectTransform panel =
            CreateRect(
                refs.root,
                "Panel"
            );

        panel.anchorMin =
            new Vector2(0.5f, 0.5f);

        panel.anchorMax =
            new Vector2(0.5f, 0.5f);

        panel.pivot =
            new Vector2(0.5f, 0.5f);

        panel.anchoredPosition =
            Vector2.zero;

        panel.sizeDelta =
            new Vector2(620f, 470f);

        Image panelImage =
            panel.gameObject
                .AddComponent<Image>();

        panelImage.color =
            new Color(
                0.11f,
                0.11f,
                0.11f,
                1f
            );

        panelImage.sprite =
            GetUISprite();

        panelImage.type =
            Image.Type.Sliced;

        TMP_Text heading =
            CreateText(
                panel,
                "Heading",
                "XÁC NHẬN MUA",
                27f,
                FontStyles.Bold,
                TextAlignmentOptions.Center,
                White
            );

        SetRect(
            heading.rectTransform,
            new Vector2(0f, -26f),
            new Vector2(-50f, 42f),
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(0.5f, 1f)
        );

        refs.itemIcon =
            CreateImage(
                panel,
                "ItemIcon",
                Color.white
            );

        SetRect(
            refs.itemIcon.rectTransform,
            new Vector2(48f, -92f),
            new Vector2(180f, 180f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f)
        );

        refs.itemIcon.preserveAspect =
            true;

        refs.itemNameText =
            CreateText(
                panel,
                "ItemNameText",
                "Tên dụng cụ",
                24f,
                FontStyles.Bold,
                TextAlignmentOptions.TopLeft,
                White
            );

        SetRect(
            refs.itemNameText.rectTransform,
            new Vector2(260f, -96f),
            new Vector2(310f, 42f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f)
        );

        refs.descriptionText =
            CreateText(
                panel,
                "DescriptionText",
                "Mô tả dụng cụ",
                16f,
                FontStyles.Normal,
                TextAlignmentOptions.TopLeft,
                MutedText
            );

        SetRect(
            refs.descriptionText.rectTransform,
            new Vector2(260f, -142f),
            new Vector2(310f, 90f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f)
        );

        refs.unitPriceText =
            CreateText(
                panel,
                "UnitPriceText",
                "Đơn giá: $0",
                18f,
                FontStyles.Bold,
                TextAlignmentOptions.Left,
                White
            );

        SetRect(
            refs.unitPriceText.rectTransform,
            new Vector2(260f, -235f),
            new Vector2(250f, 32f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f)
        );

        TMP_Text quantityLabel =
            CreateText(
                panel,
                "QuantityLabel",
                "Số lượng",
                17f,
                FontStyles.Bold,
                TextAlignmentOptions.Left,
                White
            );

        SetRect(
            quantityLabel.rectTransform,
            new Vector2(48f, -300f),
            new Vector2(120f, 32f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f)
        );

        refs.decreaseButton =
            CreateButton(
                panel,
                "DecreaseButton",
                "−",
                new Color(
                    0.2f,
                    0.2f,
                    0.2f,
                    1f
                ),
                White
            );

        SetRect(
            refs.decreaseButton
                .GetComponent<
                    RectTransform
                >(),
            new Vector2(175f, -292f),
            new Vector2(48f, 46f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f)
        );

        refs.quantityInput =
            CreateInputField(
                panel,
                "QuantityInput",
                "1"
            );

        refs.quantityInput.contentType =
            TMP_InputField
                .ContentType
                .IntegerNumber;

        SetRect(
            refs.quantityInput
                .GetComponent<
                    RectTransform
                >(),
            new Vector2(231f, -292f),
            new Vector2(100f, 46f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f)
        );

        refs.increaseButton =
            CreateButton(
                panel,
                "IncreaseButton",
                "+",
                new Color(
                    0.2f,
                    0.2f,
                    0.2f,
                    1f
                ),
                White
            );

        SetRect(
            refs.increaseButton
                .GetComponent<
                    RectTransform
                >(),
            new Vector2(339f, -292f),
            new Vector2(48f, 46f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f)
        );

        refs.totalPriceText =
            CreateText(
                panel,
                "TotalPriceText",
                "Tổng: $0",
                22f,
                FontStyles.Bold,
                TextAlignmentOptions.Right,
                new Color(
                    0.35f,
                    1f,
                    0.72f,
                    1f
                )
            );

        SetRect(
            refs.totalPriceText
                .rectTransform,
            new Vector2(-48f, -300f),
            new Vector2(210f, 38f),
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(1f, 1f)
        );

        refs.cancelButton =
            CreateButton(
                panel,
                "CancelButton",
                "HỦY",
                new Color(
                    0.22f,
                    0.22f,
                    0.22f,
                    1f
                ),
                White
            );

        SetRect(
            refs.cancelButton
                .GetComponent<
                    RectTransform
                >(),
            new Vector2(48f, 34f),
            new Vector2(220f, 54f),
            new Vector2(0f, 0f),
            new Vector2(0f, 0f),
            new Vector2(0f, 0f)
        );

        refs.confirmButton =
            CreateButton(
                panel,
                "ConfirmButton",
                "XÁC NHẬN MUA",
                new Color(
                    0.95f,
                    0.95f,
                    0.95f,
                    1f
                ),
                new Color(
                    0.06f,
                    0.06f,
                    0.06f,
                    1f
                )
            );

        SetRect(
            refs.confirmButton
                .GetComponent<
                    RectTransform
                >(),
            new Vector2(-48f, 34f),
            new Vector2(260f, 54f),
            new Vector2(1f, 0f),
            new Vector2(1f, 0f),
            new Vector2(1f, 0f)
        );

        return refs;
    }

    private static void WirePopup(
        ToolShopPurchasePopupUI popup,
        PopupReferences refs)
    {
        SerializedObject serialized =
            new SerializedObject(popup);

        serialized
            .FindProperty("popupRoot")
            .objectReferenceValue =
                refs.root.gameObject;

        serialized
            .FindProperty("itemIcon")
            .objectReferenceValue =
                refs.itemIcon;

        serialized
            .FindProperty("itemNameText")
            .objectReferenceValue =
                refs.itemNameText;

        serialized
            .FindProperty("descriptionText")
            .objectReferenceValue =
                refs.descriptionText;

        serialized
            .FindProperty("unitPriceText")
            .objectReferenceValue =
                refs.unitPriceText;

        serialized
            .FindProperty("totalPriceText")
            .objectReferenceValue =
                refs.totalPriceText;

        serialized
            .FindProperty("quantityInput")
            .objectReferenceValue =
                refs.quantityInput;

        serialized
            .FindProperty("decreaseButton")
            .objectReferenceValue =
                refs.decreaseButton;

        serialized
            .FindProperty("increaseButton")
            .objectReferenceValue =
                refs.increaseButton;

        serialized
            .FindProperty("confirmButton")
            .objectReferenceValue =
                refs.confirmButton;

        serialized
            .FindProperty("cancelButton")
            .objectReferenceValue =
                refs.cancelButton;

        serialized.ApplyModifiedProperties();
    }

    private static ScrollRect CreateScrollView(
        Transform parent,
        out RectTransform content)
    {
        RectTransform root =
            CreateRect(
                parent,
                "ToolScrollView"
            );

        ScrollRect scroll =
            root.gameObject
                .AddComponent<ScrollRect>();

        Image rootImage =
            root.gameObject
                .AddComponent<Image>();

        rootImage.color =
            new Color(
                0f,
                0f,
                0f,
                0f
            );

        RectTransform viewport =
            CreateRect(
                root,
                "Viewport"
            );

        Stretch(viewport);

        Image viewportImage =
            viewport.gameObject
                .AddComponent<Image>();

        viewportImage.color =
            new Color(
                1f,
                1f,
                1f,
                0.001f
            );

        Mask mask =
            viewport.gameObject
                .AddComponent<Mask>();

        mask.showMaskGraphic = false;

        content =
            CreateRect(
                viewport,
                "Content"
            );

        content.anchorMin =
            new Vector2(0f, 1f);

        content.anchorMax =
            new Vector2(1f, 1f);

        content.pivot =
            new Vector2(0.5f, 1f);

        content.anchoredPosition =
            Vector2.zero;

        content.sizeDelta =
            new Vector2(0f, 0f);

        GridLayoutGroup grid =
            content.gameObject
                .AddComponent<
                    GridLayoutGroup
                >();

        grid.cellSize =
            new Vector2(245f, 325f);

        grid.spacing =
            new Vector2(24f, 24f);

        grid.padding =
            new RectOffset(
                0,
                0,
                0,
                24
            );

        grid.constraint =
            GridLayoutGroup.Constraint
                .Flexible;

        grid.childAlignment =
            TextAnchor.UpperLeft;

        ContentSizeFitter fitter =
            content.gameObject
                .AddComponent<
                    ContentSizeFitter
                >();

        fitter.verticalFit =
            ContentSizeFitter.FitMode
                .PreferredSize;

        scroll.viewport = viewport;
        scroll.content = content;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType =
            ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 35f;

        return scroll;
    }

    private static TMP_Text CreateTextBox(
        Transform parent,
        string objectName,
        string content,
        Vector2 position,
        Vector2 size)
    {
        RectTransform box =
            CreateRect(
                parent,
                objectName + "_Box"
            );

        box.anchorMin =
            new Vector2(1f, 1f);

        box.anchorMax =
            new Vector2(1f, 1f);

        box.pivot =
            new Vector2(1f, 1f);

        box.anchoredPosition =
            position;

        box.sizeDelta = size;

        Image image =
            box.gameObject
                .AddComponent<Image>();

        image.color =
            new Color(
                0.11f,
                0.11f,
                0.11f,
                1f
            );

        image.sprite =
            GetUISprite();

        image.type =
            Image.Type.Sliced;

        TMP_Text text =
            CreateText(
                box,
                objectName,
                content,
                15f,
                FontStyles.Bold,
                TextAlignmentOptions.Center,
                White
            );

        Stretch(text.rectTransform);

        return text;
    }

    private static TMP_InputField
        CreateInputField(
            Transform parent,
            string objectName,
            string placeholder)
    {
        RectTransform root =
            CreateRect(
                parent,
                objectName
            );

        Image background =
            root.gameObject
                .AddComponent<Image>();

        background.color =
            new Color(
                0.11f,
                0.11f,
                0.11f,
                1f
            );

        background.sprite =
            GetUISprite();

        background.type =
            Image.Type.Sliced;

        TMP_InputField input =
            root.gameObject
                .AddComponent<
                    TMP_InputField
                >();

        RectTransform textArea =
            CreateRect(
                root,
                "Text Area"
            );

        textArea.anchorMin =
            Vector2.zero;

        textArea.anchorMax =
            Vector2.one;

        textArea.offsetMin =
            new Vector2(16f, 8f);

        textArea.offsetMax =
            new Vector2(-16f, -8f);

        RectMask2D mask =
            textArea.gameObject
                .AddComponent<
                    RectMask2D
                >();

        TMP_Text placeholderText =
            CreateText(
                textArea,
                "Placeholder",
                placeholder,
                16f,
                FontStyles.Italic,
                TextAlignmentOptions.Left,
                MutedText
            );

        Stretch(
            placeholderText
                .rectTransform
        );

        TMP_Text inputText =
            CreateText(
                textArea,
                "Text",
                string.Empty,
                17f,
                FontStyles.Normal,
                TextAlignmentOptions.Left,
                White
            );

        Stretch(inputText.rectTransform);

        input.textViewport = textArea;
        input.textComponent = inputText;
        input.placeholder =
            placeholderText;

        return input;
    }

    private static Button CreateButton(
        Transform parent,
        string objectName,
        string label,
        Color backgroundColor,
        Color textColor)
    {
        RectTransform root =
            CreateRect(
                parent,
                objectName
            );

        Image image =
            root.gameObject
                .AddComponent<Image>();

        image.color =
            backgroundColor;

        image.sprite =
            GetUISprite();

        image.type =
            Image.Type.Sliced;

        Button button =
            root.gameObject
                .AddComponent<Button>();

        ColorBlock colors =
            button.colors;

        colors.normalColor =
            backgroundColor;

        colors.highlightedColor =
            Color.Lerp(
                backgroundColor,
                Color.white,
                0.14f
            );

        colors.pressedColor =
            Color.Lerp(
                backgroundColor,
                Color.black,
                0.16f
            );

        button.colors = colors;

        TMP_Text text =
            CreateText(
                root,
                "Text",
                label,
                17f,
                FontStyles.Bold,
                TextAlignmentOptions.Center,
                textColor
            );

        Stretch(text.rectTransform);

        return button;
    }

    private static TMP_Text CreateText(
        Transform parent,
        string objectName,
        string content,
        float fontSize,
        FontStyles fontStyle,
        TextAlignmentOptions alignment,
        Color color)
    {
        RectTransform rect =
            CreateRect(
                parent,
                objectName
            );

        TextMeshProUGUI text =
            rect.gameObject
                .AddComponent<
                    TextMeshProUGUI
                >();

        text.text = content;
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.alignment = alignment;
        text.color = color;
        text.raycastTarget = false;
        text.enableWordWrapping = true;

        return text;
    }

    private static Image CreateImage(
        Transform parent,
        string objectName,
        Color color)
    {
        RectTransform rect =
            CreateRect(
                parent,
                objectName
            );

        Image image =
            rect.gameObject
                .AddComponent<Image>();

        image.color = color;

        return image;
    }

    private static RectTransform CreateRect(
        Transform parent,
        string objectName)
    {
        GameObject gameObject =
            new GameObject(
                objectName,
                typeof(RectTransform)
            );

        RectTransform rect =
            gameObject.GetComponent<
                RectTransform
            >();

        rect.SetParent(
            parent,
            false
        );

        return rect;
    }

    private static void Stretch(
        RectTransform rect)
    {
        rect.anchorMin =
            Vector2.zero;

        rect.anchorMax =
            Vector2.one;

        rect.offsetMin =
            Vector2.zero;

        rect.offsetMax =
            Vector2.zero;
    }

    private static void SetRect(
        RectTransform rect,
        Vector2 anchoredPosition,
        Vector2 sizeDelta,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition =
            anchoredPosition;
        rect.sizeDelta = sizeDelta;
    }

    private static Sprite GetUISprite()
    {
        return AssetDatabase
            .GetBuiltinExtraResource<
                Sprite
            >(
                "UI/Skin/UISprite.psd"
            );
    }

    private static void EnsureEventSystem()
    {
        EventSystem existing =
            UnityEngine.Object
                .FindFirstObjectByType<
                    EventSystem
                >(
                    FindObjectsInactive.Include
                );

        if (existing != null)
            return;

        GameObject eventSystem =
            new GameObject(
                "EventSystem",
                typeof(EventSystem),
                typeof(StandaloneInputModule)
            );

        Undo.RegisterCreatedObjectUndo(
            eventSystem,
            "Create Event System"
        );
    }

    private static MonoBehaviour
        FindPlayerStatsComponent()
    {
        MonoBehaviour[] behaviours =
            UnityEngine.Object
                .FindObjectsByType<
                    MonoBehaviour
                >(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None
                );

        return behaviours
            .FirstOrDefault(
                behaviour =>
                    behaviour != null &&
                    string.Equals(
                        behaviour
                            .GetType()
                            .Name,
                        "PlayerStats",
                        StringComparison
                            .OrdinalIgnoreCase
                    )
            );
    }

    private static GameObject
        FindDirectChild(
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

            if (child != null &&
                string.Equals(
                    child.name,
                    childName,
                    StringComparison
                        .OrdinalIgnoreCase
                ))
            {
                return child.gameObject;
            }
        }

        return null;
    }

    private static void SetString(
        SerializedObject serialized,
        string value,
        params string[] names)
    {
        SerializedProperty property =
            FindProperty(
                serialized,
                names
            );

        if (property != null &&
            property.propertyType ==
                SerializedPropertyType.String)
        {
            property.stringValue =
                value;
        }
    }

    private static void SetInteger(
        SerializedObject serialized,
        int value,
        params string[] names)
    {
        SerializedProperty property =
            FindProperty(
                serialized,
                names
            );

        if (property != null &&
            property.propertyType ==
                SerializedPropertyType.Integer)
        {
            property.intValue = value;
        }
    }

    private static void SetBoolean(
        SerializedObject serialized,
        bool value,
        params string[] names)
    {
        SerializedProperty property =
            FindProperty(
                serialized,
                names
            );

        if (property != null &&
            property.propertyType ==
                SerializedPropertyType.Boolean)
        {
            property.boolValue =
                value;
        }
    }

    private static void SetEnumByName(
        SerializedObject serialized,
        string[] preferredNames,
        params string[] propertyNames)
    {
        SerializedProperty property =
            FindProperty(
                serialized,
                propertyNames
            );

        if (property == null ||
            property.propertyType !=
                SerializedPropertyType.Enum)
        {
            return;
        }

        foreach (string preferred
                 in preferredNames)
        {
            for (int index = 0;
                 index <
                     property
                         .enumNames
                         .Length;
                 index++)
            {
                if (string.Equals(
                        property
                            .enumNames[index],
                        preferred,
                        StringComparison
                            .OrdinalIgnoreCase))
                {
                    property.enumValueIndex =
                        index;

                    return;
                }
            }
        }
    }

    private static SerializedProperty
        FindProperty(
            SerializedObject serialized,
            params string[] names)
    {
        foreach (string name in names)
        {
            SerializedProperty property =
                serialized.FindProperty(
                    name
                );

            if (property != null)
                return property;
        }

        return null;
    }

    private sealed class PopupReferences
    {
        public RectTransform root;
        public Image itemIcon;
        public TMP_Text itemNameText;
        public TMP_Text descriptionText;
        public TMP_Text unitPriceText;
        public TMP_Text totalPriceText;
        public TMP_InputField quantityInput;
        public Button decreaseButton;
        public Button increaseButton;
        public Button confirmButton;
        public Button cancelButton;
    }
}
#endif