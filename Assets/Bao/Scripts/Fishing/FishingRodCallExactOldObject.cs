using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

[DisallowMultipleComponent]
[DefaultExecutionOrder(99900)]
public class FishingRodCallExactOldObject : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private InventoryManager inventoryManager;

    [Tooltip(
        "Kéo đúng object Fishing Rod cũ đã làm sẵn trong Hierarchy vào đây."
    )]
    [SerializeField]
    private GameObject exactOldRodObject;

    [Tooltip(
        "Parent gốc của cần cũ, thường là Player/ToolHolder."
    )]
    [SerializeField]
    private Transform originalParent;

    [Tooltip(
        "Player/HeldItem. Hình item mua sẽ bị ẩn khi chọn cần."
    )]
    [SerializeField]
    private Transform heldItemRoot;

    [Header("Captured Old Rod State")]
    [Tooltip(
        "Khôi phục Position và Scale gốc, nhưng KHÔNG ghi đè Rotation."
    )]
    [SerializeField]
    private bool restoreOriginalPositionAndScale = true;

    [SerializeField]
    private Vector3 originalLocalPosition;

    [SerializeField]
    private Vector3 originalLocalScale = Vector3.one;

    [Header("Rod Recognition")]
    [SerializeField]
    private string[] rodKeywords =
    {
        "Feather Light",
        "FeatherLight",
        "Fishing Rod",
        "Cần Câu",
        "equipment_rod"
    };

    [Header("Debug")]
    [SerializeField]
    private bool showDebugLogs;

    private bool lastSelectedRod;

    private readonly Dictionary<SpriteRenderer, bool>
        heldRendererStates =
            new Dictionary<SpriteRenderer, bool>();

    private void Awake()
    {
        ResolveReferences();
        ApplyState(true);
    }

    private void OnEnable()
    {
        ResolveReferences();
        ApplyState(true);
    }

    private void LateUpdate()
    {
        ResolveReferences();

        bool selectedRod =
            IsSelectedHotbarRod();

        if (selectedRod != lastSelectedRod)
        {
            ApplyState(true);
            return;
        }

        if (!selectedRod)
            return;

        RestoreExactOldRodObject();
        HideHeldItemRenderers();
    }

    public void RefreshNow()
    {
        ResolveReferences();
        ApplyState(true);
    }

    private void ApplyState(bool force)
    {
        bool selectedRod =
            IsSelectedHotbarRod();

        if (!force &&
            selectedRod == lastSelectedRod)
        {
            return;
        }

        if (selectedRod)
        {
            CacheHeldItemStates();
            HideHeldItemRenderers();
            RestoreExactOldRodObject();

            if (exactOldRodObject != null)
                exactOldRodObject.SetActive(true);

            if (showDebugLogs)
            {
                Debug.Log(
                    "[FishingRodCallExactOldObject] " +
                    "Đã gọi đúng object cần câu cũ.",
                    this
                );
            }
        }
        else
        {
            if (exactOldRodObject != null)
                exactOldRodObject.SetActive(false);

            RestoreHeldItemRenderers();
        }

        lastSelectedRod = selectedRod;
    }

    private void RestoreExactOldRodObject()
    {
        if (exactOldRodObject == null)
            return;

        Transform rodTransform =
            exactOldRodObject.transform;

        /*
         * Gọi lại chính object cũ.
         * Không thay sprite, không Instantiate object mới.
         */
        if (originalParent != null &&
            rodTransform.parent != originalParent)
        {
            rodTransform.SetParent(
                originalParent,
                false
            );
        }

        if (restoreOriginalPositionAndScale)
        {
            rodTransform.localPosition =
                originalLocalPosition;

            rodTransform.localScale =
                originalLocalScale;
        }

        /*
         * Không chỉnh localRotation.
         * Script xoay/animation cũ của Fishing Rod vẫn hoạt động nguyên bản.
         */
        if (!exactOldRodObject.activeSelf)
            exactOldRodObject.SetActive(true);
    }

    private void CacheHeldItemStates()
    {
        heldRendererStates.Clear();

        if (heldItemRoot == null)
            return;

        SpriteRenderer[] renderers =
            heldItemRoot.GetComponentsInChildren<
                SpriteRenderer
            >(true);

        foreach (SpriteRenderer renderer
                 in renderers)
        {
            if (renderer != null)
            {
                heldRendererStates[renderer] =
                    renderer.enabled;
            }
        }
    }

    private void HideHeldItemRenderers()
    {
        if (heldItemRoot == null)
            return;

        SpriteRenderer[] renderers =
            heldItemRoot.GetComponentsInChildren<
                SpriteRenderer
            >(true);

        foreach (SpriteRenderer renderer
                 in renderers)
        {
            if (renderer != null)
                renderer.enabled = false;
        }
    }

    private void RestoreHeldItemRenderers()
    {
        foreach (
            KeyValuePair<SpriteRenderer, bool>
            state in heldRendererStates)
        {
            if (state.Key != null)
                state.Key.enabled = state.Value;
        }

        heldRendererStates.Clear();
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

    private bool IsRodName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        string normalized =
            Normalize(value);

        if (normalized.Contains("featherlight") ||
            normalized.Contains("fishingrod") ||
            normalized.Contains("cancau") ||
            normalized.Contains("equipmentrod"))
        {
            return true;
        }

        if (rodKeywords == null)
            return false;

        foreach (string keyword
                 in rodKeywords)
        {
            string key =
                Normalize(keyword);

            if (string.IsNullOrWhiteSpace(key))
                continue;

            if (normalized == key ||
                normalized.Contains(key) ||
                key.Contains(normalized))
            {
                return true;
            }
        }

        return false;
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

        if (heldItemRoot == null)
        {
            heldItemRoot =
                FindChildByName(
                    transform.root,
                    "HeldItem"
                );
        }
    }

    private static Transform FindChildByName(
        Transform root,
        string objectName)
    {
        if (root == null)
            return null;

        Transform[] children =
            root.GetComponentsInChildren<
                Transform
            >(true);

        foreach (Transform child
                 in children)
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
                    .GetUnicodeCategory(character);

            if (category ==
                UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetterOrDigit(character))
                builder.Append(character);
        }

        return builder
            .ToString()
            .Normalize(
                NormalizationForm.FormC
            );
    }

    [ContextMenu(
        "CAPTURE CURRENT OLD ROD POSITION AND SCALE"
    )]
    private void CaptureCurrentState()
    {
        if (exactOldRodObject == null)
            return;

        Transform rodTransform =
            exactOldRodObject.transform;

        originalParent =
            rodTransform.parent;

        originalLocalPosition =
            rodTransform.localPosition;

        originalLocalScale =
            rodTransform.localScale;

        Debug.Log(
            "[FishingRodCallExactOldObject] " +
            "Đã lưu đúng Parent, Position và Scale của cần cũ.",
            this
        );
    }
}
