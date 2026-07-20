using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FishingEquipmentShopUI : MonoBehaviour
{
    [Header("Root trang bị")]
    [SerializeField] private GameObject equipmentRoot;

    [Header("Player")]
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private InventoryManager inventoryManager;

    [Header("Danh sách trang bị")]
    [SerializeField] private Transform content;

    [SerializeField]
    private FishingEquipmentShopItemUI equipmentCardPrefab;

    [SerializeField]
    private FishingEquipmentShopItemData[] availableEquipment;

    [Header("Xác nhận mua")]
    [SerializeField]
    private FishingPurchasePopupUI purchasePopupUI;

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
                RefreshEquipmentList
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
        if (equipmentRoot == null)
        {
            Debug.LogWarning(
                "FishingEquipmentShopUI chưa gắn Equipment Root."
            );

            return;
        }

        equipmentRoot.SetActive(true);

        FindReferences();
        RefreshEquipmentList();
        RefreshMoney();
    }

    public void Hide()
    {
        purchasePopupUI?.Hide();

        if (equipmentRoot != null)
            equipmentRoot.SetActive(false);

        if (messageCoroutine != null)
        {
            StopCoroutine(messageCoroutine);
            messageCoroutine = null;
        }

        if (messageText != null)
            messageText.gameObject.SetActive(false);
    }

    public void RefreshEquipmentList()
    {
        ClearEquipmentList();
        FindReferences();

        if (content == null)
        {
            Debug.LogWarning(
                "FishingEquipmentShopUI chưa gắn Content."
            );

            return;
        }

        if (equipmentCardPrefab == null)
        {
            Debug.LogWarning(
                "Chưa gắn Equipment Card Prefab."
            );

            return;
        }

        if (availableEquipment == null ||
            availableEquipment.Length == 0)
        {
            Debug.LogWarning(
                "Available Equipment đang trống."
            );

            return;
        }

        int playerLevel =
            playerStats != null
                ? playerStats.Level
                : 0;

        foreach (
            FishingEquipmentShopItemData equipment
            in availableEquipment)
        {
            if (equipment == null ||
                equipment.equipmentData == null)
            {
                continue;
            }

            FishingEquipmentShopItemUI card =
                Instantiate(
                    equipmentCardPrefab,
                    content
                );

            card.gameObject.SetActive(true);
            card.transform.localScale = Vector3.one;
            card.transform.localRotation =
                Quaternion.identity;

            card.name =
                "EquipmentCard_" +
                equipment.ItemName;

            card.Setup(
                equipment,
                playerLevel,
                TryBuyEquipment
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
            " card trang bị."
        );
    }

    private void TryBuyEquipment(
        FishingEquipmentShopItemData equipment)
    {
        if (equipment == null ||
            equipment.equipmentData == null)
        {
            return;
        }

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

        if (playerStats.Level <
            equipment.requiredPlayerLevel)
        {
            ShowMessage(
                "Bạn cần Level " +
                equipment.requiredPlayerLevel +
                " để mua " +
                equipment.ItemName +
                ".",
                false
            );

            return;
        }

        if (playerStats.Money < equipment.price)
        {
            ShowMessage(
                "Bạn không đủ tiền.",
                false
            );

            return;
        }

        purchasePopupUI.ShowEquipmentPurchase(
            equipment.ItemName,
            equipment.Icon,
            equipment.price,
            quantity =>
                ConfirmBuyEquipment(equipment)
        );
    }

    private bool ConfirmBuyEquipment(
        FishingEquipmentShopItemData equipment)
    {
        if (equipment == null ||
            equipment.equipmentData == null)
        {
            return false;
        }

        FindReferences();

        if (playerStats == null ||
            inventoryManager == null)
        {
            ShowMessage(
                "Không tìm thấy dữ liệu người chơi.",
                false
            );

            return false;
        }

        if (playerStats.Level <
            equipment.requiredPlayerLevel)
        {
            ShowMessage(
                "Bạn chưa đủ cấp độ.",
                false
            );

            return false;
        }

        if (playerStats.Money < equipment.price)
        {
            ShowMessage(
                "Bạn không đủ tiền.",
                false
            );

            return false;
        }

        // Trang bị luôn chỉ mua đúng một món.
        bool added = inventoryManager.AddItem(
            equipment.ItemName,
            equipment.Icon,
            1
        );

        if (!added)
        {
            ShowMessage(
                "Balo đã đầy.",
                false
            );

            return false;
        }

        bool paid =
            playerStats.SpendMoney(
                equipment.price
            );

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
            equipment.ItemName +
            " x1.",
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

    private void ClearEquipmentList()
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