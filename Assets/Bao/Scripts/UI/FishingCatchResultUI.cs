using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FishingCatchResultUI : MonoBehaviour
{
    private static readonly Color GoldColor = new Color32(255, 211, 91, 255);
    private static readonly Color CyanColor = new Color32(119, 230, 244, 255);

    [Header("Root")]
    [SerializeField] private GameObject root;

    [Header("Presentation")]
    [SerializeField] private bool applyCompactLayout = true;
    [SerializeField] private Vector2 popupSize = new Vector2(920f, 620f);
    [SerializeField, Min(1f)] private float fadeSpeed = 8f;

    [Header("Texts")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text infoTitleText;
    [SerializeField] private TMP_Text infoText;

    [Header("Item Image")]
    [SerializeField] private Image itemImage;

    [Header("Buttons")]
    [SerializeField] private Button storeButton;
    [SerializeField] private Button releaseButton;
    [SerializeField] private Button sellButton;

    [Header("Button Texts")]
    [SerializeField] private TMP_Text storeMainText;
    [SerializeField] private TMP_Text releaseMainText;
    [SerializeField] private TMP_Text releaseSubText;
    [SerializeField] private TMP_Text sellMainText;
    [SerializeField] private TMP_Text sellSubText;

    [Header("Âm thanh câu cá")]
    [Tooltip("Kéo AudioSource đang phát tiếng kéo cần vào đây.")]
    [SerializeField] private AudioSource reelAudioSource;

    [Tooltip("Các AudioSource câu cá khác cần dừng khi kết thúc.")]
    [SerializeField] private AudioSource[] additionalFishingAudioSources;

    private string itemName;
    private Sprite itemSprite;
    private bool isFish;
    private float weightKg;
    private float expReward;
    private int sellPrice;

    private PlayerStats playerStats;
    private InventoryManager inventoryManager;
    private Action onClosed;

    private bool resultActive;
    private bool processingAction;

    private CanvasGroup rootCanvasGroup;
    private bool presentationPrepared;

    private void Awake()
    {
        PreparePresentation();
        Hide();

        if (storeButton != null)
        {
            storeButton.onClick.RemoveListener(StoreItem);
            storeButton.onClick.AddListener(StoreItem);
        }

        if (releaseButton != null)
        {
            releaseButton.onClick.RemoveListener(ReleaseItem);
            releaseButton.onClick.AddListener(ReleaseItem);
        }

        if (sellButton != null)
        {
            sellButton.onClick.RemoveListener(SellItem);
            sellButton.onClick.AddListener(SellItem);
        }
    }

    private void Update()
    {
        if (!resultActive || rootCanvasGroup == null ||
            root == null || !root.activeSelf)
        {
            return;
        }

        rootCanvasGroup.alpha = Mathf.MoveTowards(
            rootCanvasGroup.alpha,
            1f,
            fadeSpeed * Time.unscaledDeltaTime
        );

        bool readyForInput = rootCanvasGroup.alpha >= 0.85f;
        rootCanvasGroup.interactable = readyForInput;
        rootCanvasGroup.blocksRaycasts = true;
    }

    public void ShowResult(
        string resultItemName,
        Sprite resultSprite,
        bool resultIsFish,
        float resultWeightKg,
        float resultExpReward,
        int resultSellPrice,
        PlayerStats stats,
        InventoryManager inventory,
        Action closeCallback)
    {
        itemName = string.IsNullOrWhiteSpace(resultItemName)
            ? "Vật phẩm"
            : resultItemName;

        itemSprite = resultSprite;
        isFish = resultIsFish;
        weightKg = Mathf.Max(0f, resultWeightKg);
        expReward = Mathf.Max(0f, resultExpReward);
        sellPrice = Mathf.Max(0, resultSellPrice);

        playerStats = stats;
        inventoryManager = inventory;
        onClosed = closeCallback;

        resultActive = true;
        processingAction = false;

        PreparePresentation();
        SetButtonsInteractable(true);

        if (root != null)
        {
            root.SetActive(true);

            if (rootCanvasGroup != null)
            {
                rootCanvasGroup.alpha = 0f;
                rootCanvasGroup.interactable = false;
                rootCanvasGroup.blocksRaycasts = true;
            }
        }

        UpdateUI();
        CursorManager.EnsureCursorAvailable();
    }

    private void UpdateUI()
    {
        if (titleText != null)
        {
            titleText.text = isFish
                ? itemName.ToUpper()
                : "VẬT PHẨM LẠ";
        }

        if (infoTitleText != null)
            infoTitleText.text = isFish
                ? "THÔNG TIN MẺ CÂU"
                : "VẬT PHẨM TÌM THẤY";

        if (itemImage != null)
        {
            itemImage.sprite = itemSprite;
            itemImage.enabled = itemSprite != null;
            itemImage.preserveAspect = true;
        }

        if (infoText != null)
        {
            if (isFish)
            {
                infoText.text =
                    "<color=#77E6F4>TRỌNG LƯỢNG</color>\n" +
                    "<size=145%><b>" + weightKg.ToString("0.##") +
                    " kg</b></size>\n\n" +
                    "Giá bán  <color=#FFD35B><b>$" +
                    sellPrice.ToString("N0") + "</b></color>\n" +
                    "Kinh nghiệm khi thả  <color=#70EE91><b>+" +
                    Mathf.RoundToInt(expReward) + " EXP</b></color>\n\n" +
                    "Chọn cách xử lý bên dưới.";
            }
            else
            {
                infoText.text =
                    "<color=#77E6F4>VẬT PHẨM</color>\n" +
                    "<size=130%><b>" + itemName + "</b></size>\n\n" +
                    "Giá bán  <color=#FFD35B><b>$" +
                    sellPrice.ToString("N0") + "</b></color>\n\n" +
                    "Có thể cất vào túi, vứt bỏ hoặc bán ngay.";
            }
        }

        if (storeMainText != null)
            storeMainText.text = "CẤT VÀO TÚI";

        if (releaseMainText != null)
        {
            releaseMainText.text = isFish
                ? "THẢ RA"
                : "VỨT BỎ";
        }

        if (releaseSubText != null)
        {
            releaseSubText.text = isFish && expReward > 0f
                ? "+" + Mathf.RoundToInt(expReward) + " EXP"
                : string.Empty;
        }

        if (sellMainText != null)
            sellMainText.text = "BÁN NGAY";

        if (sellSubText != null)
        {
            sellSubText.text =
                "<color=#63FF65>$" +
                sellPrice.ToString("N0") +
                "</color>";
        }
    }

    private void StoreItem()
    {
        if (!CanProcessAction())
            return;

        processingAction = true;
        SetButtonsInteractable(false);

        FindReferences();

        if (inventoryManager == null)
        {
            Debug.LogWarning(
                "Không tìm thấy InventoryManager."
            );

            ShowWarning(
                "Không tìm thấy túi đồ của người chơi."
            );

            CancelProcessing();
            return;
        }

        bool added = inventoryManager.AddItem(
            itemName,
            itemSprite,
            1
        );

        if (!added)
        {
            Debug.LogWarning(
                "Balo đã đầy, không thể cất: " +
                itemName
            );

            if (FishingNotificationUI.Instance != null)
            {
                FishingNotificationUI.Instance
                    .ShowInventoryFull();
            }

            CancelProcessing();
            return;
        }

        StopFishingAudio();

        if (FishingNotificationUI.Instance != null)
        {
            if (isFish)
            {
                FishingNotificationUI.Instance
                    .ShowFishStored(itemName);
            }
            else
            {
                FishingNotificationUI.Instance.ShowNotification(
                    "Bạn đã cất " +
                    itemName +
                    " vào balo thành công.",
                    FishingNotificationUI.NotificationType.Success
                );
            }
        }

        Debug.Log(
            "Đã cất vào túi: " +
            itemName
        );

        Close();
    }

    private void ReleaseItem()
    {
        if (!CanProcessAction())
            return;

        processingAction = true;
        SetButtonsInteractable(false);

        StopFishingAudio();

        if (isFish)
        {
            int receivedExperience =
                Mathf.Max(0, Mathf.RoundToInt(expReward));

            if (playerStats != null &&
                receivedExperience > 0)
            {
                playerStats.AddXP(receivedExperience);
            }

            if (FishingNotificationUI.Instance != null)
            {
                string message =
                    "Bạn đã thả " +
                    itemName +
                    " trở lại môi trường.";

                if (receivedExperience > 0)
                {
                    message +=
                        " Bạn nhận được <color=#63FF65>+" +
                        receivedExperience +
                        " EXP</color>.";
                }

                FishingNotificationUI.Instance.ShowNotification(
                    message,
                    FishingNotificationUI.NotificationType.Success
                );
            }

            Debug.Log(
                "Đã thả cá và nhận EXP: " +
                receivedExperience
            );
        }
        else
        {
            if (FishingNotificationUI.Instance != null)
            {
                FishingNotificationUI.Instance
                    .ShowFishDiscarded(itemName);
            }

            Debug.Log(
                "Đã vứt bỏ vật phẩm: " +
                itemName
            );
        }

        Close();
    }

    private void SellItem()
    {
        if (!CanProcessAction())
            return;

        processingAction = true;
        SetButtonsInteractable(false);

        FindReferences();

        if (playerStats == null)
        {
            Debug.LogWarning(
                "Không tìm thấy PlayerStats để cộng tiền mặt."
            );

            ShowWarning(
                "Không tìm thấy dữ liệu tiền mặt của người chơi."
            );

            CancelProcessing();
            return;
        }

        int cashReceived = Mathf.Max(0, sellPrice);

        // Cộng trực tiếp vào tiền mặt.
        playerStats.AddMoney(cashReceived);

        if (isFish)
            GreenFieldQuestEvents.ReportFishSold();

        StopFishingAudio();

        if (FishingNotificationUI.Instance != null)
        {
            FishingNotificationUI.Instance.ShowFishSold(
                itemName,
                cashReceived
            );
        }

        Debug.Log(
            "Đã bán " +
            itemName +
            " và nhận tiền mặt $" +
            cashReceived.ToString("N0")
        );

        Close();
    }

    private void FindReferences()
    {
        if (inventoryManager == null)
            inventoryManager = InventoryManager.Instance;

        if (inventoryManager == null)
        {
            inventoryManager =
                FindFirstObjectByType<InventoryManager>();
        }

        if (playerStats == null)
        {
            playerStats =
                FindFirstObjectByType<PlayerStats>();
        }
    }

    private bool CanProcessAction()
    {
        return resultActive && !processingAction;
    }

    private void CancelProcessing()
    {
        processingAction = false;
        SetButtonsInteractable(true);
    }

    private void SetButtonsInteractable(bool interactable)
    {
        if (storeButton != null)
            storeButton.interactable = interactable;

        if (releaseButton != null)
            releaseButton.interactable = interactable;

        if (sellButton != null)
            sellButton.interactable = interactable;
    }

    public void StopFishingAudio()
    {
        StopAudioSource(reelAudioSource);

        if (additionalFishingAudioSources == null)
            return;

        foreach (AudioSource source
                 in additionalFishingAudioSources)
        {
            StopAudioSource(source);
        }
    }

    private static void StopAudioSource(AudioSource source)
    {
        if (source == null)
            return;

        source.Stop();
        source.loop = false;
    }

    private void ShowWarning(string message)
    {
        if (FishingNotificationUI.Instance == null)
            return;

        FishingNotificationUI.Instance.ShowNotification(
            message,
            FishingNotificationUI.NotificationType.Warning
        );
    }

    private void Close()
    {
        if (!resultActive)
            return;

        resultActive = false;
        processingAction = false;

        StopFishingAudio();
        Hide();

        Action callback = onClosed;
        onClosed = null;

        callback?.Invoke();
    }

    public void Hide()
    {
        resultActive = false;
        processingAction = false;
        SetButtonsInteractable(true);

        if (rootCanvasGroup != null)
        {
            rootCanvasGroup.alpha = 0f;
            rootCanvasGroup.interactable = false;
            rootCanvasGroup.blocksRaycasts = false;
        }

        if (root != null)
            root.SetActive(false);
    }

    private void PreparePresentation()
    {
        if (presentationPrepared || root == null)
            return;

        presentationPrepared = true;

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            canvas.overrideSorting = true;
            canvas.sortingOrder = Mathf.Max(canvas.sortingOrder, 90);

            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;
            }
        }

        rootCanvasGroup = GetOrAddComponent<CanvasGroup>(root);
        rootCanvasGroup.alpha = 0f;
        rootCanvasGroup.interactable = false;
        rootCanvasGroup.blocksRaycasts = false;

        if (!applyCompactLayout)
            return;

        RectTransform hostRect = transform as RectTransform;
        StretchToParent(hostRect);

        RectTransform rootRect = root.GetComponent<RectTransform>();
        StretchToParent(rootRect);

        Transform overlayTransform = root.transform.Find("BackgroundPanel");
        Transform panelTransform = root.transform.Find("Background");

        Image overlay = overlayTransform != null
            ? overlayTransform.GetComponent<Image>()
            : null;
        Image panel = panelTransform != null
            ? panelTransform.GetComponent<Image>()
            : null;

        if (overlayTransform != null)
        {
            StretchToParent(overlayTransform as RectTransform);
            overlayTransform.SetSiblingIndex(0);
        }

        if (overlay != null)
        {
            overlay.sprite = null;
            overlay.color = new Color(0.015f, 0.045f, 0.07f, 0.78f);
            overlay.raycastTarget = true;
        }

        if (panelTransform != null)
        {
            SetCenteredRect(panelTransform as RectTransform, Vector2.zero, popupSize);
            panelTransform.SetSiblingIndex(1);
        }

        if (panel != null)
        {
            panel.color = new Color(0.72f, 0.94f, 1f, 0.98f);
            panel.raycastTarget = true;
            AddPanelEffects(panel.gameObject);
        }

        CreateCard(
            "ItemCard",
            new Vector2(-225f, 25f),
            new Vector2(330f, 300f),
            panel
        );
        CreateCard(
            "InfoCard",
            new Vector2(205f, 25f),
            new Vector2(420f, 300f),
            panel
        );

        ConfigureTexts();
        ConfigureItemImage();
        ConfigureButtons();
    }

    private void ConfigureTexts()
    {
        ConfigureText(
            titleText,
            new Vector2(0f, 250f),
            new Vector2(790f, 64f),
            44f,
            GoldColor,
            TextAlignmentOptions.Center,
            FontStyles.Bold
        );

        ConfigureText(
            infoTitleText,
            new Vector2(205f, 145f),
            new Vector2(360f, 42f),
            25f,
            CyanColor,
            TextAlignmentOptions.Center,
            FontStyles.Bold
        );

        ConfigureText(
            infoText,
            new Vector2(205f, 4f),
            new Vector2(360f, 220f),
            22f,
            Color.white,
            TextAlignmentOptions.Center,
            FontStyles.Normal
        );

        if (infoText != null)
        {
            infoText.richText = true;
            infoText.textWrappingMode = TextWrappingModes.Normal;
            infoText.overflowMode = TextOverflowModes.Ellipsis;
        }
    }

    private void ConfigureItemImage()
    {
        if (itemImage == null)
            return;

        SetCenteredRect(
            itemImage.rectTransform,
            new Vector2(-225f, 25f),
            new Vector2(270f, 235f)
        );
        itemImage.preserveAspect = true;
        itemImage.raycastTarget = false;
        itemImage.transform.SetAsLastSibling();

        if (titleText != null)
            titleText.transform.SetAsLastSibling();
        if (infoTitleText != null)
            infoTitleText.transform.SetAsLastSibling();
        if (infoText != null)
            infoText.transform.SetAsLastSibling();
    }

    private void ConfigureButtons()
    {
        ConfigureButton(
            storeButton,
            storeMainText,
            null,
            new Vector2(-280f, -230f),
            new Color32(112, 224, 181, 255)
        );
        ConfigureButton(
            releaseButton,
            releaseMainText,
            releaseSubText,
            new Vector2(0f, -230f),
            new Color32(112, 207, 244, 255)
        );
        ConfigureButton(
            sellButton,
            sellMainText,
            sellSubText,
            new Vector2(280f, -230f),
            GoldColor
        );

        if (storeButton != null && releaseButton != null && sellButton != null)
        {
            Navigation storeNavigation = storeButton.navigation;
            storeNavigation.mode = Navigation.Mode.Explicit;
            storeNavigation.selectOnRight = releaseButton;
            storeButton.navigation = storeNavigation;

            Navigation releaseNavigation = releaseButton.navigation;
            releaseNavigation.mode = Navigation.Mode.Explicit;
            releaseNavigation.selectOnLeft = storeButton;
            releaseNavigation.selectOnRight = sellButton;
            releaseButton.navigation = releaseNavigation;

            Navigation sellNavigation = sellButton.navigation;
            sellNavigation.mode = Navigation.Mode.Explicit;
            sellNavigation.selectOnLeft = releaseButton;
            sellButton.navigation = sellNavigation;
        }
    }

    private void ConfigureButton(
        Button button,
        TMP_Text mainText,
        TMP_Text subText,
        Vector2 position,
        Color accentColor)
    {
        if (button == null)
            return;

        RectTransform buttonRect = button.GetComponent<RectTransform>();
        SetCenteredRect(buttonRect, position, new Vector2(250f, 88f));
        button.transform.SetAsLastSibling();

        Transform backgroundTransform = button.transform.Find("ButtonBG");
        Image background = backgroundTransform != null
            ? backgroundTransform.GetComponent<Image>()
            : null;

        if (backgroundTransform != null)
            SetCenteredRect(backgroundTransform as RectTransform, Vector2.zero, new Vector2(250f, 88f));

        if (background != null)
        {
            background.color = accentColor;
            background.raycastTarget = true;
            button.targetGraphic = background;
        }

        Transform iconTransform = button.transform.Find("Icon");
        if (iconTransform != null)
        {
            SetCenteredRect(
                iconTransform as RectTransform,
                new Vector2(-87f, 0f),
                new Vector2(56f, 56f)
            );

            Image icon = iconTransform.GetComponent<Image>();
            if (icon != null)
            {
                icon.preserveAspect = true;
                icon.raycastTarget = false;
            }
        }

        bool hasSubText = subText != null;
        ConfigureText(
            mainText,
            new Vector2(28f, hasSubText ? 13f : 0f),
            new Vector2(160f, 34f),
            hasSubText ? 22f : 20f,
            new Color32(20, 43, 48, 255),
            TextAlignmentOptions.Center,
            FontStyles.Bold
        );

        if (subText != null)
        {
            ConfigureText(
                subText,
                new Vector2(28f, -18f),
                new Vector2(160f, 28f),
                17f,
                new Color32(19, 74, 54, 255),
                TextAlignmentOptions.Center,
                FontStyles.Bold
            );
            subText.richText = true;
            subText.textWrappingMode = TextWrappingModes.NoWrap;
        }

        if (mainText != null)
            mainText.textWrappingMode = TextWrappingModes.NoWrap;

        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.12f, 1.12f, 1.12f, 1f);
        colors.pressedColor = new Color(0.78f, 0.78f, 0.78f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.8f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.08f;
        button.colors = colors;
        button.transition = Selectable.Transition.ColorTint;
    }

    private Image CreateCard(
        string objectName,
        Vector2 position,
        Vector2 size,
        Image styleSource)
    {
        Transform existing = root.transform.Find(objectName);
        GameObject cardObject;

        if (existing != null)
        {
            cardObject = existing.gameObject;
        }
        else
        {
            cardObject = new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image)
            );
            cardObject.transform.SetParent(root.transform, false);
        }

        Image card = cardObject.GetComponent<Image>();
        RectTransform cardRect = cardObject.GetComponent<RectTransform>();
        SetCenteredRect(cardRect, position, size);

        card.sprite = styleSource != null ? styleSource.sprite : null;
        card.type = styleSource != null ? styleSource.type : Image.Type.Simple;
        card.color = new Color(0.03f, 0.15f, 0.19f, 0.84f);
        card.raycastTarget = false;

        Outline outline = GetOrAddComponent<Outline>(cardObject);
        outline.effectColor = new Color(0.3f, 0.85f, 0.9f, 0.42f);
        outline.effectDistance = new Vector2(1.5f, -1.5f);
        outline.useGraphicAlpha = true;

        cardObject.transform.SetSiblingIndex(Mathf.Min(2, root.transform.childCount - 1));
        return card;
    }

    private static void AddPanelEffects(GameObject panelObject)
    {
        Shadow shadow = GetOrAddComponent<Shadow>(panelObject);
        shadow.effectColor = new Color(0f, 0f, 0f, 0.68f);
        shadow.effectDistance = new Vector2(12f, -12f);
        shadow.useGraphicAlpha = true;

        Outline outline = GetOrAddComponent<Outline>(panelObject);
        outline.effectColor = new Color(0.28f, 0.84f, 0.88f, 0.5f);
        outline.effectDistance = new Vector2(2f, -2f);
        outline.useGraphicAlpha = true;
    }

    private static void ConfigureText(
        TMP_Text text,
        Vector2 position,
        Vector2 size,
        float fontSize,
        Color color,
        TextAlignmentOptions alignment,
        FontStyles style)
    {
        if (text == null)
            return;

        SetCenteredRect(text.rectTransform, position, size);
        text.fontSize = fontSize;
        text.enableAutoSizing = false;
        text.color = color;
        text.alignment = alignment;
        text.fontStyle = style;
        text.margin = Vector4.zero;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.raycastTarget = false;
    }

    private static void SetCenteredRect(
        RectTransform rect,
        Vector2 position,
        Vector2 size)
    {
        if (rect == null)
            return;

        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
    }

    private static void StretchToParent(RectTransform rect)
    {
        if (rect == null)
            return;

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
    }

    private static T GetOrAddComponent<T>(GameObject target)
        where T : Component
    {
        T component = target.GetComponent<T>();
        return component != null ? component : target.AddComponent<T>();
    }

    private void OnDisable()
    {
        StopFishingAudio();
    }
}
