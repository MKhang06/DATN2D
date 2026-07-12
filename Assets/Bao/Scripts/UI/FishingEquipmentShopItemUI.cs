using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FishingEquipmentShopItemUI : MonoBehaviour
{
    [Header("Nút mua")]
    [SerializeField] private Button buyButton;

    [Header("Thông tin trang bị")]
    [SerializeField] private Image itemIcon;
    [SerializeField] private TMP_Text itemNameText;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private TMP_Text quantityText;
    [SerializeField] private TMP_Text levelText;

    [Header("Trạng thái khóa")]
    [SerializeField] private GameObject lockedOverlay;
    [SerializeField] private TMP_Text lockedText;

    private FishingEquipmentShopItemData itemData;
    private Action<FishingEquipmentShopItemData> buyCallback;

    private void Awake()
    {
        if (buyButton == null)
            buyButton = GetComponent<Button>();
    }

    public void Setup(
        FishingEquipmentShopItemData data,
        int playerLevel,
        Action<FishingEquipmentShopItemData> callback)
    {
        itemData = data;
        buyCallback = callback;

        if (itemData == null)
        {
            Debug.LogWarning(
                name + ": chưa có FishingEquipmentShopItemData."
            );

            gameObject.SetActive(false);
            return;
        }

        if (itemData.equipmentData == null)
        {
            Debug.LogWarning(
                name + ": chưa gắn Equipment Data."
            );

            gameObject.SetActive(false);
            return;
        }

        bool isUnlocked =
            playerLevel >= itemData.requiredPlayerLevel;

        RefreshIcon();
        RefreshTexts();
        RefreshLockState(isUnlocked);
        SetupBuyButton(isUnlocked);
    }

    private void RefreshIcon()
    {
        if (itemIcon == null)
            return;

        itemIcon.sprite = itemData.Icon;
        itemIcon.enabled = itemData.Icon != null;
        itemIcon.preserveAspect = true;
    }

    private void RefreshTexts()
    {
        if (itemNameText != null)
            itemNameText.text = itemData.ItemName;

        if (priceText != null)
            priceText.text = itemData.price.ToString("N0");

        if (quantityText != null)
        {
            quantityText.text =
                itemData.amountPerPurchase > 1
                    ? "x" + itemData.amountPerPurchase
                    : "";
        }

        if (levelText != null)
        {
            levelText.text =
                itemData.requiredPlayerLevel > 0
                    ? "Lv." + itemData.requiredPlayerLevel
                    : "";
        }
    }

    private void RefreshLockState(bool isUnlocked)
    {
        if (lockedOverlay != null)
            lockedOverlay.SetActive(!isUnlocked);

        if (lockedText != null)
        {
            lockedText.text =
                "YÊU CẦU LEVEL " +
                itemData.requiredPlayerLevel;
        }
    }

    private void SetupBuyButton(bool isUnlocked)
    {
        if (buyButton == null)
            return;

        buyButton.interactable = isUnlocked;

        buyButton.onClick.RemoveAllListeners();
        buyButton.onClick.AddListener(HandleBuyClicked);
    }

    private void HandleBuyClicked()
    {
        if (itemData == null)
            return;

        buyCallback?.Invoke(itemData);
    }
}