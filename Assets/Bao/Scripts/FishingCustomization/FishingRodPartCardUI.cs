using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class FishingRodPartCardUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text amountText;
    [SerializeField] private TMP_Text statsText;
    [SerializeField] private Button actionButton;
    [SerializeField] private TMP_Text actionButtonText;
    [SerializeField] private GameObject equippedMarker;

    private FishingRodPartDefinition definition;
    private Action<FishingRodPartDefinition> onAction;

    public void Setup(
        FishingRodPartDefinition partDefinition,
        int ownedAmount,
        bool isEquipped,
        Action<FishingRodPartDefinition> actionCallback)
    {
        definition = partDefinition;
        onAction = actionCallback;

        if (definition == null)
            return;

        if (iconImage != null)
        {
            iconImage.sprite = definition.Icon;
            iconImage.enabled = definition.Icon != null;
            iconImage.preserveAspect = true;
        }

        if (nameText != null)
            nameText.text = definition.ItemName;

        if (amountText != null)
        {
            amountText.text =
                definition.SlotType == FishingRodPartSlotType.Bait
                    ? "Sở hữu: x" + Mathf.Max(0, ownedAmount)
                    : "Đã sở hữu";
        }

        if (statsText != null)
            statsText.text = BuildStatsText(definition);

        if (actionButtonText != null)
        {
            actionButtonText.text =
                isEquipped
                    ? "GỠ"
                    : "TRANG BỊ";
        }

        if (equippedMarker != null)
            equippedMarker.SetActive(isEquipped);

        if (actionButton != null)
        {
            actionButton.onClick.RemoveListener(HandleAction);
            actionButton.onClick.AddListener(HandleAction);
            actionButton.interactable = true;
        }
    }

    private void HandleAction()
    {
        if (definition != null)
            onAction?.Invoke(definition);
    }

    private static string BuildStatsText(
        FishingRodPartDefinition definition)
    {
        List<string> lines = new List<string>
        {
            "Kỹ năng yêu cầu: " + definition.RequiredSkill
        };

        if (Mathf.Abs(definition.StrengthBonus) > 0.001f)
        {
            lines.Add(
                "Sức mạnh: " +
                FormatSigned(definition.StrengthBonus)
            );
        }

        if (Mathf.Abs(definition.SpeedBonus) > 0.001f)
        {
            lines.Add(
                "Tốc độ: " +
                FormatSigned(definition.SpeedBonus)
            );
        }

        if (Mathf.Abs(definition.MaxDepthBonus) > 0.001f)
        {
            lines.Add(
                "Độ sâu: " +
                FormatSigned(definition.MaxDepthBonus) +
                "m"
            );
        }

        if (definition.SlotType == FishingRodPartSlotType.Bait)
            lines.Add("Tiêu hao 1 mồi mỗi lần câu");

        return string.Join("\n", lines);
    }

    private static string FormatSigned(float value)
    {
        return value >= 0f
            ? "+" + value.ToString("0.##")
            : value.ToString("0.##");
    }

    private void OnDestroy()
    {
        if (actionButton != null)
            actionButton.onClick.RemoveListener(HandleAction);
    }
}
