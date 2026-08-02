using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
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
    [SerializeField]
    private KeyCode openKey = KeyCode.K;

    [Header("Root")]
    [SerializeField]
    private GameObject root;

    [Header("Fishing Gear")]
    [SerializeField]
    private FishingGearStats gearStats;

    [Header("Inventory")]
    [Tooltip(
        "Phải là InventoryManager mà NPC Shop đang thêm vật phẩm vào."
    )]
    [SerializeField]
    private InventoryManager inventoryManager;

    [Header("Nguồn dữ liệu Shop")]
    [Tooltip(
        "Kéo object có FishingEquipmentShopUI vào đây. " +
        "Reference này giúp bảng tùy chỉnh đọc được dữ liệu " +
        "ngay cả khi Equipment Root đang tắt."
    )]
    [SerializeField]
    private FishingEquipmentShopUI equipmentShopUI;

    [Tooltip(
        "Nguồn dự phòng. Tool Editor sẽ tự sao chép " +
        "Available Equipment của Shop vào đây."
    )]
    [SerializeField]
    private FishingEquipmentShopItemData[]
        shopEquipmentItems;

    [Header("Chỉ số")]
    [SerializeField]
    private TMP_Text requiredSkillText;

    [SerializeField]
    private TMP_Text totalPowerText;

    [SerializeField]
    private TMP_Text speedText;

    [SerializeField]
    private TMP_Text maxDepthText;

    [Header("Các ô trang bị")]
    [SerializeField]
    private EquipmentSlotView reelSlot;

    [SerializeField]
    private EquipmentSlotView lineSlot;

    [SerializeField]
    private EquipmentSlotView hookSlot;

    [SerializeField]
    private EquipmentSlotView baitSlot;

    [Header("Nút đóng")]
    [SerializeField]
    private Button closeButton;

    [Header("Bảng chọn phụ kiện")]
    [SerializeField]
    private GameObject selectionPanel;

    [SerializeField]
    private TMP_Text selectionTitleText;

    [SerializeField]
    private Transform optionContent;

    [SerializeField]
    private GameObject optionButtonPrefab;

    [SerializeField]
    private Button unequipButton;

    [Tooltip(
        "Text báo Bạn chưa sở hữu trang bị phù hợp. " +
        "Có thể để trống, script sẽ tự tìm object tên EmptyListText."
    )]
    [SerializeField]
    private TMP_Text emptyListText;

    [Header("Danh sách phụ kiện có thể chọn")]
    [Tooltip(
        "Có thể để trống. Script sẽ tự lấy FishingEquipmentData " +
        "từ FishingEquipmentShopUI trong scene."
    )]
    [SerializeField]
    private FishingEquipmentData[] availableEquipment;

    [Header("Debug")]
    [SerializeField]
    private bool showDebugLogs = true;

    private bool isOpen;
    private bool subscribed;
    private bool inventorySubscribed;
    private FishingPartType selectedPartType;

    private const BindingFlags MemberFlags =
        BindingFlags.Instance |
        BindingFlags.Public |
        BindingFlags.NonPublic;

    /*
     * Tên sở hữu lấy từ FishingEquipmentShopItemData.ItemName.
     * Project hiện tại không có field inventoryItem trong ShopItem.
     */
    private readonly Dictionary<
        FishingEquipmentData,
        string
    > equipmentOwnershipNameMap =
        new Dictionary<
            FishingEquipmentData,
            string
        >();

    private void Awake()
    {
        SetupSlot(
            reelSlot,
            FishingPartType.Reel,
            "MÁY CÂU"
        );

        SetupSlot(
            lineSlot,
            FishingPartType.Line,
            "DÂY CÂU"
        );

        SetupSlot(
            hookSlot,
            FishingPartType.Hook,
            "LƯỠI CÂU"
        );

        SetupSlot(
            baitSlot,
            FishingPartType.Bait,
            "MỒI CÂU"
        );

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(Close);
            closeButton.onClick.AddListener(Close);
        }

        if (unequipButton != null)
        {
            unequipButton.onClick.RemoveListener(
                UnequipSelectedPart
            );

            unequipButton.onClick.AddListener(
                UnequipSelectedPart
            );
        }

        FindReferences();
        FindEmptyListText();
        EnsureAvailableEquipment();

        if (selectionPanel != null)
            selectionPanel.SetActive(false);

        if (root != null)
            root.SetActive(false);
    }

    private void Start()
    {
        FindReferences();
        EnsureAvailableEquipment();
        SubscribeGearEvents();
        SubscribeInventoryEvents();
        RefreshUI();
    }

    private void Update()
    {
        if (Input.GetKeyDown(openKey))
            Toggle();

        if (!isOpen ||
            !Input.GetKeyDown(KeyCode.Escape))
        {
            return;
        }

        if (selectionPanel != null &&
            selectionPanel.activeSelf)
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
        FindReferences();
        EnsureAvailableEquipment();
        SubscribeGearEvents();
        SubscribeInventoryEvents();
    }

    private void OnDisable()
    {
        UnsubscribeGearEvents();
        UnsubscribeInventoryEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeGearEvents();
        UnsubscribeInventoryEvents();
    }

    private void FindReferences()
    {
        /*
         * Tìm Shop trước vì Inventory của Shop là nguồn chuẩn.
         * Điều này tránh trường hợp scene có nhiều InventoryManager.
         */
        if (equipmentShopUI == null)
        {
            FishingEquipmentShopUI[] shops =
                Resources.FindObjectsOfTypeAll<
                    FishingEquipmentShopUI
                >();

            foreach (
                FishingEquipmentShopUI shop
                in shops)
            {
                if (shop == null)
                    continue;

                if (!shop.gameObject.scene.IsValid())
                    continue;

                equipmentShopUI = shop;
                break;
            }
        }

        if (equipmentShopUI == null)
        {
            equipmentShopUI =
                FishingEquipmentShopUI.Instance;
        }

        if (equipmentShopUI != null &&
            equipmentShopUI.Inventory != null)
        {
            /*
             * Luôn dùng đúng Inventory mà Shop đã thêm đồ vào.
             */
            inventoryManager =
                equipmentShopUI.Inventory;
        }

        if (inventoryManager == null)
        {
            inventoryManager =
                InventoryManager.Instance;
        }

        if (inventoryManager == null)
        {
            inventoryManager =
                FindFirstObjectByType<
                    InventoryManager
                >();
        }

        if (gearStats == null)
        {
            gearStats =
                FindFirstObjectByType<
                    FishingGearStats
                >();
        }

        if (gearStats != null &&
            inventoryManager != null)
        {
            gearStats.BindInventory(
                inventoryManager
            );
        }
    }

    private void FindEmptyListText()
    {
        if (emptyListText != null ||
            selectionPanel == null)
        {
            return;
        }

        emptyListText =
            FindText(
                selectionPanel.transform,
                "EmptyListText"
            );

        if (emptyListText == null)
        {
            /*
             * Hỗ trợ tên cũ trong prefab.
             */
            emptyListText =
                FindText(
                    selectionPanel.transform,
                    "EmptyText"
                );
        }
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

    private void SubscribeInventoryEvents()
    {
        if (inventoryManager == null ||
            inventorySubscribed)
        {
            return;
        }

        inventoryManager.OnInventoryChanged +=
            HandleInventoryChanged;

        inventorySubscribed = true;
    }

    private void UnsubscribeInventoryEvents()
    {
        if (inventoryManager == null ||
            !inventorySubscribed)
        {
            return;
        }

        inventoryManager.OnInventoryChanged -=
            HandleInventoryChanged;

        inventorySubscribed = false;
    }

    private void HandleInventoryChanged()
    {
        /*
         * Mua đồ xong sẽ chạy ngay vào đây.
         */
        EnsureAvailableEquipment();

        if (selectionPanel != null &&
            selectionPanel.activeSelf)
        {
            CreateOptionList();
        }

        RefreshUI();
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
            string.IsNullOrWhiteSpace(
                slot.slotTitleText.text))
        {
            slot.slotTitleText.text =
                defaultTitle;
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
        FindReferences();
        EnsureAvailableEquipment();
        SubscribeGearEvents();
        SubscribeInventoryEvents();

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
        FindReferences();

        if (gearStats == null)
            return;

        RefreshStats();
        RefreshSlot(reelSlot);
        RefreshSlot(lineSlot);
        RefreshSlot(hookSlot);
        RefreshSlot(baitSlot);

        RefreshUnequipButton();
    }

    private void RefreshStats()
    {
        int requiredSkill =
            GetRequiredSkill();

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
            GetEquipmentRequiredSkill(
                FishingPartType.Reel
            )
        );

        required = Mathf.Max(
            required,
            GetEquipmentRequiredSkill(
                FishingPartType.Line
            )
        );

        required = Mathf.Max(
            required,
            GetEquipmentRequiredSkill(
                FishingPartType.Hook
            )
        );

        required = Mathf.Max(
            required,
            GetEquipmentRequiredSkill(
                FishingPartType.Bait
            )
        );

        return required;
    }

    private int GetEquipmentRequiredSkill(
        FishingPartType type)
    {
        FishingEquipmentData equipment =
            gearStats.GetEquipped(type);

        return equipment != null
            ? equipment.requiredSkill
            : 0;
    }

    private void RefreshSlot(
        EquipmentSlotView slot)
    {
        if (slot == null)
            return;

        FishingEquipmentData equipment =
            gearStats.GetEquipped(
                slot.partType
            );

        bool isSelected =
            selectionPanel != null &&
            selectionPanel.activeSelf &&
            selectedPartType ==
                slot.partType;

        if (slot.selectedFrame != null)
        {
            slot.selectedFrame.SetActive(
                isSelected
            );
        }

        if (equipment == null)
        {
            SetEmptySlot(slot);
            return;
        }

        if (slot.iconImage != null)
        {
            slot.iconImage.sprite =
                equipment.icon;

            slot.iconImage.enabled =
                equipment.icon != null;

            slot.iconImage.preserveAspect =
                true;
        }

        if (slot.equippedNameText != null)
        {
            slot.equippedNameText.text =
                equipment.itemName;
        }
    }

    private void SetEmptySlot(
        EquipmentSlotView slot)
    {
        if (slot.iconImage != null)
        {
            slot.iconImage.sprite =
                slot.emptyIcon;

            slot.iconImage.enabled =
                slot.emptyIcon != null;

            slot.iconImage.preserveAspect =
                true;
        }

        if (slot.equippedNameText != null)
        {
            slot.equippedNameText.text =
                slot.emptyText;
        }
    }

    private void OpenSelection(
        FishingPartType type)
    {
        selectedPartType = type;

        FindReferences();
        EnsureAvailableEquipment();

        if (selectionPanel == null ||
            optionContent == null ||
            optionButtonPrefab == null)
        {
            Debug.LogError(
                "[FishingCustomizationUI] Thiếu SelectionPanel, " +
                "OptionContent hoặc OptionButtonPrefab.",
                this
            );

            return;
        }

        selectionPanel.SetActive(true);

        if (selectionTitleText != null)
        {
            selectionTitleText.text =
                "CHỌN " +
                GetPartDisplayName(type);
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

    private FishingEquipmentShopItemData[]
        GetShopSourceItems()
    {
        FindReferences();

        if (equipmentShopUI != null &&
            equipmentShopUI
                .AvailableEquipment != null &&
            equipmentShopUI
                .AvailableEquipment.Length > 0)
        {
            return equipmentShopUI
                .AvailableEquipment;
        }

        if (FishingEquipmentShopUI
                .RegisteredEquipment != null &&
            FishingEquipmentShopUI
                .RegisteredEquipment.Length > 0)
        {
            return FishingEquipmentShopUI
                .RegisteredEquipment;
        }

        if (shopEquipmentItems != null &&
            shopEquipmentItems.Length > 0)
        {
            return shopEquipmentItems;
        }

        FishingEquipmentShopItemData[] loaded =
            Resources.FindObjectsOfTypeAll<
                FishingEquipmentShopItemData
            >();

        return loaded ??
               Array.Empty<
                   FishingEquipmentShopItemData
               >();
    }

    private InventoryItemData ResolveInventoryItem(
        FishingEquipmentShopItemData shopItem)
    {
        if (shopItem == null)
            return null;

        /*
         * Hỗ trợ project có hoặc không có field inventoryItem
         * mà không gây lỗi compile.
         */
        object reflectedItem =
            GetMemberValue(
                shopItem,
                "inventoryItem"
            ) ??
            GetMemberValue(
                shopItem,
                "itemData"
            ) ??
            GetMemberValue(
                shopItem,
                "inventoryData"
            );

        if (reflectedItem is
            InventoryItemData directItem)
        {
            return directItem;
        }

        ItemDatabase database =
            ItemDatabase.Instance;

        if (database == null)
        {
            database =
                FindFirstObjectByType<
                    ItemDatabase
                >();
        }

        if (database == null)
            return null;

        string shopName =
            shopItem.ItemName;

        if (!string.IsNullOrWhiteSpace(
                shopName))
        {
            InventoryItemData byShopName =
                database.GetByName(
                    shopName
                );

            if (byShopName != null)
                return byShopName;
        }

        FishingEquipmentData equipment =
            shopItem.equipmentData;

        if (equipment != null &&
            !string.IsNullOrWhiteSpace(
                equipment.itemName))
        {
            return database.GetByName(
                equipment.itemName
            );
        }

        return null;
    }

    private bool IsOwned(
        FishingEquipmentShopItemData shopItem,
        FishingEquipmentData equipment)
    {
        FindReferences();

        if (inventoryManager == null)
            return false;

        InventoryItemData inventoryItem =
            ResolveInventoryItem(
                shopItem
            );

        /*
         * Đây là kiểm tra chính xác và ưu tiên cao nhất.
         */
        if (inventoryItem != null &&
            inventoryManager.HasItem(
                inventoryItem
            ))
        {
            return true;
        }

        /*
         * Fallback chỉ dùng cho dữ liệu cũ.
         */
        if (shopItem != null &&
            HasItemByName(
                shopItem.ItemName
            ))
        {
            return true;
        }

        if (equipment != null &&
            HasItemByName(
                equipment.itemName
            ))
        {
            return true;
        }

        Sprite shopIcon =
            shopItem != null
                ? shopItem.Icon
                : null;

        return HasItemByIcon(
                   shopIcon
               ) ||
               (
                   equipment != null &&
                   HasItemByIcon(
                       equipment.icon
                   )
               );
    }

    private FishingPartType ResolveShopPartType(
        FishingEquipmentShopItemData shopItem,
        FishingEquipmentData equipment)
    {
        if (shopItem != null)
        {
            if (TryResolvePartTypeByName(
                    shopItem.ItemName,
                    out FishingPartType byShopName))
            {
                return byShopName;
            }

            if (TryResolvePartTypeByName(
                    shopItem.name,
                    out FishingPartType byAssetName))
            {
                return byAssetName;
            }
        }

        if (equipment != null)
        {
            if (TryResolvePartTypeByName(
                    equipment.itemName,
                    out FishingPartType byEquipmentName))
            {
                return byEquipmentName;
            }

            if (TryResolvePartTypeByName(
                    equipment.name,
                    out FishingPartType byEquipmentAsset))
            {
                return byEquipmentAsset;
            }

            return equipment.partType;
        }

        return FishingPartType.Reel;
    }

    private void CreateOptionList()
    {
        ClearOptionList();
        FindEmptyListText();
        FindReferences();

        if (gearStats == null ||
            optionContent == null)
        {
            return;
        }

        FishingEquipmentShopItemData[]
            sourceItems =
                GetShopSourceItems();

        FishingEquipmentData equipped =
            gearStats.GetEquipped(
                selectedPartType
            );

        int sourceCount =
            sourceItems != null
                ? sourceItems.Length
                : 0;

        int compatibleCount = 0;
        int ownedCount = 0;
        int createdCount = 0;

        HashSet<FishingEquipmentData>
            createdEquipment =
                new HashSet<
                    FishingEquipmentData
                >();

        if (sourceItems != null)
        {
            foreach (
                FishingEquipmentShopItemData shopItem
                in sourceItems)
            {
                if (shopItem == null ||
                    shopItem.equipmentData == null)
                {
                    continue;
                }

                FishingEquipmentData equipment =
                    shopItem.equipmentData;

                if (IsRodName(
                        shopItem.ItemName) ||
                    IsRodName(
                        equipment.itemName))
                {
                    continue;
                }

                FishingPartType resolvedType =
                    ResolveShopPartType(
                        shopItem,
                        equipment
                    );

                equipment.partType =
                    resolvedType;

                if (resolvedType !=
                    selectedPartType)
                {
                    continue;
                }

                compatibleCount++;

                bool isEquipped =
                    equipment == equipped;

                bool isOwned =
                    IsOwned(
                        shopItem,
                        equipment
                    );

                if (isOwned)
                    ownedCount++;

                if (!isOwned &&
                    !isEquipped)
                {
                    continue;
                }

                if (!createdEquipment.Add(
                        equipment))
                {
                    continue;
                }

                InventoryItemData inventoryItem =
                    ResolveInventoryItem(
                        shopItem
                    );

                if (gearStats != null)
                {
                    gearStats
                        .RegisterOwnershipName(
                            equipment,
                            !string.IsNullOrWhiteSpace(
                                shopItem.ItemName)
                                ? shopItem.ItemName
                                : equipment.itemName
                        );

                    if (inventoryItem != null)
                    {
                        gearStats
                            .RegisterOwnershipItem(
                                equipment,
                                inventoryItem
                            );
                    }
                }

                equipmentOwnershipNameMap[
                    equipment
                ] =
                    !string.IsNullOrWhiteSpace(
                        shopItem.ItemName)
                        ? shopItem.ItemName
                        : equipment.itemName;

                CreateOptionButton(
                    equipment,
                    isEquipped,
                    isOwned
                );

                createdCount++;
            }
        }

        if (emptyListText != null)
        {
            emptyListText.gameObject.SetActive(
                createdCount == 0
            );

            if (sourceCount == 0)
            {
                emptyListText.text =
                    "Không tìm thấy dữ liệu trang bị từ Shop.";
            }
            else if (compatibleCount == 0)
            {
                emptyListText.text =
                    "Shop chưa có trang bị thuộc loại này.";
            }
            else if (ownedCount == 0)
            {
                emptyListText.text =
                    "Bạn chưa sở hữu trang bị thuộc loại này.";
            }
            else
            {
                emptyListText.text =
                    "Không thể tạo danh sách trang bị. " +
                    "Kiểm tra Option Button Prefab.";
            }
        }

        RefreshUnequipButton();

        Canvas.ForceUpdateCanvases();

        if (optionContent is
            RectTransform contentRect)
        {
            LayoutRebuilder
                .ForceRebuildLayoutImmediate(
                    contentRect
                );
        }

        if (showDebugLogs)
        {
            Debug.Log(
                "[FishingCustomizationUI FIX] " +
                GetPartDisplayName(
                    selectedPartType
                ) +
                " | Source=" +
                sourceCount +
                " | Compatible=" +
                compatibleCount +
                " | Owned=" +
                ownedCount +
                " | Created=" +
                createdCount +
                " | Inventory=" +
                (inventoryManager != null
                    ? inventoryManager.name
                    : "NULL") +
                " | Shop=" +
                (equipmentShopUI != null
                    ? equipmentShopUI.name
                    : "NULL"),
                this
            );
        }
    }

    private void CreateOptionButton(
        FishingEquipmentData equipment,
        bool isEquipped,
        bool isOwned)
    {
        GameObject optionObject =
            Instantiate(
                optionButtonPrefab,
                optionContent
            );

        optionObject.SetActive(true);
        optionObject.transform.localScale =
            Vector3.one;

        optionObject.name =
            "Option_" +
            equipment.itemName;

        Button optionButton =
            optionObject
                .GetComponent<Button>();

        if (optionButton == null)
        {
            optionButton =
                optionObject
                    .GetComponentInChildren<
                        Button
                    >(true);
        }

        Image iconImage =
            FindImage(
                optionObject.transform,
                "Icon"
            );

        TMP_Text itemNameText =
            FindText(
                optionObject.transform,
                "ItemNameText"
            );

        TMP_Text statsText =
            FindText(
                optionObject.transform,
                "StatsText"
            );

        TMP_Text actionText =
            FindText(
                optionObject.transform,
                "ActionText"
            );

        if (actionText == null)
        {
            actionText =
                FindText(
                    optionObject.transform,
                    "ButtonText"
                );
        }

        Transform equippedMark =
            FindDeepChild(
                optionObject.transform,
                "EquippedMark"
            );

        if (equippedMark == null)
        {
            equippedMark =
                FindDeepChild(
                    optionObject.transform,
                    "EquippedMarker"
                );
        }

        Transform lockedObject =
            FindDeepChild(
                optionObject.transform,
                "LockedObject"
            );

        if (iconImage != null)
        {
            iconImage.sprite =
                equipment.icon;

            iconImage.enabled =
                equipment.icon != null;

            iconImage.preserveAspect =
                true;
        }

        if (itemNameText != null)
        {
            itemNameText.text =
                equipment.itemName;
        }

        if (statsText != null)
        {
            statsText.text =
                "Yêu cầu: " +
                equipment.requiredSkill +
                " | Sức mạnh: +" +
                equipment.power
                    .ToString("0.#") +
                " | Tốc độ: +" +
                equipment.speed
                    .ToString("0.#") +
                " | Độ sâu: +" +
                equipment.maxDepth
                    .ToString("0.#") +
                "m";
        }

        if (actionText != null)
        {
            actionText.text =
                isEquipped
                    ? "GỠ"
                    : "TRANG BỊ";
        }

        if (equippedMark != null)
        {
            equippedMark.gameObject
                .SetActive(isEquipped);
        }

        bool passesGearRequirement =
            gearStats.CanEquip(equipment);

        /*
         * Món đang trang bị phải luôn bấm được để gỡ.
         */
        bool canClick =
            isEquipped ||
            (
                isOwned &&
                passesGearRequirement
            );

        if (lockedObject != null)
        {
            lockedObject.gameObject.SetActive(
                !isEquipped &&
                !passesGearRequirement
            );
        }

        if (optionButton == null)
        {
            Debug.LogWarning(
                "[FishingCustomizationUI] Prefab " +
                optionButtonPrefab.name +
                " không có Button.",
                optionObject
            );

            return;
        }

        optionButton.interactable =
            canClick;

        optionButton.onClick
            .RemoveAllListeners();

        FishingEquipmentData
            selectedEquipment =
                equipment;

        if (isEquipped)
        {
            optionButton.onClick.AddListener(
                UnequipSelectedPart
            );
        }
        else
        {
            optionButton.onClick.AddListener(
                () =>
                    EquipEquipment(
                        selectedEquipment
                    )
            );
        }
    }

    private void EquipEquipment(
        FishingEquipmentData equipment)
    {
        FindReferences();

        if (gearStats == null ||
            equipment == null)
        {
            return;
        }

        if (!gearStats.CanEquip(
                equipment,
                out string equipReason))
        {
            Debug.LogWarning(
                "[FishingCustomizationUI] " +
                equipReason,
                this
            );

            CreateOptionList();
            return;
        }

        bool success =
            gearStats.TryEquip(equipment);

        if (!success)
        {
            Debug.LogWarning(
                "[FishingCustomizationUI] FishingGearStats từ chối " +
                "trang bị " +
                equipment.itemName +
                ". InventoryName=" +
                GetOwnershipName(
                    equipment
                ) +
                ". Xem warning [FishingGearStats] ngay phía trên.",
                this
            );

            CreateOptionList();
            return;
        }

        CloseSelectionPanel();
        RefreshUI();
    }

    private void UnequipSelectedPart()
    {
        if (gearStats == null)
            return;

        gearStats.Unequip(
            selectedPartType
        );

        CloseSelectionPanel();
        RefreshUI();
    }

    private void RefreshUnequipButton()
    {
        if (unequipButton == null ||
            gearStats == null)
        {
            return;
        }

        FishingEquipmentData equipped =
            gearStats.GetEquipped(
                selectedPartType
            );

        unequipButton.gameObject.SetActive(
            selectionPanel != null &&
            selectionPanel.activeSelf
        );

        unequipButton.interactable =
            equipped != null;

        TMP_Text buttonText =
            unequipButton
                .GetComponentInChildren<
                    TMP_Text
                >(true);

        if (buttonText != null)
        {
            buttonText.text =
                equipped != null
                    ? "GỠ TRANG BỊ"
                    : "CHƯA TRANG BỊ";
        }
    }

    private bool IsOwned(
        FishingEquipmentData equipment)
    {
        if (equipment == null)
            return false;

        FindReferences();

        if (inventoryManager == null)
            return false;

        string ownershipName =
            equipment.itemName;

        if (equipmentOwnershipNameMap
                .TryGetValue(
                    equipment,
                    out string mappedName) &&
            !string.IsNullOrWhiteSpace(
                mappedName))
        {
            ownershipName =
                mappedName;
        }

        return HasItemByName(
                   ownershipName
               ) ||
               HasItemByName(
                   equipment.itemName
               ) ||
               HasItemByIcon(
                   equipment.icon
               );
    }

    private bool HasItemByIcon(
        Sprite icon)
    {
        if (inventoryManager == null ||
            icon == null)
        {
            return false;
        }

        return HasItemByIcon(
                   inventoryManager
                       .HotbarSlots,
                   icon
               ) ||
               HasItemByIcon(
                   inventoryManager
                       .BagSlots,
                   icon
               );
    }

    private static bool HasItemByIcon(
        InventoryManager.InventorySlot[] slots,
        Sprite icon)
    {
        if (slots == null ||
            icon == null)
        {
            return false;
        }

        foreach (
            InventoryManager.InventorySlot slot
            in slots)
        {
            if (slot == null ||
                slot.IsEmpty)
            {
                continue;
            }

            if (slot.icon == icon)
                return true;
        }

        return false;
    }

    private bool HasItemByName(
        string itemName)
    {
        if (inventoryManager == null ||
            string.IsNullOrWhiteSpace(
                itemName))
        {
            return false;
        }

        return HasItemByName(
                   inventoryManager
                       .HotbarSlots,
                   itemName
               ) ||
               HasItemByName(
                   inventoryManager
                       .BagSlots,
                   itemName
               );
    }

    private static bool HasItemByName(
        InventoryManager.InventorySlot[]
            slots,
        string itemName)
    {
        if (slots == null)
            return false;

        string expected =
            NormalizeName(itemName);

        foreach (
            InventoryManager.InventorySlot slot
            in slots)
        {
            if (slot == null ||
                slot.IsEmpty)
            {
                continue;
            }

            if (NormalizeName(
                    slot.itemName) ==
                expected)
            {
                return true;
            }
        }

        return false;
    }

    private void EnsureAvailableEquipment()
    {
        FindReferences();

        FishingEquipmentShopItemData[]
            sourceItems = null;

        string sourceName =
            "Không có nguồn";

        if (equipmentShopUI != null &&
            equipmentShopUI
                .AvailableEquipment != null &&
            equipmentShopUI
                .AvailableEquipment.Length > 0)
        {
            sourceItems =
                equipmentShopUI
                    .AvailableEquipment;

            sourceName =
                "FishingEquipmentShopUI";
        }
        else if (
            FishingEquipmentShopUI
                .RegisteredEquipment != null &&
            FishingEquipmentShopUI
                .RegisteredEquipment.Length > 0)
        {
            sourceItems =
                FishingEquipmentShopUI
                    .RegisteredEquipment;

            sourceName =
                "Shop Registry";
        }
        else if (
            shopEquipmentItems != null &&
            shopEquipmentItems.Length > 0)
        {
            sourceItems =
                shopEquipmentItems;

            sourceName =
                "Inspector Cache";
        }

        /*
         * Fallback mạnh:
         * Shop có thể nằm trong object inactive hoặc reference
         * chưa được kéo vào Inspector. Vì người chơi đã mua được,
         * các Shop Item Data thường đang được Unity load sẵn.
         */
        if (sourceItems == null ||
            sourceItems.Length == 0)
        {
            FishingEquipmentShopItemData[]
                loadedShopItems =
                    Resources
                        .FindObjectsOfTypeAll<
                            FishingEquipmentShopItemData
                        >();

            if (loadedShopItems != null &&
                loadedShopItems.Length > 0)
            {
                sourceItems =
                    loadedShopItems;

                sourceName =
                    "Loaded Shop Item Assets";
            }
        }

        if (sourceItems == null ||
            sourceItems.Length == 0)
        {
            if (showDebugLogs)
            {
                Debug.LogWarning(
                    "[FishingCustomizationUI] " +
                    "Không tìm thấy nguồn Shop Item. " +
                    "Kéo FishingEquipmentShopUI vào Equipment Shop UI " +
                    "hoặc chạy Auto Link Shop Source.",
                    this
                );
            }

            return;
        }

        List<FishingEquipmentData> result =
            new List<FishingEquipmentData>();

        equipmentOwnershipNameMap.Clear();

        foreach (
            FishingEquipmentShopItemData shopItem
            in sourceItems)
        {
            if (shopItem == null ||
                shopItem.equipmentData == null)
            {
                continue;
            }

            FishingEquipmentData equipment =
                shopItem.equipmentData;

            string shopName =
                string.IsNullOrWhiteSpace(
                    shopItem.ItemName)
                    ? equipment.itemName
                    : shopItem.ItemName;

            if (IsRodName(shopName) ||
                IsRodName(equipment.itemName))
            {
                continue;
            }

            if (TryResolvePartTypeByName(
                    shopName,
                    out FishingPartType resolvedType) ||
                TryResolvePartTypeByName(
                    equipment.itemName,
                    out resolvedType))
            {
                equipment.partType =
                    resolvedType;
            }

            AddEquipment(
                result,
                equipment
            );

            equipmentOwnershipNameMap[
                equipment
            ] = shopName;

            /*
             * FishingGearStats cũng phải dùng đúng tên Shop đã
             * thêm vào Inventory; nếu không UI báo Owned=True
             * nhưng TryEquip() vẫn từ chối.
             */
            if (gearStats != null)
            {
                gearStats.RegisterOwnershipName(
                    equipment,
                    shopName
                );
            }
        }

        availableEquipment =
            result.ToArray();

        shopEquipmentItems =
            sourceItems;

        if (showDebugLogs)
        {
            Debug.Log(
                "[FishingCustomizationUI] Đồng bộ Shop: " +
                "Source=" +
                sourceName +
                " | Shop Items=" +
                sourceItems.Length +
                " | Equipment=" +
                availableEquipment.Length +
                " | Ownership links=" +
                equipmentOwnershipNameMap.Count,
                this
            );
        }
    }

    private static bool IsRodName(
        string value)
    {
        string normalized =
            NormalizeName(value);

        return normalized.Contains(
                   "featherlight") ||
               normalized.StartsWith(
                   "eqfeatherlight");
    }

    private static bool TryResolvePartTypeByName(
        string value,
        out FishingPartType partType)
    {
        partType =
            FishingPartType.Reel;

        string normalized =
            NormalizeName(value);

        if (string.IsNullOrWhiteSpace(
                normalized))
        {
            return false;
        }

        if (normalized.Contains(
                "callistoxsr") ||
            normalized.Contains(
                "maycau") ||
            normalized.Contains(
                "reel"))
        {
            partType =
                FishingPartType.Reel;

            return true;
        }

        if (normalized.StartsWith(
                "day") ||
            normalized.Contains(
                "line") ||
            normalized.Contains(
                "mono") ||
            normalized.Contains(
                "noodle"))
        {
            partType =
                FishingPartType.Line;

            return true;
        }

        if (normalized.StartsWith(
                "moccau") ||
            normalized.Contains(
                "hook"))
        {
            partType =
                FishingPartType.Hook;

            return true;
        }

        if (normalized.Contains(
                "bait") ||
            normalized.Contains(
                "moicau"))
        {
            partType =
                FishingPartType.Bait;

            return true;
        }

        return false;
    }

    private static string NormalizeName(
        string value)
    {
        if (string.IsNullOrWhiteSpace(
                value))
        {
            return string.Empty;
        }

        string decomposed =
            value.Trim()
                .ToLowerInvariant()
                .Normalize(
                    NormalizationForm.FormD
                );

        StringBuilder builder =
            new StringBuilder();

        foreach (char character
                 in decomposed)
        {
            UnicodeCategory category =
                CharUnicodeInfo
                    .GetUnicodeCategory(
                        character
                    );

            if (category ==
                UnicodeCategory
                    .NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetterOrDigit(
                    character))
            {
                builder.Append(character);
            }
        }

        return builder
            .ToString()
            .Normalize(
                NormalizationForm.FormC
            );
    }

    private static void AddEquipmentArray(
        List<FishingEquipmentData> target,
        FishingEquipmentData[] source)
    {
        if (source == null)
            return;

        foreach (
            FishingEquipmentData equipment
            in source)
        {
            AddEquipment(
                target,
                equipment
            );
        }
    }

    private static void AddEquipment(
        List<FishingEquipmentData> target,
        FishingEquipmentData equipment)
    {
        if (target == null ||
            equipment == null ||
            target.Contains(equipment))
        {
            return;
        }

        target.Add(equipment);
    }

    private static object GetMemberValue(
        object target,
        string memberName)
    {
        if (target == null ||
            string.IsNullOrWhiteSpace(
                memberName))
        {
            return null;
        }

        Type type =
            target.GetType();

        FieldInfo field =
            type.GetField(
                memberName,
                MemberFlags
            );

        if (field != null)
        {
            try
            {
                return field.GetValue(
                    target
                );
            }
            catch
            {
                // Thử property.
            }
        }

        PropertyInfo property =
            type.GetProperty(
                memberName,
                MemberFlags
            );

        if (property != null &&
            property.CanRead &&
            property.GetIndexParameters()
                .Length == 0)
        {
            try
            {
                return property.GetValue(
                    target,
                    null
                );
            }
            catch
            {
                // Không đọc được.
            }
        }

        return null;
    }

    private string GetOwnershipName(
        FishingEquipmentData equipment)
    {
        if (equipment == null)
            return string.Empty;

        if (equipmentOwnershipNameMap
                .TryGetValue(
                    equipment,
                    out string mappedName) &&
            !string.IsNullOrWhiteSpace(
                mappedName))
        {
            return mappedName;
        }

        return equipment.itemName;
    }

    private void ClearOptionList()
    {
        if (optionContent == null)
            return;

        for (int i =
                 optionContent.childCount - 1;
             i >= 0;
             i--)
        {
            Destroy(
                optionContent
                    .GetChild(i)
                    .gameObject
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
            FindDeepChild(
                parent,
                objectName
            );

        return target != null
            ? target.GetComponent<Image>()
            : null;
    }

    private TMP_Text FindText(
        Transform parent,
        string objectName)
    {
        Transform target =
            FindDeepChild(
                parent,
                objectName
            );

        return target != null
            ? target.GetComponent<
                TMP_Text
            >()
            : null;
    }

    private Transform FindDeepChild(
        Transform parent,
        string objectName)
    {
        if (parent == null)
            return null;

        foreach (Transform child
                 in parent)
        {
            if (child.name ==
                objectName)
            {
                return child;
            }

            Transform result =
                FindDeepChild(
                    child,
                    objectName
                );

            if (result != null)
                return result;
        }

        return null;
    }

    [ContextMenu(
        "TEST - Print Owned Equipment"
    )]
    private void PrintOwnedEquipment()
    {
        FindReferences();
        EnsureAvailableEquipment();

        Debug.Log(
            "[FishingCustomizationUI] Inventory=" +
            (inventoryManager != null
                ? inventoryManager.name
                : "NULL") +
            " | GearStats=" +
            (gearStats != null
                ? gearStats.name
                : "NULL") +
            " | Available=" +
            (availableEquipment != null
                ? availableEquipment.Length
                : 0) +
            " | ShopUI=" +
            (equipmentShopUI != null
                ? equipmentShopUI.name
                : "NULL") +
            " | ShopCache=" +
            (shopEquipmentItems != null
                ? shopEquipmentItems.Length
                : 0),
            this
        );

        if (availableEquipment == null)
            return;

        foreach (
            FishingEquipmentData equipment
            in availableEquipment)
        {
            if (equipment == null)
                continue;

            string ownershipName =
                equipment.itemName;

            if (equipmentOwnershipNameMap
                    .TryGetValue(
                        equipment,
                        out string mappedName) &&
                !string.IsNullOrWhiteSpace(
                    mappedName))
            {
                ownershipName =
                    mappedName;
            }

            Debug.Log(
                "[FishingCustomizationUI] " +
                equipment.partType +
                " | Equipment=" +
                equipment.itemName +
                " | InventoryName=" +
                ownershipName +
                " | Owned=" +
                IsOwned(equipment),
                equipment
            );
        }
    }
    [ContextMenu(
        "TEST - Print Shop And Inventory Match"
    )]
    private void PrintShopAndInventoryMatch()
    {
        FindReferences();
        EnsureAvailableEquipment();

        Debug.Log(
            "[FishingCustomizationUI TEST] " +
            "Inventory=" +
            (inventoryManager != null
                ? inventoryManager.name
                : "NULL") +
            " | Available=" +
            (availableEquipment != null
                ? availableEquipment.Length
                : 0),
            this
        );

        if (inventoryManager != null)
        {
            PrintInventorySlots(
                "Hotbar",
                inventoryManager.HotbarSlots
            );

            PrintInventorySlots(
                "Bag",
                inventoryManager.BagSlots
            );
        }

        if (availableEquipment == null)
            return;

        foreach (
            FishingEquipmentData equipment
            in availableEquipment)
        {
            if (equipment == null)
                continue;

            string inventoryName =
                GetOwnershipName(
                    equipment
                );

            Debug.Log(
                "[FishingCustomizationUI TEST] " +
                equipment.partType +
                " | Equipment=" +
                equipment.itemName +
                " | InventoryName=" +
                inventoryName +
                " | NameOwned=" +
                HasItemByName(
                    inventoryName
                ) +
                " | IconOwned=" +
                HasItemByIcon(
                    equipment.icon
                ) +
                " | FinalOwned=" +
                IsOwned(equipment),
                equipment
            );
        }
    }

    private static void PrintInventorySlots(
        string groupName,
        InventoryManager.InventorySlot[] slots)
    {
        if (slots == null)
        {
            Debug.Log(
                "[FishingCustomizationUI TEST] " +
                groupName +
                "=NULL"
            );

            return;
        }

        for (int index = 0;
             index < slots.Length;
             index++)
        {
            InventoryManager.InventorySlot slot =
                slots[index];

            if (slot == null ||
                slot.IsEmpty)
            {
                continue;
            }

            Debug.Log(
                "[FishingCustomizationUI TEST] " +
                groupName +
                "[" +
                index +
                "] Name=" +
                slot.itemName +
                " | Amount=" +
                slot.amount +
                " | Icon=" +
                (slot.icon != null
                    ? slot.icon.name
                    : "NULL")
            );
        }
    }

}
