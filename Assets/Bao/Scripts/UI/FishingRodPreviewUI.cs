using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FishingRodPreviewUI : MonoBehaviour
{
    [Header("Fishing Gear")]
    [SerializeField] private FishingGearStats gearStats;

    [Header("Rod Preview")]
    [SerializeField] private Image rodImage;
    [SerializeField] private Image rodGlow;

    [Header("Rod Information")]
    [SerializeField] private TMP_Text rodNameText;
    [SerializeField] private TMP_Text rodStatusText;

    [Header("Default Rod")]
    [SerializeField] private Sprite defaultRodSprite;
    [SerializeField] private string defaultRodName = "CẦN CÂU CƠ BẢN";

    [Header("Image Colors")]
    [SerializeField] private Color equippedRodColor =
        Color.white;

    [SerializeField] private Color unequippedRodColor =
        new Color(1f, 1f, 1f, 0.35f);

    [SerializeField] private Color completedGlowColor =
        new Color(0f, 1f, 1f, 0.2f);

    [SerializeField] private Color incompleteGlowColor =
        new Color(1f, 1f, 1f, 0.05f);

    [Header("Status Colors")]
    [SerializeField] private string completedColor = "#5DFFB2";
    [SerializeField] private string incompleteColor = "#A8B9B8";

    private bool subscribed;

    private void Awake()
    {
        FindGearStats();
    }

    private void OnEnable()
    {
        FindGearStats();
        SubscribeEvents();
        RefreshPreview();
    }

    private void OnDisable()
    {
        UnsubscribeEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeEvents();
    }

    private void FindGearStats()
    {
        if (gearStats != null)
            return;

        gearStats = FindFirstObjectByType<FishingGearStats>();
    }

    private void SubscribeEvents()
    {
        if (gearStats == null || subscribed)
            return;

        gearStats.OnGearChanged += RefreshPreview;
        subscribed = true;
    }

    private void UnsubscribeEvents()
    {
        if (gearStats == null || !subscribed)
            return;

        gearStats.OnGearChanged -= RefreshPreview;
        subscribed = false;
    }

    public void RefreshPreview()
    {
        if (gearStats == null)
        {
            ShowDefaultPreview();
            return;
        }

        FishingEquipmentData equippedReel =
            gearStats.GetEquipped(FishingPartType.Reel);

        FishingEquipmentData equippedLine =
            gearStats.GetEquipped(FishingPartType.Line);

        FishingEquipmentData equippedHook =
            gearStats.GetEquipped(FishingPartType.Hook);

        FishingEquipmentData equippedBait =
            gearStats.GetEquipped(FishingPartType.Bait);

        Sprite previewSprite = defaultRodSprite;
        string previewName = defaultRodName;

        if (equippedReel != null)
        {
            if (!string.IsNullOrEmpty(equippedReel.itemName))
                previewName = equippedReel.itemName;

            if (equippedReel.rodPreviewSprite != null)
                previewSprite = equippedReel.rodPreviewSprite;
        }

        int equippedCount = CountEquippedParts(
            equippedReel,
            equippedLine,
            equippedHook,
            equippedBait
        );

        bool hasRod = equippedReel != null;
        bool fullyEquipped = equippedCount >= 4;

        UpdateRodImages(
            previewSprite,
            hasRod,
            fullyEquipped
        );

        UpdateRodTexts(
            previewName,
            equippedCount,
            fullyEquipped
        );
    }

    private int CountEquippedParts(
        FishingEquipmentData reel,
        FishingEquipmentData line,
        FishingEquipmentData hook,
        FishingEquipmentData bait)
    {
        int count = 0;

        if (reel != null)
            count++;

        if (line != null)
            count++;

        if (hook != null)
            count++;

        if (bait != null)
            count++;

        return count;
    }

    private void UpdateRodImages(
        Sprite previewSprite,
        bool hasRod,
        bool fullyEquipped)
    {
        if (rodImage != null)
        {
            rodImage.sprite = previewSprite;
            rodImage.enabled = previewSprite != null;
            rodImage.preserveAspect = true;

            rodImage.color = hasRod
                ? equippedRodColor
                : unequippedRodColor;
        }

        if (rodGlow != null)
        {
            rodGlow.sprite = previewSprite;
            rodGlow.enabled = previewSprite != null;
            rodGlow.preserveAspect = true;

            rodGlow.color = fullyEquipped
                ? completedGlowColor
                : incompleteGlowColor;
        }
    }

    private void UpdateRodTexts(
        string previewName,
        int equippedCount,
        bool fullyEquipped)
    {
        if (rodNameText != null)
        {
            if (string.IsNullOrEmpty(previewName))
                previewName = defaultRodName;

            rodNameText.text = previewName.ToUpper();
        }

        if (rodStatusText == null)
            return;

        if (fullyEquipped)
        {
            rodStatusText.text =
                "<color=" +
                completedColor +
                ">ĐÃ LẮP ĐẦY ĐỦ PHỤ KIỆN</color>";
        }
        else
        {
            rodStatusText.text =
                "<color=" +
                incompleteColor +
                ">Đã trang bị " +
                equippedCount +
                "/4 phụ kiện</color>";
        }
    }

    private void ShowDefaultPreview()
    {
        if (rodImage != null)
        {
            rodImage.sprite = defaultRodSprite;
            rodImage.enabled = defaultRodSprite != null;
            rodImage.preserveAspect = true;
            rodImage.color = unequippedRodColor;
        }

        if (rodGlow != null)
        {
            rodGlow.sprite = defaultRodSprite;
            rodGlow.enabled = defaultRodSprite != null;
            rodGlow.preserveAspect = true;
            rodGlow.color = incompleteGlowColor;
        }

        if (rodNameText != null)
            rodNameText.text = defaultRodName;

        if (rodStatusText != null)
        {
            rodStatusText.text =
                "<color=" +
                incompleteColor +
                ">Chưa trang bị phụ kiện</color>";
        }
    }
}
