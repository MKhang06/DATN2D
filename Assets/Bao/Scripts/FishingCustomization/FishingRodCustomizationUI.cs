using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class FishingRodCustomizationUI : MonoBehaviour
{
    [Header("Hệ thống trang bị")]
    [SerializeField] private FishingRodLoadout loadout;

    [Header("Bốn ô phụ kiện")]
    [SerializeField] private FishingRodSlotUI[] slotUIs;

    [Header("Thông tin cần câu")]
    [SerializeField] private TMP_Text rodNameText;
    [SerializeField] private Image rodIconImage;
    [SerializeField] private TMP_Text equippedCountText;
    [SerializeField] private TMP_Text requiredSkillText;
    [SerializeField] private TMP_Text totalStrengthText;
    [SerializeField] private TMP_Text speedText;
    [SerializeField] private TMP_Text maxDepthText;

    [Header("Bảng chọn trang bị")]
    [SerializeField] private GameObject selectorRoot;
    [SerializeField] private TMP_Text selectorTitleText;
    [SerializeField] private Transform selectorContent;
    [SerializeField] private FishingRodPartCardUI partCardPrefab;
    [SerializeField] private TMP_Text emptyListText;
    [SerializeField] private Button closeSelectorButton;

    [Header("Thông báo nhỏ - tùy chọn")]
    [SerializeField] private TMP_Text messageText;

    private FishingRodPartSlotType currentSelectorSlot;
    private InventoryManager boundInventory;

    private readonly List<GameObject> createdCards =
        new List<GameObject>();

    private void Awake()
    {
        ResolveLoadout();
        BindSlots();

        if (closeSelectorButton != null)
        {
            closeSelectorButton.onClick.RemoveListener(CloseSelector);
            closeSelectorButton.onClick.AddListener(CloseSelector);
        }

        if (selectorRoot != null)
            selectorRoot.SetActive(false);

        if (messageText != null)
            messageText.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        ResolveLoadout();
        BindEvents();
        BindSlots();
        RefreshAll();
    }

    private void OnDisable()
    {
        UnbindEvents();
        CloseSelector();
    }

    private void ResolveLoadout()
    {
        if (loadout == null)
        {
            loadout =
                FindFirstObjectByType<FishingRodLoadout>();
        }
    }

    private void BindEvents()
    {
        UnbindEvents();

        if (loadout == null)
            return;

        loadout.OnLoadoutChanged += HandleLoadoutChanged;

        boundInventory = loadout.Inventory;

        if (boundInventory != null)
        {
            boundInventory.OnInventoryChanged +=
                HandleInventoryChanged;
        }
    }

    private void UnbindEvents()
    {
        if (loadout != null)
        {
            loadout.OnLoadoutChanged -=
                HandleLoadoutChanged;
        }

        if (boundInventory != null)
        {
            boundInventory.OnInventoryChanged -=
                HandleInventoryChanged;
        }

        boundInventory = null;
    }

    private void BindSlots()
    {
        if (slotUIs == null)
            return;

        foreach (FishingRodSlotUI slot in slotUIs)
        {
            if (slot != null)
                slot.Bind(loadout, OpenSelector);
        }
    }

    public void RefreshAll()
    {
        if (loadout == null)
        {
            ResolveLoadout();

            if (loadout == null)
                return;
        }

        if (rodNameText != null)
            rodNameText.text = loadout.RodDisplayName;

        if (rodIconImage != null)
        {
            rodIconImage.sprite = loadout.RodIcon;
            rodIconImage.enabled = loadout.RodIcon != null;
            rodIconImage.preserveAspect = true;
        }

        if (equippedCountText != null)
        {
            equippedCountText.text =
                "Đã trang bị " +
                loadout.EquippedCount +
                "/4 phụ kiện";
        }

        if (requiredSkillText != null)
        {
            requiredSkillText.text =
                "Kỹ năng yêu cầu: " +
                Highlight(loadout.RequiredSkill.ToString());
        }

        if (totalStrengthText != null)
        {
            totalStrengthText.text =
                "Sức mạnh tổng thể: " +
                Highlight(
                    loadout.TotalStrength.ToString("0.##")
                );
        }

        if (speedText != null)
        {
            speedText.text =
                "Tốc độ: " +
                Highlight(loadout.Speed.ToString("0.##"));
        }

        if (maxDepthText != null)
        {
            maxDepthText.text =
                "Độ sâu tối đa: " +
                Highlight(
                    loadout.MaxDepth.ToString("0.##") +
                    "m"
                );
        }

        if (slotUIs != null)
        {
            foreach (FishingRodSlotUI slot in slotUIs)
                slot?.Refresh();
        }

        if (selectorRoot != null &&
            selectorRoot.activeSelf)
        {
            RebuildSelector();
        }
    }

    public void OpenSelector(
        FishingRodPartSlotType slotType)
    {
        if (loadout == null)
        {
            ShowMessage(
                "Không tìm thấy FishingRodLoadout.",
                false
            );

            return;
        }

        currentSelectorSlot = slotType;

        if (selectorRoot != null)
            selectorRoot.SetActive(true);

        if (selectorTitleText != null)
        {
            selectorTitleText.text =
                "CHỌN " + GetSlotTitle(slotType);
        }

        RebuildSelector();

        EventSystem.current?.
            SetSelectedGameObject(null);
    }

    public void CloseSelector()
    {
        ClearCreatedCards();

        if (selectorRoot != null)
            selectorRoot.SetActive(false);

        EventSystem.current?.
            SetSelectedGameObject(null);
    }

    private void RebuildSelector()
    {
        ClearCreatedCards();

        if (loadout == null ||
            selectorContent == null ||
            partCardPrefab == null)
        {
            return;
        }

        List<FishingRodPartDefinition> ownedParts =
            loadout.GetOwnedParts(currentSelectorSlot);

        if (emptyListText != null)
        {
            emptyListText.gameObject.SetActive(
                ownedParts.Count == 0
            );

            if (ownedParts.Count == 0)
            {
                emptyListText.text =
                    "Bạn chưa sở hữu " +
                    GetSlotTitle(currentSelectorSlot)
                        .ToLower() +
                    " phù hợp.";
            }
        }

        foreach (FishingRodPartDefinition definition in ownedParts)
        {
            FishingRodPartCardUI card =
                Instantiate(
                    partCardPrefab,
                    selectorContent
                );

            card.gameObject.SetActive(true);
            card.transform.localScale = Vector3.one;

            card.name =
                "OwnedPart_" +
                definition.ItemName;

            card.Setup(
                definition,
                loadout.GetOwnedAmount(definition),
                loadout.IsEquipped(definition),
                HandlePartAction
            );

            createdCards.Add(card.gameObject);
        }

        Canvas.ForceUpdateCanvases();

        if (selectorContent is RectTransform contentRect)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(
                contentRect
            );
        }
    }

    private void HandlePartAction(
        FishingRodPartDefinition definition)
    {
        if (loadout == null || definition == null)
            return;

        bool success =
            loadout.TogglePart(
                definition,
                out string message
            );

        ShowMessage(message, success);
        RefreshAll();
    }

    private void HandleLoadoutChanged()
    {
        RefreshAll();
    }

    private void HandleInventoryChanged()
    {
        RefreshAll();
    }

    private void ClearCreatedCards()
    {
        foreach (GameObject card in createdCards)
        {
            if (card != null)
                Destroy(card);
        }

        createdCards.Clear();
    }

    private void ShowMessage(
        string message,
        bool success)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        if (messageText != null)
        {
            messageText.gameObject.SetActive(true);
            messageText.text = message;

            messageText.color =
                success
                    ? new Color(0.25f, 1f, 0.55f)
                    : new Color(1f, 0.35f, 0.25f);
        }

        FishingNotificationUI notification =
            FishingNotificationUI.Instance;

        if (notification != null)
        {
            notification.ShowNotification(
                message,
                success
                    ? FishingNotificationUI
                        .NotificationType.Success
                    : FishingNotificationUI
                        .NotificationType.Warning
            );
        }

        Debug.Log(message);
    }

    private static string Highlight(string value)
    {
        return "<color=#FFD900>" +
               value +
               "</color>";
    }

    private static string GetSlotTitle(
        FishingRodPartSlotType slotType)
    {
        switch (slotType)
        {
            case FishingRodPartSlotType.Reel:
                return "MÁY CÂU";
            case FishingRodPartSlotType.Line:
                return "DÂY CÂU";
            case FishingRodPartSlotType.Hook:
                return "LƯỠI CÂU";
            case FishingRodPartSlotType.Bait:
                return "MỒI CÂU";
            default:
                return "TRANG BỊ";
        }
    }

    private void OnDestroy()
    {
        UnbindEvents();

        if (closeSelectorButton != null)
        {
            closeSelectorButton.onClick.RemoveListener(
                CloseSelector
            );
        }
    }
}
