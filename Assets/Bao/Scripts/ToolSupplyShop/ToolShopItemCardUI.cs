using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class ToolShopItemCardUI : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler
{
    [SerializeField] private Image itemIcon;
    [SerializeField] private TMP_Text itemNameText;
    [SerializeField] private TMP_Text categoryText;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private GameObject buyButtonObject;
    [SerializeField] private Button buyButton;

    private ToolShopItemData currentItem;
    private Action<ToolShopItemData> buyCallback;

    public void Setup(
        ToolShopItemData item,
        Action<ToolShopItemData> onBuy)
    {
        currentItem = item;
        buyCallback = onBuy;

        if (itemIcon != null)
        {
            itemIcon.sprite = item != null ? item.Icon : null;
            itemIcon.enabled = itemIcon.sprite != null;
            itemIcon.preserveAspect = true;
        }

        if (itemNameText != null)
            itemNameText.text = item != null ? item.DisplayName : "Không có tên";

        if (categoryText != null)
            categoryText.text = item != null ? item.category : string.Empty;

        if (priceText != null)
            priceText.text = item != null ? "$" + item.Price.ToString("N0") : "$0";

        if (buyButton != null)
        {
            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(HandleBuyClicked);
        }

        SetHoverVisible(false);
    }

    public void OnPointerEnter(PointerEventData eventData) =>
        SetHoverVisible(true);

    public void OnPointerExit(PointerEventData eventData) =>
        SetHoverVisible(false);

    private void HandleBuyClicked()
    {
        if (currentItem != null)
            buyCallback?.Invoke(currentItem);
    }

    private void SetHoverVisible(bool visible)
    {
        if (buyButtonObject != null)
            buyButtonObject.SetActive(visible);
    }
}
