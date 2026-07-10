using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FishingCustomizationUI : MonoBehaviour
{
    [Serializable]
    public class EquipmentSlotView
    {
        [Header("Loại phụ kiện")]
        public FishingPartType partType;

        [Header("References")]
        public Button button;
        public Image iconImage;
        public TMP_Text slotTitleText;
        public TMP_Text equippedNameText;
        public GameObject selectedFrame;

        [Header("Khi chưa trang bị")]
        public Sprite emptyIcon;
        public string emptyText = "Chưa trang bị";
    }

    [Header("Mở giao diện")]
    [SerializeField] private KeyCode openKey = KeyCode.K;

    [Header("Root")]
    [SerializeField] private GameObject root;

    [Header("Fishing Gear")]
    [SerializeField] private FishingGearStats gearStats;

    [Header("Chỉ số")]
    [SerializeField] private TMP_Text requiredSkillText;
    [SerializeField] private TMP_Text totalPowerText;
    [SerializeField] private TMP_Text speedText;
    [SerializeField] private TMP_Text maxDepthText;

    [Header("Các ô trang bị")]
    [SerializeField] private EquipmentSlotView reelSlot;
    [SerializeField] private EquipmentSlotView lineSlot;
    [SerializeField] private EquipmentSlotView hookSlot;
    [SerializeField] private EquipmentSlotView baitSlot;

    [Header("Nút đóng")]
    [SerializeField] private Button closeButton;

    [Header("Bảng chọn phụ kiện - Có thể để trống")]
    [SerializeField] private GameObject selectionPanel;
    [SerializeField] private TMP_Text selectionTitleText;
    [SerializeField] private Transform optionContent;
    [SerializeField] private GameObject optionButtonPrefab;
    [SerializeField] private Button unequipButton;

    [Header("Danh sách phụ kiện có thể chọn")]
    [SerializeField] private FishingEquipmentData[] availableEquipment;

    private bool isOpen;
    private bool subscribed;
    private FishingPartType selectedPartType;

    private void Awake()
    {
        SetupSlot(reelSlot, FishingPartType.Reel, "MÁY CÂU");
        SetupSlot(lineSlot, FishingPartType.Line, "DÂY CÂU");
        SetupSlot(hookSlot, FishingPartType.Hook, "LƯỠI CÂU");
        SetupSlot(baitSlot, FishingPartType.Bait, "MỒI CÂU");

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(Close);
            closeButton.onClick.AddListener(Close);
        }

        if (unequipButton != null)
        {
            unequipButton.onClick.RemoveListener(UnequipSelectedPart);
            unequipButton.onClick.AddListener(UnequipSelectedPart);
        }

        if (selectionPanel != null)
            selectionPanel.SetActive(false);

        if (root != null)
            root.SetActive(false);
    }

    private void Start()
    {
        FindGearStats();
        SubscribeGearEvents();
        RefreshUI();
    }

    private void Update()
    {
        if (Input.GetKeyDown(openKey))
        {
            Toggle();
        }

        if (!isOpen || !Input.GetKeyDown(KeyCode.Escape))
            return;

        if (selectionPanel != null && selectionPanel.activeSelf)
        {
            CloseSelectionPanel();
        }
        else
        {
            Close();
        }
    }

    private void OnEnable()
    {
        FindGearStats();
        SubscribeGearEvents();
    }

    private void OnDisable()
    {
        UnsubscribeGearEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeGearEvents();
    }

    private void FindGearStats()
    {
        if (gearStats == null)
            gearStats = FindFirstObjectByType<FishingGearStats>();
    }

    private void SubscribeGearEvents()
    {
        if (gearStats == null || subscribed)
            return;

        gearStats.OnGearChanged += RefreshUI;
        subscribed = true;
    }

    private void UnsubscribeGearEvents()
    {
        if (gearStats == null || !subscribed)
            return;

        gearStats.OnGearChanged -= RefreshUI;
        subscribed = false;
    }

    private void SetupSlot(
        EquipmentSlotView slot,
        FishingPartType type,
        string defaultTitle)
    {
        if (slot == null)
            return;

        slot.partType = type;

        if (slot.slotTitleText != null &&
            string.IsNullOrWhiteSpace(slot.slotTitleText.text))
        {
            slot.slotTitleText.text = defaultTitle;
        }

        if (slot.button == null)
            return;

        slot.button.onClick.RemoveAllListeners();
        slot.button.onClick.AddListener(
            () => OpenSelection(type)
        );
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
        FindGearStats();
        SubscribeGearEvents();

        isOpen = true;

        if (root != null)
            root.SetActive(true);

        if (selectionPanel != null)
            selectionPanel.SetActive(false);

        RefreshUI();
    }

    public void Close()
    {
        isOpen = false;

        if (selectionPanel != null)
            selectionPanel.SetActive(false);

        if (root != null)
            root.SetActive(false);

        ClearOptionList();
    }

    public void RefreshUI()
    {
        if (gearStats == null)
        {
            FindGearStats();

            if (gearStats == null)
                return;
        }

        RefreshStats();

        RefreshSlot(reelSlot);
        RefreshSlot(lineSlot);
        RefreshSlot(hookSlot);
        RefreshSlot(baitSlot);
    }

    private void RefreshStats()
    {
        int requiredSkill = GetRequiredSkill();

        float totalPower =
            gearStats.RodPower +
            gearStats.LineStrength;

        float totalSpeed =
            gearStats.ReelSpeed;

        if (requiredSkillText != null)
        {
            requiredSkillText.text =
                "Kỹ năng yêu cầu: <color=#F4D900>" +
                requiredSkill +
                "</color>";
        }

        if (totalPowerText != null)
        {
            totalPowerText.text =
                "Sức mạnh tổng thể: <color=#F4D900>" +
                totalPower.ToString("0.#") +
                "</color>";
        }

        if (speedText != null)
        {
            speedText.text =
                "Tốc độ: <color=#F4D900>" +
                totalSpeed.ToString("0.#") +
                "</color>";
        }

        if (maxDepthText != null)
        {
            maxDepthText.text =
                "Độ sâu tối đa: <color=#F4D900>" +
                gearStats.MaxDepth.ToString("0.#") +
                "m</color>";
        }
    }

    private int GetRequiredSkill()
    {
        int required = 0;

        required = Mathf.Max(
            required,
            GetEquipmentRequiredSkill(FishingPartType.Reel)
        );

        required = Mathf.Max(
            required,
            GetEquipmentRequiredSkill(FishingPartType.Line)
        );

        required = Mathf.Max(
            required,
            GetEquipmentRequiredSkill(FishingPartType.Hook)
        );

        required = Mathf.Max(
            required,
            GetEquipmentRequiredSkill(FishingPartType.Bait)
        );

        return required;
    }

    private int GetEquipmentRequiredSkill(FishingPartType type)
    {
        FishingEquipmentData equipment =
            gearStats.GetEquipped(type);

        return equipment != null
            ? equipment.requiredSkill
            : 0;
    }

    private void RefreshSlot(EquipmentSlotView slot)
    {
        if (slot == null)
            return;

        FishingEquipmentData equipment =
            gearStats.GetEquipped(slot.partType);

        bool isSelected =
            selectionPanel != null &&
            selectionPanel.activeSelf &&
            selectedPartType == slot.partType;

        if (slot.selectedFrame != null)
            slot.selectedFrame.SetActive(isSelected);

        if (equipment == null)
        {
            SetEmptySlot(slot);
            return;
        }

        if (slot.iconImage != null)
        {
            slot.iconImage.sprite = equipment.icon;
            slot.iconImage.enabled = equipment.icon != null;
            slot.iconImage.preserveAspect = true;
        }

        if (slot.equippedNameText != null)
            slot.equippedNameText.text = equipment.itemName;
    }

    private void SetEmptySlot(EquipmentSlotView slot)
    {
        if (slot.iconImage != null)
        {
            slot.iconImage.sprite = slot.emptyIcon;
            slot.iconImage.enabled = slot.emptyIcon != null;
            slot.iconImage.preserveAspect = true;
        }

        if (slot.equippedNameText != null)
            slot.equippedNameText.text = slot.emptyText;
    }

    private void OpenSelection(FishingPartType type)
    {
        selectedPartType = type;

        RefreshUI();

        /*
         * Nếu chưa tạo SelectionPanel thì bấm vào slot
         * vẫn không gây lỗi.
         */
        if (selectionPanel == null ||
            optionContent == null ||
            optionButtonPrefab == null)
        {
            Debug.Log(
                "Đã chọn ô: " + GetPartDisplayName(type) +
                ". Chưa gắn SelectionPanel hoặc Option Prefab."
            );

            return;
        }

        selectionPanel.SetActive(true);

        if (selectionTitleText != null)
        {
            selectionTitleText.text =
                "CHỌN " + GetPartDisplayName(type);
        }

        CreateOptionList();
        RefreshUI();
    }

    private void CloseSelectionPanel()
    {
        if (selectionPanel != null)
            selectionPanel.SetActive(false);

        ClearOptionList();
        RefreshUI();
    }

    private void CreateOptionList()
    {
        ClearOptionList();

        if (availableEquipment == null)
            return;

        FishingEquipmentData equipped =
            gearStats.GetEquipped(selectedPartType);

        foreach (FishingEquipmentData equipment
                 in availableEquipment)
        {
            if (equipment == null)
                continue;

            if (equipment.partType != selectedPartType)
                continue;

            CreateOptionButton(
                equipment,
                equipment == equipped
            );
        }
    }

    private void CreateOptionButton(
        FishingEquipmentData equipment,
        bool isEquipped)
    {
        GameObject optionObject =
            Instantiate(optionButtonPrefab, optionContent);

        optionObject.name =
            "Option_" + equipment.itemName;

        Button optionButton =
            optionObject.GetComponent<Button>();

        if (optionButton == null)
            optionButton =
                optionObject.GetComponentInChildren<Button>();

        Image iconImage = FindImage(
            optionObject.transform,
            "Icon"
        );

        TMP_Text itemNameText = FindText(
            optionObject.transform,
            "ItemNameText"
        );

        TMP_Text statsText = FindText(
            optionObject.transform,
            "StatsText"
        );

        Transform equippedMark = FindDeepChild(
            optionObject.transform,
            "EquippedMark"
        );

        Transform lockedObject = FindDeepChild(
            optionObject.transform,
            "LockedObject"
        );

        if (iconImage != null)
        {
            iconImage.sprite = equipment.icon;
            iconImage.enabled = equipment.icon != null;
            iconImage.preserveAspect = true;
        }

        if (itemNameText != null)
            itemNameText.text = equipment.itemName;

        if (statsText != null)
        {
            statsText.text =
                "Yêu cầu: " + equipment.requiredSkill +
                " | Sức mạnh: +" +
                equipment.power.ToString("0.#") +
                " | Tốc độ: +" +
                equipment.speed.ToString("0.#") +
                " | Độ sâu: +" +
                equipment.maxDepth.ToString("0.#") +
                "m";
        }

        if (equippedMark != null)
            equippedMark.gameObject.SetActive(isEquipped);

        bool canEquip = gearStats.CanEquip(equipment);

        if (lockedObject != null)
            lockedObject.gameObject.SetActive(!canEquip);

        if (optionButton != null)
        {
            optionButton.interactable = canEquip;
            optionButton.onClick.RemoveAllListeners();

            FishingEquipmentData selectedEquipment =
                equipment;

            optionButton.onClick.AddListener(
                () => EquipEquipment(selectedEquipment)
            );
        }
    }

    private void EquipEquipment(
        FishingEquipmentData equipment)
    {
        if (gearStats == null || equipment == null)
            return;

        bool success =
            gearStats.TryEquip(equipment);

        if (!success)
            return;

        CloseSelectionPanel();
        RefreshUI();
    }

    private void UnequipSelectedPart()
    {
        if (gearStats == null)
            return;

        gearStats.Unequip(selectedPartType);

        CloseSelectionPanel();
        RefreshUI();
    }

    private void ClearOptionList()
    {
        if (optionContent == null)
            return;

        for (int i = optionContent.childCount - 1;
             i >= 0;
             i--)
        {
            Destroy(
                optionContent.GetChild(i).gameObject
            );
        }
    }

    private string GetPartDisplayName(
        FishingPartType type)
    {
        switch (type)
        {
            case FishingPartType.Reel:
                return "MÁY CÂU";

            case FishingPartType.Line:
                return "DÂY CÂU";

            case FishingPartType.Hook:
                return "LƯỠI CÂU";

            case FishingPartType.Bait:
                return "MỒI CÂU";

            default:
                return "PHỤ KIỆN";
        }
    }

    private Image FindImage(
        Transform parent,
        string objectName)
    {
        Transform target =
            FindDeepChild(parent, objectName);

        return target != null
            ? target.GetComponent<Image>()
            : null;
    }

    private TMP_Text FindText(
        Transform parent,
        string objectName)
    {
        Transform target =
            FindDeepChild(parent, objectName);

        return target != null
            ? target.GetComponent<TMP_Text>()
            : null;
    }

    private Transform FindDeepChild(
        Transform parent,
        string objectName)
    {
        if (parent == null)
            return null;

        foreach (Transform child in parent)
        {
            if (child.name == objectName)
                return child;

            Transform result =
                FindDeepChild(child, objectName);

            if (result != null)
                return result;
        }

        return null;
    }
}