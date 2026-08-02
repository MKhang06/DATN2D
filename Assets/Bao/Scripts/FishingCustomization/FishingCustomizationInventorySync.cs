using System;
using System.Reflection;
using UnityEngine;

[DefaultExecutionOrder(250)]
[DisallowMultipleComponent]
public class FishingCustomizationInventorySync : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private FishingRodLoadout loadout;

    [SerializeField]
    private InventoryManager inventoryManager;

    [Header("Behavior")]
    [SerializeField]
    private bool forceSameInventoryManager = true;

    [SerializeField]
    private bool refreshCustomizationUIOnInventoryChange = true;

    [SerializeField]
    private bool showDebugLogs = true;

    private const BindingFlags MemberFlags =
        BindingFlags.Instance |
        BindingFlags.Public |
        BindingFlags.NonPublic;

    private void Awake()
    {
        ResolveReferences();
        ApplyInventoryReference();
    }

    private void OnEnable()
    {
        ResolveReferences();
        ApplyInventoryReference();
        BindInventory();
    }

    private void Start()
    {
        ResolveReferences();
        ApplyInventoryReference();
        RefreshEverything();
        PrintOwnershipDiagnostic();
    }

    private void OnDisable()
    {
        UnbindInventory();
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

        if (loadout == null)
        {
            loadout =
                GetComponent<
                    FishingRodLoadout
                >();
        }

        if (loadout == null)
        {
            loadout =
                FindFirstObjectByType<
                    FishingRodLoadout
                >();
        }
    }

    private void ApplyInventoryReference()
    {
        if (!forceSameInventoryManager ||
            loadout == null ||
            inventoryManager == null)
        {
            return;
        }

        FieldInfo field =
            typeof(FishingRodLoadout)
                .GetField(
                    "inventoryManager",
                    MemberFlags
                );

        if (field == null)
        {
            if (showDebugLogs)
            {
                Debug.LogWarning(
                    "[Fishing Customization Sync] " +
                    "Không tìm thấy field inventoryManager " +
                    "trong FishingRodLoadout.",
                    this
                );
            }

            return;
        }

        object current =
            field.GetValue(loadout);

        if (ReferenceEquals(
                current,
                inventoryManager))
        {
            return;
        }

        field.SetValue(
            loadout,
            inventoryManager
        );

        if (showDebugLogs)
        {
            Debug.Log(
                "[Fishing Customization Sync] " +
                "Đã buộc Shop và FishingRodLoadout " +
                "dùng cùng InventoryManager.",
                this
            );
        }
    }

    private void BindInventory()
    {
        if (inventoryManager == null)
            return;

        inventoryManager
            .OnInventoryChanged -=
                HandleInventoryChanged;

        inventoryManager
            .OnInventoryChanged +=
                HandleInventoryChanged;
    }

    private void UnbindInventory()
    {
        if (inventoryManager == null)
            return;

        inventoryManager
            .OnInventoryChanged -=
                HandleInventoryChanged;
    }

    private void HandleInventoryChanged()
    {
        ResolveReferences();
        ApplyInventoryReference();
        RefreshEverything();

        if (showDebugLogs)
            PrintOwnershipDiagnostic();
    }

    private void RefreshEverything()
    {
        if (loadout != null)
            loadout.RefreshNow();

        if (!refreshCustomizationUIOnInventoryChange)
            return;

        MonoBehaviour[] behaviours =
            Resources.FindObjectsOfTypeAll<
                MonoBehaviour
            >();

        foreach (MonoBehaviour behaviour
                 in behaviours)
        {
            if (behaviour == null ||
                !behaviour.gameObject
                    .scene.IsValid())
            {
                continue;
            }

            Type type =
                behaviour.GetType();

            string typeName =
                type.Name.ToLowerInvariant();

            if (!typeName.Contains("fishing") &&
                !typeName.Contains("rod") &&
                !typeName.Contains("custom"))
            {
                continue;
            }

            TryInvokeNoArgumentMethod(
                behaviour,
                "RefreshAll"
            );

            TryInvokeNoArgumentMethod(
                behaviour,
                "RefreshUI"
            );

            TryInvokeNoArgumentMethod(
                behaviour,
                "RefreshNow"
            );

            /*
             * Chỉ rebuild khi panel lựa chọn đang Active.
             */
            if (behaviour.gameObject
                    .activeInHierarchy)
            {
                TryInvokeNoArgumentMethod(
                    behaviour,
                    "RebuildSelector"
                );

                TryInvokeNoArgumentMethod(
                    behaviour,
                    "RefreshOptions"
                );

                TryInvokeNoArgumentMethod(
                    behaviour,
                    "RebuildOptions"
                );
            }
        }
    }

    private static void TryInvokeNoArgumentMethod(
        object target,
        string methodName)
    {
        if (target == null)
            return;

        MethodInfo method =
            target.GetType()
                .GetMethod(
                    methodName,
                    MemberFlags,
                    null,
                    Type.EmptyTypes,
                    null
                );

        if (method == null)
            return;

        /*
         * Tránh gọi lại chính component sync.
         */
        if (target is
                FishingCustomizationInventorySync)
        {
            return;
        }

        try
        {
            method.Invoke(
                target,
                null
            );
        }
        catch (TargetInvocationException exception)
        {
            Exception inner =
                exception.InnerException ??
                exception;

            Debug.LogWarning(
                "[Fishing Customization Sync] " +
                "Không gọi được " +
                target.GetType().Name +
                "." +
                methodName +
                "(): " +
                inner.Message
            );
        }
        catch (Exception exception)
        {
            Debug.LogWarning(
                "[Fishing Customization Sync] " +
                "Không gọi được " +
                target.GetType().Name +
                "." +
                methodName +
                "(): " +
                exception.Message
            );
        }
    }

    [ContextMenu(
        "TEST - Refresh Fishing Customization"
    )]
    public void TestRefresh()
    {
        ResolveReferences();
        ApplyInventoryReference();
        RefreshEverything();
        PrintOwnershipDiagnostic();
    }

    [ContextMenu(
        "TEST - Print Ownership Diagnostic"
    )]
    public void PrintOwnershipDiagnostic()
    {
        ResolveReferences();

        if (!showDebugLogs)
            return;

        if (inventoryManager == null)
        {
            Debug.LogError(
                "[Fishing Customization Sync] " +
                "Không tìm thấy InventoryManager.",
                this
            );

            return;
        }

        if (loadout == null)
        {
            Debug.LogError(
                "[Fishing Customization Sync] " +
                "Không tìm thấy FishingRodLoadout.",
                this
            );

            return;
        }

        int definitionCount =
            loadout.PartDefinitions != null
                ? loadout.PartDefinitions.Count
                : 0;

        Debug.Log(
            "[Fishing Customization Sync] " +
            "Part Definitions = " +
            definitionCount +
            " | InventoryManager = " +
            inventoryManager.name,
            this
        );

        if (definitionCount <= 0)
        {
            Debug.LogError(
                "[Fishing Customization Sync] " +
                "Part Definitions đang trống. " +
                "Hãy chạy Tools > Fishing > " +
                "Customization Ownership Fix > " +
                "Repair All Links.",
                this
            );

            return;
        }

        foreach (
            FishingRodPartDefinition definition
            in loadout.PartDefinitions)
        {
            if (definition == null)
                continue;

            InventoryItemData item =
                definition.InventoryItem;

            int amount =
                item != null
                    ? inventoryManager
                        .GetItemAmount(item)
                    : 0;

            Debug.Log(
                "[Fishing Customization Sync] " +
                definition.SlotType +
                " | " +
                definition.ItemName +
                " | ItemId=" +
                definition.ItemId +
                " | Owned=" +
                amount +
                " | InventoryItem=" +
                (item != null
                    ? item.name
                    : "NULL"),
                definition
            );
        }
    }
}
