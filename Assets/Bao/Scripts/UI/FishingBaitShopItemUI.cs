using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FishingBaitShopItemUI : MonoBehaviour
{
    [Header("Button")]
    [SerializeField] private Button buyButton;

    [Header("Thông tin")]
    [SerializeField] private Image baitIcon;
    [SerializeField] private TMP_Text baitNameText;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private TMP_Text quantityText;
    [SerializeField] private TMP_Text levelText;

    [Header("Trạng thái")]
    [SerializeField] private GameObject lockedOverlay;
    [SerializeField] private TMP_Text lockedText;

    private FishingBaitData baitData;
    private Action<FishingBaitData> onBuyClicked;

    public void Setup(
        FishingBaitData data,
        int playerLevel,
        Action<FishingBaitData> buyCallback)
    {
        baitData = data;
        onBuyClicked = buyCallback;

        if (baitData == null)
        {
            gameObject.SetActive(false);
            return;
        }

        bool unlocked =
            playerLevel >= baitData.requiredLevel;

        if (baitIcon != null)
        {
            baitIcon.sprite = baitData.icon;
            baitIcon.enabled = baitData.icon != null;
            baitIcon.preserveAspect = true;
        }

        if (baitNameText != null)
            baitNameText.text = baitData.baitName;

        if (priceText != null)
            priceText.text = baitData.price.ToString();

        if (quantityText != null)
        {
            quantityText.text =
                baitData.amountPerPurchase > 1
                    ? "x" + baitData.amountPerPurchase
                    : "";
        }

        if (levelText != null)
        {
            levelText.text =
                baitData.requiredLevel > 0
                    ? "Lv." + baitData.requiredLevel
                    : "";
        }

        if (lockedOverlay != null)
            lockedOverlay.SetActive(!unlocked);

        if (lockedText != null)
        {
            lockedText.text =
                "YÊU CẦU LEVEL " +
                baitData.requiredLevel;
        }

        if (buyButton == null)
            buyButton = GetComponent<Button>();

        if (buyButton != null)
        {
            buyButton.interactable = unlocked;
            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(HandleBuyClicked);
        }
    }

    private void HandleBuyClicked()
    {
        if (baitData == null)
            return;

        onBuyClicked?.Invoke(baitData);
    }
}