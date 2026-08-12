using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class XPUI : MonoBehaviour
{
    [SerializeField] private PlayerStats playerStats;

    [SerializeField] private Image xpFill;
    [SerializeField] private TMP_Text levelText;

    private void Awake()
    {
        if (playerStats == null)
            playerStats = FindFirstObjectByType<PlayerStats>();
    }

    private void Update()
    {
        if (playerStats == null)
            return;

        if (xpFill != null)
        {
            xpFill.fillAmount = playerStats.RequiredXP <= 0
                ? 0f
                : Mathf.Clamp01((float)playerStats.CurrentXP / playerStats.RequiredXP);
        }

        if (levelText != null)
            levelText.text = "Lv." + Mathf.Max(1, playerStats.Level);
    }
}
