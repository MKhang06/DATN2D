using System.Collections;
using TMPro;
using UnityEngine;

public class FishingEquipmentShopUI : MonoBehaviour
{
    [Header("Root trang bị")]
    [SerializeField] private GameObject equipmentRoot;

    [Header("Player")]
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private InventoryManager inventoryManager;

    [Header("Danh sách trang bị")]
    [SerializeField] private Transform content;
    [SerializeField] private FishingEquipmentShopItemUI equipmentCardPrefab;
    [SerializeField] private FishingEquipmentShopItemData[] availableEquipment;

    [Header("Thông tin UI")]
    [SerializeField] private TMP_Text moneyText;
    [SerializeField] private TMP_Text messageText;

    [Header("Thông báo")]
    [SerializeField] private float messageDuration = 1.5f;

    private Coroutine messageCoroutine;

    private void FindReferences()
    {
        if (playerStats == null)
            playerStats = FindFirstObjectByType<PlayerStats>();

        if (inventoryManager == null)
            inventoryManager = InventoryManager.Instance;

        if (inventoryManager == null)
            inventoryManager = FindFirstObjectByType<InventoryManager>();
    }

    public void Show()
    {
        if (equipmentRoot != null)
            equipmentRoot.SetActive(true);

        FindReferences();
        RefreshEquipmentList();
        RefreshMoney();
    }

    public void Hide()
    {
        if (equipmentRoot != null)
            equipmentRoot.SetActive(false);
    }

    public void RefreshEquipmentList()
    {
        ClearEquipmentList();
        FindReferences();

        if (content == null)
        {
            Debug.LogWarning("Chưa gắn Content trang bị.");
            return;
        }

        if (equipmentCardPrefab == null)
        {
            Debug.LogWarning("Chưa gắn Equipment Card Prefab.");
            return;
        }

        if (availableEquipment == null ||
            availableEquipment.Length == 0)
        {
            Debug.LogWarning("Available Equipment đang trống.");
            return;
        }

        int playerLevel =
            playerStats != null ? playerStats.Level : 0;

        foreach (FishingEquipmentShopItemData item
                 in availableEquipment)
        {
            if (item == null || item.equipmentData == null)
                continue;

            FishingEquipmentShopItemUI card =
                Instantiate(equipmentCardPrefab, content);

            card.name = "EquipmentCard_" + item.ItemName;

            card.Setup(
                item,
                playerLevel,
                TryBuyEquipment
            );
        }

        Canvas.ForceUpdateCanvases();
    }

    private void TryBuyEquipment(
        FishingEquipmentShopItemData item)
    {
        if (item == null || item.equipmentData == null)
            return;

        FindReferences();

        if (playerStats == null)
        {
            ShowMessage("Không tìm thấy PlayerStats.", false);
            return;
        }

        if (inventoryManager == null)
        {
            ShowMessage("Không tìm thấy InventoryManager.", false);
            return;
        }

        if (playerStats.Level < item.requiredPlayerLevel)
        {
            ShowMessage(
                "Cần Level " +
                item.requiredPlayerLevel +
                " để mua.",
                false
            );

            return;
        }

        if (playerStats.Money < item.price)
        {
            ShowMessage("Không đủ tiền.", false);
            return;
        }

        bool added = inventoryManager.AddItem(
            item.ItemName,
            item.Icon,
            item.amountPerPurchase
        );

        if (!added)
        {
            ShowMessage("Túi đồ đã đầy.", false);
            return;
        }

        bool paid = playerStats.SpendMoney(item.price);

        if (!paid)
        {
            ShowMessage("Thanh toán thất bại.", false);
            return;
        }

        RefreshMoney();

        ShowMessage(
            "Đã mua " +
            item.ItemName +
            " x" +
            item.amountPerPurchase,
            true
        );
    }

    private void RefreshMoney()
    {
        if (moneyText == null)
            return;

        int money =
            playerStats != null ? playerStats.Money : 0;

        moneyText.text =
            "TIỀN MẶT <color=#25F2C4>$" +
            money.ToString("N0") +
            "</color>";
    }

    private void ClearEquipmentList()
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