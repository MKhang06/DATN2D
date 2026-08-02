using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FishingNotificationUI : MonoBehaviour
{
    public enum NotificationType
    {
        Info,
        Success,
        Warning,
        Sell,
        Discard
    }

    private class NotificationRequest
    {
        public string title;
        public string message;
        public NotificationType type;
        public float duration;
    }

    public static FishingNotificationUI Instance { get; private set; }

    [Header("Root")]
    [SerializeField] private GameObject notificationRoot;
    [SerializeField] private RectTransform notificationPanel;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Text")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text messageText;

    [Header("Hình ảnh")]
    [SerializeField] private Image statusIcon;
    [SerializeField] private Image accentLine;

    [Header("Icon")]
    [SerializeField] private Sprite infoIcon;
    [SerializeField] private Sprite successIcon;
    [SerializeField] private Sprite warningIcon;
    [SerializeField] private Sprite sellIcon;
    [SerializeField] private Sprite discardIcon;

    [Header("Màu trạng thái")]
    [SerializeField] private Color infoColor =
        new Color(0.2f, 0.85f, 1f);

    [SerializeField] private Color successColor =
        new Color(0.35f, 1f, 0.25f);

    [SerializeField] private Color warningColor =
        new Color(1f, 0.75f, 0.1f);

    [SerializeField] private Color sellColor =
        new Color(0.35f, 1f, 0.4f);

    [SerializeField] private Color discardColor =
        new Color(1f, 0.35f, 0.2f);

    [Header("Thời gian")]
    [SerializeField, Min(0.05f)]
    private float showAnimationDuration = 0.2f;

    [SerializeField, Min(0.05f)]
    private float hideAnimationDuration = 0.2f;

    [SerializeField, Min(0.5f)]
    private float defaultDisplayDuration = 4f;

    [Header("Chuyển động")]
    [SerializeField] private Vector2 visiblePosition =
        new Vector2(-25f, -25f);

    [SerializeField] private Vector2 hiddenOffset =
        new Vector2(470f, 0f);

    [Header("Scene")]
    [SerializeField] private bool keepAcrossScenes;

    private readonly Queue<NotificationRequest> notificationQueue =
        new Queue<NotificationRequest>();

    private Coroutine notificationCoroutine;
    private bool isShowing;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (keepAcrossScenes)
            DontDestroyOnLoad(gameObject);

        FindMissingReferences();
        HideImmediately();
    }

    private void FindMissingReferences()
    {
        if (notificationPanel == null &&
            notificationRoot != null)
        {
            notificationPanel =
                notificationRoot.GetComponent<RectTransform>();
        }

        if (canvasGroup == null &&
            notificationRoot != null)
        {
            canvasGroup =
                notificationRoot.GetComponent<CanvasGroup>();
        }
    }

    public void ShowNotification(
        string message,
        NotificationType type = NotificationType.Info,
        float duration = -1f,
        string title = "THÔNG BÁO")
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        NotificationRequest request =
            new NotificationRequest
            {
                title = string.IsNullOrWhiteSpace(title)
                    ? "THÔNG BÁO"
                    : title,

                message = message,
                type = type,

                duration = duration > 0f
                    ? duration
                    : defaultDisplayDuration
            };

        notificationQueue.Enqueue(request);

        if (!isShowing)
        {
            notificationCoroutine =
                StartCoroutine(ProcessNotificationQueue());
        }
    }
    public void ShowFishEscaped()
{
    ShowNotification(
        "Cá đã thoát khỏi lưỡi câu. Hãy giữ thanh kéo ổn định và thử lại.",
        NotificationType.Warning
    );
}

    public void ShowNoFish()
    {
        ShowNotification(
            "Bạn đã thả dây câu nhưng không có cá cắn câu. " +
            "Hãy thử lại ở độ sâu khác hoặc đổi mồi câu.",
            NotificationType.Warning
        );
    }

    public void ShowFishStored(string fishName)
    {
        string safeName = string.IsNullOrWhiteSpace(fishName)
            ? "Cá"
            : fishName;

        ShowNotification(
            "Bạn đã câu được " +
            safeName +
            " và cất vào balo thành công.",
            NotificationType.Success
        );
    }

    public void ShowFishSold(
        string fishName,
        int cashReceived)
    {
        string safeName = string.IsNullOrWhiteSpace(fishName)
            ? "cá"
            : fishName;

        cashReceived = Mathf.Max(0, cashReceived);

        ShowNotification(
            "Bạn đã bán " +
            safeName +
            " tại chỗ và nhận được tiền mặt " +
            "<color=#63FF65>$" +
            cashReceived.ToString("N0") +
            "</color>.",
            NotificationType.Sell
        );
    }

    public void ShowFishDiscarded(string fishName)
    {
        string safeName = string.IsNullOrWhiteSpace(fishName)
            ? "cá"
            : fishName;

        ShowNotification(
            "Bạn đã vứt bỏ " +
            safeName +
            ". Vật phẩm không được thêm vào balo.",
            NotificationType.Discard
        );
    }

    public void ShowInventoryFull()
    {
        ShowNotification(
            "Balo đã đầy. Không thể cất cá vào túi đồ.",
            NotificationType.Warning
        );
    }

    private IEnumerator ProcessNotificationQueue()
    {
        isShowing = true;

        while (notificationQueue.Count > 0)
        {
            NotificationRequest request =
                notificationQueue.Dequeue();

            ApplyNotification(request);

            yield return ShowAnimation();

            yield return new WaitForSecondsRealtime(
                request.duration
            );

            yield return HideAnimation();
        }

        isShowing = false;
        notificationCoroutine = null;
    }

    private void ApplyNotification(
        NotificationRequest request)
    {
        if (notificationRoot != null)
            notificationRoot.SetActive(true);

        if (titleText != null)
            titleText.text = request.title;

        if (messageText != null)
            messageText.text = request.message;

        Sprite selectedIcon =
            GetIcon(request.type);

        Color selectedColor =
            GetColor(request.type);

        if (statusIcon != null)
        {
            statusIcon.sprite = selectedIcon;
            statusIcon.enabled = selectedIcon != null;
            statusIcon.color = Color.white;
            statusIcon.preserveAspect = true;
        }

        if (accentLine != null)
            accentLine.color = selectedColor;
    }

    private IEnumerator ShowAnimation()
    {
        if (notificationRoot != null)
            notificationRoot.SetActive(true);

        Vector2 hiddenPosition =
            visiblePosition + hiddenOffset;

        float elapsed = 0f;

        while (elapsed < showAnimationDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(
                elapsed / showAnimationDuration
            );

            t = 1f - Mathf.Pow(1f - t, 3f);

            if (notificationPanel != null)
            {
                notificationPanel.anchoredPosition =
                    Vector2.Lerp(
                        hiddenPosition,
                        visiblePosition,
                        t
                    );
            }

            if (canvasGroup != null)
                canvasGroup.alpha = t;

            yield return null;
        }

        if (notificationPanel != null)
        {
            notificationPanel.anchoredPosition =
                visiblePosition;
        }

        if (canvasGroup != null)
            canvasGroup.alpha = 1f;
    }

    private IEnumerator HideAnimation()
    {
        Vector2 hiddenPosition =
            visiblePosition + hiddenOffset;

        float elapsed = 0f;

        while (elapsed < hideAnimationDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(
                elapsed / hideAnimationDuration
            );

            if (notificationPanel != null)
            {
                notificationPanel.anchoredPosition =
                    Vector2.Lerp(
                        visiblePosition,
                        hiddenPosition,
                        t
                    );
            }

            if (canvasGroup != null)
                canvasGroup.alpha = 1f - t;

            yield return null;
        }

        HideImmediately();
    }

    private void HideImmediately()
    {
        FindMissingReferences();

        if (notificationPanel != null)
        {
            notificationPanel.anchoredPosition =
                visiblePosition + hiddenOffset;
        }

        if (canvasGroup != null)
            canvasGroup.alpha = 0f;

        if (notificationRoot != null)
            notificationRoot.SetActive(false);
    }

    private Sprite GetIcon(NotificationType type)
    {
        switch (type)
        {
            case NotificationType.Success:
                return successIcon;

            case NotificationType.Warning:
                return warningIcon;

            case NotificationType.Sell:
                return sellIcon != null
                    ? sellIcon
                    : successIcon;

            case NotificationType.Discard:
                return discardIcon;

            default:
                return infoIcon;
        }
    }

    private Color GetColor(NotificationType type)
    {
        switch (type)
        {
            case NotificationType.Success:
                return successColor;

            case NotificationType.Warning:
                return warningColor;

            case NotificationType.Sell:
                return sellColor;

            case NotificationType.Discard:
                return discardColor;

            default:
                return infoColor;
        }
    }
}