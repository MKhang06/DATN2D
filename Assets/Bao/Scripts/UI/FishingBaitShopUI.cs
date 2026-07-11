using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FishingBaitShopUI : MonoBehaviour
{
    [Header("Bật / tắt giao diện")]
    [SerializeField] private KeyCode toggleKey = KeyCode.L;
    [SerializeField] private bool hideOnStart = true;

    [Header("Root mồi câu")]
    [SerializeField] private GameObject baitRoot;

    [Header("Player")]
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private InventoryManager inventoryManager;

    [Header("Danh sách mồi")]
    [SerializeField] private Transform content;
    [SerializeField] private FishingBaitShopItemUI baitCardPrefab;
    [SerializeField] private FishingBaitData[] availableBaits;

    [Header("Thông tin UI")]
    [SerializeField] private TMP_Text moneyText;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private TMP_Text categoryTitleText;

    [Header("Buttons")]
    [SerializeField] private Button refreshButton;
    [SerializeField] private Button closeButton;

    [Header("Thông báo")]
    [SerializeField] private float messageDuration = 1.5f;

    private Coroutine messageCoroutine;
    private bool isOpen;

    public bool IsOpen => isOpen;

    private void Awake()
    {
        ValidateRoot();

        if (refreshButton != null)
        {
            refreshButton.onClick.RemoveListener(RefreshBaitList);
            refreshButton.onClick.AddListener(RefreshBaitList);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(Hide);
            closeButton.onClick.AddListener(Hide);
        }

        if (messageText != null)
            messageText.gameObject.SetActive(false);

        if (categoryTitleText != null)
            categoryTitleText.text = "MỒI CÂU";

        if (hideOnStart)
        {
            isOpen = false;

            if (baitRoot != null)
                baitRoot.SetActive(false);
        }
    }

    private void Start()
    {
        FindReferences();

        if (!hideOnStart)
            Show();
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
            Toggle();

        if (isOpen && Input.GetKeyDown(KeyCode.Escape))
            Hide();
    }

    private void ValidateRoot()
    {
        if (baitRoot == null)
        {
            Debug.LogWarning(
                "FishingBaitShopUI chưa được gắn Bait Root."
            );

            return;
        }

        if (baitRoot == gameObject)
        {
            Debug.LogError(
                "Không được kéo object đang chứa FishingBaitShopUI " +
                "vào ô Bait Root. Hãy tạo một object con tên BaitRoot."
            );

            baitRoot = null;
        }
    }

    private void FindReferences()
    {
        if (playerStats == null)
            playerStats = FindFirstObjectByType<PlayerStats>();

        if (inventoryManager == null)
            inventoryManager = InventoryManager.Instance;

        if (inventoryManager == null)
        {
            inventoryManager =
                FindFirstObjectByType<InventoryManager>();
        }
    }

    public void Toggle()
    {
        if (isOpen)
            Hide();
        else
            Show();
    }

    public void Show()
    {
        if (baitRoot == null)
        {
            Debug.LogWarning("Chưa gắn Bait Root.");
            return;
        }

        isOpen = true;
        baitRoot.SetActive(true);

        FindReferences();
        RefreshBaitList();
        RefreshMoney();

        GameLockManager.Instance?.LockPlayer();
    }

    public void Hide()
    {
        isOpen = false;

        if (baitRoot != null)
            baitRoot.SetActive(false);

        if (messageCoroutine != null)
        {
            StopCoroutine(messageCoroutine);
            messageCoroutine = null;
        }

        if (messageText != null)
            messageText.gameObject.SetActive(false);

        GameLockManager.Instance?.UnlockPlayer();
    }

    public void RefreshBaitList()
    {
        ClearBaitList();

        if (content == null)
        {
            Debug.LogWarning(
                "FishingBaitShopUI chưa gắn Content."
            );

            return;
        }

        if (baitCardPrefab == null)
        {
            Debug.LogWarning(
                "FishingBaitShopUI chưa gắn Bait Card Prefab."
            );

            return;
        }

        if (availableBaits == null ||
            availableBaits.Length == 0)
        {
            Debug.LogWarning(
                "FishingBaitShopUI chưa có mồi trong Available Baits."
            );

            return;
        }

        int playerLevel =
            playerStats != null
                ? playerStats.Level
                : 0;

        foreach (FishingBaitData bait in availableBaits)
        {
            if (bait == null)
                continue;

            FishingBaitShopItemUI card =
                Instantiate(baitCardPrefab, content);

            card.name = "BaitCard_" + bait.baitName;

            card.Setup(
                bait,
                playerLevel,
                TryBuyBait
            );
        }

        Canvas.ForceUpdateCanvases();

        Debug.Log(
            "Đã tạo " +
            content.childCount +
            " card mồi câu."
        );
    }

    private void TryBuyBait(FishingBaitData bait)
    {
        if (bait == null)
            return;

        FindReferences();

        if (playerStats == null)
        {
            ShowMessage(
                "Không tìm thấy PlayerStats.",
                false
            );

            return;
        }

        if (inventoryManager == null)
        {
            ShowMessage(
                "Không tìm thấy InventoryManager.",
                false
            );

            return;
        }

        if (playerStats.Level < bait.requiredLevel)
        {
            ShowMessage(
                "Bạn cần Level " +
                bait.requiredLevel +
                " để mua " +
                bait.baitName +
                ".",
                false
            );

            return;
        }

        if (playerStats.Money < bait.price)
        {
            ShowMessage(
                "Bạn không đủ tiền.",
                false
            );

            return;
        }

        bool added = inventoryManager.AddItem(
            bait.baitName,
            bait.icon,
            bait.amountPerPurchase
        );

        if (!added)
        {
            ShowMessage(
                "Túi đồ đã đầy.",
                false
            );

            return;
        }

        bool paid = playerStats.SpendMoney(bait.price);

        if (!paid)
        {
            ShowMessage(
                "Thanh toán thất bại.",
                false
            );

            return;
        }

        RefreshMoney();

        ShowMessage(
            "Đã mua " +
            bait.baitName +
            " x" +
            bait.amountPerPurchase +
            ".",
            true
        );

        Debug.Log(
            "Đã mua mồi: " +
            bait.baitName +
            " | Giá: $" +
            bait.price
        );
    }

    private void RefreshMoney()
    {
        if (moneyText == null)
            return;

        int money =
            playerStats != null
                ? playerStats.Money
                : 0;

        moneyText.text =
            "TIỀN MẶT <color=#25F2C4>$" +
            money.ToString("N0") +
            "</color>";
    }

    private void ClearBaitList()
    {
        if (content == null)
            return;

        for (int i = content.childCount - 1; i >= 0; i--)
        {
            Destroy(content.GetChild(i).gameObject);
        }
    }

    private void ShowMessage(
        string message,
        bool success)
    {
        if (messageText == null)
            return;

        if (messageCoroutine != null)
            StopCoroutine(messageCoroutine);

        messageCoroutine = StartCoroutine(
            ShowMessageRoutine(message, success)
        );
    }

    private IEnumerator ShowMessageRoutine(
        string message,
        bool success)
    {
        messageText.gameObject.SetActive(true);

        messageText.color = success
            ? new Color(0.2f, 1f, 0.65f)
            : new Color(1f, 0.3f, 0.3f);

        messageText.text = message;

        yield return new WaitForSecondsRealtime(
            messageDuration
        );

        messageText.gameObject.SetActive(false);
        messageCoroutine = null;
    }
}