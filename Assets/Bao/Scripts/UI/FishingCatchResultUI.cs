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

    private string itemName;
    private Sprite itemSprite;
    private bool isFish;
    private float weightKg;
    private float expReward;
    private int sellPrice;

    private PlayerStats playerStats;
    private InventoryManager inventoryManager;
    private Action onClosed;

    private void Awake()
    {
        Hide();

        if (storeButton != null)
            storeButton.onClick.AddListener(StoreItem);

        if (releaseButton != null)
            releaseButton.onClick.AddListener(ReleaseItem);

        if (sellButton != null)
            sellButton.onClick.AddListener(SellItem);
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
        itemName = resultItemName;
        itemSprite = resultSprite;
        isFish = resultIsFish;
        weightKg = resultWeightKg;
        expReward = resultExpReward;
        sellPrice = resultSellPrice;

        playerStats = stats;
        inventoryManager = inventory;
        onClosed = closeCallback;

        if (root != null)
            root.SetActive(true);

        UpdateUI();
    }

    private void UpdateUI()
    {
        if (titleText != null)
            titleText.text = isFish ? itemName.ToUpper() : "VẬT PHẨM LẠ";

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
                    " kg\nvà nhận được " +
                    expReward.ToString("0.##") +
                    " điểm kinh nghiệm câu cá.\nBạn muốn làm gì với nó?";
            }
            else
            {
                infoText.text =
                    "Bạn đã câu được " +
                    itemName +
                    ".\nBạn muốn làm gì với vật phẩm này?";
            }
        }

        if (storeMainText != null)
            storeMainText.text = "CẤT VÀO";

        if (releaseMainText != null)
            releaseMainText.text = isFish ? "THẢ RA" : "VỨT BỎ";

        if (releaseSubText != null)
            releaseSubText.text = isFish ? "+" + expReward.ToString("0.##") + " EXP" : "";

        if (sellMainText != null)
            sellMainText.text = "BÁN NGAY";

        if (sellSubText != null)
            sellSubText.text = "-30%";
    }

    private void StoreItem()
    {
        if (inventoryManager == null)
            inventoryManager = InventoryManager.Instance;

        if (inventoryManager != null)
        {
            inventoryManager.AddItem(itemName, itemSprite, 1);
            Debug.Log("Đã cất vào túi: " + itemName);
        }
        else
        {
            Debug.LogWarning("Không tìm thấy InventoryManager.");
        }

        Close();
    }

    private void ReleaseItem()
    {
        if (isFish)
        {
            if (playerStats != null)
            {
                playerStats.AddXP(Mathf.RoundToInt(expReward));
                Debug.Log("Đã thả cá và nhận EXP: " + expReward);
            }
        }
        else
        {
            Debug.Log("Đã vứt bỏ vật phẩm: " + itemName);
        }

        Close();
    }

    private void SellItem()
{
    if (playerStats != null)
    {
        playerStats.AddMoney(sellPrice);
        Debug.Log("Đã bán " + itemName + " nhận $" + sellPrice);
    }
    else
    {
        Debug.LogWarning("Không tìm thấy PlayerStats để cộng tiền.");
    }

    Close();
}

    private void Close()
    {
        Hide();
        onClosed?.Invoke();
    }

    public void Hide()
    {
        if (root != null)
            root.SetActive(false);
    }
}