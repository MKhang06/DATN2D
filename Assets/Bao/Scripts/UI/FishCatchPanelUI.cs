using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FishCatchPanelUI : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private Image fishImage;
    [SerializeField] private TMP_Text Message;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text kgText;
    [SerializeField] private Button acceptButton;
    [SerializeField] private Button denyButton;
    [SerializeField] private PlayerStats playerStats;

private string currentFishName;
private Sprite currentFishSprite;
private float currentKg;

[SerializeField] private int keepFishXP = 5;
[SerializeField] private int releaseFishXP = 10;

    private void Awake()
    {
        if (panel != null)
            panel.SetActive(false);

        acceptButton.onClick.AddListener(AcceptFish);
        denyButton.onClick.AddListener(DenyFish);
    }

    public void ShowFish(string fishName, Sprite fishSprite, float kg)
{
    currentFishName = fishName;
    currentFishSprite = fishSprite;
    currentKg = kg;

    if (nameText != null)
        nameText.text = fishName;

    if (kgText != null)
        kgText.text = kg.ToString("0.00") + " Kg";

    if (fishImage != null)
    {
        fishImage.enabled = true;
        fishImage.sprite = fishSprite;
    }

    panel.SetActive(true);
}

    private void AcceptFish()
{
    if (InventoryManager.Instance != null)
    {
        InventoryManager.Instance.AddItem(
            currentFishName,
            currentFishSprite,
            1
        );
    }

    if (playerStats != null)
        playerStats.AddXP(keepFishXP);

    panel.SetActive(false);
}

    private void DenyFish()
{
    if (playerStats != null)
        playerStats.AddXP(releaseFishXP);

    panel.SetActive(false);
}
}