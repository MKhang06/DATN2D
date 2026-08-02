using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[DisallowMultipleComponent]
public class WoodChopMinigameUI : MonoBehaviour
{
    public static WoodChopMinigameUI Instance { get; private set; }

    private enum PromptKey { W, A, S, D }

    [Serializable]
    private class LaneState
    {
        public PromptKey key;
        public float progress;
        public float speed;
        public float zoneCenter;
        public float zoneHalfWidth;
        public float flashTimer;
        public bool successFlash;
    }

    private sealed class LaneView
    {
        public RectTransform row;
        public Image rowBackground;
        public TMP_Text keyText;
        public RectTransform bar;
        public RectTransform successZone;
        public RectTransform marker;
        public TMP_Text feedbackText;
    }

    [Header("Gameplay")]
    [SerializeField, Min(1)] private int laneCount = 2;
    [SerializeField] private Vector2 markerSpeedRange = new Vector2(0.34f, 0.50f);
    [SerializeField] private Vector2 successZoneCenterRange = new Vector2(0.55f, 0.82f);
    [SerializeField, Range(0.03f, 0.25f)] private float successZoneHalfWidth = 0.065f;
    [SerializeField, Min(0.1f)] private float resultDisplaySeconds = 1.1f;
    [SerializeField] private bool lockPlayerWhilePlaying = true;

    [Header("UI")]
    [SerializeField] private bool buildUIAutomatically = true;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private RectTransform panel;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text comboText;
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private TMP_Text missText;
    [SerializeField] private TMP_Text resultText;

    private LaneState[] lanes;
    private LaneView[] laneViews;
    private ChoppableTree currentTree;
    private bool isPlaying;
    private bool isEnding;
    private int successfulHits;
    private int misses;
    private int combo;
    private Sprite circleSprite;

    public bool IsPlaying => isPlaying;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        laneCount = Mathf.Max(1, laneCount);

        if (buildUIAutomatically)
            BuildUIIfNeeded();

        SetVisible(false);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        if (!isPlaying || isEnding || currentTree == null)
            return;

        if (WasEscapePressed())
        {
            CancelGame();
            return;
        }

        UpdateLaneMovement();
        ReadPromptInput();
        UpdateLaneVisuals();
        UpdateHeader();
    }

    public bool StartGame(ChoppableTree tree)
    {
        if (tree == null || isPlaying || isEnding)
            return false;

        BuildUIIfNeeded();

        currentTree = tree;
        successfulHits = 0;
        misses = 0;
        combo = 0;
        isPlaying = true;
        isEnding = false;

        EnsureLaneArrays();

        for (int i = 0; i < lanes.Length; i++)
            ResetLane(i, true);

        titleText.text = "CHẶT GỖ";
        resultText.text = string.Empty;

        SetVisible(true);
        UpdateHeader();
        UpdateLaneVisuals();

        if (lockPlayerWhilePlaying)
            GameLockManager.Instance?.LockPlayer();

        return true;
    }

    private void UpdateLaneMovement()
    {
        for (int i = 0; i < lanes.Length; i++)
        {
            LaneState lane = lanes[i];
            lane.progress += lane.speed * Time.unscaledDeltaTime;

            if (lane.flashTimer > 0f)
                lane.flashTimer -= Time.unscaledDeltaTime;

            if (lane.progress < 1f)
                continue;

            RegisterMiss(i, "QUÁ CHẬM");

            if (isEnding)
                return;
        }
    }

    private void ReadPromptInput()
    {
        if (WasPromptPressed(PromptKey.W)) HandlePromptPress(PromptKey.W);
        if (WasPromptPressed(PromptKey.A)) HandlePromptPress(PromptKey.A);
        if (WasPromptPressed(PromptKey.S)) HandlePromptPress(PromptKey.S);
        if (WasPromptPressed(PromptKey.D)) HandlePromptPress(PromptKey.D);
    }

    private void HandlePromptPress(PromptKey pressedKey)
    {
        if (!isPlaying || isEnding)
            return;

        int matchingLane = -1;
        int perfectLane = -1;
        float closestDistance = float.MaxValue;

        for (int i = 0; i < lanes.Length; i++)
        {
            LaneState lane = lanes[i];

            if (lane.key != pressedKey)
                continue;

            float distance = Mathf.Abs(lane.progress - lane.zoneCenter);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                matchingLane = i;
            }

            if (distance <= lane.zoneHalfWidth)
            {
                perfectLane = i;
                break;
            }
        }

        if (perfectLane >= 0)
        {
            RegisterSuccess(perfectLane);
            return;
        }

        if (matchingLane >= 0)
        {
            RegisterMiss(matchingLane, "SAI NHỊP");
            return;
        }

        int furthestLane = 0;

        for (int i = 1; i < lanes.Length; i++)
        {
            if (lanes[i].progress > lanes[furthestLane].progress)
                furthestLane = i;
        }

        RegisterMiss(furthestLane, "SAI PHÍM");
    }

    private void RegisterSuccess(int laneIndex)
    {
        successfulHits++;
        combo++;

        LaneState lane = lanes[laneIndex];
        lane.flashTimer = 0.22f;
        lane.successFlash = true;

        currentTree?.NotifySuccessfulHit();

        if (successfulHits >= currentTree.RequiredSuccessfulHits)
        {
            CompleteGame();
            return;
        }

        ResetLane(laneIndex, false);
    }

    private void RegisterMiss(int laneIndex, string feedback)
    {
        misses++;
        combo = 0;

        laneIndex = Mathf.Clamp(laneIndex, 0, lanes.Length - 1);

        LaneState lane = lanes[laneIndex];
        lane.flashTimer = 0.28f;
        lane.successFlash = false;

        if (laneViews != null && laneIndex < laneViews.Length)
            laneViews[laneIndex].feedbackText.text = feedback;

        currentTree?.NotifyMiss();

        if (misses >= currentTree.MaximumMisses)
        {
            FailGame();
            return;
        }

        ResetLane(laneIndex, false);
    }

    private void CompleteGame()
    {
        if (isEnding)
            return;

        isEnding = true;
        isPlaying = false;

        int rewardAmount = currentTree.RollWoodReward();
        bool rewardAdded = currentTree.TryCompleteTree(rewardAmount, out string reason);

        if (rewardAdded)
        {
            resultText.text = "THÀNH CÔNG  +" + rewardAmount + " GỖ";
            resultText.color = new Color(0.35f, 1f, 0.42f, 1f);
        }
        else
        {
            resultText.text = string.IsNullOrWhiteSpace(reason)
                ? "BALO ĐẦY"
                : reason.ToUpperInvariant();

            resultText.color = new Color(1f, 0.55f, 0.25f, 1f);
        }

        StartCoroutine(FinishRoutine());
    }

    private void FailGame()
    {
        if (isEnding)
            return;

        isEnding = true;
        isPlaying = false;

        currentTree?.NotifyMinigameFailed();

        resultText.text = "THẤT BẠI - HÃY THỬ LẠI";
        resultText.color = new Color(1f, 0.25f, 0.25f, 1f);

        StartCoroutine(FinishRoutine());
    }

    private void CancelGame()
    {
        if (!isPlaying || isEnding)
            return;

        isPlaying = false;
        isEnding = true;

        currentTree?.NotifyMinigameCancelled();

        resultText.text = "ĐÃ HỦY";
        resultText.color = Color.white;

        StartCoroutine(FinishRoutine());
    }

    private IEnumerator FinishRoutine()
    {
        yield return new WaitForSecondsRealtime(resultDisplaySeconds);

        SetVisible(false);

        if (lockPlayerWhilePlaying)
            GameLockManager.Instance?.UnlockPlayer();

        currentTree = null;
        isPlaying = false;
        isEnding = false;
    }

    private void ResetLane(int index, bool initial)
    {
        LaneState lane = lanes[index];

        lane.key = GetRandomKeyAvoiding(index);
        lane.progress = initial ? UnityEngine.Random.Range(0f, 0.18f) : 0f;
        lane.speed = UnityEngine.Random.Range(markerSpeedRange.x, markerSpeedRange.y);
        lane.zoneCenter = UnityEngine.Random.Range(successZoneCenterRange.x, successZoneCenterRange.y);
        lane.zoneHalfWidth = successZoneHalfWidth;

        if (laneViews != null && index < laneViews.Length)
            laneViews[index].feedbackText.text = string.Empty;
    }

    private PromptKey GetRandomKeyAvoiding(int laneIndex)
    {
        PromptKey candidate = (PromptKey)UnityEngine.Random.Range(0, 4);

        if (lanes == null || lanes.Length <= 1)
            return candidate;

        for (int attempt = 0; attempt < 8; attempt++)
        {
            bool duplicate = false;

            for (int i = 0; i < lanes.Length; i++)
            {
                if (i == laneIndex)
                    continue;

                if (lanes[i] != null && lanes[i].key == candidate)
                {
                    duplicate = true;
                    break;
                }
            }

            if (!duplicate)
                return candidate;

            candidate = (PromptKey)UnityEngine.Random.Range(0, 4);
        }

        return candidate;
    }

    private void UpdateHeader()
    {
        if (currentTree == null)
            return;

        comboText.text = "COMBO\n<size=34><b>x" + Mathf.Max(1, combo) + "</b></size>";
        progressText.text = successfulHits + " / " + currentTree.RequiredSuccessfulHits;
        missText.text = "SAI " + misses + " / " + currentTree.MaximumMisses;
    }

    private void UpdateLaneVisuals()
    {
        if (lanes == null || laneViews == null)
            return;

        for (int i = 0; i < lanes.Length; i++)
        {
            LaneState lane = lanes[i];
            LaneView view = laneViews[i];

            view.keyText.text = lane.key.ToString();

            float barWidth = view.bar.rect.width;
            view.marker.anchoredPosition =
                new Vector2((lane.progress - 0.5f) * barWidth, 0f);

            view.successZone.anchoredPosition =
                new Vector2((lane.zoneCenter - 0.5f) * barWidth, 0f);

            view.successZone.sizeDelta =
                new Vector2(lane.zoneHalfWidth * 2f * barWidth, 42f);

            if (lane.flashTimer <= 0f)
            {
                view.rowBackground.color = i % 2 == 0
                    ? new Color(0.28f, 0.08f, 0.08f, 0.82f)
                    : new Color(0.22f, 0.17f, 0.04f, 0.82f);
            }
            else
            {
                view.rowBackground.color = lane.successFlash
                    ? new Color(0.08f, 0.38f, 0.12f, 0.92f)
                    : new Color(0.52f, 0.06f, 0.06f, 0.95f);
            }
        }
    }

    private bool WasPromptPressed(PromptKey key)
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current == null)
            return false;

        return key switch
        {
            PromptKey.W => Keyboard.current.wKey.wasPressedThisFrame,
            PromptKey.A => Keyboard.current.aKey.wasPressedThisFrame,
            PromptKey.S => Keyboard.current.sKey.wasPressedThisFrame,
            PromptKey.D => Keyboard.current.dKey.wasPressedThisFrame,
            _ => false
        };
#else
        return key switch
        {
            PromptKey.W => Input.GetKeyDown(KeyCode.W),
            PromptKey.A => Input.GetKeyDown(KeyCode.A),
            PromptKey.S => Input.GetKeyDown(KeyCode.S),
            PromptKey.D => Input.GetKeyDown(KeyCode.D),
            _ => false
        };
#endif
    }

    private bool WasEscapePressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null &&
               Keyboard.current.escapeKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Escape);
#endif
    }

    private void EnsureLaneArrays()
    {
        if (lanes == null || lanes.Length != laneCount)
        {
            lanes = new LaneState[laneCount];

            for (int i = 0; i < lanes.Length; i++)
                lanes[i] = new LaneState();
        }

        if (laneViews == null || laneViews.Length != laneCount)
            BuildLaneViews();
    }

    private void SetVisible(bool visible)
    {
        if (canvasGroup == null)
            return;

        canvasGroup.alpha = visible ? 1f : 0f;
        canvasGroup.interactable = visible;
        canvasGroup.blocksRaycasts = visible;
    }

    private void BuildUIIfNeeded()
    {
        if (canvasGroup != null && panel != null)
        {
            EnsureLaneArrays();
            return;
        }

        circleSprite = CreateCircleSprite();

        GameObject canvasObject = new GameObject(
            "WoodChopMinigameCanvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster),
            typeof(CanvasGroup)
        );

        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5000;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600f, 900f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGroup = canvasObject.GetComponent<CanvasGroup>();

        panel = CreateRect(canvasObject.transform, "Panel");
        panel.anchorMin = new Vector2(0.5f, 1f);
        panel.anchorMax = new Vector2(0.5f, 1f);
        panel.pivot = new Vector2(0.5f, 1f);
        panel.anchoredPosition = new Vector2(0f, -35f);
        panel.sizeDelta = new Vector2(760f, 235f);

        Image panelImage = panel.gameObject.AddComponent<Image>();
        panelImage.color = new Color(0.035f, 0.03f, 0.025f, 0.92f);

        titleText = CreateText(panel, "Title", "CHẶT GỖ", 26f, FontStyles.Bold, TextAlignmentOptions.Center);
        SetRect(titleText.rectTransform, new Vector2(0f, -16f), new Vector2(340f, 36f), new Vector2(0.5f, 1f));

        comboText = CreateText(panel, "Combo", "COMBO\nx1", 17f, FontStyles.Bold, TextAlignmentOptions.Center);
        SetRect(comboText.rectTransform, new Vector2(58f, -16f), new Vector2(95f, 62f), new Vector2(0f, 1f));

        progressText = CreateText(panel, "Progress", "0 / 8", 19f, FontStyles.Bold, TextAlignmentOptions.Center);
        SetRect(progressText.rectTransform, new Vector2(-65f, -22f), new Vector2(100f, 32f), new Vector2(1f, 1f));

        missText = CreateText(panel, "Misses", "SAI 0 / 3", 14f, FontStyles.Bold, TextAlignmentOptions.Center);
        missText.color = new Color(1f, 0.65f, 0.28f, 1f);
        SetRect(missText.rectTransform, new Vector2(-65f, -50f), new Vector2(120f, 24f), new Vector2(1f, 1f));

        resultText = CreateText(panel, "Result", string.Empty, 24f, FontStyles.Bold, TextAlignmentOptions.Center);
        SetRect(resultText.rectTransform, new Vector2(0f, -205f), new Vector2(620f, 34f), new Vector2(0.5f, 1f));

        BuildLaneViews();
    }

    private void BuildLaneViews()
    {
        if (panel == null)
            return;

        Transform existing = panel.Find("Lanes");
        if (existing != null)
            Destroy(existing.gameObject);

        RectTransform lanesRoot = CreateRect(panel, "Lanes");
        lanesRoot.anchorMin = new Vector2(0f, 1f);
        lanesRoot.anchorMax = new Vector2(1f, 1f);
        lanesRoot.pivot = new Vector2(0.5f, 1f);
        lanesRoot.anchoredPosition = new Vector2(0f, -75f);
        lanesRoot.sizeDelta = new Vector2(0f, laneCount * 58f);

        laneViews = new LaneView[laneCount];

        for (int i = 0; i < laneCount; i++)
            laneViews[i] = CreateLaneView(lanesRoot, i);

        if (lanes == null || lanes.Length != laneCount)
        {
            lanes = new LaneState[laneCount];

            for (int i = 0; i < laneCount; i++)
                lanes[i] = new LaneState();
        }
    }

    private LaneView CreateLaneView(RectTransform parent, int index)
    {
        LaneView view = new LaneView();

        view.row = CreateRect(parent, "Lane_" + index);
        view.row.anchorMin = new Vector2(0f, 1f);
        view.row.anchorMax = new Vector2(1f, 1f);
        view.row.pivot = new Vector2(0.5f, 1f);
        view.row.anchoredPosition = new Vector2(0f, -index * 58f);
        view.row.sizeDelta = new Vector2(-24f, 52f);

        view.rowBackground = view.row.gameObject.AddComponent<Image>();
        view.rowBackground.color = index % 2 == 0
            ? new Color(0.28f, 0.08f, 0.08f, 0.82f)
            : new Color(0.22f, 0.17f, 0.04f, 0.82f);

        RectTransform keyRect = CreateRect(view.row, "KeyCircle");
        SetRect(keyRect, new Vector2(31f, -26f), new Vector2(42f, 42f), new Vector2(0f, 1f));

        Image keyCircle = keyRect.gameObject.AddComponent<Image>();
        keyCircle.sprite = circleSprite;
        keyCircle.color = index % 2 == 0
            ? new Color(0.72f, 0.13f, 0.16f, 1f)
            : new Color(0.86f, 0.61f, 0.08f, 1f);

        view.keyText = CreateText(keyRect, "KeyText", "W", 22f, FontStyles.Bold, TextAlignmentOptions.Center);
        StretchToParent(view.keyText.rectTransform);

        view.bar = CreateRect(view.row, "Bar");
        view.bar.anchorMin = new Vector2(0f, 0.5f);
        view.bar.anchorMax = new Vector2(1f, 0.5f);
        view.bar.pivot = new Vector2(0.5f, 0.5f);
        view.bar.anchoredPosition = new Vector2(29f, 0f);
        view.bar.sizeDelta = new Vector2(-128f, 30f);

        Image barImage = view.bar.gameObject.AddComponent<Image>();
        barImage.color = new Color(0.03f, 0.03f, 0.03f, 0.72f);

        view.successZone = CreateRect(view.bar, "SuccessZone");
        view.successZone.anchorMin = new Vector2(0.5f, 0.5f);
        view.successZone.anchorMax = new Vector2(0.5f, 0.5f);
        view.successZone.pivot = new Vector2(0.5f, 0.5f);

        Image zoneImage = view.successZone.gameObject.AddComponent<Image>();
        zoneImage.color = new Color(1f, 1f, 1f, 0.28f);

        view.marker = CreateRect(view.bar, "Marker");
        view.marker.anchorMin = new Vector2(0.5f, 0.5f);
        view.marker.anchorMax = new Vector2(0.5f, 0.5f);
        view.marker.pivot = new Vector2(0.5f, 0.5f);
        view.marker.sizeDelta = new Vector2(31f, 31f);

        Image markerImage = view.marker.gameObject.AddComponent<Image>();
        markerImage.sprite = circleSprite;
        markerImage.color = new Color(0.95f, 0.97f, 1f, 1f);

        view.feedbackText = CreateText(view.row, "Feedback", string.Empty, 13f, FontStyles.Bold, TextAlignmentOptions.Center);
        view.feedbackText.color = new Color(1f, 0.85f, 0.4f, 1f);
        SetRect(view.feedbackText.rectTransform, new Vector2(-30f, -26f), new Vector2(92f, 28f), new Vector2(1f, 1f));

        return view;
    }

    private static RectTransform CreateRect(Transform parent, string objectName)
    {
        GameObject gameObject = new GameObject(objectName, typeof(RectTransform));
        RectTransform rect = gameObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }

    private static TMP_Text CreateText(
        Transform parent,
        string objectName,
        string content,
        float fontSize,
        FontStyles style,
        TextAlignmentOptions alignment)
    {
        RectTransform rect = CreateRect(parent, objectName);
        TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.text = content;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.alignment = alignment;
        text.color = Color.white;
        text.raycastTarget = false;
        return text;
    }

    private static void SetRect(RectTransform rect, Vector2 anchoredPosition, Vector2 size, Vector2 anchor)
    {
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
    }

    private static void StretchToParent(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static Sprite CreateCircleSprite()
    {
        const int size = 64;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = "RuntimeCircle";
        texture.hideFlags = HideFlags.HideAndDontSave;

        Color[] pixels = new Color[size * size];
        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float radius = size * 0.48f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                float alpha = Mathf.Clamp01(radius - distance + 1f);
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();

        return Sprite.Create(
            texture,
            new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.5f),
            100f
        );
    }
}
