using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class ToolShopPurchasePopupUI : MonoBehaviour
{
    [SerializeField] private GameObject popupRoot;
    [SerializeField] private Image itemIcon;
    [SerializeField] private TMP_Text itemNameText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text unitPriceText;
    [SerializeField] private TMP_Text totalPriceText;
    [SerializeField] private TMP_InputField quantityInput;
    [SerializeField] private Button decreaseButton;
    [SerializeField] private Button increaseButton;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;

    private ToolShopItemData currentItem;
    private Action<ToolShopItemData, int> confirmCallback;

    private void Awake()
    {
        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveAllListeners();
            confirmButton.onClick.AddListener(ConfirmPurchase);
        }

        if (cancelButton != null)
        {
            cancelButton.onClick.RemoveAllListeners();
            cancelButton.onClick.AddListener(Hide);
        }

        if (decreaseButton != null)
        {
            decreaseButton.onClick.RemoveAllListeners();
            decreaseButton.onClick.AddListener(() => ChangeQuantity(-1));
        }

        if (increaseButton != null)
        {
            increaseButton.onClick.RemoveAllListeners();
            increaseButton.onClick.AddListener(() => ChangeQuantity(1));
        }

        if (quantityInput != null)
        {
            quantityInput.contentType = TMP_InputField.ContentType.IntegerNumber;
            quantityInput.onValueChanged.RemoveAllListeners();
            quantityInput.onValueChanged.AddListener(_ => RefreshTotalPrice());
        }

        Hide();
    }

    public void Show(
        ToolShopItemData item,
        Action<ToolShopItemData, int> callback)
    {
        if (item == null)
            return;

        currentItem = item;
        confirmCallback = callback;

        if (popupRoot != null)
            popupRoot.SetActive(true);

        if (itemIcon != null)
        {
            itemIcon.sprite = item.Icon;
            itemIcon.enabled = item.Icon != null;
            itemIcon.preserveAspect = true;
        }

        if (itemNameText != null)
            itemNameText.text = item.DisplayName;

        if (descriptionText != null)
            descriptionText.text = item.Description;

        if (unitPriceText != null)
            unitPriceText.text = "Đơn giá: $" + item.Price.ToString("N0");

        int maximum = GetMaximumQuantity(item);

        if (quantityInput != null)
        {
            quantityInput.interactable = maximum > 1;
            quantityInput.text = "1";
        }

        if (decreaseButton != null)
            decreaseButton.interactable = maximum > 1;

        if (increaseButton != null)
            increaseButton.interactable = maximum > 1;

        RefreshTotalPrice();
    }

    public void Hide()
    {
        currentItem = null;
        confirmCallback = null;

        if (popupRoot != null)
            popupRoot.SetActive(false);
    }

    private void ChangeQuantity(int delta)
    {
        if (currentItem == null)
            return;

        int quantity = Mathf.Clamp(
            GetQuantity() + delta,
            1,
            GetMaximumQuantity(currentItem)
        );

        if (quantityInput != null)
            quantityInput.text = quantity.ToString();
    }

    private void ConfirmPurchase()
    {
        if (currentItem == null)
            return;

        int quantity = Mathf.Clamp(
            GetQuantity(),
            1,
            GetMaximumQuantity(currentItem)
        );

        confirmCallback?.Invoke(currentItem, quantity);
    }

    private int GetQuantity()
    {
        if (quantityInput == null)
            return 1;

        if (!int.TryParse(quantityInput.text, out int quantity))
            quantity = 1;

        return Mathf.Max(1, quantity);
    }

    private static int GetMaximumQuantity(ToolShopItemData item)
    {
        if (item == null || item.UniqueOwnership)
            return 1;

        return Mathf.Max(1, item.maxPurchaseQuantity);
    }

    private void RefreshTotalPrice()
    {
        int quantity = currentItem == null
            ? 1
            : Mathf.Clamp(
                GetQuantity(),
                1,
                GetMaximumQuantity(currentItem)
            );

        int total = currentItem != null
            ? currentItem.Price * quantity
            : 0;

        if (totalPriceText != null)
            totalPriceText.text = "Tổng: $" + total.ToString("N0");
    }
}
