using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class FishingSupplyShopUI : MonoBehaviour
{
    public enum ShopTab
    {
        Equipment,
        Bait
    }

    [Header("Root cửa hàng")]
    [Tooltip(
        "Kéo GameObject FishingShopUI vào đây. " +
        "Script có thể nằm trên controller riêng hoặc ngay trên FishingShopUI."
    )]
    [SerializeField]
    private GameObject shopRoot;

    [SerializeField]
    private CanvasGroup shopCanvasGroup;

    [Tooltip(
        "Kéo object Root nằm bên trong FishingShopUI vào đây. " +
        "Đây là phần chứa Background, Header và hai Page."
    )]
    [SerializeField]
    private GameObject shopContentRoot;

    [Header("Hai trang")]
    [Tooltip("Kéo Root/EquipmentPage vào đây.")]
    [SerializeField]
    private GameObject equipmentPageRoot;

    [Tooltip("Kéo Root/BaitPage vào đây.")]
    [SerializeField]
    private GameObject baitPageRoot;

    [Header("Hai component shop hiện có")]
    [SerializeField]
    private FishingEquipmentShopUI equipmentShopUI;

    [SerializeField]
    private FishingBaitShopUI baitShopUI;

    [Header("Nút")]
    [SerializeField]
    private Button equipmentTabButton;

    [SerializeField]
    private Button baitTabButton;

    [SerializeField]
    private Button closeButton;

    [Header("Đánh dấu tab")]
    [SerializeField]
    private GameObject equipmentSelectedObject;

    [SerializeField]
    private GameObject baitSelectedObject;

    [Header("Cài đặt")]
    [SerializeField]
    private ShopTab defaultTab =
        ShopTab.Equipment;

    [SerializeField]
    private KeyCode closeKey =
        KeyCode.Escape;

    [SerializeField]
    private bool lockPlayerWhileOpen = true;

    [Header("Debug")]
    [SerializeField]
    private bool showDebugLogs = true;

    private ShopTab currentTab;
    private bool isOpen;

    public bool IsOpen => isOpen;
    public ShopTab CurrentTab => currentTab;

    private void Awake()
    {
        ResolveEverything();
        BindButtons();

        /*
         * Chỉ ẩn bằng CanvasGroup.
         * Không SetActive(false) để script luôn nhận được lệnh mở.
         */
        SetShopVisible(false);
    }

    private void Update()
    {
        if (isOpen &&
            Input.GetKeyDown(closeKey))
        {
            CloseShop();
        }
    }

    public void OpenShop()
    {
        ResolveEverything();

        if (shopRoot == null)
        {
            Debug.LogError(
                "[FishingSupplyShop] Shop Root đang trống. " +
                "Kéo FishingShopUI vào trường Shop Root.",
                this
            );

            return;
        }

        /*
         * Bật toàn bộ chuỗi Canvas -> FishingShopUI -> Root -> Page.
         */
        ActivateParents(shopRoot.transform);

        if (!shopRoot.activeSelf)
            shopRoot.SetActive(true);

        if (shopContentRoot != null)
        {
            ActivateParents(
                shopContentRoot.transform
            );

            shopContentRoot.SetActive(true);
        }

        ForceParentCanvasesVisible();
        ForceParentCanvasGroupsVisible();

        ResolveCanvasGroup();
        SetShopVisible(true);

        isOpen = true;

        if (lockPlayerWhileOpen)
        {
            GameLockManager.Instance?.
                LockPlayer();
        }

        if (defaultTab ==
            ShopTab.Bait)
        {
            ShowBaitTab();
        }
        else
        {
            ShowEquipmentTab();
        }

        ClearSelectedObject();

        if (showDebugLogs)
        {
            Debug.Log(
                BuildVisibilityReport(),
                shopRoot
            );
        }
    }

    public void CloseShop()
    {
        isOpen = false;

        if (equipmentPageRoot != null)
            equipmentPageRoot.SetActive(false);

        if (baitPageRoot != null)
            baitPageRoot.SetActive(false);

        SetShopVisible(false);

        if (lockPlayerWhileOpen)
        {
            GameLockManager.Instance?.
                UnlockPlayer();
        }

        ClearSelectedObject();

        if (showDebugLogs)
        {
            Debug.Log(
                "[FishingSupplyShop] CLOSE.",
                this
            );
        }
    }

    public void ToggleShop()
    {
        if (isOpen)
            CloseShop();
        else
            OpenShop();
    }

    public void ShowEquipmentTab()
    {
        ResolveEverything();

        currentTab =
            ShopTab.Equipment;

        if (baitPageRoot != null)
            baitPageRoot.SetActive(false);

        if (equipmentPageRoot != null)
        {
            ActivateParents(
                equipmentPageRoot.transform
            );

            equipmentPageRoot.SetActive(true);
        }

        /*
         * Gọi Show() của UI hiện có để nó tự dựng item.
         */
        if (equipmentShopUI != null)
        {
            equipmentShopUI.Show();
        }
        else
        {
            Debug.LogWarning(
                "[FishingSupplyShop] Chưa tìm thấy " +
                "FishingEquipmentShopUI.",
                this
            );
        }

        RefreshTabVisuals();
    }

    public void ShowBaitTab()
    {
        ResolveEverything();

        currentTab =
            ShopTab.Bait;

        if (equipmentPageRoot != null)
            equipmentPageRoot.SetActive(false);

        if (baitPageRoot != null)
        {
            ActivateParents(
                baitPageRoot.transform
            );

            baitPageRoot.SetActive(true);
        }

        if (baitShopUI != null)
        {
            baitShopUI.Show();
        }
        else
        {
            Debug.LogWarning(
                "[FishingSupplyShop] Chưa tìm thấy " +
                "FishingBaitShopUI.",
                this
            );
        }

        RefreshTabVisuals();
    }

    private void ResolveEverything()
    {
        if (shopRoot == null)
        {
            /*
             * Trường hợp script nằm ngay trên FishingShopUI.
             */
            if (string.Equals(
                    gameObject.name,
                    "FishingShopUI",
                    StringComparison.OrdinalIgnoreCase))
            {
                shopRoot = gameObject;
            }
            else
            {
                GameObject found =
                    GameObject.Find(
                        "FishingShopUI"
                    );

                if (found != null)
                    shopRoot = found;
            }
        }

        if (shopRoot == null)
            return;

        if (shopContentRoot == null)
        {
            Transform rootTransform =
                FindDeepChild(
                    shopRoot.transform,
                    "Root"
                );

            if (rootTransform != null)
            {
                shopContentRoot =
                    rootTransform.gameObject;
            }
        }

        if (equipmentPageRoot == null)
        {
            Transform page =
                FindDeepChild(
                    shopRoot.transform,
                    "EquipmentPage"
                );

            if (page != null)
                equipmentPageRoot =
                    page.gameObject;
        }

        if (baitPageRoot == null)
        {
            Transform page =
                FindDeepChild(
                    shopRoot.transform,
                    "BaitPage"
                );

            if (page != null)
                baitPageRoot =
                    page.gameObject;
        }

        if (equipmentShopUI == null)
        {
            equipmentShopUI =
                FindComponentInScene<
                    FishingEquipmentShopUI
                >(
                    equipmentPageRoot != null
                        ? equipmentPageRoot.transform
                        : shopRoot.transform
                );
        }

        if (baitShopUI == null)
        {
            baitShopUI =
                FindComponentInScene<
                    FishingBaitShopUI
                >(
                    baitPageRoot != null
                        ? baitPageRoot.transform
                        : shopRoot.transform
                );
        }

        ResolveCanvasGroup();
    }

    private void ResolveCanvasGroup()
    {
        if (shopRoot == null)
            return;

        if (shopCanvasGroup == null)
        {
            shopCanvasGroup =
                shopRoot.GetComponent<
                    CanvasGroup
                >();
        }

        if (shopCanvasGroup == null)
        {
            shopCanvasGroup =
                shopRoot.AddComponent<
                    CanvasGroup
                >();
        }
    }

    private void BindButtons()
    {
        if (equipmentTabButton != null)
        {
            equipmentTabButton.onClick
                .RemoveListener(
                    ShowEquipmentTab
                );

            equipmentTabButton.onClick
                .AddListener(
                    ShowEquipmentTab
                );
        }

        if (baitTabButton != null)
        {
            baitTabButton.onClick
                .RemoveListener(
                    ShowBaitTab
                );

            baitTabButton.onClick
                .AddListener(
                    ShowBaitTab
                );
        }

        if (closeButton != null)
        {
            closeButton.onClick
                .RemoveListener(
                    CloseShop
                );

            closeButton.onClick
                .AddListener(
                    CloseShop
                );
        }
    }

    private void SetShopVisible(
        bool visible)
    {
        if (shopRoot == null)
            return;

        if (visible)
        {
            ActivateParents(
                shopRoot.transform
            );

            if (!shopRoot.activeSelf)
                shopRoot.SetActive(true);
        }

        ResolveCanvasGroup();

        if (shopCanvasGroup != null)
        {
            shopCanvasGroup.alpha =
                visible ? 1f : 0f;

            shopCanvasGroup.interactable =
                visible;

            shopCanvasGroup.blocksRaycasts =
                visible;
        }

        /*
         * Không tắt shopRoot khi đóng.
         * Nhờ vậy object chứa script không bị chết.
         */
    }

    private void RefreshTabVisuals()
    {
        bool equipmentSelected =
            currentTab ==
            ShopTab.Equipment;

        if (equipmentSelectedObject != null)
        {
            equipmentSelectedObject
                .SetActive(
                    equipmentSelected
                );
        }

        if (baitSelectedObject != null)
        {
            baitSelectedObject
                .SetActive(
                    !equipmentSelected
                );
        }

        /*
         * Không disable Button của tab đang chọn.
         *
         * Trước đây Button.interactable = false làm Unity chuyển
         * sang Disabled Color. Khi đổi tab, một số setup Button
         * không phục hồi màu và không click lại được.
         *
         * Giữ cả hai Button luôn tương tác; trạng thái đang chọn
         * được thể hiện bằng Selected Object hoặc màu nền.
         */
        SetTabButtonState(
            equipmentTabButton,
            equipmentSelected
        );

        SetTabButtonState(
            baitTabButton,
            !equipmentSelected
        );
    }

    private static void SetTabButtonState(
        Button button,
        bool selected)
    {
        if (button == null)
            return;

        button.interactable = true;

        ColorBlock colors =
            button.colors;

        Color selectedColor =
            new Color(
                0.16f,
                0.42f,
                0.68f,
                1f
            );

        Color unselectedColor =
            new Color(
                0.20f,
                0.23f,
                0.28f,
                1f
            );

        Color baseColor =
            selected
                ? selectedColor
                : unselectedColor;

        colors.normalColor = baseColor;
        colors.selectedColor = baseColor;

        colors.highlightedColor =
            new Color(
                Mathf.Min(
                    1f,
                    baseColor.r + 0.08f
                ),
                Mathf.Min(
                    1f,
                    baseColor.g + 0.08f
                ),
                Mathf.Min(
                    1f,
                    baseColor.b + 0.08f
                ),
                1f
            );

        colors.pressedColor =
            new Color(
                baseColor.r * 0.82f,
                baseColor.g * 0.82f,
                baseColor.b * 0.82f,
                1f
            );

        /*
         * Disabled Color không còn được dùng vì Button luôn bật,
         * nhưng vẫn đặt cùng màu để tránh nháy xám.
         */
        colors.disabledColor = baseColor;

        button.colors = colors;

        if (button.targetGraphic != null)
        {
            button.targetGraphic.color =
                baseColor;
        }
    }

    private static void ActivateParents(
        Transform target)
    {
        if (target == null)
            return;

        Transform current = target;

        while (current != null)
        {
            if (!current.gameObject.activeSelf)
            {
                current.gameObject.SetActive(
                    true
                );
            }

            current = current.parent;
        }
    }

    private static Transform FindDeepChild(
        Transform parent,
        string targetName)
    {
        if (parent == null)
            return null;

        if (string.Equals(
                parent.name,
                targetName,
                StringComparison.OrdinalIgnoreCase))
        {
            return parent;
        }

        for (int i = 0;
             i < parent.childCount;
             i++)
        {
            Transform result =
                FindDeepChild(
                    parent.GetChild(i),
                    targetName
                );

            if (result != null)
                return result;
        }

        return null;
    }

    private static T FindComponentInScene<T>(
        Transform preferredRoot)
        where T : Component
    {
        if (preferredRoot != null)
        {
            T preferred =
                preferredRoot
                    .GetComponentInChildren<T>(
                        true
                    );

            if (preferred != null)
                return preferred;
        }

        T[] all =
            Resources
                .FindObjectsOfTypeAll<T>();

        foreach (T candidate in all)
        {
            if (candidate == null)
                continue;

            if (!candidate.gameObject
                    .scene.IsValid())
            {
                continue;
            }

            return candidate;
        }

        return null;
    }

    private void ForceParentCanvasesVisible()
    {
        if (shopRoot == null)
            return;

        Canvas[] canvases =
            shopRoot.GetComponentsInParent<
                Canvas
            >(true);

        foreach (Canvas canvas in canvases)
        {
            if (canvas == null)
                continue;

            canvas.enabled = true;

            /*
             * Canvas shop riêng nên nằm trên UI khác.
             * Không thay Render Mode để tránh phá setup camera.
             */
            if (canvas.sortingOrder < 1000)
                canvas.sortingOrder = 1000;
        }
    }

    private void ForceParentCanvasGroupsVisible()
    {
        if (shopRoot == null)
            return;

        CanvasGroup[] groups =
            shopRoot.GetComponentsInParent<
                CanvasGroup
            >(true);

        foreach (CanvasGroup group in groups)
        {
            if (group == null)
                continue;

            group.alpha = 1f;
            group.interactable = true;
            group.blocksRaycasts = true;
        }
    }

    private string BuildVisibilityReport()
    {
        Canvas canvas =
            shopRoot != null
                ? shopRoot.GetComponentInParent<
                    Canvas
                >(true)
                : null;

        RectTransform rect =
            shopRoot != null
                ? shopRoot.GetComponent<
                    RectTransform
                >()
                : null;

        string rectInfo =
            rect != null
                ? rect.rect.width.ToString("0") +
                  "x" +
                  rect.rect.height.ToString("0")
                : "NO RECT";

        return
            "[FishingSupplyShop] OPEN thành công" +
            "\nShopRoot: " +
            (shopRoot != null
                ? shopRoot.name
                : "NULL") +
            "\nShop Active: " +
            (shopRoot != null &&
             shopRoot.activeInHierarchy) +
            "\nShop Alpha: " +
            (shopCanvasGroup != null
                ? shopCanvasGroup.alpha
                : -1f) +
            "\nContent Root: " +
            (shopContentRoot != null
                ? shopContentRoot.name +
                  " / Active=" +
                  shopContentRoot.activeInHierarchy
                : "NULL") +
            "\nEquipment Page Active: " +
            (equipmentPageRoot != null &&
             equipmentPageRoot.activeInHierarchy) +
            "\nBait Page Active: " +
            (baitPageRoot != null &&
             baitPageRoot.activeInHierarchy) +
            "\nCanvas: " +
            (canvas != null
                ? canvas.name +
                  " / Enabled=" +
                  canvas.enabled +
                  " / Mode=" +
                  canvas.renderMode +
                  " / Order=" +
                  canvas.sortingOrder
                : "NULL") +
            "\nRect Size: " +
            rectInfo;
    }

    private static void ClearSelectedObject()
    {
        if (EventSystem.current != null)
        {
            EventSystem.current
                .SetSelectedGameObject(null);
        }
    }

    [ContextMenu(
        "TEST - Force Open Shop"
    )]
    private void TestForceOpenShop()
    {
        OpenShop();
    }

    [ContextMenu(
        "TEST - Print References"
    )]
    private void TestPrintReferences()
    {
        ResolveEverything();

        Debug.Log(
            "[FishingSupplyShop] REFERENCES\n" +
            "Shop Root: " +
            (shopRoot != null
                ? shopRoot.name
                : "NULL") +
            "\nEquipment Page: " +
            (equipmentPageRoot != null
                ? equipmentPageRoot.name
                : "NULL") +
            "\nBait Page: " +
            (baitPageRoot != null
                ? baitPageRoot.name
                : "NULL") +
            "\nEquipment UI: " +
            (equipmentShopUI != null
                ? equipmentShopUI.name
                : "NULL") +
            "\nBait UI: " +
            (baitShopUI != null
                ? baitShopUI.name
                : "NULL"),
            this
        );
    }

    private void OnDestroy()
    {
        if (equipmentTabButton != null)
        {
            equipmentTabButton.onClick
                .RemoveListener(
                    ShowEquipmentTab
                );
        }

        if (baitTabButton != null)
        {
            baitTabButton.onClick
                .RemoveListener(
                    ShowBaitTab
                );
        }

        if (closeButton != null)
        {
            closeButton.onClick
                .RemoveListener(
                    CloseShop
                );
        }

        if (isOpen &&
            lockPlayerWhileOpen)
        {
            GameLockManager.Instance?.
                UnlockPlayer();
        }
    }
}