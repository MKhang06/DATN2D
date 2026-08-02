using UnityEngine;
using UnityEngine.UI;

public class FishingShopTabsUI : MonoBehaviour
{
    [Header("Mở / đóng cửa hàng")]
    [SerializeField] private KeyCode toggleKey = KeyCode.L;
    [SerializeField] private bool allowKeyboardToggle = true;
    [SerializeField] private bool hideOnStart = true;

    [Header("Root")]
    [SerializeField] private GameObject root;

    [Header("Các trang")]
    [SerializeField] private GameObject equipmentPage;
    [SerializeField] private GameObject baitPage;

    [Header("Script cửa hàng")]
    [SerializeField] private FishingEquipmentShopUI equipmentShopUI;
    [SerializeField] private FishingBaitShopUI baitShopUI;
    [SerializeField] private FishingPurchasePopupUI purchasePopupUI;

    [Header("Button danh mục")]
    [SerializeField] private Button equipmentButton;
    [SerializeField] private Button baitButton;

    [Header("Hiệu ứng đang chọn")]
    [SerializeField] private GameObject equipmentSelectedBackground;
    [SerializeField] private GameObject baitSelectedBackground;

    [Header("Nút đóng")]
    [SerializeField] private Button closeButton;

    private bool isOpen;

    public bool IsOpen => isOpen;

    private void Awake()
    {
        FindReferences();
        SetupButtons();

        if (hideOnStart)
        {
            isOpen = false;

            if (root != null)
                root.SetActive(false);
        }
    }

    private void Start()
    {
        if (!hideOnStart)
            Open();
    }

    private void Update()
    {
        if (allowKeyboardToggle &&
            Input.GetKeyDown(toggleKey))
        {
            Toggle();
        }

        if (isOpen &&
            Input.GetKeyDown(KeyCode.Escape) &&
            (purchasePopupUI == null ||
             !purchasePopupUI.IsOpen))
        {
            Close();
        }
    }

    private void FindReferences()
    {
        if (equipmentShopUI == null)
            equipmentShopUI =
                GetComponent<FishingEquipmentShopUI>();

        if (baitShopUI == null)
            baitShopUI =
                GetComponent<FishingBaitShopUI>();

        if (purchasePopupUI == null)
            purchasePopupUI =
                GetComponent<FishingPurchasePopupUI>();
    }

    private void SetupButtons()
    {
        if (equipmentButton != null)
        {
            equipmentButton.onClick.RemoveAllListeners();
            equipmentButton.onClick.AddListener(
                ShowEquipment
            );
        }

        if (baitButton != null)
        {
            baitButton.onClick.RemoveAllListeners();
            baitButton.onClick.AddListener(
                ShowBait
            );
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(Close);
        }
    }

    public void Toggle()
    {
        if (isOpen)
            Close();
        else
            Open();
    }

    public void Open()
    {
        if (root == null)
        {
            Debug.LogWarning(
                "FishingShopTabsUI chưa gắn Root."
            );

            return;
        }

        isOpen = true;
        root.SetActive(true);

        ShowEquipment();

        GameLockManager.Instance?.LockPlayer();
    }

    public void Close()
    {
        isOpen = false;

        purchasePopupUI?.Hide();

        if (equipmentPage != null)
            equipmentPage.SetActive(false);

        if (baitPage != null)
            baitPage.SetActive(false);

        if (root != null)
            root.SetActive(false);

        GameLockManager.Instance?.UnlockPlayer();
    }

    public void ShowEquipment()
    {
        EnsureOpen();

        purchasePopupUI?.Hide();

        if (baitPage != null)
            baitPage.SetActive(false);

        if (equipmentPage != null)
            equipmentPage.SetActive(true);

        equipmentShopUI?.Show();

        SetSelectedTab(true);
    }

    public void ShowBait()
    {
        EnsureOpen();

        purchasePopupUI?.Hide();

        if (equipmentPage != null)
            equipmentPage.SetActive(false);

        if (baitPage != null)
            baitPage.SetActive(true);

        baitShopUI?.Show();

        SetSelectedTab(false);
    }

    private void EnsureOpen()
    {
        if (isOpen)
            return;

        isOpen = true;

        if (root != null)
            root.SetActive(true);

        GameLockManager.Instance?.LockPlayer();
    }

    private void SetSelectedTab(
        bool equipmentSelected)
    {
        if (equipmentSelectedBackground != null)
        {
            equipmentSelectedBackground.SetActive(
                equipmentSelected
            );
        }

        if (baitSelectedBackground != null)
        {
            baitSelectedBackground.SetActive(
                !equipmentSelected
            );
        }
    }
}