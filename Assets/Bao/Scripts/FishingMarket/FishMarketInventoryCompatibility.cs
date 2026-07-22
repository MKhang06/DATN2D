using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

/// <summary>
/// Lớp tương thích cho các phiên bản InventoryManager cũ chưa có
/// GetItemAmount và RemoveItem.
///
/// Không cần sửa InventoryManager hiện tại. Khi InventoryManager đã có
/// các hàm cùng tên, C# sẽ tự ưu tiên hàm của InventoryManager.
/// </summary>
public static class FishMarketInventoryCompatibility
{
    private sealed class SlotHandle
    {
        public object container;
        public int index;
        public object slot;
        public InventoryItemData item;
        public string itemName;
        public int amount;
    }

    private static readonly BindingFlags MemberFlags =
        BindingFlags.Instance |
        BindingFlags.Public |
        BindingFlags.NonPublic;

    public static int GetItemAmount(
        this InventoryManager manager,
        InventoryItemData item)
    {
        if (manager == null || item == null)
            return 0;

        /*
         * Trường hợp InventoryManager thật sự có hàm nhưng nằm ở
         * assembly/phiên bản khác, ưu tiên gọi nó bằng reflection.
         */
        MethodInfo nativeMethod =
            FindMethod(
                manager.GetType(),
                "GetItemAmount",
                typeof(InventoryItemData)
            );

        if (nativeMethod != null &&
            nativeMethod.DeclaringType !=
                typeof(FishMarketInventoryCompatibility))
        {
            try
            {
                object result =
                    nativeMethod.Invoke(
                        manager,
                        new object[] { item }
                    );

                if (result is int nativeAmount)
                    return Mathf.Max(0, nativeAmount);
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    "[FishMarket] Không gọi được " +
                    "InventoryManager.GetItemAmount: " +
                    GetExceptionMessage(exception)
                );
            }
        }

        List<SlotHandle> slots =
            ReadInventorySlots(manager);

        int total = 0;

        foreach (SlotHandle handle in slots)
        {
            if (handle == null ||
                handle.amount <= 0 ||
                !MatchesItem(
                    handle,
                    item))
            {
                continue;
            }

            total += handle.amount;
        }

        return Mathf.Max(0, total);
    }

    public static bool RemoveItem(
        this InventoryManager manager,
        InventoryItemData item,
        int amount)
    {
        return RemoveItem(
            manager,
            item,
            amount,
            out _
        );
    }

    public static bool RemoveItem(
        this InventoryManager manager,
        InventoryItemData item,
        int amount,
        out string reason)
    {
        reason = string.Empty;

        if (manager == null)
        {
            reason = "Không tìm thấy túi đồ.";
            return false;
        }

        if (item == null)
        {
            reason = "Vật phẩm không hợp lệ.";
            return false;
        }

        if (amount <= 0)
        {
            reason = "Số lượng cần bán phải lớn hơn 0.";
            return false;
        }

        /*
         * Ưu tiên các API RemoveItem/TakeItem/ConsumeItem có sẵn.
         */
        if (TryInvokeNativeRemove(
                manager,
                item,
                amount,
                out bool nativeResult,
                out string nativeReason))
        {
            reason = nativeReason;
            return nativeResult;
        }

        List<SlotHandle> slots =
            ReadInventorySlots(manager);

        int available = 0;

        foreach (SlotHandle handle in slots)
        {
            if (handle != null &&
                handle.amount > 0 &&
                MatchesItem(handle, item))
            {
                available += handle.amount;
            }
        }

        if (available < amount)
        {
            reason =
                "Không đủ " +
                GetSafeItemName(item) +
                " trong túi.";

            return false;
        }

        int remaining = amount;
        bool changed = false;

        /*
         * Xóa từ cuối về đầu để hạn chế thay đổi index nếu container
         * là List.
         */
        for (int i = slots.Count - 1;
             i >= 0 && remaining > 0;
             i--)
        {
            SlotHandle handle = slots[i];

            if (handle == null ||
                handle.amount <= 0 ||
                !MatchesItem(handle, item))
            {
                continue;
            }

            int removeAmount =
                Mathf.Min(
                    remaining,
                    handle.amount
                );

            int newAmount =
                handle.amount - removeAmount;

            bool slotChanged;

            if (newAmount > 0)
            {
                slotChanged =
                    TrySetSlotAmount(
                        handle,
                        newAmount
                    );
            }
            else
            {
                slotChanged =
                    TryClearSlot(handle);
            }

            if (!slotChanged)
            {
                reason =
                    "InventoryManager hiện tại không cho phép " +
                    "xóa vật phẩm khỏi slot. Hãy gửi file " +
                    "InventoryManager.cs đang dùng để nối API chính xác.";

                Debug.LogError(
                    "[FishMarket] Không thể sửa slot index " +
                    handle.index +
                    ". Slot type: " +
                    (handle.slot != null
                        ? handle.slot.GetType().FullName
                        : "null")
                );

                return false;
            }

            remaining -= removeAmount;
            changed = true;
        }

        if (!changed || remaining > 0)
        {
            reason =
                "Không thể hoàn tất việc lấy cá khỏi túi.";

            return false;
        }

        NotifyInventoryChanged(manager);

        reason = string.Empty;
        return true;
    }

    private static bool TryInvokeNativeRemove(
        InventoryManager manager,
        InventoryItemData item,
        int amount,
        out bool result,
        out string reason)
    {
        result = false;
        reason = string.Empty;

        Type managerType = manager.GetType();

        string[] methodNames =
        {
            "RemoveItem",
            "TakeItem",
            "ConsumeItem",
            "DeleteItem"
        };

        foreach (string methodName in methodNames)
        {
            MethodInfo[] methods =
                managerType.GetMethods(MemberFlags);

            foreach (MethodInfo method in methods)
            {
                if (method.Name != methodName)
                    continue;

                ParameterInfo[] parameters =
                    method.GetParameters();

                /*
                 * bool RemoveItem(InventoryItemData, int, out string)
                 */
                if (parameters.Length == 3 &&
                    parameters[0].ParameterType
                        .IsAssignableFrom(
                            typeof(InventoryItemData)) &&
                    parameters[1].ParameterType ==
                        typeof(int) &&
                    parameters[2].ParameterType ==
                        typeof(string).MakeByRefType())
                {
                    object[] arguments =
                    {
                        item,
                        amount,
                        string.Empty
                    };

                    try
                    {
                        object invocationResult =
                            method.Invoke(
                                manager,
                                arguments
                            );

                        result =
                            invocationResult is bool boolResult &&
                            boolResult;

                        reason =
                            arguments[2] as string ??
                            string.Empty;

                        return true;
                    }
                    catch (Exception exception)
                    {
                        Debug.LogWarning(
                            "[FishMarket] Lỗi gọi " +
                            methodName +
                            ": " +
                            GetExceptionMessage(exception)
                        );
                    }
                }

                /*
                 * bool RemoveItem(InventoryItemData, int)
                 */
                if (parameters.Length == 2 &&
                    parameters[0].ParameterType
                        .IsAssignableFrom(
                            typeof(InventoryItemData)) &&
                    parameters[1].ParameterType ==
                        typeof(int))
                {
                    try
                    {
                        object invocationResult =
                            method.Invoke(
                                manager,
                                new object[]
                                {
                                    item,
                                    amount
                                }
                            );

                        result =
                            invocationResult is bool boolResult &&
                            boolResult;

                        reason =
                            result
                                ? string.Empty
                                : "InventoryManager từ chối xóa vật phẩm.";

                        return true;
                    }
                    catch (Exception exception)
                    {
                        Debug.LogWarning(
                            "[FishMarket] Lỗi gọi " +
                            methodName +
                            ": " +
                            GetExceptionMessage(exception)
                        );
                    }
                }

                /*
                 * bool RemoveItem(string, int)
                 */
                if (parameters.Length == 2 &&
                    parameters[0].ParameterType ==
                        typeof(string) &&
                    parameters[1].ParameterType ==
                        typeof(int))
                {
                    try
                    {
                        object invocationResult =
                            method.Invoke(
                                manager,
                                new object[]
                                {
                                    GetSafeItemName(item),
                                    amount
                                }
                            );

                        result =
                            invocationResult is bool boolResult &&
                            boolResult;

                        reason =
                            result
                                ? string.Empty
                                : "InventoryManager từ chối xóa vật phẩm.";

                        return true;
                    }
                    catch (Exception exception)
                    {
                        Debug.LogWarning(
                            "[FishMarket] Lỗi gọi " +
                            methodName +
                            ": " +
                            GetExceptionMessage(exception)
                        );
                    }
                }
            }
        }

        return false;
    }

    private static List<SlotHandle>
        ReadInventorySlots(
            InventoryManager manager)
    {
        List<SlotHandle> result =
            new List<SlotHandle>();

        if (manager == null)
            return result;

        /*
         * Ưu tiên field chứa dữ liệu thật để có thể chỉnh sửa/xóa.
         */
        string[] mainContainerNames =
        {
            "slots",
            "inventorySlots",
            "allSlots",
            "items",
            "inventory"
        };

        foreach (string name
                 in mainContainerNames)
        {
            object container =
                GetMemberValue(
                    manager,
                    name
                );

            if (!IsSlotContainer(container))
                continue;

            AddSlotsFromContainer(
                container,
                result
            );

            if (result.Count > 0)
                return result;
        }

        /*
         * Một số bản InventoryManager chia Hotbar và Bag thành hai mảng.
         */
        string[] splitContainerNames =
        {
            "HotbarSlots",
            "hotbarSlots",
            "BagSlots",
            "bagSlots",
            "InventorySlots",
            "BackpackSlots",
            "backpackSlots"
        };

        HashSet<object> visitedContainers =
            new HashSet<object>();

        foreach (string name
                 in splitContainerNames)
        {
            object container =
                GetMemberValue(
                    manager,
                    name
                );

            if (!IsSlotContainer(container) ||
                !visitedContainers.Add(container))
            {
                continue;
            }

            AddSlotsFromContainer(
                container,
                result
            );
        }

        return result;
    }

    private static bool IsSlotContainer(
        object value)
    {
        if (value == null ||
            value is string)
        {
            return false;
        }

        return value is IList ||
               value.GetType().IsArray ||
               value is IEnumerable;
    }

    private static void AddSlotsFromContainer(
        object container,
        List<SlotHandle> result)
    {
        if (container == null)
            return;

        if (container is IList list)
        {
            for (int i = 0;
                 i < list.Count;
                 i++)
            {
                object slot = list[i];

                result.Add(
                    CreateSlotHandle(
                        container,
                        i,
                        slot
                    )
                );
            }

            return;
        }

        if (container is Array array)
        {
            for (int i = 0;
                 i < array.Length;
                 i++)
            {
                object slot =
                    array.GetValue(i);

                result.Add(
                    CreateSlotHandle(
                        container,
                        i,
                        slot
                    )
                );
            }

            return;
        }

        if (container is IEnumerable enumerable)
        {
            int index = 0;

            foreach (object slot in enumerable)
            {
                result.Add(
                    CreateSlotHandle(
                        container,
                        index,
                        slot
                    )
                );

                index++;
            }
        }
    }

    private static SlotHandle CreateSlotHandle(
        object container,
        int index,
        object slot)
    {
        SlotHandle handle =
            new SlotHandle
            {
                container = container,
                index = index,
                slot = slot,
                amount = 0
            };

        if (slot == null)
            return handle;

        object itemValue =
            GetFirstMemberValue(
                slot,
                "item",
                "Item",
                "itemData",
                "ItemData",
                "inventoryItem",
                "InventoryItem",
                "data",
                "Data"
            );

        handle.item =
            itemValue as InventoryItemData;

        object nameValue =
            GetFirstMemberValue(
                slot,
                "itemName",
                "ItemName",
                "displayName",
                "DisplayName",
                "name",
                "Name"
            );

        handle.itemName =
            nameValue as string;

        object amountValue =
            GetFirstMemberValue(
                slot,
                "amount",
                "Amount",
                "quantity",
                "Quantity",
                "count",
                "Count",
                "stack",
                "Stack"
            );

        handle.amount =
            ConvertToInt(
                amountValue,
                handle.item != null ||
                !string.IsNullOrWhiteSpace(
                    handle.itemName)
                    ? 1
                    : 0
            );

        object emptyValue =
            GetFirstMemberValue(
                slot,
                "IsEmpty",
                "isEmpty",
                "Empty",
                "empty"
            );

        if (emptyValue is bool isEmpty &&
            isEmpty)
        {
            handle.amount = 0;
        }

        return handle;
    }

    private static bool MatchesItem(
        SlotHandle handle,
        InventoryItemData target)
    {
        if (handle == null ||
            target == null)
        {
            return false;
        }

        if (handle.item != null)
        {
            if (ReferenceEquals(
                    handle.item,
                    target))
            {
                return true;
            }

            string leftId =
                GetInventoryItemId(
                    handle.item
                );

            string rightId =
                GetInventoryItemId(
                    target
                );

            if (!string.IsNullOrWhiteSpace(
                    leftId) &&
                string.Equals(
                    leftId,
                    rightId,
                    StringComparison
                        .OrdinalIgnoreCase))
            {
                return true;
            }

            return string.Equals(
                GetSafeItemName(handle.item),
                GetSafeItemName(target),
                StringComparison
                    .CurrentCultureIgnoreCase
            );
        }

        return !string.IsNullOrWhiteSpace(
                   handle.itemName) &&
               string.Equals(
                   handle.itemName.Trim(),
                   GetSafeItemName(target),
                   StringComparison
                       .CurrentCultureIgnoreCase
               );
    }

    private static bool TrySetSlotAmount(
        SlotHandle handle,
        int newAmount)
    {
        if (handle == null ||
            handle.slot == null)
        {
            return false;
        }

        bool set =
            TrySetFirstMemberValue(
                handle.slot,
                newAmount,
                "amount",
                "Amount",
                "quantity",
                "Quantity",
                "count",
                "Count",
                "stack",
                "Stack"
            );

        if (!set)
        {
            MethodInfo removeAmount =
                FindMethod(
                    handle.slot.GetType(),
                    "RemoveAmount",
                    typeof(int)
                );

            if (removeAmount != null)
            {
                int difference =
                    Mathf.Max(
                        0,
                        handle.amount -
                        newAmount
                    );

                try
                {
                    removeAmount.Invoke(
                        handle.slot,
                        new object[]
                        {
                            difference
                        }
                    );

                    set = true;
                }
                catch
                {
                    set = false;
                }
            }
        }

        if (!set)
            return false;

        /*
         * Slot có thể là struct bị box; ghi lại vào container.
         */
        TryWriteSlotBack(handle);
        handle.amount = newAmount;

        return true;
    }

    private static bool TryClearSlot(
        SlotHandle handle)
    {
        if (handle == null)
            return false;

        /*
         * Tốt nhất là xóa phần tử khỏi mảng/list bằng null.
         */
        if (TrySetContainerElement(
                handle.container,
                handle.index,
                null))
        {
            handle.slot = null;
            handle.item = null;
            handle.itemName = null;
            handle.amount = 0;
            return true;
        }

        if (handle.slot == null)
            return true;

        MethodInfo clearMethod =
            handle.slot.GetType()
                .GetMethod(
                    "Clear",
                    MemberFlags,
                    null,
                    Type.EmptyTypes,
                    null
                );

        if (clearMethod != null)
        {
            try
            {
                clearMethod.Invoke(
                    handle.slot,
                    null
                );

                TryWriteSlotBack(handle);
                handle.amount = 0;
                return true;
            }
            catch
            {
                // Thử cách tiếp theo.
            }
        }

        bool amountCleared =
            TrySetFirstMemberValue(
                handle.slot,
                0,
                "amount",
                "Amount",
                "quantity",
                "Quantity",
                "count",
                "Count",
                "stack",
                "Stack"
            );

        bool itemCleared =
            TrySetFirstMemberValue(
                handle.slot,
                null,
                "item",
                "Item",
                "itemData",
                "ItemData",
                "inventoryItem",
                "InventoryItem",
                "data",
                "Data"
            );

        bool nameCleared =
            TrySetFirstMemberValue(
                handle.slot,
                string.Empty,
                "itemName",
                "ItemName",
                "displayName",
                "DisplayName"
            );

        if (!amountCleared &&
            !itemCleared &&
            !nameCleared)
        {
            return false;
        }

        TryWriteSlotBack(handle);

        handle.item = null;
        handle.itemName = null;
        handle.amount = 0;

        return true;
    }

    private static void TryWriteSlotBack(
        SlotHandle handle)
    {
        if (handle == null ||
            handle.container == null)
        {
            return;
        }

        TrySetContainerElement(
            handle.container,
            handle.index,
            handle.slot
        );
    }

    private static bool TrySetContainerElement(
        object container,
        int index,
        object value)
    {
        if (container == null ||
            index < 0)
        {
            return false;
        }

        if (container is Array array)
        {
            if (index >= array.Length)
                return false;

            Type elementType =
                array.GetType()
                    .GetElementType();

            if (value == null &&
                elementType != null &&
                elementType.IsValueType)
            {
                value =
                    Activator.CreateInstance(
                        elementType
                    );
            }

            try
            {
                array.SetValue(
                    value,
                    index
                );

                return true;
            }
            catch
            {
                return false;
            }
        }

        if (container is IList list)
        {
            if (index >= list.Count ||
                list.IsReadOnly)
            {
                return false;
            }

            try
            {
                list[index] = value;
                return true;
            }
            catch
            {
                return false;
            }
        }

        return false;
    }

    private static void NotifyInventoryChanged(
        InventoryManager manager)
    {
        if (manager == null)
            return;

        string[] refreshMethods =
        {
            "RefreshInventoryUI",
            "NotifyInventoryChanged",
            "RaiseInventoryChanged",
            "InvokeInventoryChanged",
            "RefreshUI"
        };

        foreach (string methodName
                 in refreshMethods)
        {
            MethodInfo method =
                manager.GetType()
                    .GetMethod(
                        methodName,
                        MemberFlags,
                        null,
                        Type.EmptyTypes,
                        null
                    );

            if (method == null)
                continue;

            try
            {
                method.Invoke(
                    manager,
                    null
                );

                return;
            }
            catch
            {
                // Thử method khác.
            }
        }

        /*
         * Fallback cuối: gọi backing delegate của event nếu có.
         */
        string[] eventFieldNames =
        {
            "OnInventoryChanged",
            "onInventoryChanged",
            "InventoryChanged"
        };

        foreach (string fieldName
                 in eventFieldNames)
        {
            FieldInfo field =
                manager.GetType()
                    .GetField(
                        fieldName,
                        MemberFlags
                    );

            if (field == null)
                continue;

            if (field.GetValue(manager)
                is Action callback)
            {
                callback.Invoke();
                return;
            }
        }
    }

    private static MethodInfo FindMethod(
        Type type,
        string methodName,
        params Type[] parameterTypes)
    {
        if (type == null)
            return null;

        return type.GetMethod(
            methodName,
            MemberFlags,
            null,
            parameterTypes,
            null
        );
    }

    private static object GetFirstMemberValue(
        object target,
        params string[] names)
    {
        foreach (string name in names)
        {
            object value =
                GetMemberValue(
                    target,
                    name
                );

            if (value != null)
                return value;
        }

        return null;
    }

    private static object GetMemberValue(
        object target,
        string name)
    {
        if (target == null ||
            string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        Type type = target.GetType();

        PropertyInfo property =
            type.GetProperty(
                name,
                MemberFlags
            );

        if (property != null &&
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
                // Thử field.
            }
        }

        FieldInfo field =
            type.GetField(
                name,
                MemberFlags
            );

        if (field != null)
        {
            try
            {
                return field.GetValue(target);
            }
            catch
            {
                return null;
            }
        }

        return null;
    }

    private static bool TrySetFirstMemberValue(
        object target,
        object value,
        params string[] names)
    {
        if (target == null)
            return false;

        Type type = target.GetType();

        foreach (string name in names)
        {
            PropertyInfo property =
                type.GetProperty(
                    name,
                    MemberFlags
                );

            if (property != null &&
                property.CanWrite &&
                property.GetIndexParameters()
                    .Length == 0)
            {
                try
                {
                    object converted =
                        ConvertValue(
                            value,
                            property.PropertyType
                        );

                    property.SetValue(
                        target,
                        converted,
                        null
                    );

                    return true;
                }
                catch
                {
                    // Thử field hoặc tên tiếp theo.
                }
            }

            FieldInfo field =
                type.GetField(
                    name,
                    MemberFlags
                );

            if (field == null ||
                field.IsInitOnly)
            {
                continue;
            }

            try
            {
                object converted =
                    ConvertValue(
                        value,
                        field.FieldType
                    );

                field.SetValue(
                    target,
                    converted
                );

                return true;
            }
            catch
            {
                // Thử tên tiếp theo.
            }
        }

        return false;
    }

    private static object ConvertValue(
        object value,
        Type targetType)
    {
        if (targetType == null)
            return value;

        if (value == null)
        {
            return targetType.IsValueType
                ? Activator.CreateInstance(
                    targetType
                )
                : null;
        }

        if (targetType.IsInstanceOfType(
                value))
        {
            return value;
        }

        return Convert.ChangeType(
            value,
            targetType
        );
    }

    private static int ConvertToInt(
        object value,
        int fallback)
    {
        if (value == null)
            return fallback;

        try
        {
            return Mathf.Max(
                0,
                Convert.ToInt32(value)
            );
        }
        catch
        {
            return fallback;
        }
    }

    private static string
        GetInventoryItemId(
            InventoryItemData item)
    {
        if (item == null)
            return string.Empty;

        object value =
            GetFirstMemberValue(
                item,
                "ItemId",
                "itemId",
                "ID",
                "Id",
                "id"
            );

        return value as string ??
               string.Empty;
    }

    private static string GetSafeItemName(
        InventoryItemData item)
    {
        if (item == null)
            return "vật phẩm";

        object value =
            GetFirstMemberValue(
                item,
                "DisplayName",
                "displayName",
                "ItemName",
                "itemName"
            );

        string displayName =
            value as string;

        return string.IsNullOrWhiteSpace(
                   displayName)
            ? item.name
            : displayName;
    }

    private static string GetExceptionMessage(
        Exception exception)
    {
        if (exception == null)
            return "Không rõ lỗi.";

        if (exception is
                TargetInvocationException invocation &&
            invocation.InnerException != null)
        {
            return invocation.InnerException.Message;
        }

        return exception.Message;
    }
}
