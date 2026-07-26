using System;
using System.Globalization;
using System.Text;
using UnityEngine;

[DisallowMultipleComponent]
[DefaultExecutionOrder(20000)]
public class HideHeldItemWhenFishingRodSelected : MonoBehaviour
{
    [SerializeField] private InventoryManager inventoryManager;
    [SerializeField] private GameObject heldItemObject;
    [SerializeField] private SpriteRenderer heldItemRenderer;

    [SerializeField] private string[] rodKeywords =
    {
        "Feather Light",
        "FeatherLight",
        "Fishing Rod",
        "Cần Câu",
        "equipment_rod"
    };

    private void Awake()
    {
        ResolveReferences();
    }

    private void LateUpdate()
    {
        ResolveReferences();

        if (inventoryManager == null || heldItemRenderer == null)
            return;

        InventoryManager.InventorySlot selected = inventoryManager.SelectedSlot;

        string selectedName =
            selected != null && !selected.IsEmpty
                ? selected.itemName
                : string.Empty;

        heldItemRenderer.enabled = !IsRodName(selectedName);
    }

    private void ResolveReferences()
    {
        if (inventoryManager == null)
            inventoryManager = InventoryManager.Instance;

        if (inventoryManager == null)
            inventoryManager = FindFirstObjectByType<InventoryManager>(
                FindObjectsInactive.Include
            );

        if (heldItemObject == null)
        {
            Transform[] children = transform.GetComponentsInChildren<Transform>(true);

            foreach (Transform child in children)
            {
                if (child != null &&
                    string.Equals(
                        child.name,
                        "HeldItem",
                        StringComparison.OrdinalIgnoreCase
                    ))
                {
                    heldItemObject = child.gameObject;
                    break;
                }
            }
        }

        if (heldItemRenderer == null && heldItemObject != null)
            heldItemRenderer =
                heldItemObject.GetComponentInChildren<SpriteRenderer>(true);
    }

    private bool IsRodName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        string normalized = Normalize(value);

        if (normalized.Contains("featherlight") ||
            normalized.Contains("fishingrod") ||
            normalized.Contains("cancau") ||
            normalized.Contains("equipmentrod"))
        {
            return true;
        }

        if (rodKeywords == null)
            return false;

        foreach (string keyword in rodKeywords)
        {
            string normalizedKeyword = Normalize(keyword);

            if (string.IsNullOrWhiteSpace(normalizedKeyword))
                continue;

            if (normalized == normalizedKeyword ||
                normalized.Contains(normalizedKeyword) ||
                normalizedKeyword.Contains(normalized))
            {
                return true;
            }
        }

        return false;
    }

    private static string Normalize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        string decomposed =
            value.Trim()
                .ToLowerInvariant()
                .Normalize(NormalizationForm.FormD);

        StringBuilder builder = new StringBuilder();

        foreach (char character in decomposed)
        {
            UnicodeCategory category =
                CharUnicodeInfo.GetUnicodeCategory(character);

            if (category == UnicodeCategory.NonSpacingMark)
                continue;

            if (char.IsLetterOrDigit(character))
                builder.Append(character);
        }

        return builder
            .ToString()
            .Normalize(NormalizationForm.FormC);
    }
}
