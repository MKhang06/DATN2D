using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FishingBarMiniGameUI : MonoBehaviour
{
    public enum State
    {
        Hidden,
        SelectingDepth,
        WaitingBite,
        Reeling
    }

    [Header("Root")]
    [SerializeField] private GameObject root;

    [Header("Left Bar - Lực người chơi")]
    [SerializeField] private Image playerPullFill;

    [Header("Middle Bar - Chọn độ sâu")]
    [SerializeField] private RectTransform waterMaskRect;
    [SerializeField] private RectTransform greenZoneRect;
    [SerializeField] private RectTransform fishIconRect;
    [SerializeField] private Image fishIconImage;

    [Header("Right Bar - Lực cá kéo")]
    [SerializeField] private Image fishForceFill;

    [Header("Text")]
    [SerializeField] private TMP_Text depthText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text hintText;

    [Header("Depth Settings")]
    [SerializeField] private float depthMoveSpeed = 4f;
    [SerializeField] private float fishIconOffsetX = 0f;

    [Header("Power Settings")]
    [SerializeField] private float playerPullGainSpeed = 0.35f;
    [SerializeField] private float playerPullLoseSpeed = 0.04f;

    [SerializeField] private float fishForceGainSpeed = 0.25f;
    [SerializeField] private float fishForceRecoverSpeed = 0.35f;

    [SerializeField] private float fishRandomMin = 0.6f;
    [SerializeField] private float fishRandomMax = 1.5f;

    [Header("Fish Icon Shake")]
    [SerializeField] private float fishIconShakeAmount = 10f;
    [SerializeField] private float fishIconShakeSpeed = 6f;

    [Header("Presentation")]
    [SerializeField] private bool applyPolishedLayout = true;
    [SerializeField] private Vector2 panelSize = new Vector2(560f, 460f);
    [SerializeField, Range(1f, 20f)] private float transitionSpeed = 9f;
    [SerializeField] private Color playerPullColor = new Color(0.25f, 0.95f, 0.55f, 1f);
    [SerializeField] private Color fishSafeColor = new Color(1f, 0.72f, 0.18f, 1f);
    [SerializeField] private Color fishDangerColor = new Color(1f, 0.18f, 0.12f, 1f);

    private State state = State.Hidden;

    private float maxDepth = 10f;
    private float currentDepth = 5f;

    private float playerPull;
    private float fishForce;

    private float fishDifficulty = 1f;
    private float currentFishPull = 1f;
    private float nextPullChangeTime;

    private Action<float> onDepthSelected;
    private Action<bool> onReelFinished;

    private RectTransform rootRect;
    private RectTransform depthGroupRect;
    private Image backgroundImage;
    private Image greenZoneImage;
    private CanvasGroup rootCanvasGroup;
    private TMP_Text playerMeterLabel;
    private TMP_Text fishMeterLabel;
    private Image playerMeterTrack;
    private Image fishMeterTrack;
    private Image dangerMarker;
    private bool showingResult;
    private bool resultWasSuccess;
    private bool presentationReady;
    private bool battleMeterStateInitialized;
    private bool battleMetersVisible;

    private void Awake()
    {
        PreparePresentation();
        Hide();
    }

    private void OnEnable()
    {
        PreparePresentation();
    }

    private void Update()
    {
        if (state == State.SelectingDepth)
        {
            UpdateDepthSelect();
        }
        else if (state == State.Reeling)
        {
            UpdateReeling();
        }

        UpdateVisual();
        UpdatePresentationFeedback();
    }

    public void ShowDepthSelect(float maxDepthValue, Action<float> callback)
    {
        maxDepth = Mathf.Max(1f, maxDepthValue);
        currentDepth = maxDepth * 0.5f;

        playerPull = 0f;
        fishForce = 0f;

        onDepthSelected = callback;
        state = State.SelectingDepth;
        showingResult = false;

        ShowRoot();

        HideFishIcon();

        if (statusText != null)
            statusText.text = "CHỌN ĐỘ SÂU";

        if (hintText != null)
            hintText.text = "W/S hoặc ↑/↓ để chỉnh độ sâu - SPACE để thả móc";

        UpdateVisual();
    }

    private void UpdateDepthSelect()
    {
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            currentDepth -= depthMoveSpeed * Time.deltaTime;

        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            currentDepth += depthMoveSpeed * Time.deltaTime;

        currentDepth = Mathf.Clamp(currentDepth, 0f, maxDepth);

        if (Input.GetKeyDown(KeyCode.Space) ||
            Input.GetKeyDown(KeyCode.Return))
        {
            state = State.WaitingBite;

            if (statusText != null)
                statusText.text = "ĐÃ THẢ MÓC";

            if (hintText != null)
                hintText.text = "Đang chờ cá cắn ở độ sâu " +
                                currentDepth.ToString("0.0") + "m";

            Action<float> callback = onDepthSelected;
            onDepthSelected = null;
            callback?.Invoke(currentDepth);
        }
    }

    public void ShowWaitingBite()
    {
        state = State.WaitingBite;
        showingResult = false;

        ShowRoot();

        HideFishIcon();

        if (statusText != null)
            statusText.text = "ĐANG ĐỢI CÁ CẮN...";

        if (hintText != null)
            hintText.text = "Mồi đang ở độ sâu " +
                            currentDepth.ToString("0.0") + "m";

        UpdateVisual();
    }

    public void ShowFishBite(Sprite fishSprite)
    {
        if (fishIconRect != null)
            fishIconRect.gameObject.SetActive(true);

        if (fishIconImage != null && fishSprite != null)
            fishIconImage.sprite = fishSprite;

        UpdateFishIconPosition(false);

        if (statusText != null)
            statusText.text = "CÁ CẮN CÂU!";

        if (hintText != null)
            hintText.text = "Giữ SPACE để kéo cá - thả SPACE khi lực cá quá cao!";
    }

    public void ShowJunkBite(Sprite junkSprite)
    {
        if (fishIconRect != null)
            fishIconRect.gameObject.SetActive(true);

        if (fishIconImage != null && junkSprite != null)
            fishIconImage.sprite = junkSprite;

        UpdateFishIconPosition(false);

        if (statusText != null)
            statusText.text = "CÓ VẬT PHẨM LẠ!";

        if (hintText != null)
            hintText.text = "Bạn câu được vật phẩm lạ.";
    }

    public void StartReelGame(float fishDifficultyValue, Action<bool> callback)
    {
        fishDifficulty = Mathf.Max(0.1f, fishDifficultyValue);

        playerPull = 0f;
        fishForce = 0f;

        onReelFinished = callback;
        state = State.Reeling;
        showingResult = false;

        currentFishPull = 1f;
        nextPullChangeTime = Time.time + 0.5f;

        ShowRoot();

        if (statusText != null)
            statusText.text = "CÁ ĐANG KÉO!";

        if (hintText != null)
            hintText.text = "Giữ SPACE để kéo - thả SPACE khi lực cá quá cao";

        UpdateVisual();
    }

    private void UpdateReeling()
    {
        UpdateFishPull();

        bool holdingSpace = Input.GetKey(KeyCode.Space);

        if (holdingSpace)
        {
            playerPull += playerPullGainSpeed * Time.deltaTime;

            fishForce +=
                fishForceGainSpeed *
                fishDifficulty *
                currentFishPull *
                Time.deltaTime;
        }
        else
        {
            playerPull -= playerPullLoseSpeed * Time.deltaTime;
            fishForce -= fishForceRecoverSpeed * Time.deltaTime;
        }

        playerPull = Mathf.Clamp01(playerPull);
        fishForce = Mathf.Clamp01(fishForce);

        UpdateFishIconPosition(true);

        if (fishForce >= 1f)
        {
            FinishReel(false);
            return;
        }

        if (playerPull >= 1f)
        {
            FinishReel(true);
        }
    }

    private void UpdateFishPull()
    {
        if (Time.time < nextPullChangeTime)
            return;

        currentFishPull = UnityEngine.Random.Range(fishRandomMin, fishRandomMax);

        nextPullChangeTime =
            Time.time + UnityEngine.Random.Range(0.35f, 1f);
    }

    private void FinishReel(bool success)
    {
        state = State.Hidden;
        showingResult = true;
        resultWasSuccess = success;

        if (statusText != null)
            statusText.text = success ? "KÉO THÀNH CÔNG!" : "ĐỨT DÂY CÂU!";

        if (hintText != null)
            hintText.text = success ? "Bắt được cá!" : "Lực cá quá cao, dây bị đứt.";

        Action<bool> callback = onReelFinished;
        onReelFinished = null;
        callback?.Invoke(success);

        CancelInvoke(nameof(Hide));
        Invoke(nameof(Hide), 0.8f);
    }

    private void UpdateVisual()
    {
        if (playerPullFill != null)
            playerPullFill.fillAmount = playerPull;

        if (fishForceFill != null)
            fishForceFill.fillAmount = fishForce;

        UpdateGreenZonePosition();
        UpdateDepthText();
    }

    private void UpdateGreenZonePosition()
    {
        if (waterMaskRect == null || greenZoneRect == null)
            return;

        float y = GetGreenZoneY();

        greenZoneRect.anchoredPosition =
            new Vector2(0f, y);
    }

    private float GetGreenZoneY()
    {
        if (waterMaskRect == null || greenZoneRect == null)
            return 0f;

        float t = maxDepth <= 0f ? 0f : currentDepth / maxDepth;

        float waterHeight = waterMaskRect.rect.height;
        float greenHeight = greenZoneRect.rect.height;

        float topY = waterHeight * 0.5f - greenHeight * 0.5f;
        float bottomY = -waterHeight * 0.5f + greenHeight * 0.5f;

        return Mathf.Lerp(topY, bottomY, t);
    }

    private void UpdateFishIconPosition(bool shake)
    {
        if (fishIconRect == null || greenZoneRect == null)
            return;

        Vector3 greenWorldPosition =
            greenZoneRect.TransformPoint(Vector3.zero);

        Vector3 fishParentPosition =
            fishIconRect.parent != null
                ? fishIconRect.parent.InverseTransformPoint(
                    greenWorldPosition
                )
                : greenWorldPosition;

        float y = fishParentPosition.y;

        if (shake && state == State.Reeling)
        {
            y += Mathf.Sin(Time.time * fishIconShakeSpeed) *
                 fishIconShakeAmount;
        }

        fishIconRect.anchoredPosition =
            new Vector2(fishIconOffsetX, y);
    }

    private void UpdateDepthText()
    {
        if (depthText == null)
            return;

        depthText.text =
            currentDepth.ToString("0.0") +
            "m / " +
            maxDepth.ToString("0.0") +
            "m";
    }

    private void HideFishIcon()
    {
        if (fishIconRect != null)
            fishIconRect.gameObject.SetActive(false);
    }

    public void Hide()
    {
        CancelInvoke(nameof(Hide));
        state = State.Hidden;
        showingResult = false;
        onDepthSelected = null;
        onReelFinished = null;

        if (root != null)
            root.SetActive(false);

        HideFishIcon();
    }

    private void ShowRoot()
    {
        CancelInvoke(nameof(Hide));

        if (root == null)
            return;

        bool wasInactive = !root.activeSelf;
        root.SetActive(true);

        if (!presentationReady)
            PreparePresentation();

        if (!wasInactive || rootCanvasGroup == null || rootRect == null)
            return;

        rootCanvasGroup.alpha = 0f;
        rootRect.localScale = Vector3.one * 0.96f;
    }

    private void PreparePresentation()
    {
        if (presentationReady || !applyPolishedLayout)
            return;

        if (root == null)
            root = gameObject;

        rootRect = root.GetComponent<RectTransform>();

        if (rootRect == null)
            return;

        ConfigureCanvas();

        rootCanvasGroup = root.GetComponent<CanvasGroup>();
        if (rootCanvasGroup == null)
            rootCanvasGroup = root.AddComponent<CanvasGroup>();

        rootCanvasGroup.interactable = false;
        rootCanvasGroup.blocksRaycasts = false;

        ConfigureRect(rootRect, Vector2.zero, panelSize);

        backgroundImage = FindImage("Background");
        if (backgroundImage != null)
        {
            ConfigureRect(
                backgroundImage.rectTransform,
                Vector2.zero,
                new Vector2(panelSize.x, panelSize.y - 20f)
            );
            backgroundImage.color = new Color(0.72f, 0.9f, 0.92f, 0.98f);
            backgroundImage.raycastTarget = false;
            AddPanelEffects(backgroundImage.gameObject);
        }

        depthGroupRect = FindRect("DepthBar");
        RectTransform frameRect = FindRect("Frame");

        /*
         * Scene cũ gán nhầm WaterBar vào field waterMaskRect, làm cả cụm
         * nước bị giữ ở tọa độ âm và văng sang mép trái màn hình.
         */
        RectTransform actualWaterMask = FindRect("WaterMask");
        if (actualWaterMask != null)
            waterMaskRect = actualWaterMask;

        if (depthGroupRect != null)
            ConfigureRect(depthGroupRect, new Vector2(0f, -12f), new Vector2(320f, 286f));

        if (frameRect != null)
        {
            ConfigureRect(frameRect, Vector2.zero, new Vector2(82f, 286f));
            Image frameImage = frameRect.GetComponent<Image>();
            if (frameImage != null)
                frameImage.raycastTarget = false;
        }

        if (waterMaskRect != null)
        {
            ConfigureRect(waterMaskRect, Vector2.zero, new Vector2(60f, 258f));

            Image maskGraphic = waterMaskRect.GetComponent<Image>();
            if (maskGraphic != null)
            {
                maskGraphic.raycastTarget = false;
                maskGraphic.enabled = false;
            }
        }

        RectTransform waterBarRect = FindRect("WaterBar");
        if (waterBarRect != null)
        {
            ConfigureRect(waterBarRect, Vector2.zero, new Vector2(60f, 258f));
            Image waterImage = waterBarRect.GetComponent<Image>();
            if (waterImage != null)
            {
                waterImage.preserveAspect = false;
                waterImage.raycastTarget = false;
            }
        }

        if (greenZoneRect != null)
            ConfigureRect(greenZoneRect, Vector2.zero, new Vector2(56f, 62f));

        greenZoneImage =
            greenZoneRect != null
                ? greenZoneRect.GetComponent<Image>()
                : null;

        if (fishIconRect != null && depthGroupRect != null)
        {
            fishIconRect.SetParent(depthGroupRect, false);
            ConfigureRect(fishIconRect, new Vector2(74f, 0f), new Vector2(62f, 62f));
            fishIconOffsetX = 74f;
        }

        ConfigurePowerMeter(
            playerPullFill,
            "Player Meter Track",
            new Vector2(-108f, 0f),
            playerPullColor,
            out playerMeterTrack
        );
        ConfigurePowerMeter(
            fishForceFill,
            "Fish Meter Track",
            new Vector2(108f, 0f),
            fishSafeColor,
            out fishMeterTrack
        );

        if (depthGroupRect != null)
        {
            playerMeterLabel = CreateLabel(
                depthGroupRect,
                "Player Meter Label",
                "LỰC KÉO",
                new Vector2(-108f, 153f),
                new Vector2(108f, 24f),
                playerPullColor
            );
            fishMeterLabel = CreateLabel(
                depthGroupRect,
                "Fish Meter Label",
                "SỨC CÁ",
                new Vector2(108f, 153f),
                new Vector2(108f, 24f),
                fishSafeColor
            );
            dangerMarker = CreateImage(
                depthGroupRect,
                "Danger Marker",
                new Vector2(108f, 64f),
                new Vector2(46f, 3f),
                new Color(1f, 0.84f, 0.32f, 0.95f)
            );
        }

        ConfigureText(
            statusText,
            new Vector2(0f, 190f),
            new Vector2(500f, 44f),
            28f,
            new Color(1f, 0.84f, 0.34f, 1f)
        );
        ConfigureText(
            depthText,
            new Vector2(0f, 148f),
            new Vector2(230f, 32f),
            20f,
            Color.white
        );
        ConfigureText(
            hintText,
            new Vector2(0f, -192f),
            new Vector2(510f, 54f),
            16f,
            new Color(0.84f, 0.94f, 0.96f, 1f)
        );

        presentationReady = true;
        RefreshBattleMeterVisibility(true);
        UpdateGreenZonePosition();
        UpdateFishIconPosition(false);
    }

    private void ConfigureCanvas()
    {
        Canvas canvas = root.GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            canvas.overrideSorting = true;
            canvas.sortingOrder = 80;
        }

        CanvasScaler scaler = root.GetComponentInParent<CanvasScaler>();
        if (scaler == null)
            return;

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
    }

    private void ConfigurePowerMeter(
        Image fill,
        string trackName,
        Vector2 position,
        Color color,
        out Image track)
    {
        track = null;

        if (fill == null || depthGroupRect == null)
            return;

        fill.rectTransform.SetParent(depthGroupRect, false);
        ConfigureRect(fill.rectTransform, position, new Vector2(28f, 255f));
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Vertical;
        fill.fillOrigin = 0;
        fill.fillClockwise = true;
        fill.color = color;
        fill.raycastTarget = false;

        track = CreateImage(
            depthGroupRect,
            trackName,
            position,
            new Vector2(42f, 267f),
            new Color(0.025f, 0.07f, 0.085f, 0.94f)
        );

        if (track != null)
        {
            int fillIndex = fill.transform.GetSiblingIndex();
            track.transform.SetSiblingIndex(fillIndex);
            fill.transform.SetSiblingIndex(fillIndex + 1);

            Outline outline = track.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.42f, 0.8f, 0.82f, 0.75f);
            outline.effectDistance = new Vector2(2f, -2f);
            outline.useGraphicAlpha = true;
        }
    }

    private void UpdatePresentationFeedback()
    {
        if (!presentationReady)
            return;

        RefreshBattleMeterVisibility(false);

        if (rootCanvasGroup != null)
        {
            rootCanvasGroup.alpha = Mathf.MoveTowards(
                rootCanvasGroup.alpha,
                1f,
                transitionSpeed * Time.unscaledDeltaTime
            );
        }

        if (rootRect != null)
        {
            rootRect.localScale = Vector3.Lerp(
                rootRect.localScale,
                Vector3.one,
                transitionSpeed * Time.unscaledDeltaTime
            );
        }

        float danger = Mathf.InverseLerp(0.52f, 1f, fishForce);
        float pulse = 0.84f + Mathf.Sin(Time.unscaledTime * 9f) * 0.16f * danger;
        Color fishColor = Color.Lerp(fishSafeColor, fishDangerColor, danger);
        fishColor *= pulse;
        fishColor.a = 1f;

        if (playerPullFill != null)
            playerPullFill.color = playerPullColor;

        if (fishForceFill != null)
            fishForceFill.color = fishColor;

        if (fishMeterLabel != null)
            fishMeterLabel.color = Color.Lerp(fishSafeColor, fishDangerColor, danger);

        if (dangerMarker != null)
            dangerMarker.color = new Color(1f, 0.76f - danger * 0.4f, 0.18f, 0.95f);

        if (greenZoneImage != null)
        {
            float zonePulse = 0.82f + Mathf.Sin(Time.unscaledTime * 5f) * 0.1f;
            greenZoneImage.color = new Color(0.68f, 1f, 0.55f, zonePulse);
        }

        if (statusText != null)
        {
            if (showingResult)
            {
                statusText.color = resultWasSuccess
                    ? new Color(0.42f, 1f, 0.55f, 1f)
                    : fishDangerColor;
            }
            else if (state == State.Reeling && danger > 0.72f)
            {
                statusText.color = fishDangerColor;
            }
            else
            {
                statusText.color = new Color(1f, 0.84f, 0.34f, 1f);
            }
        }
    }

    private void RefreshBattleMeterVisibility(bool force)
    {
        bool shouldShow = state == State.Reeling || showingResult;

        if (!force &&
            battleMeterStateInitialized &&
            battleMetersVisible == shouldShow)
        {
            return;
        }

        battleMeterStateInitialized = true;
        battleMetersVisible = shouldShow;

        SetActive(playerPullFill, shouldShow);
        SetActive(fishForceFill, shouldShow);
        SetActive(playerMeterTrack, shouldShow);
        SetActive(fishMeterTrack, shouldShow);
        SetActive(playerMeterLabel, shouldShow);
        SetActive(fishMeterLabel, shouldShow);
        SetActive(dangerMarker, shouldShow);

        if (depthText != null)
            depthText.gameObject.SetActive(!shouldShow);
    }

    private static void SetActive(Component component, bool active)
    {
        if (component != null && component.gameObject.activeSelf != active)
            component.gameObject.SetActive(active);
    }

    private TMP_Text CreateLabel(
        RectTransform parent,
        string objectName,
        string value,
        Vector2 position,
        Vector2 size,
        Color color)
    {
        GameObject labelObject = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(TextMeshProUGUI)
        );
        labelObject.layer = root.layer;
        labelObject.transform.SetParent(parent, false);

        TMP_Text label = labelObject.GetComponent<TMP_Text>();
        label.text = value;
        label.font = statusText != null ? statusText.font : null;
        label.fontSize = 14f;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.color = color;
        label.raycastTarget = false;

        ConfigureRect(label.rectTransform, position, size);
        return label;
    }

    private Image CreateImage(
        RectTransform parent,
        string objectName,
        Vector2 position,
        Vector2 size,
        Color color)
    {
        GameObject imageObject = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(Image)
        );
        imageObject.layer = root.layer;
        imageObject.transform.SetParent(parent, false);

        Image image = imageObject.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        ConfigureRect(image.rectTransform, position, size);
        return image;
    }

    private void ConfigureText(
        TMP_Text text,
        Vector2 position,
        Vector2 size,
        float fontSize,
        Color color)
    {
        if (text == null)
            return;

        ConfigureRect(text.rectTransform, position, size);
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = fontSize;
        text.enableAutoSizing = true;
        text.fontSizeMin = Mathf.Max(12f, fontSize - 7f);
        text.fontSizeMax = fontSize;
        text.color = color;
        text.raycastTarget = false;
        text.overflowMode = TextOverflowModes.Ellipsis;
    }

    private static void ConfigureRect(
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
        rect.localRotation = Quaternion.identity;
        rect.localScale = Vector3.one;
    }

    private void AddPanelEffects(GameObject panel)
    {
        Shadow shadow = panel.GetComponent<Shadow>();
        if (shadow == null)
            shadow = panel.AddComponent<Shadow>();

        shadow.effectColor = new Color(0f, 0.04f, 0.05f, 0.78f);
        shadow.effectDistance = new Vector2(8f, -10f);
        shadow.useGraphicAlpha = true;

        Outline outline = panel.GetComponent<Outline>();
        if (outline == null)
            outline = panel.AddComponent<Outline>();

        outline.effectColor = new Color(0.3f, 0.78f, 0.8f, 0.7f);
        outline.effectDistance = new Vector2(2f, -2f);
        outline.useGraphicAlpha = true;
    }

    private RectTransform FindRect(string objectName)
    {
        Transform target = FindChild(objectName);
        return target != null ? target.GetComponent<RectTransform>() : null;
    }

    private Image FindImage(string objectName)
    {
        Transform target = FindChild(objectName);
        return target != null ? target.GetComponent<Image>() : null;
    }

    private Transform FindChild(string objectName)
    {
        if (root == null)
            return null;

        Transform[] children = root.GetComponentsInChildren<Transform>(true);
        foreach (Transform child in children)
        {
            if (child != null && child.name == objectName)
                return child;
        }

        return null;
    }
}
