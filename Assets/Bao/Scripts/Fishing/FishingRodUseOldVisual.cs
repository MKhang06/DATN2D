using System;
using System.Globalization;
using System.Text;
using UnityEngine;

[DisallowMultipleComponent]
[DefaultExecutionOrder(70000)]
public class FishingRodUseOldVisual : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private InventoryManager inventoryManager;

    [Tooltip(
        "Kéo Player/ToolHolder/Fishing Rod cũ vào đây."
    )]
    [SerializeField]
    private GameObject oldRodObject;

    [SerializeField]
    private SpriteRenderer oldRodRenderer;

    [Tooltip(
        "Sprite cần câu cũ muốn luôn sử dụng ngoài thế giới."
    )]
    [SerializeField]
    private Sprite oldRodSprite;

    [Tooltip(
        "SpriteRenderer của Player/HeldItem. " +
        "Nó sẽ bị ẩn khi đang chọn cần câu."
    )]
    [SerializeField]
    private SpriteRenderer heldItemRenderer;

    [Header("Old Rod Transform")]
    [Tooltip(
        "Khôi phục vị trí, góc xoay và kích thước cũ mỗi khi cầm cần."
    )]
    [SerializeField]
    private bool restoreOldTransform = true;

    [SerializeField]
    private Vector3 oldLocalPosition;

    [SerializeField]
    private Vector3 oldLocalEulerAngles;

    [SerializeField]
    private Vector3 oldLocalScale =
        Vector3.one;

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

    private bool lastHoldingRod;

    private void Awake()
    {
        ResolveReferences();
        CacheOldVisualWhenMissing();
        ApplyState(false, true);
    }

    private void OnEnable()
    {
        ResolveReferences();
        CacheOldVisualWhenMissing();
        RefreshNow();
    }

    private void LateUpdate()
    {
        ResolveReferences();

        bool holdingRod =
            IsSelectedHotbarRod();

        if (holdingRod != lastHoldingRod)
        {
            ApplyState(
                holdingRod,
                false
            );
        }
        else if (holdingRod)
        {
            /*
             * Luôn khôi phục hình cần cũ để script khác
             * không thay lại sprite cần mới.
             */
            RestoreOldRodVisual();
        }
    }

    public void RefreshNow()
    {
        ApplyState(
            IsSelectedHotbarRod(),
            true
        );
    }

    private void ApplyState(
        bool holdingRod,
        bool force)
    {
        if (!force &&
            holdingRod == lastHoldingRod)
        {
            return;
        }

        lastHoldingRod =
            holdingRod;

        if (heldItemRenderer != null)
        {
            heldItemRenderer.enabled =
                !holdingRod;
        }

        if (oldRodObject == null)
            return;

        if (!holdingRod)
        {
            oldRodObject.SetActive(false);

            if (showDebugLogs)
            {
                Debug.Log(
                    "[FishingRodUseOldVisual] Đã cất cần câu cũ.",
                    this
                );
            }

            return;
        }

        RestoreOldRodVisual();
        oldRodObject.SetActive(true);

        if (showDebugLogs)
        {
            Debug.Log(
                "[FishingRodUseOldVisual] " +
                "Đang dùng vật phẩm cần câu đã mua, " +
                "nhưng hiển thị bằng cần câu cũ.",
                this
            );
        }
    }

    private void RestoreOldRodVisual()
    {
        if (oldRodObject == null)
            return;

        if (oldRodRenderer != null &&
            oldRodSprite != null)
        {
            oldRodRenderer.sprite =
                oldRodSprite;

            oldRodRenderer.flipX = false;
            oldRodRenderer.flipY = false;
        }

        if (!restoreOldTransform)
            return;

        Transform rodTransform =
            oldRodObject.transform;

        rodTransform.localPosition =
            oldLocalPosition;

        rodTransform.localRotation =
            Quaternion.Euler(
                oldLocalEulerAngles
            );

        rodTransform.localScale =
            oldLocalScale;
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

        if (oldRodRenderer == null &&
            oldRodObject != null)
        {
            oldRodRenderer =
                oldRodObject
                    .GetComponentInChildren<
                        SpriteRenderer
                    >(true);
        }

        if (heldItemRenderer == null)
        {
            Transform[] children =
                transform.root
                    .GetComponentsInChildren<
                        Transform
                    >(true);

            foreach (Transform child
                     in children)
            {
                if (child == null ||
                    !string.Equals(
                        child.name,
                        "HeldItem",
                        StringComparison
                            .OrdinalIgnoreCase))
                {
                    continue;
                }

                heldItemRenderer =
                    child.GetComponentInChildren<
                        SpriteRenderer
                    >(true);

                break;
            }
        }
    }

    private void CacheOldVisualWhenMissing()
    {
        if (oldRodObject == null)
            return;

        if (oldRodRenderer == null)
        {
            oldRodRenderer =
                oldRodObject
                    .GetComponentInChildren<
                        SpriteRenderer
                    >(true);
        }

        if (oldRodSprite == null &&
            oldRodRenderer != null)
        {
            oldRodSprite =
                oldRodRenderer.sprite;
        }

        Transform rodTransform =
            oldRodObject.transform;

        if (oldLocalScale ==
            Vector3.zero)
        {
            oldLocalPosition =
                rodTransform.localPosition;

            oldLocalEulerAngles =
                rodTransform.localEulerAngles;

            oldLocalScale =
                rodTransform.localScale;
        }
    }

    private static string Normalize(
        string value)
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
                    .GetUnicodeCategory(
                        character
                    );

            if (category ==
                UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetterOrDigit(character))
            {
                builder.Append(character);
            }
        }

        return builder
            .ToString()
            .Normalize(
                NormalizationForm.FormC
            );
    }

    [ContextMenu(
        "CAPTURE CURRENT OLD ROD TRANSFORM"
    )]
    private void CaptureCurrentOldRodTransform()
    {
        if (oldRodObject == null)
            return;

        Transform rodTransform =
            oldRodObject.transform;

        oldLocalPosition =
            rodTransform.localPosition;

        oldLocalEulerAngles =
            rodTransform.localEulerAngles;

        oldLocalScale =
            rodTransform.localScale;

        if (oldRodRenderer != null)
        {
            oldRodSprite =
                oldRodRenderer.sprite;
        }

        Debug.Log(
            "[FishingRodUseOldVisual] " +
            "Đã lưu sprite và Transform hiện tại của cần câu cũ.",
            this
        );
    }
}
