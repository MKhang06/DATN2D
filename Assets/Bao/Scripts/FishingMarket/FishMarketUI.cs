using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class FishMarketUI : MonoBehaviour
{
    [Serializable]
    private sealed class FishPriceOverride
    {
        public InventoryItemData fishItem;

        [Min(0.01f)]
        public float minimumMultiplier =
            0.75f;

        [Min(0.01f)]
        public float maximumMultiplier =
            1.25f;
    }

    private sealed class MarketState
    {
        public InventoryItemData item;

        public float historyPrice3;
        public float historyPrice2;
        public float historyPrice1;
        public float currentPrice;

        public double lastUpdateUnix;
    }

    [Header("Root")]
    [Tooltip(
        "Panel cửa hàng. Script nên nằm " +
        "trên một object luôn Active."
    )]
    [SerializeField]
    private GameObject root;

    [SerializeField]
    private CanvasGroup rootCanvasGroup;

    [Header("Danh sách")]
    [SerializeField]
    private Transform content;

    [SerializeField]
    private FishMarketRowUI rowPrefab;

    [SerializeField]
    private bool showOnlyOwnedFish;

    [Header("Header")]
    [SerializeField]
    private TMP_Text moneyText;

    [SerializeField]
    private TMP_Text nextUpdateText;

    [SerializeField]
    private Button closeButton;

    [Header("References")]
    [SerializeField]
    private PlayerStats playerStats;

    [SerializeField]
    private InventoryManager inventoryManager;

    [Header("Danh sách cá")]
    [Tooltip(
        "Tool tạo UI sẽ tự gắn toàn bộ " +
        "InventoryItemData có ItemType = Fish."
    )]
    [SerializeField]
    private InventoryItemData[] fishItems;

    [Header("Giá thị trường")]
    [Tooltip(
        "Mặc định 3600 giây = 1 giờ. " +
        "Khi test có thể đặt 10 giây."
    )]
    [SerializeField, Min(1f)]
    private float priceUpdateInterval =
        3600f;

    [SerializeField, Range(0f, 1f)]
    private float defaultVolatility =
        0.18f;

    [SerializeField, Min(0.01f)]
    private float defaultMinimumMultiplier =
        0.7f;

    [SerializeField, Min(0.01f)]
    private float defaultMaximumMultiplier =
        1.3f;

    [SerializeField]
    private FishPriceOverride[]
        priceOverrides;

    [Header("Điều khiển")]
    [SerializeField]
    private KeyCode closeKey =
        KeyCode.Escape;

    [SerializeField]
    private bool lockPlayerWhileOpen = true;

    [SerializeField]
    private bool saveMarketPrices = true;

    private readonly Dictionary<
        InventoryItemData,
        MarketState
    > states =
        new Dictionary<
            InventoryItemData,
            MarketState
        >();

    private readonly Dictionary<
        InventoryItemData,
        FishMarketRowUI
    > rows =
        new Dictionary<
            InventoryItemData,
            FishMarketRowUI
        >();

    private bool isOpen;
    private bool subscribed;

    public bool IsOpen => isOpen;

    private void Awake()
    {
        ResolveReferences();
        ResolveRootCanvasGroup();

        if (closeButton != null)
        {
            closeButton.onClick
                .RemoveListener(CloseShop);

            closeButton.onClick
                .AddListener(CloseShop);
        }

        SetVisible(false);
    }

    private void Start()
    {
        InitializeMarket();
    }

    private void OnEnable()
    {
        ResolveReferences();
        SubscribeInventory();
    }

    private void OnDisable()
    {
        UnsubscribeInventory();
    }

    private void Update()
    {
        UpdateMarketByElapsedTime();
        UpdateCountdown();

        if (isOpen &&
            Input.GetKeyDown(closeKey))
        {
            CloseShop();
        }
    }

    public void OpenShop()
    {
        ResolveReferences();
        InitializeMarket();
        SubscribeInventory();

        isOpen = true;
        SetVisible(true);

        RefreshAllRows();
        RefreshMoney();

        if (lockPlayerWhileOpen)
        {
            GameLockManager.Instance?.
                LockPlayer();
        }

        if (EventSystem.current != null)
        {
            EventSystem.current
                .SetSelectedGameObject(null);
        }
    }

    public void CloseShop()
    {
        if (!isOpen &&
            (root == null ||
             !root.activeSelf))
        {
            return;
        }

        isOpen = false;
        SetVisible(false);

        if (lockPlayerWhileOpen)
        {
            GameLockManager.Instance?.
                UnlockPlayer();
        }

        if (EventSystem.current != null)
        {
            EventSystem.current
                .SetSelectedGameObject(null);
        }
    }

    private void InitializeMarket()
    {
        ResolveReferences();

        List<InventoryItemData> validFish =
            fishItems == null
                ? new List<InventoryItemData>()
                : fishItems
                    .Where(
                        item =>
                            item != null &&
                            item.ItemType ==
                                InventoryItemType.Fish
                    )
                    .Distinct()
                    .OrderBy(
                        item =>
                            item.DisplayName
                    )
                    .ToList();

        if (validFish.Count == 0)
        {
            Debug.LogError(
                "FishMarketUI chưa có danh sách cá. " +
                "Chạy lại Tools > Fishing > " +
                "Create Fish Market UI hoặc kéo " +
                "InventoryItemData cá vào Fish Items.",
                this
            );

            return;
        }

        foreach (InventoryItemData fishItem
                 in validFish)
        {
            if (states.ContainsKey(fishItem))
                continue;

            states.Add(
                fishItem,
                LoadOrCreateState(
                    fishItem
                )
            );
        }

        BuildRows(validFish);
        RefreshAllRows();
        RefreshMoney();
    }

    private void BuildRows(
        List<InventoryItemData> fishItems)
    {
        if (content == null ||
            rowPrefab == null)
        {
            return;
        }

        foreach (FishMarketRowUI row
                 in rows.Values)
        {
            if (row != null)
                Destroy(row.gameObject);
        }

        rows.Clear();

        foreach (InventoryItemData item
                 in fishItems)
        {
            int owned =
                GetOwnedAmount(item);

            if (showOnlyOwnedFish &&
                owned <= 0)
            {
                continue;
            }

            FishMarketRowUI row =
                Instantiate(
                    rowPrefab,
                    content
                );

            row.gameObject.SetActive(true);
            row.name =
                "FishMarketRow_" +
                item.ItemId;

            rows[item] = row;

            row.Bind(
                BuildRowView(item),
                SellAllOfFish
            );
        }

        Canvas.ForceUpdateCanvases();

        if (content is RectTransform rect)
        {
            LayoutRebuilder
                .ForceRebuildLayoutImmediate(
                    rect
                );
        }
    }

    private void RefreshAllRows()
    {
        if (showOnlyOwnedFish)
        {
            List<InventoryItemData> visibleFish =
                states.Keys
                    .Where(item => item != null)
                    .OrderBy(
                        item =>
                            item.DisplayName
                    )
                    .ToList();

            BuildRows(visibleFish);
            RefreshMoney();
            return;
        }

        foreach (
            KeyValuePair<
                InventoryItemData,
                FishMarketRowUI
            > pair in rows)
        {
            if (pair.Key == null ||
                pair.Value == null)
            {
                continue;
            }

            pair.Value.Refresh(
                BuildRowView(pair.Key)
            );
        }

        RefreshMoney();
    }

    private FishMarketRowView BuildRowView(
        InventoryItemData item)
    {
        if (item == null ||
            !states.TryGetValue(
                item,
                out MarketState state))
        {
            return new FishMarketRowView(
                item,
                0f,
                0f,
                0f,
                0f,
                0
            );
        }

        return new FishMarketRowView(
            item,
            state.historyPrice3,
            state.historyPrice2,
            state.historyPrice1,
            state.currentPrice,
            GetOwnedAmount(item)
        );
    }

    private int GetOwnedAmount(
        InventoryItemData item)
    {
        if (inventoryManager == null ||
            item == null)
        {
            return 0;
        }

        return inventoryManager
            .GetItemAmount(item);
    }

    private void SellAllOfFish(
        InventoryItemData fishItem)
    {
        ResolveReferences();

        if (fishItem == null ||
            fishItem.ItemType !=
                InventoryItemType.Fish)
        {
            ShowMessage(
                "Vật phẩm này không phải cá.",
                false
            );

            return;
        }

        if (inventoryManager == null ||
            playerStats == null)
        {
            ShowMessage(
                "Không tìm thấy dữ liệu " +
                "người chơi hoặc túi đồ.",
                false
            );

            return;
        }

        int amount =
            inventoryManager
                .GetItemAmount(fishItem);

        if (amount <= 0)
        {
            ShowMessage(
                "Bạn không có " +
                fishItem.DisplayName +
                " để bán.",
                false
            );

            RefreshAllRows();
            return;
        }

        if (!states.TryGetValue(
                fishItem,
                out MarketState state))
        {
            ShowMessage(
                "Không tìm thấy giá thị trường.",
                false
            );

            return;
        }

        long payoutLong =
            (long)Mathf.RoundToInt(
                state.currentPrice *
                amount
            );

        if (payoutLong <= 0 ||
            payoutLong > int.MaxValue)
        {
            ShowMessage(
                "Tổng tiền giao dịch " +
                "không hợp lệ.",
                false
            );

            return;
        }

        int payout =
            (int)payoutLong;

        bool removed =
            inventoryManager.RemoveItem(
                fishItem,
                amount,
                out string reason
            );

        if (!removed)
        {
            ShowMessage(
                string.IsNullOrWhiteSpace(
                    reason)
                    ? "Không thể lấy cá " +
                      "khỏi túi."
                    : reason,
                false
            );

            return;
        }

        playerStats.AddMoney(payout);

        ShowMessage(
            "Đã bán " +
            fishItem.DisplayName +
            " x" +
            amount.ToString("N0") +
            " và nhận $" +
            payout.ToString("N0") +
            ".",
            true
        );

        RefreshAllRows();
    }

    private void ShowMessage(
        string message,
        bool success)
    {
        FishingNotificationUI notification =
            FishingNotificationUI.Instance;

        if (notification != null)
        {
            notification.ShowNotification(
                message,
                success
                    ? FishingNotificationUI
                        .NotificationType.Sell
                    : FishingNotificationUI
                        .NotificationType.Warning
            );
        }

        Debug.Log(message);
    }

    private MarketState LoadOrCreateState(
        InventoryItemData item)
    {
        float basePrice =
            Mathf.Max(
                1f,
                item.SellPrice
            );

        MarketState state =
            new MarketState
            {
                item = item
            };

        string prefix =
            GetSavePrefix(item);

        bool hasSave =
            saveMarketPrices &&
            PlayerPrefs.HasKey(
                prefix + "_current"
            );

        if (hasSave)
        {
            state.historyPrice3 =
                PlayerPrefs.GetFloat(
                    prefix + "_h3",
                    basePrice
                );

            state.historyPrice2 =
                PlayerPrefs.GetFloat(
                    prefix + "_h2",
                    basePrice
                );

            state.historyPrice1 =
                PlayerPrefs.GetFloat(
                    prefix + "_h1",
                    basePrice
                );

            state.currentPrice =
                PlayerPrefs.GetFloat(
                    prefix + "_current",
                    basePrice
                );

            state.lastUpdateUnix =
                ReadDouble(
                    prefix + "_time",
                    GetUnixTime()
                );
        }
        else
        {
            state.historyPrice3 =
                GeneratePrice(
                    item,
                    basePrice
                );

            state.historyPrice2 =
                GeneratePrice(
                    item,
                    state.historyPrice3
                );

            state.historyPrice1 =
                GeneratePrice(
                    item,
                    state.historyPrice2
                );

            state.currentPrice =
                GeneratePrice(
                    item,
                    state.historyPrice1
                );

            state.lastUpdateUnix =
                GetUnixTime();

            SaveState(state);
        }

        ApplyElapsedUpdates(state);
        return state;
    }

    private void UpdateMarketByElapsedTime()
    {
        bool changed = false;

        foreach (MarketState state
                 in states.Values)
        {
            if (state == null)
                continue;

            if (ApplyElapsedUpdates(state))
                changed = true;
        }

        if (changed && isOpen)
            RefreshAllRows();
    }

    private bool ApplyElapsedUpdates(
        MarketState state)
    {
        if (state == null ||
            state.item == null)
        {
            return false;
        }

        double now = GetUnixTime();

        double elapsed =
            now - state.lastUpdateUnix;

        int periods =
            Mathf.FloorToInt(
                (float)(
                    elapsed /
                    priceUpdateInterval
                )
            );

        if (periods <= 0)
            return false;

        periods =
            Mathf.Clamp(periods, 1, 24);

        for (int i = 0;
             i < periods;
             i++)
        {
            state.historyPrice3 =
                state.historyPrice2;

            state.historyPrice2 =
                state.historyPrice1;

            state.historyPrice1 =
                state.currentPrice;

            state.currentPrice =
                GeneratePrice(
                    state.item,
                    state.currentPrice
                );
        }

        state.lastUpdateUnix +=
            periods *
            priceUpdateInterval;

        SaveState(state);
        return true;
    }

    private float GeneratePrice(
        InventoryItemData item,
        float previousPrice)
    {
        float basePrice =
            Mathf.Max(
                1f,
                item.SellPrice
            );

        GetPriceRange(
            item,
            out float minimumMultiplier,
            out float maximumMultiplier
        );

        float minimumPrice =
            basePrice *
            minimumMultiplier;

        float maximumPrice =
            basePrice *
            maximumMultiplier;

        float randomChange =
            UnityEngine.Random.Range(
                -defaultVolatility,
                defaultVolatility
            );

        float candidate =
            previousPrice *
            (1f + randomChange);

        candidate =
            Mathf.Clamp(
                candidate,
                minimumPrice,
                maximumPrice
            );

        return Mathf.Round(
                   candidate * 100f
               ) / 100f;
    }

    private void GetPriceRange(
        InventoryItemData item,
        out float minimumMultiplier,
        out float maximumMultiplier)
    {
        minimumMultiplier =
            Mathf.Max(
                0.01f,
                defaultMinimumMultiplier
            );

        maximumMultiplier =
            Mathf.Max(
                minimumMultiplier,
                defaultMaximumMultiplier
            );

        if (priceOverrides == null)
            return;

        foreach (FishPriceOverride entry
                 in priceOverrides)
        {
            if (entry == null ||
                entry.fishItem != item)
            {
                continue;
            }

            minimumMultiplier =
                Mathf.Max(
                    0.01f,
                    entry.minimumMultiplier
                );

            maximumMultiplier =
                Mathf.Max(
                    minimumMultiplier,
                    entry.maximumMultiplier
                );

            return;
        }
    }

    private void SaveState(
        MarketState state)
    {
        if (!saveMarketPrices ||
            state == null ||
            state.item == null)
        {
            return;
        }

        string prefix =
            GetSavePrefix(state.item);

        PlayerPrefs.SetFloat(
            prefix + "_h3",
            state.historyPrice3
        );

        PlayerPrefs.SetFloat(
            prefix + "_h2",
            state.historyPrice2
        );

        PlayerPrefs.SetFloat(
            prefix + "_h1",
            state.historyPrice1
        );

        PlayerPrefs.SetFloat(
            prefix + "_current",
            state.currentPrice
        );

        PlayerPrefs.SetString(
            prefix + "_time",
            state.lastUpdateUnix
                .ToString(
                    System.Globalization
                        .CultureInfo
                        .InvariantCulture
                )
        );

        PlayerPrefs.Save();
    }

    private static double ReadDouble(
        string key,
        double fallback)
    {
        string raw =
            PlayerPrefs.GetString(
                key,
                string.Empty
            );

        if (double.TryParse(
                raw,
                System.Globalization
                    .NumberStyles.Float,
                System.Globalization
                    .CultureInfo
                    .InvariantCulture,
                out double value))
        {
            return value;
        }

        return fallback;
    }

    private static string GetSavePrefix(
        InventoryItemData item)
    {
        string id =
            item != null &&
            !string.IsNullOrWhiteSpace(
                item.ItemId)
                ? item.ItemId
                : item != null
                    ? item.name
                    : "unknown";

        return "FishMarket_" + id;
    }

    private void UpdateCountdown()
    {
        if (nextUpdateText == null ||
            states.Count == 0)
        {
            return;
        }

        double now = GetUnixTime();

        double nextUpdate =
            states.Values
                .Where(
                    state =>
                        state != null
                )
                .Select(
                    state =>
                        state.lastUpdateUnix +
                        priceUpdateInterval
                )
                .DefaultIfEmpty(now)
                .Min();

        double remaining =
            Math.Max(
                0d,
                nextUpdate - now
            );

        TimeSpan time =
            TimeSpan.FromSeconds(
                remaining
            );

        nextUpdateText.text =
            "GIÁ MỚI SAU " +
            time.ToString(
                time.TotalHours >= 1d
                    ? @"hh\:mm\:ss"
                    : @"mm\:ss"
            );
    }

    private void RefreshMoney()
    {
        if (moneyText == null)
            return;

        int money =
            playerStats != null
                ? playerStats.Money
                : 0;

        moneyText.text =
            "TIỀN MẶT  <color=#55F6A9>$" +
            money.ToString("N0") +
            "</color>";
    }

    private void ResolveReferences()
    {
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

        if (playerStats == null)
        {
            playerStats =
                FindFirstObjectByType<
                    PlayerStats
                >();
        }

    }

    private void SubscribeInventory()
    {
        if (subscribed ||
            inventoryManager == null)
        {
            return;
        }

        inventoryManager.OnInventoryChanged +=
            HandleInventoryChanged;

        subscribed = true;
    }

    private void UnsubscribeInventory()
    {
        if (!subscribed ||
            inventoryManager == null)
        {
            return;
        }

        inventoryManager.OnInventoryChanged -=
            HandleInventoryChanged;

        subscribed = false;
    }

    private void HandleInventoryChanged()
    {
        if (isOpen)
            RefreshAllRows();
    }

    private void ResolveRootCanvasGroup()
    {
        if (root == null)
            return;

        if (rootCanvasGroup == null)
        {
            rootCanvasGroup =
                root.GetComponent<
                    CanvasGroup
                >();
        }

        if (rootCanvasGroup == null)
        {
            rootCanvasGroup =
                root.AddComponent<
                    CanvasGroup
                >();
        }
    }

    private void SetVisible(bool visible)
    {
        ResolveRootCanvasGroup();

        if (root == null)
            return;

        if (visible &&
            !root.activeSelf)
        {
            root.SetActive(true);
        }

        if (rootCanvasGroup != null)
        {
            rootCanvasGroup.alpha =
                visible ? 1f : 0f;

            rootCanvasGroup.interactable =
                visible;

            rootCanvasGroup.blocksRaycasts =
                visible;
        }

        if (!visible &&
            root.activeSelf)
        {
            root.SetActive(false);
        }
    }

    private static double GetUnixTime()
    {
        return DateTimeOffset.UtcNow
            .ToUnixTimeSeconds();
    }

    private void OnDestroy()
    {
        UnsubscribeInventory();

        if (closeButton != null)
        {
            closeButton.onClick
                .RemoveListener(CloseShop);
        }
    }
}
