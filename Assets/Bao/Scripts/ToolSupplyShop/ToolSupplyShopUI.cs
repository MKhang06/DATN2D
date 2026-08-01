using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[DisallowMultipleComponent]
[DefaultExecutionOrder(-500)]
public class ToolSupplyShopUI : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject shopRoot;

    [Header("Inventory + Money")]
    [SerializeField] private InventoryManager inventoryManager;
    [SerializeField] private MonoBehaviour walletSource;

    [Header("Product List")]
    [SerializeField] private RectTransform content;
    [SerializeField] private ToolShopItemCardUI itemCardPrefab;
    [SerializeField] private ToolShopItemData[] availableItems;

    [Header("Popup")]
    [SerializeField] private ToolShopPurchasePopupUI purchasePopup;

    [Header("UI")]
    [SerializeField] private TMP_Text moneyText;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private TMP_InputField searchInput;
    [SerializeField] private Button closeButton;

    [Header("Settings")]
    [SerializeField] private KeyCode debugOpenKey = KeyCode.F8;
    [SerializeField] private float messageDuration = 1.6f;
    [SerializeField] private int canvasSortingOrder = 32760;
    [SerializeField] private bool showSetupErrorsInShop = true;

    private Coroutine messageRoutine;
    private readonly List<GameObject> spawnedCards =
        new List<GameObject>();

    private Canvas owningCanvas;
    private CanvasGroup shopCanvasGroup;

    public bool IsOpen =>
        shopRoot != null &&
        shopRoot.activeSelf;

    private void Awake()
    {
        ResolveReferences();
        BindUIEvents();
        EnsureEventSystem();
        EnsureUIUsable();

        if (messageText != null)
            messageText.gameObject.SetActive(false);

        if (shopRoot != null)
            shopRoot.SetActive(false);
    }

    private void OnEnable()
    {
        ResolveReferences();
        BindUIEvents();
    }

    private void Update()
    {
        if (WasDebugOpenPressed())
        {
            if (IsOpen)
                CloseShop();
            else
                OpenShop();
        }

        if (IsOpen && WasEscapePressed())
            CloseShop();
    }

    public void OpenShop()
    {
        ResolveReferences();
        EnsureEventSystem();

        if (shopRoot == null)
        {
            Debug.LogError(
                "[ToolSupplyShopUI] Shop Root chưa được gắn.",
                this
            );
            return;
        }

        shopRoot.SetActive(true);
        EnsureUIUsable();

        if (searchInput != null)
            searchInput.SetTextWithoutNotify(string.Empty);

        RefreshShop();
        RefreshMoney();
    }

    public void CloseShop()
    {
        purchasePopup?.Hide();

        if (shopRoot != null)
            shopRoot.SetActive(false);

        HideMessage();
    }

    public void RefreshShop()
    {
        ClearCards();
        EnsureUIUsable();

        List<string> missing = new List<string>();

        if (content == null)
            missing.Add("Content");

        if (itemCardPrefab == null)
            missing.Add("Item Card Prefab");

        if (availableItems == null ||
            availableItems.Length == 0)
        {
            missing.Add("Available Items");
        }

        if (missing.Count > 0)
        {
            string error =
                "Shop thiếu: " +
                string.Join(", ", missing);

            Debug.LogError(
                "[ToolSupplyShopUI] " + error,
                this
            );

            if (showSetupErrorsInShop)
                ShowPersistentMessage(error, false);

            return;
        }

        string keyword =
            searchInput != null
                ? searchInput.text
                    .Trim()
                    .ToLowerInvariant()
                : string.Empty;

        int createdCount = 0;

        foreach (ToolShopItemData item
                 in availableItems)
        {
            if (item == null)
                continue;

            if (item.inventoryItem == null)
            {
                Debug.LogWarning(
                    "[ToolSupplyShopUI] " +
                    item.name +
                    " chưa gắn Inventory Item.",
                    item
                );
                continue;
            }

            string searchable =
                (
                    item.DisplayName + " " +
                    item.category + " " +
                    item.Description + " " +
                    item.ItemId
                ).ToLowerInvariant();

            if (!string.IsNullOrWhiteSpace(keyword) &&
                !searchable.Contains(keyword))
            {
                continue;
            }

            ToolShopItemCardUI card =
                Instantiate(
                    itemCardPrefab,
                    content,
                    false
                );

            if (card == null)
                continue;

            GameObject cardObject =
                card.gameObject;

            cardObject.SetActive(true);
            cardObject.name =
                "ToolCard_" +
                (
                    string.IsNullOrWhiteSpace(item.ItemId)
                        ? item.name
                        : item.ItemId
                );

            RectTransform cardRect =
                card.transform as RectTransform;

            if (cardRect != null)
            {
                cardRect.localScale = Vector3.one;
                cardRect.localRotation =
                    Quaternion.identity;
                cardRect.anchoredPosition =
                    Vector2.zero;
            }

            card.Setup(
                item,
                OpenPurchasePopup
            );

            spawnedCards.Add(cardObject);
            createdCount++;
        }

        Canvas.ForceUpdateCanvases();

        if (content != null)
        {
            LayoutRebuilder
                .ForceRebuildLayoutImmediate(
                    content
                );
        }

        if (createdCount == 0)
        {
            string message =
                string.IsNullOrWhiteSpace(keyword)
                    ? "Không có sản phẩm hợp lệ."
                    : "Không tìm thấy sản phẩm.";

            ShowPersistentMessage(
                message,
                false
            );
        }
        else if (messageText != null)
        {
            messageText.gameObject
                .SetActive(false);
        }

        Debug.Log(
            "[ToolSupplyShopUI] Đã tạo " +
            createdCount +
            " card sản phẩm.",
            this
        );
    }

    private void OpenPurchasePopup(
        ToolShopItemData item)
    {
        if (item == null)
            return;

        if (item.UniqueOwnership &&
            OwnsItem(item))
        {
            ShowMessage(
                "Bạn đã sở hữu " +
                item.DisplayName +
                ".",
                false
            );
            return;
        }

        if (purchasePopup == null)
        {
            ShowMessage(
                "Chưa gắn Purchase Popup.",
                false
            );
            return;
        }

        purchasePopup.Show(
            item,
            ConfirmPurchase
        );
    }

    private void ConfirmPurchase(
        ToolShopItemData item,
        int quantity)
    {
        ResolveReferences();

        if (item == null ||
            item.inventoryItem == null)
        {
            ShowMessage(
                "Dữ liệu sản phẩm không hợp lệ.",
                false
            );
            return;
        }

        if (inventoryManager == null)
        {
            ShowMessage(
                "Không tìm thấy InventoryManager.",
                false
            );
            return;
        }

        quantity = Mathf.Max(1, quantity);

        if (item.UniqueOwnership)
        {
            quantity = 1;

            if (OwnsItem(item))
            {
                ShowMessage(
                    "Bạn đã sở hữu " +
                    item.DisplayName +
                    ".",
                    false
                );
                return;
            }
        }
        else
        {
            quantity = Mathf.Min(
                quantity,
                Mathf.Max(
                    1,
                    item.maxPurchaseQuantity
                )
            );
        }

        int totalPrice =
            Mathf.Max(0, item.Price) *
            quantity;

        if (!TrySpendMoney(
                totalPrice,
                out string moneyReason))
        {
            ShowMessage(
                string.IsNullOrWhiteSpace(moneyReason)
                    ? "Bạn không đủ tiền."
                    : moneyReason,
                false
            );
            return;
        }

        bool added =
            inventoryManager.TryAddItem(
                item.inventoryItem,
                quantity,
                out string addReason
            );

        if (!added)
        {
            RefundMoney(totalPrice);

            ShowMessage(
                string.IsNullOrWhiteSpace(addReason)
                    ? "Túi đồ không đủ chỗ. Đã hoàn tiền."
                    : addReason + " Đã hoàn tiền.",
                false
            );
            return;
        }

        purchasePopup?.Hide();
        RefreshMoney();

        ShowMessage(
            "Đã mua " +
            item.DisplayName +
            " x" +
            quantity +
            ".",
            true
        );
    }

    private void BindUIEvents()
    {
        if (closeButton != null)
        {
            closeButton.onClick
                .RemoveListener(CloseShop);

            closeButton.onClick
                .AddListener(CloseShop);
        }

        if (searchInput != null)
        {
            searchInput.onValueChanged
                .RemoveListener(
                    HandleSearchChanged
                );

            searchInput.onValueChanged
                .AddListener(
                    HandleSearchChanged
                );
        }
    }

    private void HandleSearchChanged(
        string value)
    {
        if (IsOpen)
            RefreshShop();
    }

    private void EnsureUIUsable()
    {
        if (owningCanvas == null)
        {
            owningCanvas =
                GetComponent<Canvas>();

            if (owningCanvas == null)
            {
                owningCanvas =
                    GetComponentInParent<
                        Canvas
                    >(true);
            }
        }

        if (owningCanvas != null)
        {
            owningCanvas.enabled = true;
            owningCanvas.renderMode =
                RenderMode.ScreenSpaceOverlay;
            owningCanvas.overrideSorting = true;
            owningCanvas.sortingOrder =
                canvasSortingOrder;

            GraphicRaycaster raycaster =
                owningCanvas.GetComponent<
                    GraphicRaycaster
                >();

            if (raycaster == null)
            {
                raycaster =
                    owningCanvas.gameObject
                        .AddComponent<
                            GraphicRaycaster
                        >();
            }

            raycaster.enabled = true;
        }

        if (shopRoot == null)
            return;

        shopRoot.transform.SetAsLastSibling();

        RectTransform rootRect =
            shopRoot.transform
                as RectTransform;

        if (rootRect != null)
        {
            rootRect.anchorMin =
                Vector2.zero;
            rootRect.anchorMax =
                Vector2.one;
            rootRect.pivot =
                new Vector2(0.5f, 0.5f);
            rootRect.offsetMin =
                Vector2.zero;
            rootRect.offsetMax =
                Vector2.zero;
            rootRect.localScale =
                Vector3.one;
            rootRect.localRotation =
                Quaternion.identity;
        }

        if (shopCanvasGroup == null)
        {
            shopCanvasGroup =
                shopRoot.GetComponent<
                    CanvasGroup
                >();

            if (shopCanvasGroup == null)
            {
                shopCanvasGroup =
                    shopRoot.AddComponent<
                        CanvasGroup
                    >();
            }
        }

        shopCanvasGroup.alpha = 1f;
        shopCanvasGroup.interactable = true;
        shopCanvasGroup.blocksRaycasts = true;

        CanvasGroup[] parentGroups =
            shopRoot.GetComponentsInParent<
                CanvasGroup
            >(true);

        foreach (CanvasGroup group
                 in parentGroups)
        {
            if (group == null)
                continue;

            group.alpha = 1f;
            group.interactable = true;
            group.blocksRaycasts = true;
        }

        Cursor.visible = true;
        Cursor.lockState =
            CursorLockMode.None;
    }

    private static void EnsureEventSystem()
    {
        EventSystem eventSystem =
            EventSystem.current;

        if (eventSystem == null)
        {
            eventSystem =
                FindFirstObjectByType<
                    EventSystem
                >(
                    FindObjectsInactive.Include
                );
        }

        if (eventSystem == null)
        {
            GameObject eventObject =
                new GameObject(
                    "EventSystem",
                    typeof(EventSystem)
                );

            eventSystem =
                eventObject.GetComponent<
                    EventSystem
                >();
        }

        eventSystem.gameObject
            .SetActive(true);

        BaseInputModule[] modules =
            eventSystem.GetComponents<
                BaseInputModule
            >();

        bool hasEnabledModule = false;

        foreach (BaseInputModule module
                 in modules)
        {
            if (module != null &&
                module.enabled)
            {
                hasEnabledModule = true;
                break;
            }
        }

        if (hasEnabledModule)
            return;

        Type inputSystemModule =
            Type.GetType(
                "UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem"
            );

        if (inputSystemModule != null &&
            typeof(BaseInputModule)
                .IsAssignableFrom(
                    inputSystemModule
                ))
        {
            Component module =
                eventSystem.gameObject
                    .GetComponent(
                        inputSystemModule
                    );

            if (module == null)
            {
                module =
                    eventSystem.gameObject
                        .AddComponent(
                            inputSystemModule
                        );
            }

            if (module is Behaviour behaviour)
                behaviour.enabled = true;

            return;
        }

        StandaloneInputModule standalone =
            eventSystem.GetComponent<
                StandaloneInputModule
            >();

        if (standalone == null)
        {
            standalone =
                eventSystem.gameObject
                    .AddComponent<
                        StandaloneInputModule
                    >();
        }

        standalone.enabled = true;
    }

    private bool OwnsItem(
        ToolShopItemData item)
    {
        if (inventoryManager == null ||
            item == null)
        {
            return false;
        }

        string targetId =
            Normalize(item.ItemId);

        string targetName =
            Normalize(item.DisplayName);

        object manager =
            inventoryManager;

        Type type =
            manager.GetType();

        string[] memberNames =
        {
            "HotbarSlots",
            "hotbarSlots",
            "BagSlots",
            "bagSlots",
            "Slots",
            "slots"
        };

        foreach (string memberName
                 in memberNames)
        {
            object collection =
                GetMemberValue(
                    manager,
                    type,
                    memberName
                );

            if (!(collection is
                    System.Collections.IEnumerable
                    enumerable))
            {
                continue;
            }

            foreach (object slot
                     in enumerable)
            {
                if (slot == null)
                    continue;

                string slotName =
                    ReadStringMember(
                        slot,
                        "itemName",
                        "displayName",
                        "ItemName"
                    );

                string slotId =
                    ReadStringMember(
                        slot,
                        "itemId",
                        "ItemId",
                        "id"
                    );

                if ((!string.IsNullOrWhiteSpace(
                        targetId) &&
                     Normalize(slotId) ==
                        targetId) ||
                    (!string.IsNullOrWhiteSpace(
                        targetName) &&
                     Normalize(slotName) ==
                        targetName))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private bool TrySpendMoney(
        int amount,
        out string reason)
    {
        reason = string.Empty;

        if (amount <= 0)
            return true;

        if (walletSource == null)
        {
            reason =
                "Chưa gắn Wallet Source/PlayerStats.";
            return false;
        }

        bool hasMoneyValue =
            TryGetMoney(
                out int currentMoney
            );

        if (hasMoneyValue &&
            currentMoney < amount)
        {
            reason =
                "Bạn không đủ tiền.";
            return false;
        }

        string[] methodNames =
        {
            "TrySpendMoney",
            "SpendMoney",
            "RemoveMoney",
            "UseMoney",
            "Pay"
        };

        foreach (string methodName
                 in methodNames)
        {
            MethodInfo method =
                FindNumberMethod(
                    walletSource.GetType(),
                    methodName
                );

            if (method == null)
                continue;

            object argument =
                ConvertNumber(
                    amount,
                    method.GetParameters()[0]
                        .ParameterType
                );

            object result =
                method.Invoke(
                    walletSource,
                    new[] { argument }
                );

            if (method.ReturnType ==
                typeof(bool))
            {
                bool success =
                    result is bool value &&
                    value;

                if (!success)
                    reason =
                        "Bạn không đủ tiền.";

                return success;
            }

            return true;
        }

        if (hasMoneyValue &&
            TrySetMoney(
                currentMoney - amount
            ))
        {
            return true;
        }

        reason =
            "Không tìm thấy hàm trừ tiền.";
        return false;
    }

    private void RefundMoney(int amount)
    {
        if (amount <= 0 ||
            walletSource == null)
        {
            return;
        }

        string[] methodNames =
        {
            "AddMoney",
            "GiveMoney",
            "EarnMoney",
            "ReceiveMoney"
        };

        foreach (string methodName
                 in methodNames)
        {
            MethodInfo method =
                FindNumberMethod(
                    walletSource.GetType(),
                    methodName
                );

            if (method == null)
                continue;

            object argument =
                ConvertNumber(
                    amount,
                    method.GetParameters()[0]
                        .ParameterType
                );

            method.Invoke(
                walletSource,
                new[] { argument }
            );
            return;
        }

        if (TryGetMoney(out int money))
            TrySetMoney(money + amount);
    }

    private bool TryGetMoney(out int money)
    {
        money = 0;

        if (walletSource == null)
            return false;

        string[] names =
        {
            "Money",
            "money",
            "CurrentMoney",
            "currentMoney",
            "Cash",
            "cash"
        };

        foreach (string name in names)
        {
            object value =
                GetMemberValue(
                    walletSource,
                    walletSource.GetType(),
                    name
                );

            if (value == null)
                continue;

            try
            {
                money =
                    Convert.ToInt32(value);
                return true;
            }
            catch
            {
                // Skip incompatible member.
            }
        }

        return false;
    }

    private bool TrySetMoney(int value)
    {
        if (walletSource == null)
            return false;

        Type type =
            walletSource.GetType();

        string[] names =
        {
            "Money",
            "money",
            "CurrentMoney",
            "currentMoney",
            "Cash",
            "cash"
        };

        const BindingFlags flags =
            BindingFlags.Instance |
            BindingFlags.Public |
            BindingFlags.NonPublic |
            BindingFlags.IgnoreCase;

        foreach (string name in names)
        {
            FieldInfo field =
                type.GetField(name, flags);

            if (field != null)
            {
                field.SetValue(
                    walletSource,
                    ConvertNumber(
                        value,
                        field.FieldType
                    )
                );
                return true;
            }

            PropertyInfo property =
                type.GetProperty(name, flags);

            if (property != null &&
                property.CanWrite)
            {
                property.SetValue(
                    walletSource,
                    ConvertNumber(
                        value,
                        property.PropertyType
                    )
                );
                return true;
            }
        }

        return false;
    }

    private void RefreshMoney()
    {
        if (moneyText == null)
            return;

        int money =
            TryGetMoney(out int current)
                ? current
                : 0;

        moneyText.text =
            "TIỀN MẶT\n<b>$" +
            money.ToString("N0") +
            "</b>";
    }

    private void ResolveReferences()
    {
        if (inventoryManager == null)
            inventoryManager =
                InventoryManager.Instance;

        if (inventoryManager == null)
        {
            inventoryManager =
                FindFirstObjectByType<
                    InventoryManager
                >(
                    FindObjectsInactive.Include
                );
        }

        if (walletSource == null)
        {
            MonoBehaviour[] behaviours =
                FindObjectsByType<
                    MonoBehaviour
                >(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None
                );

            foreach (MonoBehaviour behaviour
                     in behaviours)
            {
                if (behaviour != null &&
                    string.Equals(
                        behaviour.GetType().Name,
                        "PlayerStats",
                        StringComparison.OrdinalIgnoreCase
                    ))
                {
                    walletSource = behaviour;
                    break;
                }
            }
        }

        if (shopRoot == null)
        {
            Transform found =
                FindTransformByName(
                    transform,
                    "ShopRoot"
                );

            if (found != null)
                shopRoot =
                    found.gameObject;
        }

        if (content == null)
        {
            content =
                FindTransformByName(
                    transform,
                    "Content"
                ) as RectTransform;
        }

        if (searchInput == null)
        {
            searchInput =
                FindComponentByName<
                    TMP_InputField
                >(
                    transform,
                    "SearchInput"
                );
        }

        if (moneyText == null)
        {
            moneyText =
                FindComponentByName<
                    TMP_Text
                >(
                    transform,
                    "MoneyText"
                );
        }

        if (messageText == null)
        {
            messageText =
                FindComponentByName<
                    TMP_Text
                >(
                    transform,
                    "MessageText"
                );
        }

        if (closeButton == null)
        {
            closeButton =
                FindComponentByName<
                    Button
                >(
                    transform,
                    "CloseButton"
                );
        }

        if (purchasePopup == null)
        {
            purchasePopup =
                GetComponent<
                    ToolShopPurchasePopupUI
                >();

            if (purchasePopup == null)
            {
                purchasePopup =
                    GetComponentInChildren<
                        ToolShopPurchasePopupUI
                    >(true);
            }
        }
    }

    private void ClearCards()
    {
        foreach (GameObject card
                 in spawnedCards)
        {
            if (card != null)
                Destroy(card);
        }

        spawnedCards.Clear();

        if (content == null)
            return;

        ToolShopItemCardUI[] staleCards =
            content.GetComponentsInChildren<
                ToolShopItemCardUI
            >(true);

        foreach (ToolShopItemCardUI stale
                 in staleCards)
        {
            if (stale != null)
                Destroy(stale.gameObject);
        }
    }

    private void ShowMessage(
        string message,
        bool success)
    {
        if (messageText == null)
        {
            if (success)
                Debug.Log(message, this);
            else
                Debug.LogWarning(message, this);
            return;
        }

        if (messageRoutine != null)
            StopCoroutine(messageRoutine);

        messageRoutine =
            StartCoroutine(
                MessageRoutine(
                    message,
                    success
                )
            );
    }

    private void ShowPersistentMessage(
        string message,
        bool success)
    {
        if (messageRoutine != null)
        {
            StopCoroutine(messageRoutine);
            messageRoutine = null;
        }

        if (messageText == null)
        {
            Debug.LogError(message, this);
            return;
        }

        messageText.gameObject
            .SetActive(true);
        messageText.text = message;
        messageText.color =
            success
                ? new Color(
                    0.25f,
                    1f,
                    0.62f,
                    1f
                )
                : new Color(
                    1f,
                    0.32f,
                    0.32f,
                    1f
                );
    }

    private IEnumerator MessageRoutine(
        string message,
        bool success)
    {
        ShowPersistentMessage(
            message,
            success
        );

        yield return
            new WaitForSecondsRealtime(
                messageDuration
            );

        HideMessage();
    }

    private void HideMessage()
    {
        if (messageRoutine != null)
        {
            StopCoroutine(messageRoutine);
            messageRoutine = null;
        }

        if (messageText != null)
            messageText.gameObject
                .SetActive(false);
    }

    private static MethodInfo FindNumberMethod(
        Type type,
        string name)
    {
        const BindingFlags flags =
            BindingFlags.Instance |
            BindingFlags.Public |
            BindingFlags.NonPublic |
            BindingFlags.IgnoreCase;

        foreach (MethodInfo method
                 in type.GetMethods(flags))
        {
            if (!string.Equals(
                    method.Name,
                    name,
                    StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            ParameterInfo[] parameters =
                method.GetParameters();

            if (parameters.Length != 1)
                continue;

            Type parameterType =
                parameters[0].ParameterType;

            if (parameterType ==
                    typeof(int) ||
                parameterType ==
                    typeof(float) ||
                parameterType ==
                    typeof(double) ||
                parameterType ==
                    typeof(long))
            {
                return method;
            }
        }

        return null;
    }

    private static object ConvertNumber(
        int value,
        Type targetType)
    {
        if (targetType == typeof(float))
            return (float)value;

        if (targetType == typeof(double))
            return (double)value;

        if (targetType == typeof(long))
            return (long)value;

        return value;
    }

    private static object GetMemberValue(
        object source,
        Type type,
        string name)
    {
        const BindingFlags flags =
            BindingFlags.Instance |
            BindingFlags.Public |
            BindingFlags.NonPublic |
            BindingFlags.IgnoreCase;

        FieldInfo field =
            type.GetField(name, flags);

        if (field != null)
            return field.GetValue(source);

        PropertyInfo property =
            type.GetProperty(name, flags);

        if (property != null &&
            property.CanRead)
        {
            return property.GetValue(source);
        }

        return null;
    }

    private static string ReadStringMember(
        object source,
        params string[] names)
    {
        Type type = source.GetType();

        foreach (string name in names)
        {
            object value =
                GetMemberValue(
                    source,
                    type,
                    name
                );

            if (value != null)
                return value.ToString();
        }

        return string.Empty;
    }

    private static Transform FindTransformByName(
        Transform root,
        string objectName)
    {
        if (root == null)
            return null;

        Transform[] children =
            root.GetComponentsInChildren<
                Transform
            >(true);

        foreach (Transform child in children)
        {
            if (child != null &&
                string.Equals(
                    child.name,
                    objectName,
                    StringComparison.OrdinalIgnoreCase
                ))
            {
                return child;
            }
        }

        return null;
    }

    private static T FindComponentByName<T>(
        Transform root,
        string objectName)
        where T : Component
    {
        if (root == null)
            return null;

        T[] components =
            root.GetComponentsInChildren<T>(
                true
            );

        foreach (T component in components)
        {
            if (component != null &&
                string.Equals(
                    component.gameObject.name,
                    objectName,
                    StringComparison.OrdinalIgnoreCase
                ))
            {
                return component;
            }
        }

        return null;
    }

    private static string Normalize(
        string value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Trim()
                .ToLowerInvariant()
                .Replace(" ", string.Empty)
                .Replace("_", string.Empty)
                .Replace("-", string.Empty);
    }

    private bool WasDebugOpenPressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current == null)
            return false;

        return debugOpenKey == KeyCode.F8 &&
               Keyboard.current.f8Key
                   .wasPressedThisFrame;
#else
        return Input.GetKeyDown(debugOpenKey);
#endif
    }

    private bool WasEscapePressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null &&
               Keyboard.current.escapeKey
                   .wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Escape);
#endif
    }
}
