using UnityEngine;
using UnityEngine.UI;

public class PlayerStatsUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerStats playerStats;

    [Header("UI")]
    [SerializeField] private Image healthFill;
    [SerializeField] private Image energyFill;
    [SerializeField] private Image staminaFill;

    private void Update()
    {
        if (playerStats == null)
            return;

        if (healthFill != null)
        {
            healthFill.fillAmount =
                playerStats.currentHealth /
                playerStats.maxHealth;
        }

        if (energyFill != null)
        {
            energyFill.fillAmount =
                playerStats.currentEnergy /
                playerStats.maxEnergy;
        }

        if (staminaFill != null)
        {
            staminaFill.fillAmount =
                playerStats.currentStamina /
                playerStats.maxStamina;
        }
    }
}