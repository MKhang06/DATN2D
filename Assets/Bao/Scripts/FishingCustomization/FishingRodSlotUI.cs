using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class FishingRodSlotUI : MonoBehaviour
{
    [Header("Loại ô")]
    [SerializeField]
    private FishingRodPartSlotType slotType;

    [Header("UI")]
    [SerializeField] private Button slotButton;
    [SerializeField] private Image itemIcon;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text itemNameText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private GameObject equippedMarker;

    [Header("Tên mặc định")]
    [SerializeField] private string customDefaultTitle;

    private FishingRodLoadout loadout;
    private Action<FishingRodPartSlotType> onClicked;

    public FishingRodPartSlotType SlotType => slotType;

    private void Reset()
    {
        slotButton = GetComponent<Button>();

        if (slotButton == null)
            slotButton = gameObject.AddComponent<Button>();
    }

    public void Bind(
        FishingRodLoadout targetLoadout,
        Action<FishingRodPartSlotType> clickCallback)
    {
        loadout = targetLoadout;
        onClicked = clickCallback;

        if (slotButton == null)
            slotButton = GetComponent<Button>();

        if (slotButton != null)
        {
            slotButton.onClick.RemoveListener(HandleClick);
            slotButton.onClick.AddListener(HandleClick);
            slotButton.interactable = true;
        }

        Refresh();
    }

    public void Refresh()
    {
        FishingRodPartDefinition equipped =
            loadout != null
                ? loadout.GetEquipped(slotType)
                : null;

        if (titleText != null)
        {
            titleText.text =
                string.IsNullOrWhiteSpace(customDefaultTitle)
                    ? GetDefaultTitle(slotType)
                    : customDefaultTitle;
        }

        if (itemIcon != null)
        {
            Sprite sprite =
                equipped != null
                    ? equipped.Icon
                    : null;

            itemIcon.sprite = sprite;
            itemIcon.enabled = sprite != null;
            itemIcon.preserveAspect = true;
        }

        if (itemNameText != null)
        {
            itemNameText.text =
                equipped != null
                    ? equipped.ItemName
                    : "Chưa trang bị";
        }

        if (statusText != null)
        {
            if (equipped == null)
            {
                statusText.text = "Chưa trang bị";
            }
            else if (slotType == FishingRodPartSlotType.Bait)
            {
                int amount =
                    loadout != null
                        ? loadout.GetOwnedAmount(equipped)
                        : 0;

                statusText.text =
                    "Đã trang bị x" + amount;
            }
            else
            {
                statusText.text = "Đã trang bị";
            }
        }

        if (equippedMarker != null)
            equippedMarker.SetActive(equipped != null);
    }

    private void HandleClick()
    {
        onClicked?.Invoke(slotType);
    }

    private static string GetDefaultTitle(
        FishingRodPartSlotType type)
    {
        switch (type)
        {
            case FishingRodPartSlotType.Reel:
                return "MÁY CÂU";
            case FishingRodPartSlotType.Line:
                return "DÂY CÂU";
            case FishingRodPartSlotType.Hook:
                return "LƯỠI CÂU";
            case FishingRodPartSlotType.Bait:
                return "MỒI CÂU";
            default:
                return "TRANG BỊ";
        }
    }

    private void OnDestroy()
    {
        if (slotButton != null)
            slotButton.onClick.RemoveListener(HandleClick);
    }
}
