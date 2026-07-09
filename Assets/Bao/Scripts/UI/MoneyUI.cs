using TMPro;
using UnityEngine;

public class MoneyUI : MonoBehaviour
{
    [SerializeField] private TMP_Text moneyText;
    [SerializeField] private PlayerStats playerStats;

    private void Awake()
    {
        if (playerStats == null)
            playerStats = FindFirstObjectByType<PlayerStats>();
    }

    private void Update()
    {
        if (moneyText == null || playerStats == null)
            return;

        moneyText.text = " " + playerStats.Money;
    }
}