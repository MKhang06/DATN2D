using System;
using System.Collections;
using System.Globalization;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FishingEquipmentShopUI : MonoBehaviour
{
    public static FishingEquipmentShopUI Instance
    {
        get;
        private set;
    }

    public static FishingEquipmentShopItemData[]
        RegisteredEquipment
    {
        get;
        private set;
    }

    public FishingEquipmentShopItemData[]
        AvailableEquipment =>
            availableEquipment;

    public InventoryManager Inventory =>
        inventoryManager;

    [Header("Root trang bị")]
    [SerializeField]
    private GameObject equipmentRoot;

    [Header("Player")]
    [SerializeField]
    private PlayerStats playerStats;

    [SerializeField]
    private InventoryManager inventoryManager;

    [Header("Danh sách trang bị")]
    [SerializeField]
    private Transform content;

    [SerializeField]
    private FishingEquipmentShopItemUI
        equipmentCardPrefab;

    [SerializeField]
    private FishingEquipmentShopItemData[]
        availableEquipment;

    [Header("Xác nhận mua")]
    [SerializeField]
    private FishingPurchasePopupUI purchasePopupUI;

    [Header("Thông tin UI")]
    [SerializeField]
    private TMP_Text moneyText;

    [SerializeField]
    private TMP_Text messageText;

    [Header("Button tùy chọn")]
    [SerializeField]
    private Button refreshButton;

    [Header("Thông báo")]
    [SerializeField]
    private float messageDuration = 1.5f;

    private Coroutine messageCoroutine;

    private void Awake()
    {
        Instance = this;
        RegisterEquipmentSource();

        if (refreshButton != null)
        {
            refreshButton.onClick
                .RemoveAllListeners();

            refreshButton.onClick.AddListener(
                RefreshShopUI
            );
        }

        if (messageText != null)
        {
            messageText.gameObject
                .SetActive(false);
        }
    }

    private void OnEnable()
    {
        Instance = this;
        RegisterEquipmentSource();
    }

    private void Start()
    {
        FindReferences();
        RegisterEquipmentSource();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void RegisterEquipmentSource()
    {
        if (availableEquipment == null ||
            availableEquipment.Length == 0)
        {
            return;
        }

        RegisteredEquipment =
            availableEquipment;
    }

    private void FindReferences()
    {
        if (playerStats == null)
        {
            playerStats =
                FindFirstObjectByType<
                    PlayerStats
                >();
        }

        if (inventoryManager == null)
        {
            inventoryManager =
                InventoryManager.Instance;
        }

        if (inventoryManager == null)
        {
            inventoryManager =
                FindFirstObjectByType<
                    InventoryManager
                >();
        }

        if (purchasePopupUI == null)
        {
            purchasePopupUI =
                FindFirstObjectByType<
                    FishingPurchasePopupUI
                >();
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

        Instance = this;
        RegisterEquipmentSource();

        equipmentRoot.SetActive(true);

        FindReferences();
        RefreshShopUI();
    }

    public void Hide()
    {
        purchasePopupUI?.Hide();

        if (equipmentRoot != null)
            equipmentRoot.SetActive(false);

        if (messageCoroutine != null)
        {
            StopCoroutine(
                messageCoroutine
            );

            messageCoroutine = null;
        }

        if (messageText != null)
        {
            messageText.gameObject
                .SetActive(false);
        }
    }

    public void RefreshShopUI()
    {
        RegisterEquipmentSource();
        FindReferences();
        RefreshMoney();
        RefreshEquipmentList();
    }

    public void RefreshEquipmentList()
    {
        ClearEquipmentList();
        FindReferences();
        RegisterEquipmentSource();

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

        int createdCount = 0;

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
            card.transform.localScale =
                Vector3.one;

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

            createdCount++;
        }

        Canvas.ForceUpdateCanvases();

        if (content is
                RectTransform contentRect)
        {
            LayoutRebuilder
                .ForceRebuildLayoutImmediate(
                    contentRect
                );
        }

        Debug.Log(
            "Đã tạo " +
            createdCount +
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
            ShowPurchaseMessage(
                "Không tìm thấy PlayerStats.",
                false
            );

            return;
        }

        if (inventoryManager == null)
        {
            ShowPurchaseMessage(
                "Không tìm thấy InventoryManager.",
                false
            );

            return;
        }

        if (purchasePopupUI == null)
        {
            ShowPurchaseMessage(
                "Chưa gắn FishingPurchasePopupUI.",
                false
            );

            return;
        }

        if (playerStats.Level <
            equipment.requiredPlayerLevel)
        {
            ShowPurchaseMessage(
                "Bạn cần Level " +
                equipment.requiredPlayerLevel +
                " để mua " +
                equipment.ItemName +
                ".",
                false
            );

            return;
        }

        if (HasOwnedEquipment(
                equipment.ItemName))
        {
            ShowPurchaseMessage(
                "Bạn đã sở hữu " +
                equipment.ItemName +
                ".",
                false
            );

            return;
        }

        if (playerStats.Money <
            equipment.price)
        {
            ShowPurchaseMessage(
                "Bạn không đủ tiền mặt.",
                false
            );

            return;
        }

        purchasePopupUI
            .ShowEquipmentPurchase(
                equipment.ItemName,
                equipment.Icon,
                equipment.price,
                ignoredQuantity =>
                    ConfirmBuyEquipment(
                        equipment
                    )
            );
    }

    private bool ConfirmBuyEquipment(
        FishingEquipmentShopItemData shopItem)
    {
        if (shopItem == null ||
            shopItem.equipmentData == null)
        {
            ShowPurchaseMessage(
                "Dữ liệu trang bị không hợp lệ.",
                false
            );

            return false;
        }

        FindReferences();

        if (playerStats == null ||
            inventoryManager == null)
        {
            ShowPurchaseMessage(
                "Thiếu PlayerStats hoặc InventoryManager.",
                false
            );

            return false;
        }

        if (HasOwnedEquipment(
                shopItem.ItemName))
        {
            ShowPurchaseMessage(
                "Bạn đã sở hữu " +
                shopItem.ItemName +
                ".",
                false
            );

            return false;
        }

        int price =
            Mathf.Max(
                0,
                shopItem.price
            );

        if (playerStats.Money < price)
        {
            ShowPurchaseMessage(
                "Bạn không đủ tiền mặt.",
                false
            );

            return false;
        }

        bool paid =
            playerStats.SpendMoney(price);

        if (!paid)
        {
            ShowPurchaseMessage(
                "Thanh toán không thành công.",
                false
            );

            return false;
        }

        bool added =
            inventoryManager.AddItem(
                shopItem.ItemName,
                shopItem.Icon,
                1
            );

        if (!added)
        {
            if (price > 0)
                playerStats.AddMoney(price);

            ShowPurchaseMessage(
                "Túi đồ đã đầy. Tiền đã được hoàn lại.",
                false
            );

            return false;
        }

        ShowPurchaseMessage(
            "Đã mua " +
            shopItem.ItemName +
            ".",
            true
        );

        RegisterEquipmentSource();
        RefreshShopUI();

        return true;
    }

    private bool HasOwnedEquipment(
        string equipmentName)
    {
        if (inventoryManager == null ||
            string.IsNullOrWhiteSpace(
                equipmentName))
        {
            return false;
        }

        return HasName(
                   inventoryManager.HotbarSlots,
                   equipmentName
               ) ||
               HasName(
                   inventoryManager.BagSlots,
                   equipmentName
               );
    }

    private static bool HasName(
        InventoryManager.InventorySlot[] slots,
        string equipmentName)
    {
        if (slots == null)
            return false;

        string expected =
            NormalizeName(
                equipmentName
            );

        foreach (
            InventoryManager.InventorySlot slot
            in slots)
        {
            if (slot == null ||
                slot.IsEmpty)
            {
                continue;
            }

            if (NormalizeName(
                    slot.itemName) ==
                expected)
            {
                return true;
            }
        }

        return false;
    }

    private static string NormalizeName(
        string value)
    {
        if (string.IsNullOrWhiteSpace(
                value))
        {
            return string.Empty;
        }

        string decomposed =
            value.Trim()
                .ToLowerInvariant()
                .Normalize(
                    NormalizationForm.FormD
                );

        StringBuilder builder =
            new StringBuilder();

        foreach (char character
                 in decomposed)
        {
            UnicodeCategory category =
                CharUnicodeInfo
                    .GetUnicodeCategory(
                        character
                    );

            if (category ==
                UnicodeCategory
                    .NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetterOrDigit(
                    character))
            {
                builder.Append(character);
            }
        }

        return builder
            .ToString()
            .Normalize(
                NormalizationForm.FormC
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

    private void ClearEquipmentList()
    {
        if (content == null)
            return;

        for (int i =
                 content.childCount - 1;
             i >= 0;
             i--)
        {
            Destroy(
                content.GetChild(i)
                    .gameObject
            );
        }
    }

    private void ShowPurchaseMessage(
        string message,
        bool success)
    {
        ShowMessage(
            message,
            success
        );

        FishingNotificationUI notification =
            FishingNotificationUI.Instance;

        if (notification != null)
        {
            notification.ShowNotification(
                message,
                success
                    ? FishingNotificationUI
                        .NotificationType
                        .Success
                    : FishingNotificationUI
                        .NotificationType
                        .Warning
            );
        }

        Debug.Log(message);
    }

    private void ShowMessage(
        string message,
        bool success)
    {
        if (messageText == null)
            return;

        if (messageCoroutine != null)
        {
            StopCoroutine(
                messageCoroutine
            );
        }

        messageCoroutine =
            StartCoroutine(
                ShowMessageRoutine(
                    message,
                    success
                )
            );
    }

    private IEnumerator
        ShowMessageRoutine(
            string message,
            bool success)
    {
        messageText.gameObject
            .SetActive(true);

        messageText.color =
            success
                ? new Color(
                    0.2f,
                    1f,
                    0.65f
                )
                : new Color(
                    1f,
                    0.3f,
                    0.3f
                );

        messageText.text = message;

        yield return
            new WaitForSecondsRealtime(
                messageDuration
            );

        messageText.gameObject
            .SetActive(false);

        messageCoroutine = null;
    }
}
