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
        public InventoryItemData fishItem = null;

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
    private bool marketInitialized;
    private bool transactionInProgress;
    private bool marketPrefsDirty;
    private bool configurationErrorLogged;
    private InventoryManager subscribedInventoryManager;
    private GameLockManager playerLockManager;
    private float nextMaintenanceTime;
    private long lastCountdownSeconds = long.MinValue;
    private int lastDisplayedMoney = int.MinValue;
    private bool cursorStateCaptured;
    private bool previousCursorVisible;
    private CursorLockMode previousCursorLockMode;

    private const float MaintenanceInterval = 0.5f;

    public bool IsOpen => isOpen;

    private void Awake()
    {
        ClampSettings();
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
        if (isOpen ||
            playerLockManager != null ||
            cursorStateCaptured)
            CloseShop();

        UnsubscribeInventory();
        FlushMarketPrefs();
    }

    private void Update()
    {
        if (Time.unscaledTime >= nextMaintenanceTime)
        {
            nextMaintenanceTime =
                Time.unscaledTime +
                MaintenanceInterval;

            UpdateMarketByElapsedTime();

            if (isOpen)
            {
                UpdateCountdown();
                RefreshMoney();
            }
        }

        if (isOpen &&
            Input.GetKeyDown(closeKey))
        {
            CloseShop();
        }
    }

    public void OpenShop()
    {
        ResolveReferences();
        SubscribeInventory();

        if (!marketInitialized)
            InitializeMarket();

        if (!marketInitialized || root == null)
        {
            ShowMessage(
                "Chợ cá chưa được cấu hình đầy đủ.",
                false
            );
            return;
        }

        if (isOpen)
        {
            RefreshAllRows();
            RefreshMoney(true);
            UpdateCountdown(true);
            return;
        }

        UpdateMarketByElapsedTime();

        isOpen = true;
        SetVisible(true);
        CaptureCursorState();

        RefreshAllRows();
        RefreshMoney(true);
        UpdateCountdown(true);

        AcquirePlayerLock();

        if (EventSystem.current != null)
        {
            EventSystem.current
                .SetSelectedGameObject(null);
        }
    }

    public void CloseShop()
    {
        if (!isOpen &&
            playerLockManager == null &&
            !cursorStateCaptured &&
            (root == null ||
             !root.activeSelf))
        {
            return;
        }

        isOpen = false;
        SetVisible(false);

        ReleasePlayerLock();
        RestoreCursorState();

        if (EventSystem.current != null)
        {
            EventSystem.current
                .SetSelectedGameObject(null);
        }
    }

    private void InitializeMarket()
    {
        ResolveReferences();

        if (root == null ||
            content == null ||
            rowPrefab == null)
        {
            LogConfigurationErrorOnce();
            marketInitialized = false;
            return;
        }

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

            marketInitialized = false;
            return;
        }

        HashSet<InventoryItemData> validSet =
            new HashSet<InventoryItemData>(validFish);

        List<InventoryItemData> staleStates =
            states.Keys
                .Where(item => !validSet.Contains(item))
                .ToList();

        foreach (InventoryItemData staleItem in staleStates)
            states.Remove(staleItem);

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
        marketInitialized = true;
        RefreshAllRows();
        FlushMarketPrefs();
    }

    private void BuildRows(
        List<InventoryItemData> fishItems)
    {
        if (content == null ||
            rowPrefab == null)
        {
            return;
        }

        HashSet<InventoryItemData> validItems =
            new HashSet<InventoryItemData>(fishItems);

        List<InventoryItemData> staleRows =
            rows
                .Where(
                    pair =>
                        pair.Key == null ||
                        pair.Value == null ||
                        !validItems.Contains(pair.Key)
                )
                .Select(pair => pair.Key)
                .ToList();

        foreach (InventoryItemData staleItem in staleRows)
        {
            if (staleItem != null &&
                rows.TryGetValue(staleItem, out FishMarketRowUI staleRow) &&
                staleRow != null)
            {
                Destroy(staleRow.gameObject);
            }

            rows.Remove(staleItem);
        }

        for (int index = 0;
             index < fishItems.Count;
             index++)
        {
            InventoryItemData item = fishItems[index];
            int owned =
                GetOwnedAmount(item);

            if (!rows.TryGetValue(
                    item,
                    out FishMarketRowUI row) ||
                row == null)
            {
                row = Instantiate(
                    rowPrefab,
                    content
                );

                row.name =
                    "FishMarketRow_" +
                    item.ItemId;

                rows[item] = row;
                row.Bind(
                    BuildRowView(item),
                    SellAllOfFish
                );
            }

            row.transform.SetSiblingIndex(index);

            bool shouldShow =
                !showOnlyOwnedFish ||
                owned > 0;

            row.gameObject.SetActive(shouldShow);
        }

        RebuildContentLayout();
    }

    private void RefreshAllRows()
    {
        bool layoutChanged = false;

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

            FishMarketRowView view =
                BuildRowView(pair.Key);

            bool shouldShow =
                !showOnlyOwnedFish ||
                view.OwnedAmount > 0;

            if (pair.Value.gameObject.activeSelf != shouldShow)
            {
                pair.Value.gameObject.SetActive(shouldShow);
                layoutChanged = true;
            }

            if (shouldShow)
                pair.Value.Refresh(view);
        }

        RefreshMoney();

        if (layoutChanged)
            RebuildContentLayout();
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
        if (transactionInProgress)
            return;

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

        double rawPayout =
            state.currentPrice *
            (double)amount;

        long payoutLong =
            double.IsNaN(rawPayout) ||
            double.IsInfinity(rawPayout) ||
            rawPayout <= 0d ||
            rawPayout > int.MaxValue
                ? 0L
                : (long)Math.Round(
                    rawPayout,
                    MidpointRounding.AwayFromZero
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

        float moneyMultiplier =
            CharacterPassiveManager.Instance != null
                ? CharacterPassiveManager.Instance.MoneyMultiplier
                : 1f;

        double creditedPayout =
            payout *
            Math.Max(0d, moneyMultiplier);

        long expectedCredit =
            !double.IsNaN(creditedPayout) &&
            !double.IsInfinity(creditedPayout) &&
            creditedPayout >= 0d &&
            creditedPayout <= int.MaxValue
                ? (long)Math.Round(
                    (float)creditedPayout,
                    MidpointRounding.ToEven
                )
                : 0L;

        if (double.IsNaN(creditedPayout) ||
            double.IsInfinity(creditedPayout) ||
            expectedCredit <= 0L ||
            expectedCredit > (long)int.MaxValue -
                Mathf.Max(0, playerStats.Money))
        {
            ShowMessage(
                "Số tiền sau thưởng vượt giới hạn cho phép.",
                false
            );
            return;
        }

        transactionInProgress = true;

        try
        {
            bool removed =
                inventoryManager.RemoveItem(
                    fishItem,
                    amount,
                    out string reason
                );

            if (!removed)
            {
                ShowMessage(
                    string.IsNullOrWhiteSpace(reason)
                        ? "Không thể lấy cá khỏi túi."
                        : reason,
                    false
                );
                return;
            }

            int moneyBefore =
                playerStats.Money;

            playerStats.AddMoney(payout);

            long creditedDifference =
                (long)playerStats.Money -
                moneyBefore;

            int creditedAmount =
                (int)Math.Min(
                    int.MaxValue,
                    Math.Max(0L, creditedDifference)
                );

            ShowMessage(
                "Đã bán " +
                fishItem.DisplayName +
                " x" +
                amount.ToString("N0") +
                " và nhận $" +
                creditedAmount.ToString("N0") +
                ".",
                true
            );
        }
        finally
        {
            transactionInProgress = false;
            RefreshAllRows();
        }
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
        double now = GetUnixTime();

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

        bool stateChanged = !hasSave;

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
                    now
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
                now;
        }

        float sanitizedHistory3 =
            SanitizePrice(item, state.historyPrice3, basePrice);
        float sanitizedHistory2 =
            SanitizePrice(item, state.historyPrice2, basePrice);
        float sanitizedHistory1 =
            SanitizePrice(item, state.historyPrice1, basePrice);
        float sanitizedCurrent =
            SanitizePrice(item, state.currentPrice, basePrice);

        stateChanged |=
            !Mathf.Approximately(state.historyPrice3, sanitizedHistory3) ||
            !Mathf.Approximately(state.historyPrice2, sanitizedHistory2) ||
            !Mathf.Approximately(state.historyPrice1, sanitizedHistory1) ||
            !Mathf.Approximately(state.currentPrice, sanitizedCurrent);

        state.historyPrice3 = sanitizedHistory3;
        state.historyPrice2 = sanitizedHistory2;
        state.historyPrice1 = sanitizedHistory1;
        state.currentPrice = sanitizedCurrent;

        if (double.IsNaN(state.lastUpdateUnix) ||
            double.IsInfinity(state.lastUpdateUnix) ||
            state.lastUpdateUnix <= 0d ||
            state.lastUpdateUnix > now + priceUpdateInterval)
        {
            state.lastUpdateUnix = now;
            stateChanged = true;
        }

        if (stateChanged)
            SaveState(state);

        ApplyElapsedUpdates(state, now);
        return state;
    }

    private void UpdateMarketByElapsedTime()
    {
        bool changed = false;
        double now = GetUnixTime();

        foreach (MarketState state
                 in states.Values)
        {
            if (state == null)
                continue;

            if (ApplyElapsedUpdates(state, now))
                changed = true;
        }

        if (!changed)
            return;

        FlushMarketPrefs();

        if (isOpen)
            RefreshAllRows();
    }

    private bool ApplyElapsedUpdates(
        MarketState state,
        double now)
    {
        if (state == null ||
            state.item == null)
        {
            return false;
        }

        double elapsed =
            now - state.lastUpdateUnix;

        if (elapsed < priceUpdateInterval)
            return false;

        double rawPeriods =
            Math.Floor(
                elapsed /
                priceUpdateInterval
            );

        long elapsedPeriods =
            rawPeriods >= long.MaxValue
                ? long.MaxValue
                : (long)rawPeriods;

        if (elapsedPeriods <= 0)
            return false;

        int simulatedPeriods =
            (int)Math.Min(elapsedPeriods, 24L);

        for (int i = 0;
             i < simulatedPeriods;
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

        state.lastUpdateUnix =
            elapsedPeriods >= long.MaxValue ||
            elapsedPeriods * (double)priceUpdateInterval > now
                ? now
                : state.lastUpdateUnix +
                  elapsedPeriods *
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

    private float SanitizePrice(
        InventoryItemData item,
        float value,
        float fallback)
    {
        if (float.IsNaN(value) ||
            float.IsInfinity(value))
        {
            value = fallback;
        }

        float basePrice =
            Mathf.Max(1f, item.SellPrice);

        GetPriceRange(
            item,
            out float minimumMultiplier,
            out float maximumMultiplier
        );

        value = Mathf.Clamp(
            value,
            basePrice * minimumMultiplier,
            basePrice * maximumMultiplier
        );

        return Mathf.Round(value * 100f) / 100f;
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

        marketPrefsDirty = true;
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

    private void UpdateCountdown(
        bool force = false)
    {
        if (nextUpdateText == null ||
            states.Count == 0)
        {
            return;
        }

        double now = GetUnixTime();

        double nextUpdate = double.MaxValue;

        foreach (MarketState state in states.Values)
        {
            if (state == null)
                continue;

            nextUpdate = Math.Min(
                nextUpdate,
                state.lastUpdateUnix +
                priceUpdateInterval
            );
        }

        if (nextUpdate == double.MaxValue)
            nextUpdate = now;

        double remaining =
            Math.Max(
                0d,
                nextUpdate - now
            );

        long remainingSeconds =
            (long)Math.Ceiling(remaining);

        if (!force &&
            remainingSeconds == lastCountdownSeconds)
        {
            return;
        }

        lastCountdownSeconds = remainingSeconds;

        long hours = remainingSeconds / 3600L;
        long minutes =
            (remainingSeconds % 3600L) / 60L;
        long seconds = remainingSeconds % 60L;

        nextUpdateText.text =
            "GIÁ MỚI SAU " +
            (hours > 0L
                ? hours.ToString("00") + ":" +
                  minutes.ToString("00") + ":" +
                  seconds.ToString("00")
                : minutes.ToString("00") + ":" +
                  seconds.ToString("00"));
    }

    private void RefreshMoney(
        bool force = false)
    {
        if (moneyText == null)
            return;

        int money =
            playerStats != null
                ? playerStats.Money
                : 0;

        money = Mathf.Max(0, money);

        if (!force && money == lastDisplayedMoney)
            return;

        lastDisplayedMoney = money;

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
        if (subscribed &&
            subscribedInventoryManager ==
                inventoryManager)
        {
            return;
        }

        if (subscribed)
            UnsubscribeInventory();

        if (inventoryManager == null)
        {
            return;
        }

        inventoryManager.OnInventoryChanged +=
            HandleInventoryChanged;

        subscribedInventoryManager =
            inventoryManager;
        subscribed = true;
    }

    private void UnsubscribeInventory()
    {
        if (!subscribed)
            return;

        if (subscribedInventoryManager != null)
        {
            subscribedInventoryManager.OnInventoryChanged -=
                HandleInventoryChanged;
        }

        subscribedInventoryManager = null;
        subscribed = false;
    }

    private void HandleInventoryChanged()
    {
        if (!isOpen || transactionInProgress)
            return;

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

    private void AcquirePlayerLock()
    {
        if (!lockPlayerWhileOpen ||
            playerLockManager != null)
        {
            return;
        }

        GameLockManager lockManager =
            GameLockManager.Instance;

        if (lockManager == null ||
            lockManager.IsLocked)
        {
            return;
        }

        lockManager.LockPlayer();
        playerLockManager = lockManager;
    }

    private void ReleasePlayerLock()
    {
        if (playerLockManager == null)
            return;

        playerLockManager.UnlockPlayer();
        playerLockManager = null;
    }

    private void CaptureCursorState()
    {
        if (cursorStateCaptured)
            return;

        previousCursorVisible = Cursor.visible;
        previousCursorLockMode = Cursor.lockState;
        cursorStateCaptured = true;

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    private void RestoreCursorState()
    {
        if (!cursorStateCaptured)
            return;

        Cursor.visible = previousCursorVisible;
        Cursor.lockState = previousCursorLockMode;
        cursorStateCaptured = false;
    }

    private void RebuildContentLayout()
    {
        if (!(content is RectTransform rect))
            return;

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
    }

    private void LogConfigurationErrorOnce()
    {
        if (configurationErrorLogged)
            return;

        configurationErrorLogged = true;
        Debug.LogError(
            "[FishMarket] Thiếu Root, Content hoặc Row Prefab. " +
            "Hãy chạy Tools > Fishing > Rebuild Fish Market UI - Fix White.",
            this
        );
    }

    private void FlushMarketPrefs()
    {
        if (!marketPrefsDirty)
            return;

        PlayerPrefs.Save();
        marketPrefsDirty = false;
    }

    private void ClampSettings()
    {
        priceUpdateInterval =
            Mathf.Max(1f, priceUpdateInterval);
        defaultVolatility =
            Mathf.Clamp01(defaultVolatility);
        defaultMinimumMultiplier =
            Mathf.Max(0.01f, defaultMinimumMultiplier);
        defaultMaximumMultiplier =
            Mathf.Max(
                defaultMinimumMultiplier,
                defaultMaximumMultiplier
            );

        if (priceOverrides == null)
            return;

        foreach (FishPriceOverride entry in priceOverrides)
        {
            if (entry == null)
                continue;

            entry.minimumMultiplier =
                Mathf.Max(0.01f, entry.minimumMultiplier);
            entry.maximumMultiplier =
                Mathf.Max(
                    entry.minimumMultiplier,
                    entry.maximumMultiplier
                );
        }
    }

    private void OnValidate()
    {
        ClampSettings();
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused)
            FlushMarketPrefs();
    }

    private void OnApplicationQuit()
    {
        FlushMarketPrefs();
    }

    private static double GetUnixTime()
    {
        return DateTimeOffset.UtcNow
            .ToUnixTimeSeconds();
    }

    private void OnDestroy()
    {
        ReleasePlayerLock();
        RestoreCursorState();
        UnsubscribeInventory();
        FlushMarketPrefs();

        if (closeButton != null)
        {
            closeButton.onClick
                .RemoveListener(CloseShop);
        }
    }
}
