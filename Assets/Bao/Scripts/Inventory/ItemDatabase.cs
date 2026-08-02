using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

[DisallowMultipleComponent]
public class ItemDatabase : MonoBehaviour
{
    public static ItemDatabase Instance { get; private set; }

    [Header("Danh sách vật phẩm")]
    [SerializeField]
    private List<InventoryItemData> items =
        new List<InventoryItemData>();

    [Header("Scene")]
    [SerializeField]
    private bool keepAcrossScenes;

    [SerializeField]
    private bool logValidationErrors = true;

    private readonly Dictionary<
        string,
        InventoryItemData
    > itemsById =
        new Dictionary<
            string,
            InventoryItemData
        >();

    private readonly Dictionary<
        string,
        InventoryItemData
    > itemsByNormalizedName =
        new Dictionary<
            string,
            InventoryItemData
        >();

    public IReadOnlyList<InventoryItemData> Items =>
        items;

    public int Count => items != null
        ? items.Count
        : 0;

    private void Awake()
    {
        if (Instance != null &&
            Instance != this)
        {
            Debug.LogWarning(
                "Có nhiều ItemDatabase trong Scene. " +
                "Đã xóa bản trùng.",
                this
            );

            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (keepAcrossScenes)
            DontDestroyOnLoad(gameObject);

        RebuildLookup();
    }

    private void OnEnable()
    {
        if (Instance == null)
            Instance = this;

        RebuildLookup();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void OnValidate()
    {
        RemoveNullAndDuplicateReferences();

        if (Application.isPlaying)
            RebuildLookup();
    }

    [ContextMenu("Rebuild Lookup")]
    public void RebuildLookup()
    {
        itemsById.Clear();
        itemsByNormalizedName.Clear();

        if (items == null)
            items = new List<InventoryItemData>();

        for (int i = 0; i < items.Count; i++)
        {
            InventoryItemData item = items[i];

            if (item == null)
                continue;

            string id = NormalizeId(item.ItemId);

            if (string.IsNullOrWhiteSpace(id))
            {
                LogProblem(
                    "Vật phẩm \"" +
                    item.name +
                    "\" chưa có Item ID."
                );

                continue;
            }

            if (itemsById.ContainsKey(id))
            {
                LogProblem(
                    "Trùng Item ID \"" +
                    id +
                    "\" giữa \"" +
                    itemsById[id].name +
                    "\" và \"" +
                    item.name +
                    "\"."
                );
            }
            else
            {
                itemsById.Add(id, item);
            }

            string normalizedName =
                NormalizeDisplayName(
                    item.DisplayName
                );

            if (string.IsNullOrWhiteSpace(
                    normalizedName))
            {
                continue;
            }

            if (itemsByNormalizedName.ContainsKey(
                    normalizedName))
            {
                LogProblem(
                    "Trùng tên vật phẩm \"" +
                    item.DisplayName +
                    "\"."
                );
            }
            else
            {
                itemsByNormalizedName.Add(
                    normalizedName,
                    item
                );
            }
        }
    }

    public InventoryItemData GetById(
        string itemId)
    {
        if (itemsById.Count == 0)
            RebuildLookup();

        string normalizedId =
            NormalizeId(itemId);

        if (string.IsNullOrWhiteSpace(
                normalizedId))
        {
            return null;
        }

        itemsById.TryGetValue(
            normalizedId,
            out InventoryItemData item
        );

        return item;
    }

    public InventoryItemData GetByName(
        string displayName)
    {
        if (itemsByNormalizedName.Count == 0)
            RebuildLookup();

        string normalizedName =
            NormalizeDisplayName(
                displayName
            );

        if (string.IsNullOrWhiteSpace(
                normalizedName))
        {
            return null;
        }

        itemsByNormalizedName.TryGetValue(
            normalizedName,
            out InventoryItemData item
        );

        return item;
    }

    public InventoryItemData Get(
        string idOrName)
    {
        InventoryItemData byId =
            GetById(idOrName);

        return byId != null
            ? byId
            : GetByName(idOrName);
    }

    public bool TryGetById(
        string itemId,
        out InventoryItemData item)
    {
        item = GetById(itemId);
        return item != null;
    }

    public bool TryGetByName(
        string displayName,
        out InventoryItemData item)
    {
        item = GetByName(displayName);
        return item != null;
    }

    public bool Contains(
        InventoryItemData item)
    {
        if (item == null)
            return false;

        InventoryItemData found =
            GetById(item.ItemId);

        return found == item;
    }

    public void SetItems(
        IEnumerable<InventoryItemData>
            newItems)
    {
        items.Clear();

        if (newItems != null)
        {
            foreach (InventoryItemData item
                     in newItems)
            {
                if (item == null ||
                    items.Contains(item))
                {
                    continue;
                }

                items.Add(item);
            }
        }

        RebuildLookup();
    }

    private void RemoveNullAndDuplicateReferences()
    {
        if (items == null)
        {
            items =
                new List<InventoryItemData>();

            return;
        }

        HashSet<InventoryItemData> unique =
            new HashSet<InventoryItemData>();

        for (int i = items.Count - 1;
             i >= 0;
             i--)
        {
            InventoryItemData item = items[i];

            if (item == null ||
                !unique.Add(item))
            {
                items.RemoveAt(i);
            }
        }
    }

    private void LogProblem(string message)
    {
        if (!logValidationErrors)
            return;

        Debug.LogError(
            "[ItemDatabase] " + message,
            this
        );
    }

    public static string NormalizeId(
        string value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Trim().ToLowerInvariant();
    }

    public static string NormalizeDisplayName(
        string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        string decomposed =
            value.Trim()
                .Normalize(
                    NormalizationForm.FormD
                );

        StringBuilder builder =
            new StringBuilder();

        foreach (char character in decomposed)
        {
            UnicodeCategory category =
                CharUnicodeInfo.GetUnicodeCategory(
                    character
                );

            if (category ==
                UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetterOrDigit(character))
            {
                builder.Append(
                    char.ToLowerInvariant(character)
                );
            }
        }

        return builder
            .ToString()
            .Normalize(
                NormalizationForm.FormC
            );
    }
}
