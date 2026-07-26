using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

[DisallowMultipleComponent]
[DefaultExecutionOrder(95000)]
public class FishingRodPurchasedItemCallsOldRod : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private InventoryManager inventoryManager;

    [Tooltip(
        "Kéo đúng Player/ToolHolder/Fishing Rod cũ vào đây."
    )]
    [SerializeField]
    private GameObject oldFishingRod;

    [Tooltip(
        "Player/HeldItem. Renderer của item mua sẽ bị ẩn khi chọn cần câu."
    )]
    [SerializeField]
    private Transform heldItemRoot;

    [Header("Rules")]
    [Tooltip(
        "Không sửa Position, Rotation, Scale hoặc Sprite của cần cũ. " +
        "Nhờ vậy script xoay cũ trên Fishing Rod hoạt động nguyên bản."
    )]
    [SerializeField]
    private bool preserveOldRodExactly = true;

    [Tooltip(
        "Ẩn mọi SpriteRenderer nằm trong HeldItem khi chọn cần câu."
    )]
    [SerializeField]
    private bool hideHeldItemRenderers = true;

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

    private readonly Dictionary<
        SpriteRenderer,
        bool
    > heldRendererStates =
        new Dictionary<
            SpriteRenderer,
            bool
        >();

    private void Awake()
    {
        ResolveReferences();
        DisableConflictingDisplayControllers();
        ApplyState(true);
    }

    private void OnEnable()
    {
        ResolveReferences();
        DisableConflictingDisplayControllers();
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

        if (selectedRod)
        {
            /*
             * Chỉ bảo đảm cần cũ đang bật và HeldItem đang ẩn.
             *
             * TUYỆT ĐỐI không ghi localRotation ở đây.
             * Script xoay cũ trên Fishing Rod được toàn quyền điều khiển.
             */
            if (oldFishingRod != null &&
                !oldFishingRod.activeSelf)
            {
                oldFishingRod.SetActive(true);
            }

            HideHeldItem();
        }
    }

    public void RefreshNow()
    {
        ResolveReferences();
        ApplyState(true);
    }

    private void ApplyState(
        bool force)
    {
        bool selectedRod =
            IsSelectedHotbarRod();

        if (!force &&
            selectedRod ==
                lastSelectedRod)
        {
            return;
        }

        if (selectedRod)
        {
            CacheHeldRendererStates();
            HideHeldItem();

            if (oldFishingRod != null)
            {
                oldFishingRod.SetActive(true);
            }

            if (showDebugLogs)
            {
                Debug.Log(
                    "[FishingRodPurchasedItemCallsOldRod] " +
                    "Item cần câu đã mua được chọn. " +
                    "Đang gọi lại Fishing Rod cũ và giữ nguyên hệ thống xoay cũ.",
                    this
                );
            }
        }
        else
        {
            if (oldFishingRod != null)
            {
                oldFishingRod.SetActive(false);
            }

            RestoreHeldItem();

            if (showDebugLogs)
            {
                Debug.Log(
                    "[FishingRodPurchasedItemCallsOldRod] " +
                    "Đã cất Fishing Rod cũ.",
                    this
                );
            }
        }

        lastSelectedRod =
            selectedRod;
    }

    private void CacheHeldRendererStates()
    {
        heldRendererStates.Clear();

        if (heldItemRoot == null)
            return;

        SpriteRenderer[] renderers =
            heldItemRoot.GetComponentsInChildren<
                SpriteRenderer
            >(true);

        foreach (
            SpriteRenderer renderer
            in renderers)
        {
            if (renderer == null)
                continue;

            heldRendererStates[renderer] =
                renderer.enabled;
        }
    }

    private void HideHeldItem()
    {
        if (!hideHeldItemRenderers ||
            heldItemRoot == null)
        {
            return;
        }

        SpriteRenderer[] renderers =
            heldItemRoot.GetComponentsInChildren<
                SpriteRenderer
            >(true);

        foreach (
            SpriteRenderer renderer
            in renderers)
        {
            if (renderer != null)
            {
                renderer.enabled =
                    false;
            }
        }
    }

    private void RestoreHeldItem()
    {
        foreach (
            KeyValuePair<
                SpriteRenderer,
                bool
            > state
            in heldRendererStates)
        {
            if (state.Key != null)
            {
                state.Key.enabled =
                    state.Value;
            }
        }

        heldRendererStates.Clear();
    }

    private bool IsSelectedHotbarRod()
    {
        if (inventoryManager == null)
            return false;

        InventoryManager.InventorySlot slot =
            inventoryManager.SelectedSlot;

        if (slot == null ||
            slot.IsEmpty)
        {
            return false;
        }

        return IsRodName(
            slot.itemName
        );
    }

    private bool IsRodName(
        string value)
    {
        if (string.IsNullOrWhiteSpace(
                value))
        {
            return false;
        }

        string normalized =
            Normalize(value);

        if (normalized.Contains(
                "featherlight") ||
            normalized.Contains(
                "fishingrod") ||
            normalized.Contains(
                "cancau") ||
            normalized.Contains(
                "equipmentrod"))
        {
            return true;
        }

        if (rodKeywords == null)
            return false;

        foreach (
            string keyword
            in rodKeywords)
        {
            string normalizedKeyword =
                Normalize(keyword);

            if (string.IsNullOrWhiteSpace(
                    normalizedKeyword))
            {
                continue;
            }

            if (normalized ==
                    normalizedKeyword ||
                normalized.Contains(
                    normalizedKeyword) ||
                normalizedKeyword.Contains(
                    normalized))
            {
                return true;
            }
        }

        return false;
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
                >(
                    FindObjectsInactive.Include
                );
        }

        Transform playerRoot =
            transform.root;

        if (heldItemRoot == null)
        {
            heldItemRoot =
                FindChildByName(
                    playerRoot,
                    "HeldItem"
                );
        }

        if (oldFishingRod == null)
        {
            Transform toolHolder =
                FindChildByName(
                    playerRoot,
                    "ToolHolder"
                );

            Transform rod =
                toolHolder != null
                    ? FindChildByName(
                        toolHolder,
                        "Fishing Rod"
                      )
                    : FindChildByName(
                        playerRoot,
                        "Fishing Rod"
                      );

            if (rod != null)
            {
                oldFishingRod =
                    rod.gameObject;
            }
        }
    }

    private void DisableConflictingDisplayControllers()
    {
        string[] conflictingTypes =
        {
            "FishingRodHotbarController",
            "FishingRodVisualReplacer",
            "FishingRodUseOldVisual",
            "FishingRodSingleVisualController",
            "HideHeldItemWhenFishingRodSelected"
        };

        MonoBehaviour[] behaviours =
            transform.root
                .GetComponentsInChildren<
                    MonoBehaviour
                >(true);

        foreach (
            MonoBehaviour behaviour
            in behaviours)
        {
            if (behaviour == null ||
                behaviour == this)
            {
                continue;
            }

            /*
             * Không tắt bất kỳ component nào nằm trong Fishing Rod cũ.
             * Nhờ vậy script xoay/animation cũ của nó vẫn hoạt động.
             */
            if (oldFishingRod != null &&
                behaviour.transform
                    .IsChildOf(
                        oldFishingRod.transform
                    ))
            {
                continue;
            }

            string typeName =
                behaviour.GetType().Name;

            foreach (
                string conflictingType
                in conflictingTypes)
            {
                if (!string.Equals(
                        typeName,
                        conflictingType,
                        StringComparison.Ordinal))
                {
                    continue;
                }

                behaviour.enabled =
                    false;

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
            root.GetComponentsInChildren<
                Transform
            >(true);

        foreach (
            Transform child
            in children)
        {
            if (child != null &&
                string.Equals(
                    child.name,
                    objectName,
                    StringComparison
                        .OrdinalIgnoreCase))
            {
                return child;
            }
        }

        return null;
    }

    private static string Normalize(
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

        foreach (
            char character
            in decomposed)
        {
            UnicodeCategory category =
                CharUnicodeInfo
                    .GetUnicodeCategory(
                        character
                    );

            if (category ==
                UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetterOrDigit(
                    character))
            {
                builder.Append(
                    character
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
