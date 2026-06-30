using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class XPUI : MonoBehaviour
{
    [SerializeField] private PlayerStats playerStats;

    [SerializeField] private Image xpFill;
    [SerializeField] private TMP_Text levelText;

    private void Update()
    {
        xpFill.fillAmount =
            (float)playerStats.CurrentXP /
            playerStats.RequiredXP;

        levelText.text =
            "Lv." + playerStats.Level;
    }
}