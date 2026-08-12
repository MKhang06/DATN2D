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

    private void Awake()
    {
        if (playerStats == null)
            playerStats = FindFirstObjectByType<PlayerStats>();
    }

    private void Update()
    {
        if (playerStats == null)
            return;

        if (healthFill != null)
        {
            healthFill.fillAmount = SafeRatio(
                playerStats.currentHealth,
                playerStats.maxHealth
            );
        }

        if (energyFill != null)
        {
            energyFill.fillAmount = SafeRatio(
                playerStats.currentEnergy,
                playerStats.maxEnergy
            );
        }

        if (staminaFill != null)
        {
            staminaFill.fillAmount = SafeRatio(
                playerStats.currentStamina,
                playerStats.maxStamina
            );
        }
    }

    private static float SafeRatio(float current, float maximum)
    {
        return maximum <= 0f ? 0f : Mathf.Clamp01(current / maximum);
    }
}
