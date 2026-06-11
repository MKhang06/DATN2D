using TMPro;
using UnityEngine;

public class MoneyUI : MonoBehaviour
{
    [SerializeField] private TMP_Text moneyText;
    [SerializeField] private PlayerStats playerStats;

    private void Update()
    {
        moneyText.text =
            "🪙 " + playerStats.Money;
    }
}