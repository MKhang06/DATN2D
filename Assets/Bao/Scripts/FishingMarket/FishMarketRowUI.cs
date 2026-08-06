using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class FishMarketRowUI : MonoBehaviour
{
    [Header("Lịch sử giá")]
    [SerializeField]
    private TMP_Text[] historyPriceTexts =
        new TMP_Text[3];

    [SerializeField]
    private TMP_Text[] historyTimeTexts =
        new TMP_Text[3];

    [Header("Giá hiện tại")]
    [SerializeField]
    private TMP_Text currentPriceText;

    [SerializeField]
    private TMP_Text priceUnitText;

    [Header("Thông tin cá")]
    [SerializeField]
    private Image fishIcon;

    [SerializeField]
    private TMP_Text fishNameText;

    [SerializeField]
    private TMP_Text ownedAmountText;

    [Header("Bán")]
    [SerializeField]
    private Button sellButton;

    [SerializeField]
    private TMP_Text sellButtonText;

    [SerializeField]
    private TMP_Text payoutText;

    [Header("Trạng thái")]
    [SerializeField]
    private CanvasGroup canvasGroup;

    [SerializeField]
    private Color availableColor =
        Color.white;

    [SerializeField]
    private Color unavailableColor =
        new Color(1f, 1f, 1f, 0.38f);

    private InventoryItemData boundItem;
    private Action<InventoryItemData> onSell;
    private bool canSell;
    private bool invokingSell;

    private void Awake()
    {
        if (canvasGroup == null)
        {
            canvasGroup =
                GetComponent<CanvasGroup>();
        }

        if (canvasGroup == null)
        {
            canvasGroup =
                gameObject.AddComponent<
                    CanvasGroup
                >();
        }

        if (sellButton != null)
        {
            sellButton.onClick
                .RemoveListener(
                    HandleSellClicked
                );

            sellButton.onClick
                .AddListener(
                    HandleSellClicked
                );
        }
    }

    public void Bind(
        FishMarketRowView view,
        Action<InventoryItemData>
            sellCallback)
    {
        boundItem = view.Item;
        onSell = sellCallback;

        Refresh(view);
    }

    public void Refresh(
        FishMarketRowView view)
    {
        boundItem = view.Item;

        SetHistoryText(
            0,
            view.HistoryPrice3,
            "3 GIỜ TRƯỚC"
        );

        SetHistoryText(
            1,
            view.HistoryPrice2,
            "2 GIỜ TRƯỚC"
        );

        SetHistoryText(
            2,
            view.HistoryPrice1,
            "1 GIỜ TRƯỚC"
        );

        if (currentPriceText != null)
        {
            currentPriceText.text =
                FormatPrice(
                    view.CurrentPrice
                );
        }

        if (priceUnitText != null)
            priceUnitText.text = "MỖI CON";

        if (fishNameText != null)
        {
            fishNameText.text =
                view.FishName
                    .ToUpperInvariant();
        }

        if (ownedAmountText != null)
        {
            ownedAmountText.text =
                "ĐANG CÓ " +
                view.OwnedAmount
                    .ToString("N0") +
                " CON";
        }

        if (fishIcon != null)
        {
            fishIcon.sprite =
                view.Icon;

            fishIcon.enabled =
                view.Icon != null;

            fishIcon.preserveAspect = true;
            fishIcon.raycastTarget = false;
        }

        if (sellButtonText != null)
        {
            sellButtonText.text = view.CanSell
                ? "BÁN " +
                  view.OwnedAmount.ToString("N0") +
                  " CON"
                : "HẾT CÁ";
        }

        if (payoutText != null)
        {
            payoutText.text =
                "+ $" +
                view.TotalPayout
                    .ToString("N0");
        }

        canSell = view.CanSell;

        if (sellButton != null)
            sellButton.interactable =
                canSell &&
                !invokingSell;

        if (canvasGroup != null)
        {
            canvasGroup.alpha =
                canSell
                    ? availableColor.a
                    : unavailableColor.a;

            canvasGroup.interactable = canSell;
            canvasGroup.blocksRaycasts = true;
        }
    }

    private void SetHistoryText(
        int index,
        float price,
        string timeLabel)
    {
        if (historyPriceTexts != null &&
            index >= 0 &&
            index < historyPriceTexts.Length &&
            historyPriceTexts[index] != null)
        {
            historyPriceTexts[index].text =
                FormatPrice(price);
        }

        if (historyTimeTexts != null &&
            index >= 0 &&
            index < historyTimeTexts.Length &&
            historyTimeTexts[index] != null)
        {
            historyTimeTexts[index].text =
                timeLabel;
        }
    }

    private static string FormatPrice(
        float value)
    {
        return "$" +
               Mathf.Max(0f, value)
                   .ToString("N2");
    }

    private void HandleSellClicked()
    {
        if (boundItem == null ||
            onSell == null ||
            !canSell ||
            invokingSell)
        {
            return;
        }

        invokingSell = true;

        if (sellButton != null)
            sellButton.interactable = false;

        try
        {
            onSell.Invoke(boundItem);
        }
        finally
        {
            invokingSell = false;

            if (sellButton != null)
                sellButton.interactable = canSell;
        }
    }

    private void OnDestroy()
    {
        if (sellButton != null)
        {
            sellButton.onClick
                .RemoveListener(
                    HandleSellClicked
                );
        }
    }
}
