using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

[DisallowMultipleComponent]
[DefaultExecutionOrder(90000)]
public class FishingRodSingleVisualController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private InventoryManager inventoryManager;

    [Tooltip("Cây cần cũ đã làm sẵn: Player/ToolHolder/Fishing Rod.")]
    [SerializeField] private GameObject oldRodObject;

    [SerializeField] private SpriteRenderer oldRodRenderer;

    [Tooltip("Sprite cây cần cũ. Không dùng icon của cần mua để hiển thị ngoài world.")]
    [SerializeField] private Sprite oldRodSprite;

    [Tooltip("Player/HeldItem. Toàn bộ SpriteRenderer trong đây sẽ bị ẩn khi cầm cần.")]
    [SerializeField] private Transform heldItemRoot;

    [Tooltip("Player/ToolHolder.")]
    [SerializeField] private Transform toolHolder;

    [Header("Old Rod Transform")]
    [SerializeField] private bool forceOldTransform = true;

    [SerializeField] private Vector3 oldLocalPosition =
        new Vector3(0.457f, 1.059f, 0f);

    [SerializeField] private Vector3 oldLocalEulerAngles =
        Vector3.zero;

    [SerializeField] private Vector3 oldLocalScale =
        new Vector3(0.2622f, 0.2622f, 0.2622f);

    [Header("Rod Recognition")]
    [SerializeField] private string[] rodKeywords =
    {
        "Feather Light",
        "FeatherLight",
        "Fishing Rod",
        "Cần Câu",
        "equipment_rod"
    };

    [Header("Duplicate Cleanup")]
    [Tooltip(
        "Ẩn mọi SpriteRenderer khác trong Player đang dùng đúng icon của item cần câu được chọn."
    )]
    [SerializeField] private bool hideMatchingRodSprites = true;

    [Tooltip(
        "Ẩn các SpriteRenderer khác có tên chứa Rod/Fishing/Feather, trừ cây Fishing Rod cũ."
    )]
    [SerializeField] private bool hideNamedRodRenderers = true;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs;

    private bool lastRodSelected;
    private readonly Dictionary<SpriteRenderer, bool> cachedRendererStates =
        new Dictionary<SpriteRenderer, bool>();

    private void Awake()
    {
        ResolveReferences();
        DisableConflictingRuntimeScripts();
        ApplySelectedState(true);
    }

    private void OnEnable()
    {
        ResolveReferences();
        DisableConflictingRuntimeScripts();
        ApplySelectedState(true);
    }

    private void LateUpdate()
    {
        ResolveReferences();

        bool rodSelected = IsSelectedHotbarRod();

        if (rodSelected != lastRodSelected)
        {
            ApplySelectedState(true);
        }
        else if (rodSelected)
        {
            /*
             * Ép trạng thái ở execution order rất cao:
             * - Chỉ cây cần cũ được hiện.
             * - Mọi hình cần mua từ HeldItem/script cũ đều bị ẩn.
             */
            ShowOnlyOldRod();
        }
    }

    public void RefreshNow()
    {
        ResolveReferences();
        ApplySelectedState(true);
    }

    private void ApplySelectedState(bool force)
    {
        bool rodSelected = IsSelectedHotbarRod();

        if (!force && rodSelected == lastRodSelected)
            return;

        if (rodSelected)
        {
            CacheRendererStates();
            ShowOnlyOldRod();
        }
        else
        {
            HideOldRod();
            RestoreRendererStates();
        }

        lastRodSelected = rodSelected;
    }

    private void ShowOnlyOldRod()
    {
        if (oldRodObject == null || oldRodRenderer == null)
            return;

        if (toolHolder != null &&
            oldRodObject.transform.parent != toolHolder)
        {
            oldRodObject.transform.SetParent(toolHolder, false);
        }

        if (forceOldTransform)
        {
            Transform rodTransform = oldRodObject.transform;
            rodTransform.localPosition = oldLocalPosition;
            rodTransform.localRotation =
                Quaternion.Euler(oldLocalEulerAngles);
            rodTransform.localScale = oldLocalScale;
        }

        if (oldRodSprite != null)
            oldRodRenderer.sprite = oldRodSprite;

        oldRodRenderer.enabled = true;
        oldRodRenderer.flipX = false;
        oldRodRenderer.flipY = false;
        oldRodObject.SetActive(true);

        HideDuplicateRodRenderers();

        if (showDebugLogs)
        {
            Debug.Log(
                "[FishingRodSingleVisualController] " +
                "Đang chọn cần đã mua: chỉ hiện Fishing Rod cũ.",
                this
            );
        }
    }

    private void HideOldRod()
    {
        if (oldRodObject != null)
            oldRodObject.SetActive(false);
    }

    private void CacheRendererStates()
    {
        cachedRendererStates.Clear();

        Transform playerRoot = transform.root;
        SpriteRenderer[] renderers =
            playerRoot.GetComponentsInChildren<SpriteRenderer>(true);

        foreach (SpriteRenderer renderer in renderers)
        {
            if (renderer == null || renderer == oldRodRenderer)
                continue;

            cachedRendererStates[renderer] = renderer.enabled;
        }
    }

    private void RestoreRendererStates()
    {
        foreach (KeyValuePair<SpriteRenderer, bool> pair
                 in cachedRendererStates)
        {
            if (pair.Key != null)
                pair.Key.enabled = pair.Value;
        }

        cachedRendererStates.Clear();
    }

    private void HideDuplicateRodRenderers()
    {
        Transform playerRoot = transform.root;
        Sprite selectedIcon = GetSelectedSlotIcon();

        SpriteRenderer[] renderers =
            playerRoot.GetComponentsInChildren<SpriteRenderer>(true);

        foreach (SpriteRenderer renderer in renderers)
        {
            if (renderer == null || renderer == oldRodRenderer)
                continue;

            bool shouldHide = false;

            if (heldItemRoot != null &&
                renderer.transform.IsChildOf(heldItemRoot))
            {
                shouldHide = true;
            }

            if (!shouldHide &&
                hideMatchingRodSprites &&
                selectedIcon != null &&
                renderer.sprite == selectedIcon)
            {
                shouldHide = true;
            }

            if (!shouldHide &&
                hideNamedRodRenderers &&
                IsDuplicateRodName(renderer.gameObject.name))
            {
                shouldHide = true;
            }

            if (shouldHide)
                renderer.enabled = false;
        }
    }

    private bool IsDuplicateRodName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        string normalized = Normalize(value);

        return normalized.Contains("helditem") ||
               normalized.Contains("featherlight") ||
               normalized.Contains("fishingrodaimpivot") ||
               normalized.Contains("rodpreview") ||
               normalized.Contains("purchasedrod");
    }

    private bool IsSelectedHotbarRod()
    {
        if (inventoryManager == null)
            return false;

        InventoryManager.InventorySlot slot =
            inventoryManager.SelectedSlot;

        return slot != null &&
               !slot.IsEmpty &&
               IsRodName(slot.itemName);
    }

    private Sprite GetSelectedSlotIcon()
    {
        if (inventoryManager == null)
            return null;

        InventoryManager.InventorySlot slot =
            inventoryManager.SelectedSlot;

        return slot != null && !slot.IsEmpty
            ? slot.icon
            : null;
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

    private void ResolveReferences()
    {
        if (inventoryManager == null)
            inventoryManager = InventoryManager.Instance;

        if (inventoryManager == null)
        {
            inventoryManager =
                FindFirstObjectByType<InventoryManager>(
                    FindObjectsInactive.Include
                );
        }

        Transform playerRoot = transform.root;

        if (toolHolder == null)
            toolHolder = FindChildByName(playerRoot, "ToolHolder");

        if (heldItemRoot == null)
            heldItemRoot = FindChildByName(playerRoot, "HeldItem");

        if (oldRodObject == null && toolHolder != null)
        {
            Transform oldRod = FindChildByName(toolHolder, "Fishing Rod");

            if (oldRod != null)
                oldRodObject = oldRod.gameObject;
        }

        if (oldRodRenderer == null && oldRodObject != null)
        {
            oldRodRenderer =
                oldRodObject.GetComponentInChildren<SpriteRenderer>(true);
        }

        if (oldRodSprite == null && oldRodRenderer != null)
            oldRodSprite = oldRodRenderer.sprite;
    }

    private void DisableConflictingRuntimeScripts()
    {
        string[] conflictingTypes =
        {
            "FishingRodHotbarController",
            "FishingRodVisualReplacer",
            "FishingRodUseOldVisual",
            "FishingRodMouseAim",
            "FishingRodMouseAimStable",
            "FishingRodMouseAimFinal",
            "FishingRodMouseAimPivot",
            "FishingRodAutoRotate",
            "FishingRodAimByTip",
            "FishingRodAimExact",
            "HideHeldItemWhenFishingRodSelected"
        };

        MonoBehaviour[] behaviours =
            transform.root.GetComponentsInChildren<MonoBehaviour>(true);

        foreach (MonoBehaviour behaviour in behaviours)
        {
            if (behaviour == null || behaviour == this)
                continue;

            string typeName = behaviour.GetType().Name;

            foreach (string conflictingType in conflictingTypes)
            {
                if (!string.Equals(
                        typeName,
                        conflictingType,
                        StringComparison.Ordinal))
                {
                    continue;
                }

                behaviour.enabled = false;
                break;
            }
        }
    }

    private static Transform FindChildByName(
        Transform root,
        string objectName)
    {
        if (root == null)
            return null;

        Transform[] children =
            root.GetComponentsInChildren<Transform>(true);

        foreach (Transform child in children)
        {
            if (child != null &&
                string.Equals(
                    child.name,
                    objectName,
                    StringComparison.OrdinalIgnoreCase))
            {
                return child;
            }
        }

        return null;
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

    [ContextMenu("CAPTURE OLD FISHING ROD")]
    private void CaptureOldFishingRod()
    {
        ResolveReferences();

        if (oldRodObject == null)
            return;

        Transform rodTransform = oldRodObject.transform;

        oldLocalPosition = rodTransform.localPosition;
        oldLocalEulerAngles = rodTransform.localEulerAngles;
        oldLocalScale = rodTransform.localScale;

        if (oldRodRenderer != null)
            oldRodSprite = oldRodRenderer.sprite;

        Debug.Log(
            "[FishingRodSingleVisualController] " +
            "Đã lưu Fishing Rod cũ.",
            this
        );
    }
}
