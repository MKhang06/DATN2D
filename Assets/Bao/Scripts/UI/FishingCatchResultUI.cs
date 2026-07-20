using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FishingCatchResultUI : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject root;

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

    private void Awake()
    {
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

        SetButtonsInteractable(true);

        if (root != null)
            root.SetActive(true);

        UpdateUI();
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
            infoTitleText.text = "THÔNG TIN";

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
                    "Bạn đã câu được một con " +
                    itemName +
                    " có trọng lượng " +
                    weightKg.ToString("0.##") +
                    " kg.\n\nBạn muốn làm gì với nó?";
            }
            else
            {
                infoText.text =
                    "Bạn đã câu được " +
                    itemName +
                    ".\n\nBạn muốn làm gì với vật phẩm này?";
            }
        }

        if (storeMainText != null)
            storeMainText.text = "CẤT VÀO";

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
        SetButtonsInteractable(true);

        if (root != null)
            root.SetActive(false);
    }

    private void OnDisable()
    {
        StopFishingAudio();
    }
}