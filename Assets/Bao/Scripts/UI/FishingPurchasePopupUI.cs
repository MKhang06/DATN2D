using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FishingPurchasePopupUI : MonoBehaviour
{
    [Header("Root popup")]
    [SerializeField] private GameObject popupRoot;

    [Header("Bảng nhập số lượng")]
    [SerializeField] private GameObject quantityPanel;
    [SerializeField] private Image quantityItemPreview;
    [SerializeField] private TMP_Text quantityItemNameText;
    [SerializeField] private TMP_InputField quantityInput;
    [SerializeField] private Button quantityCancelButton;
    [SerializeField] private Button quantityContinueButton;

    [Header("Bảng xác nhận cuối")]
    [SerializeField] private GameObject finalConfirmPanel;
    [SerializeField] private Image finalItemPreview;
    [SerializeField] private TMP_Text finalItemNameText;
    [SerializeField] private TMP_Text finalDescriptionText;
    [SerializeField] private TMP_Text orderValueText;
    [SerializeField] private Button finalCancelButton;
    [SerializeField] private Button finalConfirmButton;

    [Header("Giới hạn mặc định")]
    [SerializeField, Min(1)]
    private int defaultMaximumQuantity = 999;

    private string currentItemName;
    private Sprite currentItemIcon;

    private int unitPrice;
    private int selectedQuantity = 1;
    private int maximumQuantity = 1;

    private bool allowMultipleQuantity;
    private bool isOpen;

    private Func<int, bool> purchaseCallback;

    public bool IsOpen => isOpen;

    private void Awake()
    {
        SetupButtons();

        if (quantityInput != null)
        {
            quantityInput.contentType =
                TMP_InputField.ContentType.IntegerNumber;

            quantityInput.lineType =
                TMP_InputField.LineType.SingleLine;

            quantityInput.onEndEdit.RemoveAllListeners();
            quantityInput.onEndEdit.AddListener(
                ValidateQuantityInput
            );
        }

        Hide();
    }

    private void Update()
    {
        if (!isOpen)
            return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Hide();
            return;
        }

        bool enterPressed =
            Input.GetKeyDown(KeyCode.Return) ||
            Input.GetKeyDown(KeyCode.KeypadEnter);

        if (!enterPressed)
            return;

        if (quantityPanel != null &&
            quantityPanel.activeSelf)
        {
            ContinueToFinalConfirmation();
        }
        else if (finalConfirmPanel != null &&
                 finalConfirmPanel.activeSelf)
        {
            ConfirmFinalPurchase();
        }
    }

    private void SetupButtons()
    {
        if (quantityCancelButton != null)
        {
            quantityCancelButton.onClick.RemoveAllListeners();
            quantityCancelButton.onClick.AddListener(Hide);
        }

        if (quantityContinueButton != null)
        {
            quantityContinueButton.onClick.RemoveAllListeners();
            quantityContinueButton.onClick.AddListener(
                ContinueToFinalConfirmation
            );
        }

        if (finalCancelButton != null)
        {
            finalCancelButton.onClick.RemoveAllListeners();
            finalCancelButton.onClick.AddListener(
                BackToQuantityPanel
            );
        }

        if (finalConfirmButton != null)
        {
            finalConfirmButton.onClick.RemoveAllListeners();
            finalConfirmButton.onClick.AddListener(
                ConfirmFinalPurchase
            );
        }
    }

    public void ShowBaitPurchase(
        string itemName,
        Sprite icon,
        int price,
        int maximum,
        Func<int, bool> onPurchaseConfirmed)
    {
        PreparePurchase(
            itemName,
            icon,
            price,
            canBuyMultiple: true,
            maximum: maximum,
            onPurchaseConfirmed
        );
    }

    public void ShowEquipmentPurchase(
        string itemName,
        Sprite icon,
        int price,
        Func<int, bool> onPurchaseConfirmed)
    {
        PreparePurchase(
            itemName,
            icon,
            price,
            canBuyMultiple: false,
            maximum: 1,
            onPurchaseConfirmed
        );
    }

    private void PreparePurchase(
        string itemName,
        Sprite icon,
        int price,
        bool canBuyMultiple,
        int maximum,
        Func<int, bool> onPurchaseConfirmed)
    {
        currentItemName = itemName;
        currentItemIcon = icon;

        unitPrice = Mathf.Max(0, price);
        allowMultipleQuantity = canBuyMultiple;

        maximumQuantity = canBuyMultiple
            ? Mathf.Clamp(
                maximum,
                1,
                defaultMaximumQuantity
            )
            : 1;

        selectedQuantity = 1;
        purchaseCallback = onPurchaseConfirmed;

        OpenQuantityPanel();
    }

    private void OpenQuantityPanel()
    {
        isOpen = true;

        if (popupRoot != null)
            popupRoot.SetActive(true);

        if (quantityPanel != null)
            quantityPanel.SetActive(true);

        if (finalConfirmPanel != null)
            finalConfirmPanel.SetActive(false);

        SetItemImage(
            quantityItemPreview,
            currentItemIcon
        );

        if (quantityItemNameText != null)
            quantityItemNameText.text = currentItemName;

        if (quantityInput != null)
        {
            quantityInput.interactable =
                allowMultipleQuantity;

            quantityInput.SetTextWithoutNotify("1");

            if (allowMultipleQuantity)
            {
                quantityInput.Select();
                quantityInput.ActivateInputField();
            }
        }
    }

    private void ContinueToFinalConfirmation()
    {
        ReadQuantityFromInput();
        OpenFinalConfirmation();
    }

    private void OpenFinalConfirmation()
    {
        if (popupRoot != null)
            popupRoot.SetActive(true);

        if (quantityPanel != null)
            quantityPanel.SetActive(false);

        if (finalConfirmPanel != null)
            finalConfirmPanel.SetActive(true);

        SetItemImage(
            finalItemPreview,
            currentItemIcon
        );

        if (finalItemNameText != null)
        {
            finalItemNameText.text =
                currentItemName +
                " x" +
                selectedQuantity;
        }

        if (finalDescriptionText != null)
        {
            finalDescriptionText.text =
                "Bạn có chắc chắn muốn thanh toán " +
                "bằng Tiền mặt không?";
        }

        long totalPrice =
            (long)unitPrice * selectedQuantity;

        if (orderValueText != null)
        {
            orderValueText.text =
                "<color=#80FF63>$" +
                totalPrice.ToString("N0") +
                "</color>";
        }
    }

    private void BackToQuantityPanel()
    {
        OpenQuantityPanel();

        if (quantityInput != null)
        {
            quantityInput.SetTextWithoutNotify(
                selectedQuantity.ToString()
            );
        }
    }

    private void ValidateQuantityInput(string value)
    {
        if (!allowMultipleQuantity)
        {
            selectedQuantity = 1;

            if (quantityInput != null)
                quantityInput.SetTextWithoutNotify("1");

            return;
        }

        if (!int.TryParse(value, out int parsed))
            parsed = 1;

        selectedQuantity = Mathf.Clamp(
            parsed,
            1,
            maximumQuantity
        );

        if (quantityInput != null)
        {
            quantityInput.SetTextWithoutNotify(
                selectedQuantity.ToString()
            );
        }
    }

    private void ReadQuantityFromInput()
    {
        if (!allowMultipleQuantity)
        {
            selectedQuantity = 1;
            return;
        }

        if (quantityInput == null ||
            !int.TryParse(
                quantityInput.text,
                out int parsed))
        {
            parsed = 1;
        }

        selectedQuantity = Mathf.Clamp(
            parsed,
            1,
            maximumQuantity
        );

        if (quantityInput != null)
        {
            quantityInput.SetTextWithoutNotify(
                selectedQuantity.ToString()
            );
        }
    }

    private void ConfirmFinalPurchase()
    {
        if (purchaseCallback == null)
        {
            Hide();
            return;
        }

        bool success =
            purchaseCallback.Invoke(selectedQuantity);

        if (success)
            Hide();
    }

    public void Hide()
    {
        isOpen = false;
        purchaseCallback = null;

        if (quantityInput != null)
            quantityInput.DeactivateInputField();

        if (quantityPanel != null)
            quantityPanel.SetActive(false);

        if (finalConfirmPanel != null)
            finalConfirmPanel.SetActive(false);

        if (popupRoot != null)
            popupRoot.SetActive(false);
    }

    private void SetItemImage(
        Image targetImage,
        Sprite sprite)
    {
        if (targetImage == null)
            return;

        targetImage.sprite = sprite;
        targetImage.enabled = sprite != null;
        targetImage.preserveAspect = true;
    }
}