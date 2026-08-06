using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

#pragma warning disable S3903 // Giữ namespace hiện tại để không làm mất liên kết MonoBehaviour đã serialize trong scene/prefab.

[DisallowMultipleComponent]
[DefaultExecutionOrder(32000)]
public class ToolSupplyShopStandalone : MonoBehaviour
{
    /*
     * COMPACT LAYOUT:
     * Card: 210 x 255
     * Grid spacing: 16 x 16
     * Columns: 7 ở độ phân giải tham chiếu 1920 x 1080
     */
    [Header("Data")]
    [SerializeField] private ToolShopItemData[] products;

    [Header("Game References")]
    [SerializeField] private InventoryManager inventoryManager;
    [SerializeField] private MonoBehaviour walletSource;

    [Header("Open/Close")]
    [SerializeField] private KeyCode testOpenKey = KeyCode.F8;
    [SerializeField] private bool disableOtherGraphicRaycastersWhileOpen = true;
    [SerializeField] private int sortingOrder = 32767;

    [Header("Player Lock - Triệt để")]
    [Tooltip(
        "Khóa hoàn toàn Player trong toàn bộ thời gian cửa hàng đang mở."
    )]
    [SerializeField] private bool lockPlayerWhileShopOpen = true;

    [Tooltip(
        "Có thể để trống khi mở bằng NPC mới. NPC sẽ truyền đúng Player vào shop."
    )]
    [SerializeField] private GameObject playerRoot;

    [Tooltip(
        "Tắt toàn bộ MonoBehaviour trên Player, trừ các component an toàn."
    )]
    [SerializeField] private bool disableAllPlayerBehaviours = true;

    [Tooltip(
        "Các component trên Player vẫn được giữ hoạt động khi shop mở."
    )]
    [SerializeField] private MonoBehaviour[] keepEnabledWhileShopOpen;

    [Tooltip(
        "Giữ nguyên tuyệt đối vị trí và góc quay của Player."
    )]
    [SerializeField] private bool forcePlayerTransformEveryFrame = true;

    [Header("Purchase")]
    [SerializeField] private float messageSeconds = 1.5f;

    private Canvas canvas;
    private GraphicRaycaster ownRaycaster;
    private CanvasGroup rootGroup;
    private GameObject root;
    private RectTransform content;
    private TMP_InputField searchInput;
    private TMP_Text moneyText;
    private TMP_Text messageText;

    private GameObject popupRoot;
    private Image popupIcon;
    private TMP_Text popupName;
    private TMP_Text popupDescription;
    private TMP_Text popupUnitPrice;
    private TMP_Text popupTotalPrice;
    private TMP_InputField quantityInput;

    private ToolShopItemData selectedProduct;
    private Coroutine messageRoutine;

    private readonly List<GraphicRaycaster>
        disabledRaycasters =
            new List<GraphicRaycaster>();

    private bool cursorStateCaptured;

    private GameObject interactingPlayer;
    private bool playerLockedByShop;
    private Transform lockedPlayerTransform;
    private Vector3 lockedPlayerPosition;
    private Quaternion lockedPlayerRotation;

    private Rigidbody2D lockedPlayerBody;
    private bool previousBodySimulated;
    private RigidbodyType2D previousBodyType;
    private RigidbodyConstraints2D previousBodyConstraints;
    private Vector2 previousBodyVelocity;
    private float previousBodyAngularVelocity;

    private readonly Dictionary<MonoBehaviour, bool>
        playerBehaviourStates =
            new Dictionary<MonoBehaviour, bool>();

    public bool IsOpen =>
        root != null &&
        root.activeSelf;

    private void Awake()
    {
        ResolveGameReferences();
        EnsureEventSystem();
        BuildUI();
        LoadProductsFallback();
        CloseShop();
    }

    private void Update()
    {
        if (WasOpenKeyPressed())
        {
            if (IsOpen)
                CloseShop();
            else
                OpenShop();
        }

        if (IsOpen &&
            WasEscapePressed())
        {
            CloseShop();
        }

        if (playerLockedByShop)
            EnforcePlayerLock();
    }

    private void FixedUpdate()
    {
        if (playerLockedByShop)
            EnforcePlayerLock();
    }

    private void LateUpdate()
    {
        if (playerLockedByShop)
            EnforcePlayerLock();
    }

    public void OpenShop(
        GameObject playerWhoInteracted)
    {
        interactingPlayer =
            FindActualPlayerRoot(
                playerWhoInteracted
            );

        OpenShop();
    }

    public void OpenShop()
    {
        if (IsOpen)
            return;

        ResolveGameReferences();
        EnsureEventSystem();

        if (root == null)
            BuildUI();

        LoadProductsFallback();

        root.SetActive(true);
        root.transform.SetAsLastSibling();

        if (rootGroup != null)
        {
            rootGroup.alpha = 1f;
            rootGroup.interactable = true;
            rootGroup.blocksRaycasts = true;
        }

        if (canvas != null)
        {
            canvas.enabled = true;
            canvas.overrideSorting = true;
            canvas.sortingOrder = sortingOrder;
        }

        if (ownRaycaster != null)
            ownRaycaster.enabled = true;

        cursorStateCaptured = true;
        CursorManager.EnsureCursorAvailable();

        DisableCompetingRaycasters();
        LockPlayerForShop();

        if (searchInput != null)
            searchInput.SetTextWithoutNotify(string.Empty);

        RebuildProducts();
        RefreshMoney();
    }

    public void CloseShop()
    {
        HidePopup();
        HideMessage();

        if (root != null)
            root.SetActive(false);

        RestoreCompetingRaycasters();
        UnlockPlayerForShop();

        if (cursorStateCaptured)
            cursorStateCaptured = false;

        CursorManager.EnsureCursorAvailable();
    }

    public void RefreshProducts()
    {
        LoadProductsFallback();
        RebuildProducts();
    }

    private void BuildUI()
    {
        canvas =
            GetComponent<Canvas>();

        if (canvas == null)
            canvas =
                gameObject.AddComponent<Canvas>();

        canvas.renderMode =
            RenderMode.ScreenSpaceOverlay;

        canvas.overrideSorting = true;
        canvas.sortingOrder = sortingOrder;

        CanvasScaler scaler =
            GetComponent<CanvasScaler>();

        if (scaler == null)
            scaler =
                gameObject.AddComponent<CanvasScaler>();

        scaler.uiScaleMode =
            CanvasScaler.ScaleMode
                .ScaleWithScreenSize;

        scaler.referenceResolution =
            new Vector2(1920f, 1080f);

        scaler.matchWidthOrHeight = 0.5f;

        ownRaycaster =
            GetComponent<GraphicRaycaster>();

        if (ownRaycaster == null)
            ownRaycaster =
                gameObject.AddComponent<
                    GraphicRaycaster
                >();

        ownRaycaster.enabled = true;

        Transform oldRoot =
            transform.Find(
                "StandaloneShopRoot"
            );

        if (oldRoot != null)
            Destroy(oldRoot.gameObject);

        RectTransform rootRect =
            CreateRect(
                transform,
                "StandaloneShopRoot"
            );

        Stretch(rootRect);

        root = rootRect.gameObject;

        Image background =
            root.AddComponent<Image>();

        background.color =
            new Color(
                0.045f,
                0.045f,
                0.045f,
                0.995f
            );

        background.raycastTarget = true;

        rootGroup =
            root.AddComponent<CanvasGroup>();

        rootGroup.alpha = 1f;
        rootGroup.interactable = true;
        rootGroup.blocksRaycasts = true;

        TMP_Text title =
            CreateText(
                rootRect,
                "Title",
                "CỬA HÀNG DỤNG CỤ",
                30f,
                FontStyles.Bold,
                TextAlignmentOptions.Left,
                Color.white
            );

        SetRect(
            title.rectTransform,
            new Vector2(36f, -26f),
            new Vector2(500f, 46f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f)
        );

        searchInput =
            CreateInputField(
                rootRect,
                "SearchInput",
                "Tìm kiếm dụng cụ..."
            );

        SetRect(
            searchInput.GetComponent<
                RectTransform
            >(),
            new Vector2(36f, -82f),
            new Vector2(360f, 46f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f)
        );

        searchInput.onValueChanged
            .AddListener(
                _ => RebuildProducts()
            );

        moneyText =
            CreateTextBox(
                rootRect,
                "MoneyText",
                "TIỀN MẶT\n<b>$0</b>",
                new Vector2(-190f, -22f),
                new Vector2(150f, 56f)
            );

        Button closeButton =
            CreateButton(
                rootRect,
                "CloseButton",
                "ESC  ĐÓNG",
                new Color(
                    0.88f,
                    0.88f,
                    0.88f,
                    1f
                ),
                new Color(
                    0.08f,
                    0.08f,
                    0.08f,
                    1f
                )
            );

        SetRect(
            closeButton.GetComponent<
                RectTransform
            >(),
            new Vector2(-26f, -25f),
            new Vector2(130f, 44f),
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(1f, 1f)
        );

        closeButton.onClick
            .AddListener(CloseShop);

        ScrollRect scroll =
            CreateScrollView(
                rootRect,
                out content
            );

        SetRect(
            scroll.GetComponent<
                RectTransform
            >(),
            new Vector2(0f, -58f),
            new Vector2(-72f, -196f),
            Vector2.zero,
            Vector2.one,
            new Vector2(0.5f, 0.5f)
        );

        messageText =
            CreateText(
                rootRect,
                "MessageText",
                string.Empty,
                22f,
                FontStyles.Bold,
                TextAlignmentOptions.Center,
                Color.white
            );

        SetRect(
            messageText.rectTransform,
            new Vector2(0f, 22f),
            new Vector2(900f, 42f),
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f)
        );

        messageText.gameObject
            .SetActive(false);

        BuildPopup(rootRect);
    }

    private void RebuildProducts()
    {
        if (content == null)
            return;

        for (int index =
                 content.childCount - 1;
             index >= 0;
             index--)
        {
            Destroy(
                content.GetChild(index)
                    .gameObject
            );
        }

        string keyword =
            searchInput != null
                ? searchInput.text
                    .Trim()
                    .ToLowerInvariant()
                : string.Empty;

        int count = CreateMatchingProductCards(keyword);

        /*
         * Bảo đảm toàn bộ Graphic của card không bị alpha/culling
         * từ thiết lập runtime cũ.
         */
        ForceProductGraphicsVisible();

        Canvas.ForceUpdateCanvases();

        LayoutRebuilder
            .ForceRebuildLayoutImmediate(
                content
            );

        if (count == 0)
        {
            ShowPersistentMessage(
                products == null ||
                products.Length == 0
                    ? "Không có dữ liệu sản phẩm. Hãy chạy Editor FINAL REBUILD."
                    : "Không tìm thấy sản phẩm.",
                false
            );
        }
        else
        {
            HideMessage();
        }

        Debug.Log(
            "[ToolSupplyShopStandalone] " +
            "Đã tạo " +
            count +
            " sản phẩm.",
            this
        );
    }

    private int CreateMatchingProductCards(
        string keyword)
    {
        if (products == null)
            return 0;

        int count = 0;

        foreach (ToolShopItemData item in products)
        {
            if (!MatchesProductSearch(item, keyword))
                continue;

            CreateProductCard(content, item);
            count++;
        }

        return count;
    }

    private static bool MatchesProductSearch(
        ToolShopItemData item,
        string keyword)
    {
        if (item == null || item.inventoryItem == null)
            return false;

        if (string.IsNullOrWhiteSpace(keyword))
            return true;

        string searchable =
            (
                item.DisplayName +
                " " +
                item.Description +
                " " +
                item.category +
                " " +
                item.ItemId
            ).ToLowerInvariant();

        return searchable.Contains(keyword);
    }

    private void CreateProductCard(
        Transform parent,
        ToolShopItemData item)
    {
        RectTransform card =
            CreateRect(
                parent,
                "Product_" +
                item.ItemId
            );

        card.sizeDelta =
            new Vector2(210f, 255f);

        Image background =
            card.gameObject
                .AddComponent<Image>();

        background.color =
            new Color(
                0.15f,
                0.15f,
                0.15f,
                1f
            );

        background.raycastTarget = true;

        Button cardButton =
            card.gameObject
                .AddComponent<Button>();

        cardButton.targetGraphic =
            background;

        cardButton.onClick
            .AddListener(
                () => ShowPopup(item)
            );

        TMP_Text name =
            CreateText(
                card,
                "Name",
                item.DisplayName,
                16f,
                FontStyles.Bold,
                TextAlignmentOptions.TopLeft,
                Color.white
            );

        SetRect(
            name.rectTransform,
            new Vector2(12f, -12f),
            new Vector2(-76f, 26f),
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(0f, 1f)
        );

        TMP_Text category =
            CreateText(
                card,
                "Category",
                "Dụng cụ",
                12f,
                FontStyles.Normal,
                TextAlignmentOptions.TopLeft,
                new Color(
                    0.58f,
                    0.58f,
                    0.58f,
                    1f
                )
            );

        SetRect(
            category.rectTransform,
            new Vector2(12f, -38f),
            new Vector2(-24f, 20f),
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(0f, 1f)
        );

        TMP_Text price =
            CreateText(
                card,
                "Price",
                "$" +
                item.Price.ToString("N0"),
                15f,
                FontStyles.Bold,
                TextAlignmentOptions.TopRight,
                Color.white
            );

        SetRect(
            price.rectTransform,
            new Vector2(-12f, -12f),
            new Vector2(72f, 26f),
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(1f, 1f)
        );

        Image icon =
            CreateImage(
                card,
                "Icon",
                Color.white
            );

        icon.sprite = item.Icon;
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        icon.enabled =
            item.Icon != null;

        SetRect(
            icon.rectTransform,
            new Vector2(0f, -58f),
            new Vector2(-26f, 145f),
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(0.5f, 1f)
        );

        CanvasGroup hoverGroup;
        RectTransform buyVisual =
            CreateRect(
                card,
                "BuyHover"
            );

        SetRect(
            buyVisual,
            new Vector2(12f, 12f),
            new Vector2(-24f, 38f),
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(0.5f, 0f)
        );

        Image buyBackground =
            buyVisual.gameObject
                .AddComponent<Image>();

        buyBackground.color =
            new Color(
                0.96f,
                0.96f,
                0.96f,
                1f
            );

        buyBackground.raycastTarget =
            false;

        TMP_Text buyText =
            CreateText(
                buyVisual,
                "Text",
                "MUA",
                15f,
                FontStyles.Bold,
                TextAlignmentOptions.Center,
                new Color(
                    0.08f,
                    0.08f,
                    0.08f,
                    1f
                )
            );

        Stretch(
            buyText.rectTransform
        );

        hoverGroup =
            buyVisual.gameObject
                .AddComponent<CanvasGroup>();

        hoverGroup.alpha = 0f;
        hoverGroup.interactable = false;
        hoverGroup.blocksRaycasts = false;

        ToolSupplyShopStandaloneCard hover =
            card.gameObject
                .AddComponent<
                    ToolSupplyShopStandaloneCard
                >();

        hover.Configure(hoverGroup);
    }

    private void ForceProductGraphicsVisible()
    {
        if (content == null)
            return;

        CanvasGroup[] groups =
            content.GetComponentsInChildren<
                CanvasGroup
            >(true);

        foreach (CanvasGroup group
                 in groups)
        {
            if (group == null)
                continue;

            /*
             * BuyHover được điều khiển riêng khi hover.
             * Không ép alpha của nó để giữ hiệu ứng MUA.
             */
            if (string.Equals(
                    group.gameObject.name,
                    "BuyHover",
                    StringComparison
                        .OrdinalIgnoreCase))
            {
                continue;
            }

            group.alpha = 1f;
            group.interactable = true;
            group.blocksRaycasts = true;
        }

        Graphic[] graphics =
            content.GetComponentsInChildren<
                Graphic
            >(true);

        foreach (Graphic graphic
                 in graphics)
        {
            if (graphic == null)
                continue;

            CanvasRenderer renderer =
                graphic.canvasRenderer;

            if (renderer != null)
            {
                renderer.SetAlpha(1f);
                renderer.cull = false;
            }

            graphic.enabled = true;
        }
    }

    private void BuildPopup(
        Transform parent)
    {
        RectTransform overlay =
            CreateRect(
                parent,
                "PurchasePopup"
            );

        Stretch(overlay);

        popupRoot =
            overlay.gameObject;

        Image overlayImage =
            popupRoot.AddComponent<Image>();

        overlayImage.color =
            new Color(
                0f,
                0f,
                0f,
                0.72f
            );

        overlayImage.raycastTarget = true;

        RectTransform panel =
            CreateRect(
                overlay,
                "Panel"
            );

        panel.anchorMin =
            new Vector2(0.5f, 0.5f);

        panel.anchorMax =
            new Vector2(0.5f, 0.5f);

        panel.pivot =
            new Vector2(0.5f, 0.5f);

        panel.sizeDelta =
            new Vector2(650f, 490f);

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

        panelImage.raycastTarget = true;

        TMP_Text heading =
            CreateText(
                panel,
                "Heading",
                "XÁC NHẬN MUA",
                28f,
                FontStyles.Bold,
                TextAlignmentOptions.Center,
                Color.white
            );

        SetRect(
            heading.rectTransform,
            new Vector2(0f, -22f),
            new Vector2(-40f, 44f),
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(0.5f, 1f)
        );

        popupIcon =
            CreateImage(
                panel,
                "ItemIcon",
                Color.white
            );

        popupIcon.preserveAspect = true;
        popupIcon.raycastTarget = false;

        SetRect(
            popupIcon.rectTransform,
            new Vector2(42f, -90f),
            new Vector2(190f, 190f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f)
        );

        popupName =
            CreateText(
                panel,
                "ItemName",
                "Tên dụng cụ",
                25f,
                FontStyles.Bold,
                TextAlignmentOptions.TopLeft,
                Color.white
            );

        SetRect(
            popupName.rectTransform,
            new Vector2(260f, -90f),
            new Vector2(340f, 42f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f)
        );

        popupDescription =
            CreateText(
                panel,
                "Description",
                string.Empty,
                17f,
                FontStyles.Normal,
                TextAlignmentOptions.TopLeft,
                new Color(
                    0.65f,
                    0.65f,
                    0.65f,
                    1f
                )
            );

        SetRect(
            popupDescription.rectTransform,
            new Vector2(260f, -140f),
            new Vector2(340f, 90f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f)
        );

        popupUnitPrice =
            CreateText(
                panel,
                "UnitPrice",
                "Đơn giá: $0",
                18f,
                FontStyles.Bold,
                TextAlignmentOptions.Left,
                Color.white
            );

        SetRect(
            popupUnitPrice.rectTransform,
            new Vector2(260f, -240f),
            new Vector2(280f, 34f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f)
        );

        TMP_Text quantityLabel =
            CreateText(
                panel,
                "QuantityLabel",
                "Số lượng",
                18f,
                FontStyles.Bold,
                TextAlignmentOptions.Left,
                Color.white
            );

        SetRect(
            quantityLabel.rectTransform,
            new Vector2(42f, -318f),
            new Vector2(125f, 34f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f)
        );

        Button minus =
            CreateButton(
                panel,
                "Minus",
                "−",
                new Color(
                    0.22f,
                    0.22f,
                    0.22f,
                    1f
                ),
                Color.white
            );

        SetRect(
            minus.GetComponent<
                RectTransform
            >(),
            new Vector2(170f, -310f),
            new Vector2(50f, 48f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f)
        );

        quantityInput =
            CreateInputField(
                panel,
                "Quantity",
                "1"
            );

        quantityInput.contentType =
            TMP_InputField
                .ContentType
                .IntegerNumber;

        SetRect(
            quantityInput.GetComponent<
                RectTransform
            >(),
            new Vector2(228f, -310f),
            new Vector2(105f, 48f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f)
        );

        Button plus =
            CreateButton(
                panel,
                "Plus",
                "+",
                new Color(
                    0.22f,
                    0.22f,
                    0.22f,
                    1f
                ),
                Color.white
            );

        SetRect(
            plus.GetComponent<
                RectTransform
            >(),
            new Vector2(341f, -310f),
            new Vector2(50f, 48f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f)
        );

        popupTotalPrice =
            CreateText(
                panel,
                "Total",
                "Tổng: $0",
                23f,
                FontStyles.Bold,
                TextAlignmentOptions.Right,
                new Color(
                    0.3f,
                    1f,
                    0.65f,
                    1f
                )
            );

        SetRect(
            popupTotalPrice.rectTransform,
            new Vector2(-42f, -318f),
            new Vector2(230f, 40f),
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(1f, 1f)
        );

        Button cancel =
            CreateButton(
                panel,
                "Cancel",
                "HỦY",
                new Color(
                    0.24f,
                    0.24f,
                    0.24f,
                    1f
                ),
                Color.white
            );

        SetRect(
            cancel.GetComponent<
                RectTransform
            >(),
            new Vector2(42f, 34f),
            new Vector2(220f, 56f),
            new Vector2(0f, 0f),
            new Vector2(0f, 0f),
            new Vector2(0f, 0f)
        );

        Button confirm =
            CreateButton(
                panel,
                "Confirm",
                "XÁC NHẬN MUA",
                new Color(
                    0.96f,
                    0.96f,
                    0.96f,
                    1f
                ),
                new Color(
                    0.07f,
                    0.07f,
                    0.07f,
                    1f
                )
            );

        SetRect(
            confirm.GetComponent<
                RectTransform
            >(),
            new Vector2(-42f, 34f),
            new Vector2(270f, 56f),
            new Vector2(1f, 0f),
            new Vector2(1f, 0f),
            new Vector2(1f, 0f)
        );

        minus.onClick.AddListener(
            () => ChangeQuantity(-1)
        );

        plus.onClick.AddListener(
            () => ChangeQuantity(1)
        );

        cancel.onClick.AddListener(
            HidePopup
        );

        confirm.onClick.AddListener(
            ConfirmPurchase
        );

        quantityInput.onValueChanged
            .AddListener(
                _ => RefreshPopupTotal()
            );

        popupRoot.SetActive(false);
    }

    private void ShowPopup(
        ToolShopItemData item)
    {
        if (item == null)
            return;

        if (item.UniqueOwnership &&
            OwnsItem(item))
        {
            ShowMessage(
                "Bạn đã sở hữu " +
                item.DisplayName +
                ".",
                false
            );
            return;
        }

        selectedProduct = item;
        popupRoot.SetActive(true);
        popupRoot.transform
            .SetAsLastSibling();

        popupIcon.sprite = item.Icon;
        popupIcon.enabled =
            item.Icon != null;

        popupName.text =
            item.DisplayName;

        popupDescription.text =
            item.Description;

        popupUnitPrice.text =
            "Đơn giá: $" +
            item.Price.ToString("N0");

        quantityInput.interactable =
            !item.UniqueOwnership &&
            item.maxPurchaseQuantity > 1;

        quantityInput.SetTextWithoutNotify(
            "1"
        );

        RefreshPopupTotal();
    }

    private void HidePopup()
    {
        selectedProduct = null;

        if (popupRoot != null)
            popupRoot.SetActive(false);
    }

    private void ChangeQuantity(
        int delta)
    {
        if (selectedProduct == null)
            return;

        int current =
            GetQuantity();

        int maximum =
            GetMaximumQuantity(
                selectedProduct
            );

        current =
            Mathf.Clamp(
                current + delta,
                1,
                maximum
            );

        quantityInput
            .SetTextWithoutNotify(
                current.ToString()
            );

        RefreshPopupTotal();
    }

    private void RefreshPopupTotal()
    {
        if (popupTotalPrice == null)
            return;

        int quantity =
            selectedProduct != null
                ? Mathf.Clamp(
                    GetQuantity(),
                    1,
                    GetMaximumQuantity(
                        selectedProduct
                    )
                  )
                : 1;

        int total =
            selectedProduct != null
                ? selectedProduct.Price *
                  quantity
                : 0;

        popupTotalPrice.text =
            "Tổng: $" +
            total.ToString("N0");
    }

    private void ConfirmPurchase()
    {
        if (selectedProduct == null)
            return;

        ResolveGameReferences();

        if (inventoryManager == null)
        {
            ShowMessage(
                "Không tìm thấy InventoryManager.",
                false
            );
            return;
        }

        int quantity =
            Mathf.Clamp(
                GetQuantity(),
                1,
                GetMaximumQuantity(
                    selectedProduct
                )
            );

        if (selectedProduct.UniqueOwnership)
        {
            quantity = 1;

            if (OwnsItem(
                    selectedProduct))
            {
                ShowMessage(
                    "Bạn đã sở hữu " +
                    selectedProduct
                        .DisplayName +
                    ".",
                    false
                );
                return;
            }
        }

        int total =
            selectedProduct.Price *
            quantity;

        if (!TrySpendMoney(
                total,
                out string reason))
        {
            ShowMessage(
                reason,
                false
            );
            return;
        }

        bool added =
            inventoryManager.TryAddItem(
                selectedProduct
                    .inventoryItem,
                quantity,
                out string inventoryReason
            );

        if (!added)
        {
            RefundMoney(total);

            ShowMessage(
                string.IsNullOrWhiteSpace(
                    inventoryReason)
                    ? "Túi đầy. Đã hoàn tiền."
                    : inventoryReason +
                      " Đã hoàn tiền.",
                false
            );
            return;
        }

        string purchasedName =
            selectedProduct.DisplayName;

        HidePopup();
        RefreshMoney();

        ShowMessage(
            "Đã mua " +
            purchasedName +
            " x" +
            quantity +
            ".",
            true
        );
    }

    private int GetQuantity()
    {
        if (quantityInput == null)
            return 1;

        if (!int.TryParse(
                quantityInput.text,
                out int value))
        {
            value = 1;
        }

        return Mathf.Max(1, value);
    }

    private static int GetMaximumQuantity(
        ToolShopItemData item)
    {
        if (item == null ||
            item.UniqueOwnership)
        {
            return 1;
        }

        return Mathf.Max(
            1,
            item.maxPurchaseQuantity
        );
    }

    private void LoadProductsFallback()
    {
        if (products != null &&
            products.Length > 0)
        {
            return;
        }

        products =
            Resources.LoadAll<
                ToolShopItemData
            >(
                "ToolShopProducts"
            );
    }

    private void DisableCompetingRaycasters()
    {
        RestoreCompetingRaycasters();

        if (!disableOtherGraphicRaycastersWhileOpen)
            return;

        GraphicRaycaster[] raycasters =
            FindObjectsByType<
                GraphicRaycaster
            >(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        foreach (GraphicRaycaster raycaster
                 in raycasters)
        {
            if (raycaster == null ||
                raycaster == ownRaycaster ||
                !raycaster.enabled)
            {
                continue;
            }

            raycaster.enabled = false;
            disabledRaycasters.Add(
                raycaster
            );
        }
    }

    private void RestoreCompetingRaycasters()
    {
        foreach (GraphicRaycaster raycaster
                 in disabledRaycasters)
        {
            if (raycaster != null)
                raycaster.enabled = true;
        }

        disabledRaycasters.Clear();
    }

    private void LockPlayerForShop()
    {
        if (!lockPlayerWhileShopOpen ||
            playerLockedByShop)
        {
            return;
        }

        GameObject resolvedPlayer =
            FindActualPlayerRoot(
                interactingPlayer != null
                    ? interactingPlayer
                    : playerRoot
            );

        if (resolvedPlayer == null)
        {
            resolvedPlayer =
                FindPlayerAutomatically();
        }

        if (resolvedPlayer == null)
        {
            Debug.LogError(
                "[ToolSupplyShopStandalone] Không tìm thấy đúng Player để khóa. " +
                "Hãy thay ToolShopStandaloneNPC bằng file mới trong gói fix.",
                this
            );

            return;
        }

        playerRoot = resolvedPlayer;
        interactingPlayer = resolvedPlayer;

        lockedPlayerTransform =
            resolvedPlayer.transform;

        lockedPlayerPosition =
            lockedPlayerTransform.position;

        lockedPlayerRotation =
            lockedPlayerTransform.rotation;

        playerLockedByShop = true;

        /*
         * Gọi hệ thống khóa có sẵn trước, nhưng không phụ thuộc vào nó.
         */
        if (GameLockManager.Instance != null)
        {
            GameLockManager.Instance.LockPlayer();
        }

        if (disableAllPlayerBehaviours)
        {
            DisableAllPlayerBehaviours(
                resolvedPlayer
            );
        }

        lockedPlayerBody =
            resolvedPlayer
                .GetComponentInChildren<
                    Rigidbody2D
                >(true);

        if (lockedPlayerBody != null)
        {
            previousBodySimulated =
                lockedPlayerBody.simulated;

            previousBodyType =
                lockedPlayerBody.bodyType;

            previousBodyConstraints =
                lockedPlayerBody.constraints;

            previousBodyVelocity =
                lockedPlayerBody.linearVelocity;

            previousBodyAngularVelocity =
                lockedPlayerBody.angularVelocity;

            lockedPlayerBody.linearVelocity =
                Vector2.zero;

            lockedPlayerBody.angularVelocity =
                0f;

            lockedPlayerBody.constraints =
                RigidbodyConstraints2D
                    .FreezeAll;

            /*
             * simulated = false chặn hoàn toàn physics,
             * collision push và FixedUpdate movement.
             */
            lockedPlayerBody.simulated = false;
        }

        EnforcePlayerLock();

        Debug.Log(
            "[ToolSupplyShopStandalone] Đã khóa Player: " +
            resolvedPlayer.name,
            resolvedPlayer
        );
    }

    private void UnlockPlayerForShop()
    {
        if (!playerLockedByShop)
            return;

        if (lockedPlayerBody != null)
        {
            lockedPlayerBody.simulated =
                previousBodySimulated;

            lockedPlayerBody.bodyType =
                previousBodyType;

            lockedPlayerBody.constraints =
                previousBodyConstraints;

            lockedPlayerBody.linearVelocity =
                Vector2.zero;

            lockedPlayerBody.angularVelocity =
                0f;
        }

        foreach (
            KeyValuePair<MonoBehaviour, bool>
            state in playerBehaviourStates)
        {
            if (state.Key != null)
            {
                state.Key.enabled =
                    state.Value;
            }
        }

        playerBehaviourStates.Clear();

        if (GameLockManager.Instance != null)
        {
            GameLockManager.Instance.UnlockPlayer();
        }

        playerLockedByShop = false;
        lockedPlayerTransform = null;
        lockedPlayerBody = null;
        interactingPlayer = null;
    }

    private void EnforcePlayerLock()
    {
        if (!playerLockedByShop)
            return;

        if (lockedPlayerBody != null)
        {
            lockedPlayerBody.linearVelocity =
                Vector2.zero;

            lockedPlayerBody.angularVelocity =
                0f;
        }

        /*
         * Execution Order = 32000 và chạy cả Update,
         * FixedUpdate, LateUpdate nên kể cả script khác
         * chỉnh transform trực tiếp thì Player vẫn bị
         * đưa về vị trí ban đầu ngay trong cùng frame.
         */
        if (forcePlayerTransformEveryFrame &&
            lockedPlayerTransform != null)
        {
            lockedPlayerTransform
                .SetPositionAndRotation(
                    lockedPlayerPosition,
                    lockedPlayerRotation
                );
        }
    }

    private void DisableAllPlayerBehaviours(
        GameObject resolvedPlayer)
    {
        playerBehaviourStates.Clear();

        MonoBehaviour[] behaviours =
            resolvedPlayer
                .GetComponentsInChildren<
                    MonoBehaviour
                >(true);

        foreach (MonoBehaviour behaviour
                 in behaviours)
        {
            if (behaviour == null ||
                behaviour == this ||
                ShouldKeepBehaviourEnabled(
                    behaviour))
            {
                continue;
            }

            if (!playerBehaviourStates
                    .ContainsKey(behaviour))
            {
                playerBehaviourStates.Add(
                    behaviour,
                    behaviour.enabled
                );
            }

            behaviour.enabled = false;
        }
    }

    private bool ShouldKeepBehaviourEnabled(
        MonoBehaviour behaviour)
    {
        if (behaviour == null)
            return true;

        if (keepEnabledWhileShopOpen != null)
        {
            foreach (MonoBehaviour kept
                     in keepEnabledWhileShopOpen)
            {
                if (kept == behaviour)
                    return true;
            }
        }

        string typeName =
            behaviour.GetType().Name;

        /*
         * Các component này không điều khiển di chuyển
         * và cần giữ để giao dịch hoạt động ổn định.
         */
        return string.Equals(
                   typeName,
                   "InventoryManager",
                   StringComparison
                       .OrdinalIgnoreCase
               ) ||
               string.Equals(
                   typeName,
                   "PlayerStats",
                   StringComparison
                       .OrdinalIgnoreCase
               ) ||
               string.Equals(
                   typeName,
                   "GameLockManager",
                   StringComparison
                       .OrdinalIgnoreCase
               );
    }

    private GameObject FindPlayerAutomatically()
    {
        try
        {
            GameObject tagged =
                GameObject.FindGameObjectWithTag(
                    "Player"
                );

            if (tagged != null)
                return FindActualPlayerRoot(tagged);
        }
        catch (UnityException)
        {
            /*
             * Tag Player chưa tồn tại.
             */
        }

        if (walletSource != null)
        {
            GameObject fromWallet =
                FindActualPlayerRoot(
                    walletSource.gameObject
                );

            if (fromWallet != null)
                return fromWallet;
        }

        Rigidbody2D[] bodies =
            FindObjectsByType<
                Rigidbody2D
            >(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        foreach (Rigidbody2D body in bodies)
        {
            if (body == null)
                continue;

            GameObject candidate =
                FindActualPlayerRoot(
                    body.gameObject
                );

            if (candidate != null &&
                string.Equals(
                    candidate.name,
                    "Player",
                    StringComparison
                        .OrdinalIgnoreCase
                ))
            {
                return candidate;
            }
        }

        return null;
    }

    private static GameObject FindActualPlayerRoot(
        GameObject candidate)
    {
        if (candidate == null)
            return null;

        Transform current =
            candidate.transform;

        GameObject bestCandidate =
            candidate;

        while (current != null)
        {
            GameObject currentObject =
                current.gameObject;

            if (IsPlayerObject(
                    currentObject))
            {
                bestCandidate =
                    currentObject;
            }

            current = current.parent;
        }

        return bestCandidate;
    }

    private static bool IsPlayerObject(
        GameObject candidate)
    {
        if (candidate == null)
            return false;

        if (string.Equals(
                candidate.name,
                "Player",
                StringComparison
                    .OrdinalIgnoreCase))
        {
            return true;
        }

        try
        {
            if (candidate.CompareTag("Player"))
                return true;
        }
        catch (UnityException)
        {
            // Nếu tag Player chưa tồn tại, tiếp tục nhận diện bằng component ở bên dưới.
        }

        MonoBehaviour[] behaviours =
            candidate.GetComponents<
                MonoBehaviour
            >();

        foreach (MonoBehaviour behaviour
                 in behaviours)
        {
            if (behaviour == null)
                continue;

            string typeName =
                behaviour.GetType().Name;

            if (string.Equals(
                    typeName,
                    "PlayerInput",
                    StringComparison
                        .OrdinalIgnoreCase) ||
                string.Equals(
                    typeName,
                    "PlayerController",
                    StringComparison
                        .OrdinalIgnoreCase) ||
                string.Equals(
                    typeName,
                    "PlayerMovement",
                    StringComparison
                        .OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private void ResolveGameReferences()
    {
        if (inventoryManager == null)
            inventoryManager =
                InventoryManager.Instance;

        if (inventoryManager == null)
        {
            inventoryManager =
                FindFirstObjectByType<
                    InventoryManager
                >(
                    FindObjectsInactive.Include
                );
        }

        if (walletSource == null)
        {
            MonoBehaviour[] behaviours =
                FindObjectsByType<
                    MonoBehaviour
                >(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None
                );

            foreach (MonoBehaviour behaviour
                     in behaviours)
            {
                if (behaviour != null &&
                    string.Equals(
                        behaviour.GetType()
                            .Name,
                        "PlayerStats",
                        StringComparison
                            .OrdinalIgnoreCase
                    ))
                {
                    walletSource = behaviour;
                    break;
                }
            }
        }
    }

    private bool OwnsItem(
        ToolShopItemData item)
    {
        if (inventoryManager == null ||
            item == null)
        {
            return false;
        }

        string targetId =
            Normalize(item.ItemId);

        string targetName =
            Normalize(item.DisplayName);

        Type type =
            inventoryManager.GetType();

        string[] names =
        {
            "HotbarSlots",
            "hotbarSlots",
            "BagSlots",
            "bagSlots",
            "Slots",
            "slots"
        };

        foreach (string memberName
                 in names)
        {
            if (CollectionContainsItem(
                    type,
                    memberName,
                    targetId,
                    targetName))
                return true;
        }

        return false;
    }

    private bool CollectionContainsItem(
        Type inventoryType,
        string memberName,
        string targetId,
        string targetName)
    {
        object collection =
            GetMemberValue(
                inventoryManager,
                inventoryType,
                memberName
            );

        if (!(collection is
                System.Collections.IEnumerable enumerable))
        {
            return false;
        }

        foreach (object slot in enumerable)
        {
            if (slot != null &&
                SlotMatchesItem(slot, targetId, targetName))
            {
                return true;
            }
        }

        return false;
    }

    private static bool SlotMatchesItem(
        object slot,
        string targetId,
        string targetName)
    {
        string slotName =
            ReadStringMember(
                slot,
                "itemName",
                "displayName",
                "ItemName"
            );

        string slotId =
            ReadStringMember(
                slot,
                "itemId",
                "ItemId",
                "id"
            );

        return MatchesNormalizedValue(targetId, slotId) ||
               MatchesNormalizedValue(targetName, slotName);
    }

    private static bool MatchesNormalizedValue(
        string expected,
        string actual)
    {
        return !string.IsNullOrWhiteSpace(expected) &&
               Normalize(actual) == expected;
    }

    private bool TrySpendMoney(
        int amount,
        out string reason)
    {
        reason = string.Empty;

        if (amount <= 0)
            return true;

        if (walletSource == null)
        {
            reason =
                "Chưa gắn PlayerStats/Wallet.";
            return false;
        }

        bool hasValue =
            TryGetMoney(
                out int money
            );

        if (hasValue &&
            money < amount)
        {
            reason =
                "Bạn không đủ tiền.";
            return false;
        }

        string[] methodNames =
        {
            "TrySpendMoney",
            "SpendMoney",
            "RemoveMoney",
            "UseMoney",
            "Pay"
        };

        foreach (string methodName
                 in methodNames)
        {
            MethodInfo method =
                FindNumberMethod(
                    walletSource.GetType(),
                    methodName
                );

            if (method == null)
                continue;

            object argument =
                ConvertNumber(
                    amount,
                    method.GetParameters()[0]
                        .ParameterType
                );

            object result =
                method.Invoke(
                    walletSource,
                    new[] { argument }
                );

            if (method.ReturnType ==
                typeof(bool))
            {
                bool success =
                    result is bool value &&
                    value;

                if (!success)
                    reason =
                        "Bạn không đủ tiền.";

                return success;
            }

            return true;
        }

        if (hasValue &&
            TrySetMoney(
                money - amount
            ))
        {
            return true;
        }

        reason =
            "Không tìm thấy hàm trừ tiền.";
        return false;
    }

    private void RefundMoney(
        int amount)
    {
        if (amount <= 0 ||
            walletSource == null)
        {
            return;
        }

        string[] methods =
        {
            "AddMoney",
            "GiveMoney",
            "EarnMoney",
            "ReceiveMoney"
        };

        foreach (string methodName
                 in methods)
        {
            MethodInfo method =
                FindNumberMethod(
                    walletSource.GetType(),
                    methodName
                );

            if (method == null)
                continue;

            object argument =
                ConvertNumber(
                    amount,
                    method.GetParameters()[0]
                        .ParameterType
                );

            method.Invoke(
                walletSource,
                new[] { argument }
            );

            return;
        }

        if (TryGetMoney(out int money))
            TrySetMoney(money + amount);
    }

    private void RefreshMoney()
    {
        if (moneyText == null)
            return;

        int money =
            TryGetMoney(out int value)
                ? value
                : 0;

        moneyText.text =
            "TIỀN MẶT\n<b>$" +
            money.ToString("N0") +
            "</b>";
    }

    private bool TryGetMoney(
        out int money)
    {
        money = 0;

        if (walletSource == null)
            return false;

        string[] names =
        {
            "Money",
            "money",
            "CurrentMoney",
            "currentMoney",
            "Cash",
            "cash"
        };

        foreach (string name in names)
        {
            object value =
                GetMemberValue(
                    walletSource,
                    walletSource.GetType(),
                    name
                );

            if (value == null)
                continue;

            try
            {
                money =
                    Convert.ToInt32(value);
                return true;
            }
            catch
            {
                // Bỏ qua member không thể chuyển sang số và thử tên member tiếp theo.
            }
        }

        return false;
    }

    private bool TrySetMoney(
        int value)
    {
        if (walletSource == null)
            return false;

        Type type =
            walletSource.GetType();

        string[] names =
        {
            "Money",
            "money",
            "CurrentMoney",
            "currentMoney",
            "Cash",
            "cash"
        };

        const BindingFlags flags =
            BindingFlags.Instance |
            BindingFlags.Public |
            BindingFlags.NonPublic |
            BindingFlags.IgnoreCase;

        foreach (string name in names)
        {
            FieldInfo field =
                type.GetField(name, flags);

            if (field != null)
            {
                field.SetValue(
                    walletSource,
                    ConvertNumber(
                        value,
                        field.FieldType
                    )
                );
                return true;
            }

            PropertyInfo property =
                type.GetProperty(
                    name,
                    flags
                );

            if (property != null &&
                property.CanWrite)
            {
                property.SetValue(
                    walletSource,
                    ConvertNumber(
                        value,
                        property.PropertyType
                    )
                );
                return true;
            }
        }

        return false;
    }

    private void ShowMessage(
        string message,
        bool success)
    {
        if (messageRoutine != null)
            StopCoroutine(messageRoutine);

        messageRoutine =
            StartCoroutine(
                MessageRoutine(
                    message,
                    success
                )
            );
    }

    private void ShowPersistentMessage(
        string message,
        bool success)
    {
        if (messageText == null)
        {
            Debug.LogWarning(
                message,
                this
            );
            return;
        }

        messageText.gameObject
            .SetActive(true);

        messageText.text = message;

        messageText.color =
            success
                ? new Color(
                    0.25f,
                    1f,
                    0.6f,
                    1f
                )
                : new Color(
                    1f,
                    0.3f,
                    0.3f,
                    1f
                );
    }

    private IEnumerator MessageRoutine(
        string message,
        bool success)
    {
        ShowPersistentMessage(
            message,
            success
        );

        yield return
            new WaitForSecondsRealtime(
                messageSeconds
            );

        HideMessage();
    }

    private void HideMessage()
    {
        if (messageRoutine != null)
        {
            StopCoroutine(
                messageRoutine
            );

            messageRoutine = null;
        }

        if (messageText != null)
            messageText.gameObject
                .SetActive(false);
    }

    private static void EnsureEventSystem()
    {
        EventSystem eventSystem =
            EventSystem.current;

        if (eventSystem == null)
        {
            eventSystem =
                FindFirstObjectByType<
                    EventSystem
                >(
                    FindObjectsInactive.Include
                );
        }

        if (eventSystem == null)
        {
            GameObject eventObject =
                new GameObject(
                    "EventSystem",
                    typeof(EventSystem)
                );

            eventSystem =
                eventObject.GetComponent<
                    EventSystem
                >();
        }

        eventSystem.gameObject
            .SetActive(true);

        Type inputModuleType =
            Type.GetType(
                "UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem"
            );

        if (inputModuleType != null)
        {
            Component module =
                eventSystem.gameObject
                    .GetComponent(
                        inputModuleType
                    );

            if (module == null)
            {
                module =
                    eventSystem.gameObject
                        .AddComponent(
                            inputModuleType
                        );
            }

            MethodInfo assignDefaults =
                inputModuleType.GetMethod(
                    "AssignDefaultActions",
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic
                );

            assignDefaults?.Invoke(
                module,
                null
            );

            if (module is Behaviour behaviour)
                behaviour.enabled = true;

            StandaloneInputModule standalone =
                eventSystem.GetComponent<
                    StandaloneInputModule
                >();

            if (standalone != null)
                standalone.enabled = false;

            return;
        }

        StandaloneInputModule oldInput =
            eventSystem.GetComponent<
                StandaloneInputModule
            >();

        if (oldInput == null)
        {
            oldInput =
                eventSystem.gameObject
                    .AddComponent<
                        StandaloneInputModule
                    >();
        }

        oldInput.enabled = true;
    }

    private static ScrollRect
        CreateScrollView(
            Transform parent,
            out RectTransform content)
    {
        RectTransform root =
            CreateRect(
                parent,
                "ScrollView"
            );

        Image rootImage =
            root.gameObject
                .AddComponent<Image>();

        rootImage.color =
            new Color(0f, 0f, 0f, 0f);

        rootImage.raycastTarget = true;

        ScrollRect scroll =
            root.gameObject
                .AddComponent<ScrollRect>();

        RectTransform viewport =
            CreateRect(
                root,
                "Viewport"
            );

        Stretch(viewport);

        /*
         * KHÔNG dùng Mask + Image rỗng ở đây.
         * Một số phiên bản Unity sẽ tạo stencil rỗng và cắt toàn bộ
         * card sản phẩm: button vẫn bấm được nhưng hình/text không hiện.
         *
         * RectMask2D chỉ cắt theo RectTransform nên ổn định hơn.
         */
        RectMask2D viewportMask =
            viewport.gameObject
                .AddComponent<RectMask2D>();

        viewportMask.padding =
            Vector4.zero;

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
            new Vector2(0f, 800f);

        GridLayoutGroup grid =
            content.gameObject
                .AddComponent<
                    GridLayoutGroup
                >();

        grid.cellSize =
            new Vector2(210f, 255f);

        grid.spacing =
            new Vector2(16f, 16f);

        grid.padding =
            new RectOffset(0, 0, 0, 16);

        grid.constraint =
            GridLayoutGroup.Constraint
                .FixedColumnCount;

        grid.constraintCount = 7;

        grid.childAlignment =
            TextAnchor.UpperLeft;

        ContentSizeFitter fitter =
            content.gameObject
                .AddComponent<
                    ContentSizeFitter
                >();

        fitter.horizontalFit =
            ContentSizeFitter.FitMode
                .Unconstrained;

        fitter.verticalFit =
            ContentSizeFitter.FitMode
                .PreferredSize;

        scroll.viewport = viewport;
        scroll.content = content;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType =
            ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 40f;

        return scroll;
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
                0.12f,
                0.12f,
                0.12f,
                1f
            );

        background.raycastTarget = true;

        TMP_InputField input =
            root.gameObject
                .AddComponent<
                    TMP_InputField
                >();

        RectTransform viewport =
            CreateRect(
                root,
                "Text Area"
            );

        viewport.anchorMin =
            Vector2.zero;

        viewport.anchorMax =
            Vector2.one;

        viewport.offsetMin =
            new Vector2(14f, 7f);

        viewport.offsetMax =
            new Vector2(-14f, -7f);

        viewport.gameObject
            .AddComponent<RectMask2D>();

        TMP_Text placeholderText =
            CreateText(
                viewport,
                "Placeholder",
                placeholder,
                16f,
                FontStyles.Italic,
                TextAlignmentOptions.Left,
                new Color(
                    0.55f,
                    0.55f,
                    0.55f,
                    1f
                )
            );

        Stretch(
            placeholderText
                .rectTransform
        );

        TMP_Text inputText =
            CreateText(
                viewport,
                "Text",
                string.Empty,
                17f,
                FontStyles.Normal,
                TextAlignmentOptions.Left,
                Color.white
            );

        Stretch(
            inputText.rectTransform
        );

        input.textViewport = viewport;
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

        image.raycastTarget = true;

        Button button =
            root.gameObject
                .AddComponent<Button>();

        button.targetGraphic = image;
        button.interactable = true;

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

        Stretch(
            text.rectTransform
        );

        return button;
    }

    private static TMP_Text
        CreateTextBox(
            Transform parent,
            string objectName,
            string value,
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

        image.raycastTarget = false;

        TMP_Text text =
            CreateText(
                box,
                objectName,
                value,
                15f,
                FontStyles.Bold,
                TextAlignmentOptions.Center,
                Color.white
            );

        Stretch(text.rectTransform);

        return text;
    }

    private static TMP_Text CreateText(
        Transform parent,
        string objectName,
        string value,
        float size,
        FontStyles style,
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

        text.text = value;
        text.fontSize = size;
        text.fontStyle = style;
        text.alignment = alignment;
        text.color = color;
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.Normal;

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
        Vector2 position,
        Vector2 size,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition =
            position;
        rect.sizeDelta = size;
    }

    private static MethodInfo FindNumberMethod(
        Type type,
        string name)
    {
        const BindingFlags flags =
            BindingFlags.Instance |
            BindingFlags.Public |
            BindingFlags.NonPublic |
            BindingFlags.IgnoreCase;

        foreach (MethodInfo method
                 in type.GetMethods(flags))
        {
            if (!string.Equals(
                    method.Name,
                    name,
                    StringComparison
                        .OrdinalIgnoreCase))
            {
                continue;
            }

            ParameterInfo[] parameters =
                method.GetParameters();

            if (parameters.Length != 1)
                continue;

            Type parameter =
                parameters[0]
                    .ParameterType;

            if (parameter == typeof(int) ||
                parameter == typeof(float) ||
                parameter == typeof(double) ||
                parameter == typeof(long))
            {
                return method;
            }
        }

        return null;
    }

    private static object ConvertNumber(
        int value,
        Type type)
    {
        if (type == typeof(float))
            return (float)value;

        if (type == typeof(double))
            return (double)value;

        if (type == typeof(long))
            return (long)value;

        return value;
    }

    private static object GetMemberValue(
        object source,
        Type type,
        string name)
    {
        const BindingFlags flags =
            BindingFlags.Instance |
            BindingFlags.Public |
            BindingFlags.NonPublic |
            BindingFlags.IgnoreCase;

        FieldInfo field =
            type.GetField(
                name,
                flags
            );

        if (field != null)
            return field.GetValue(source);

        PropertyInfo property =
            type.GetProperty(
                name,
                flags
            );

        if (property != null &&
            property.CanRead)
        {
            return property.GetValue(
                source
            );
        }

        return null;
    }

    private static string ReadStringMember(
        object source,
        params string[] names)
    {
        Type type =
            source.GetType();

        foreach (string name
                 in names)
        {
            object value =
                GetMemberValue(
                    source,
                    type,
                    name
                );

            if (value != null)
                return value.ToString();
        }

        return string.Empty;
    }

    private static string Normalize(
        string value)
    {
        return string.IsNullOrWhiteSpace(
                value)
            ? string.Empty
            : value.Trim()
                .ToLowerInvariant()
                .Replace(" ", string.Empty)
                .Replace("_", string.Empty)
                .Replace("-", string.Empty);
    }

    private bool WasOpenKeyPressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current == null)
            return false;

        string inputSystemKeyName =
            GetInputSystemKeyName(testOpenKey);

        return Enum.TryParse(
                   inputSystemKeyName,
                   true,
                   out Key inputSystemKey) &&
               inputSystemKey != Key.None &&
               Keyboard.current[inputSystemKey]
                   .wasPressedThisFrame;
#else
        return Input.GetKeyDown(
            testOpenKey
        );
#endif
    }

    private static bool WasEscapePressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null &&
               Keyboard.current
                   .escapeKey
                   .wasPressedThisFrame;
#else
        return Input.GetKeyDown(
            KeyCode.Escape
        );
#endif
    }

#if ENABLE_INPUT_SYSTEM
    private static string GetInputSystemKeyName(
        KeyCode keyCode)
    {
        string keyName = keyCode.ToString();

        if (keyName.StartsWith(
                "Alpha",
                StringComparison.Ordinal))
        {
            return "Digit" + keyName.Substring(5);
        }

        if (keyName.StartsWith(
                "Keypad",
                StringComparison.Ordinal))
        {
            return "Numpad" + keyName.Substring(6);
        }

        switch (keyCode)
        {
            case KeyCode.Return:
                return nameof(Key.Enter);
            case KeyCode.LeftControl:
                return nameof(Key.LeftCtrl);
            case KeyCode.RightControl:
                return nameof(Key.RightCtrl);
            default:
                return keyName;
        }
    }
#endif

    private void OnDisable()
    {
        RestoreCompetingRaycasters();
        UnlockPlayerForShop();
    }

    private void OnDestroy()
    {
        UnlockPlayerForShop();
    }
}

public class ToolSupplyShopStandaloneCard :
    MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler
{
    private CanvasGroup hoverGroup;

    public void Configure(
        CanvasGroup group)
    {
        hoverGroup = group;
        SetHover(false);
    }

    public void OnPointerEnter(
        PointerEventData eventData)
    {
        SetHover(true);
    }

    public void OnPointerExit(
        PointerEventData eventData)
    {
        SetHover(false);
    }

    private void SetHover(bool visible)
    {
        if (hoverGroup == null)
            return;

        hoverGroup.alpha =
            visible ? 1f : 0f;
    }
}

#pragma warning restore S3903
