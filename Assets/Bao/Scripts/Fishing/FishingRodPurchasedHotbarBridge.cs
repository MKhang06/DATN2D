using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

[DisallowMultipleComponent]
[DefaultExecutionOrder(100000)]
public class FishingRodPurchasedHotbarBridge : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private InventoryManager inventoryManager;

    [SerializeField]
    private FishingRodOwnershipGate ownershipGate;

    [SerializeField]
    private PlayerToolAnimation playerToolAnimation;

    [Tooltip(
        "Đúng object Player/ToolHolder/Fishing Rod cũ."
    )]
    [SerializeField]
    private GameObject oldFishingRod;

    [Tooltip(
        "Player/HeldItem. Hình item mua sẽ bị ẩn khi đang chọn cần câu."
    )]
    [SerializeField]
    private Transform heldItemRoot;

    [Header("Old Fishing Rod Transform")]
    [Tooltip(
        "Luôn khôi phục Position và Scale chuẩn của Fishing Rod cũ. " +
        "Không ghi đè Rotation để hệ thống xoay cũ vẫn hoạt động."
    )]
    [SerializeField]
    private bool forceOldRodPositionAndScale = true;

    [SerializeField]
    private Vector3 oldRodLocalPosition =
        new Vector3(
            0.4570001f,
            1.059f,
            0f
        );

    [SerializeField]
    private Vector3 oldRodLocalScale =
        new Vector3(
            0.2622464f,
            0.2622464f,
            0.2622464f
        );

    [Header("Rules")]
    [Tooltip(
        "Chọn item cần câu trong Hotbar sẽ tự gọi cần cũ, không cần nhấn F."
    )]
    [SerializeField]
    private bool autoEquipFromHotbar = true;

    [Tooltip(
        "Khi không chọn item cần câu, phím F cũ không thể làm cần hiện."
    )]
    [SerializeField]
    private bool blockLegacyFWithoutPurchasedRod = true;

    [Tooltip(
        "Luôn mở khóa LookAtMouse của PlayerToolAnimation khi đang cầm cần."
    )]
    [SerializeField]
    private bool forceEnableMouseRotation = true;

    [Header("Fallback Rod Names")]
    [SerializeField]
    private string[] rodKeywords =
    {
        "Feather Light",
        "FeatherLight",
        "EQ_FeatherLight",
        "Fishing Rod",
        "Cần Câu",
        "equipment_rod"
    };

    [Header("Debug")]
    [SerializeField]
    private bool showDebugLogs;

    private bool lastHoldingRod;

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

    private void Update()
    {
        ResolveReferences();

        bool shouldHoldRod =
            ShouldHoldPurchasedRod();

        if (shouldHoldRod != lastHoldingRod)
        {
            ApplyState(true);
        }
    }

    private void LateUpdate()
    {
        ResolveReferences();

        bool shouldHoldRod =
            ShouldHoldPurchasedRod();

        if (shouldHoldRod)
        {
            /*
             * PlayerToolAnimation.LookAtMouse() thoát sớm khi fishingLock=true.
             * Vì vậy phải chủ động mở khóa, không đợi người chơi nhấn F lần nữa.
             */
            if (forceEnableMouseRotation &&
                playerToolAnimation != null)
            {
                playerToolAnimation.SetFishingLock(false);
            }

            if (oldFishingRod != null &&
                !oldFishingRod.activeSelf)
            {
                oldFishingRod.SetActive(true);
            }

            ApplyOldRodPositionAndScale();
            HideHeldItemRenderers();
        }
        else if (blockLegacyFWithoutPurchasedRod)
        {
            /*
             * Script F cũ có thể vừa bật cần trong Update.
             * Bridge chạy rất muộn và tắt lại ngay trong cùng frame.
             */
            if (oldFishingRod != null &&
                oldFishingRod.activeSelf)
            {
                oldFishingRod.SetActive(false);
            }

            if (playerToolAnimation != null)
            {
                playerToolAnimation.SetFishingLock(false);
            }
        }
    }

    public void RefreshNow()
    {
        ResolveReferences();
        ApplyState(true);
    }

    private void ApplyState(bool force)
    {
        bool shouldHoldRod =
            ShouldHoldPurchasedRod();

        if (!force &&
            shouldHoldRod == lastHoldingRod)
        {
            return;
        }

        if (shouldHoldRod)
        {
            CacheHeldItemStates();
            HideHeldItemRenderers();

            if (autoEquipFromHotbar &&
                playerToolAnimation != null)
            {
                /*
                 * Đây chính là phần thay cho lần nhấn F đầu tiên.
                 * Nó kích hoạt đúng Fishing Rod cũ bằng hệ thống gốc.
                 */
                playerToolAnimation.ShowTool(
                    PlayerToolController
                        .ToolType
                        .FishingRod
                );
            }
            else if (oldFishingRod != null)
            {
                oldFishingRod.SetActive(true);
            }

            if (forceEnableMouseRotation &&
                playerToolAnimation != null)
            {
                /*
                 * Không khóa chuột. LookAtMouse sẽ xoay ngay lập tức.
                 */
                playerToolAnimation.SetFishingLock(false);
            }

            ApplyOldRodPositionAndScale();

            if (showDebugLogs)
            {
                Debug.Log(
                    "[FishingRodPurchasedHotbarBridge] " +
                    "Đã tự gọi Fishing Rod cũ và bật xoay theo chuột.",
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

            if (playerToolAnimation != null)
            {
                playerToolAnimation.SetFishingLock(false);
            }

            RestoreHeldItemRenderers();

            if (showDebugLogs)
            {
                Debug.Log(
                    "[FishingRodPurchasedHotbarBridge] " +
                    "Không chọn cần đã mua: cần cũ bị khóa, F không thể gọi ra.",
                    this
                );
            }
        }

        lastHoldingRod =
            shouldHoldRod;
    }

    private void ApplyOldRodPositionAndScale()
    {
        if (!forceOldRodPositionAndScale ||
            oldFishingRod == null)
        {
            return;
        }

        Transform rodTransform =
            oldFishingRod.transform;

        rodTransform.localPosition =
            oldRodLocalPosition;

        rodTransform.localScale =
            oldRodLocalScale;

        /*
         * Không chỉnh localRotation ở đây.
         * PlayerToolAnimation vẫn được quyền xoay cần câu theo chuột.
         */
    }

    private bool ShouldHoldPurchasedRod()
    {
        if (ownershipGate != null)
        {
            return ownershipGate
                .IsHoldingRequiredRod;
        }

        if (inventoryManager == null)
            return false;

        InventoryManager.InventorySlot slot =
            inventoryManager.SelectedSlot;

        return slot != null &&
               !slot.IsEmpty &&
               IsRodName(slot.itemName);
    }

    private void CacheHeldItemStates()
    {
        heldRendererStates.Clear();

        if (heldItemRoot == null)
            return;

        SpriteRenderer[] renderers =
            heldItemRoot
                .GetComponentsInChildren<
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
            heldItemRoot
                .GetComponentsInChildren<
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
            entry in heldRendererStates)
        {
            if (entry.Key != null)
                entry.Key.enabled = entry.Value;
        }

        heldRendererStates.Clear();
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
            string normalizedKeyword =
                Normalize(keyword);

            if (string.IsNullOrWhiteSpace(
                    normalizedKeyword))
            {
                continue;
            }

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

        if (ownershipGate == null)
        {
            ownershipGate =
                FindFirstObjectByType<
                    FishingRodOwnershipGate
                >(
                    FindObjectsInactive.Include
                );
        }

        if (playerToolAnimation == null)
        {
            playerToolAnimation =
                GetComponent<
                    PlayerToolAnimation
                >();
        }

        if (playerToolAnimation == null)
        {
            playerToolAnimation =
                transform.root
                    .GetComponentInChildren<
                        PlayerToolAnimation
                    >(true);
        }

        if (heldItemRoot == null)
        {
            heldItemRoot =
                FindChildByName(
                    transform.root,
                    "HeldItem"
                );
        }

        if (oldFishingRod == null)
        {
            Transform toolHolder =
                FindChildByName(
                    transform.root,
                    "ToolHolder"
                );

            Transform rod =
                toolHolder != null
                    ? FindChildByName(
                        toolHolder,
                        "Fishing Rod"
                      )
                    : FindChildByName(
                        transform.root,
                        "Fishing Rod"
                      );

            if (rod != null)
                oldFishingRod = rod.gameObject;
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
                    StringComparison
                        .OrdinalIgnoreCase))
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
}