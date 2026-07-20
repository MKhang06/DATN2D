using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FishingBaitShopUI : MonoBehaviour
{
    [Header("Root mồi câu")]
    [SerializeField] private GameObject baitRoot;

    [Header("Player")]
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private InventoryManager inventoryManager;

    [Header("Danh sách mồi")]
    [SerializeField] private Transform content;
    [SerializeField] private FishingBaitShopItemUI baitCardPrefab;
    [SerializeField] private FishingBaitData[] availableBaits;

    [Header("Xác nhận mua")]
    [SerializeField]
    private FishingPurchasePopupUI purchasePopupUI;

    [SerializeField, Min(1)]
    private int maxPurchaseQuantity = 999;

    [Header("Thông tin UI")]
    [SerializeField] private TMP_Text moneyText;
    [SerializeField] private TMP_Text messageText;

    [Header("Button tùy chọn")]
    [SerializeField] private Button refreshButton;

    [Header("Thông báo")]
    [SerializeField] private float messageDuration = 1.5f;

    private Coroutine messageCoroutine;

    private void Awake()
    {
        if (refreshButton != null)
        {
            refreshButton.onClick.RemoveAllListeners();
            refreshButton.onClick.AddListener(
                RefreshBaitList
            );
        }

        if (messageText != null)
            messageText.gameObject.SetActive(false);
    }

    private void Start()
    {
        FindReferences();
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

        if (purchasePopupUI == null)
        {
            purchasePopupUI =
                GetComponent<FishingPurchasePopupUI>();
        }
    }

    public void Show()
    {
        if (baitRoot == null)
        {
            Debug.LogWarning(
                "FishingBaitShopUI chưa gắn Bait Root."
            );

            return;
        }

        baitRoot.SetActive(true);

        FindReferences();
        RefreshBaitList();
        RefreshMoney();
    }

    public void Hide()
    {
        purchasePopupUI?.Hide();

        if (baitRoot != null)
            baitRoot.SetActive(false);

        if (messageCoroutine != null)
        {
            StopCoroutine(messageCoroutine);
            messageCoroutine = null;
        }

        if (messageText != null)
            messageText.gameObject.SetActive(false);
    }

    public void RefreshBaitList()
    {
        ClearBaitList();
        FindReferences();

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
                "Available Baits đang trống."
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

            card.gameObject.SetActive(true);
            card.transform.localScale = Vector3.one;
            card.transform.localRotation =
                Quaternion.identity;

            card.name =
                "BaitCard_" + bait.baitName;

            card.Setup(
                bait,
                playerLevel,
                TryBuyBait
            );
        }

        Canvas.ForceUpdateCanvases();

        if (content is RectTransform contentRect)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(
                contentRect
            );
        }

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

        if (purchasePopupUI == null)
        {
            ShowMessage(
                "Chưa gắn FishingPurchasePopupUI.",
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

        int maximumByMoney;

        if (bait.price <= 0)
        {
            maximumByMoney = maxPurchaseQuantity;
        }
        else
        {
            maximumByMoney =
                playerStats.Money / bait.price;
        }

        if (maximumByMoney < 1)
        {
            ShowMessage(
                "Bạn không đủ tiền.",
                false
            );

            return;
        }

        int maximumQuantity = Mathf.Min(
            maximumByMoney,
            maxPurchaseQuantity
        );

        purchasePopupUI.ShowBaitPurchase(
            bait.baitName,
            bait.icon,
            bait.price,
            maximumQuantity,
            quantity =>
                ConfirmBuyBait(bait, quantity)
        );
    }

    private bool ConfirmBuyBait(
        FishingBaitData bait,
        int purchaseQuantity)
    {
        if (bait == null)
            return false;

        FindReferences();

        purchaseQuantity = Mathf.Clamp(
            purchaseQuantity,
            1,
            maxPurchaseQuantity
        );

        int amountPerPurchase =
            Mathf.Max(1, bait.amountPerPurchase);

        long totalAmountLong =
            (long)amountPerPurchase *
            purchaseQuantity;

        long totalPriceLong =
            (long)Mathf.Max(0, bait.price) *
            purchaseQuantity;

        if (totalAmountLong > int.MaxValue ||
            totalPriceLong > int.MaxValue)
        {
            ShowMessage(
                "Số lượng mua quá lớn.",
                false
            );

            return false;
        }

        int totalAmount = (int)totalAmountLong;
        int totalPrice = (int)totalPriceLong;

        if (playerStats == null ||
            inventoryManager == null)
        {
            ShowMessage(
                "Không tìm thấy dữ liệu người chơi.",
                false
            );

            return false;
        }

        if (playerStats.Level < bait.requiredLevel)
        {
            ShowMessage(
                "Bạn chưa đủ cấp độ.",
                false
            );

            return false;
        }

        if (playerStats.Money < totalPrice)
        {
            ShowMessage(
                "Bạn không đủ tiền.",
                false
            );

            return false;
        }

        bool added = inventoryManager.AddItem(
            bait.baitName,
            bait.icon,
            totalAmount
        );

        if (!added)
        {
            ShowMessage(
                "Balo không đủ chỗ.",
                false
            );

            return false;
        }

        bool paid =
            playerStats.SpendMoney(totalPrice);

        if (!paid)
        {
            ShowMessage(
                "Thanh toán thất bại.",
                false
            );

            return false;
        }

        RefreshMoney();

        ShowMessage(
            "Đã mua " +
            bait.baitName +
            " x" +
            totalAmount +
            ".",
            true
        );

        return true;
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

        for (int i = content.childCount - 1;
             i >= 0;
             i--)
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