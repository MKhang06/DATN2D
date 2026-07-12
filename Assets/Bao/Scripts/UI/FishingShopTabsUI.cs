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
        FindShopScripts();
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
        {
            Open();
        }
        else
        {
            SetPages(false, false);
        }
    }

    private void Update()
    {
        if (allowKeyboardToggle &&
            Input.GetKeyDown(toggleKey))
        {
            Toggle();
        }

        if (isOpen &&
            Input.GetKeyDown(KeyCode.Escape))
        {
            Close();
        }
    }

    private void FindShopScripts()
    {
        if (equipmentShopUI == null)
            equipmentShopUI = GetComponent<FishingEquipmentShopUI>();

        if (baitShopUI == null)
            baitShopUI = GetComponent<FishingBaitShopUI>();
    }

    private void SetupButtons()
    {
        if (equipmentButton != null)
        {
            equipmentButton.onClick.RemoveAllListeners();
            equipmentButton.onClick.AddListener(ShowEquipment);
        }

        if (baitButton != null)
        {
            baitButton.onClick.RemoveAllListeners();
            baitButton.onClick.AddListener(ShowBait);
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
            Debug.LogError(
                "FishingShopTabsUI chưa gắn Root."
            );

            return;
        }

        FindShopScripts();

        isOpen = true;
        root.SetActive(true);

        ShowEquipment();

        GameLockManager.Instance?.LockPlayer();
    }

    public void Close()
    {
        isOpen = false;

        SetPages(false, false);

        if (root != null)
            root.SetActive(false);

        GameLockManager.Instance?.UnlockPlayer();
    }

    public void ShowEquipment()
    {
        EnsureShopOpen();
        FindShopScripts();

        SetPages(true, false);
        SetSelectedTab(true);

        if (equipmentShopUI == null)
        {
            Debug.LogError(
                "Không tìm thấy FishingEquipmentShopUI " +
                "trên object FishingShopUI."
            );

            return;
        }

        equipmentShopUI.Show();

        Debug.Log(
            "Đã chuyển sang tab Trang bị."
        );
    }

    public void ShowBait()
    {
        EnsureShopOpen();
        FindShopScripts();

        SetPages(false, true);
        SetSelectedTab(false);

        if (baitShopUI == null)
        {
            Debug.LogError(
                "Không tìm thấy FishingBaitShopUI " +
                "trên object FishingShopUI."
            );

            return;
        }

        baitShopUI.Show();

        Debug.Log(
            "Đã chuyển sang tab Mồi câu."
        );
    }

    private void EnsureShopOpen()
    {
        if (!isOpen)
        {
            isOpen = true;

            if (root != null)
                root.SetActive(true);

            GameLockManager.Instance?.LockPlayer();
        }
    }

    private void SetPages(
        bool showEquipment,
        bool showBait)
    {
        if (equipmentPage != null)
            equipmentPage.SetActive(showEquipment);

        if (baitPage != null)
            baitPage.SetActive(showBait);
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