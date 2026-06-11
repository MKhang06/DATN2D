using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ShopManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject shopPanel;
    [SerializeField] private PlayerStats playerStats;

    [Header("Carrot Seed")]
    [SerializeField] private int carrotSeedPrice = 20;
    [SerializeField] private TMP_Text carrotPriceText;

    [Header("Potato Seed")]
    [SerializeField] private int potatoSeedPrice = 30;
    [SerializeField] private TMP_Text potatoPriceText;

    private void Start()
    {
        shopPanel.SetActive(false);

        if (carrotPriceText != null)
            carrotPriceText.text = carrotSeedPrice + "G";

        if (potatoPriceText != null)
            potatoPriceText.text = potatoSeedPrice + "G";
    }

    public void OpenShop()
    {
        shopPanel.SetActive(true);
    }

    public void CloseShop()
    {
        shopPanel.SetActive(false);
    }

    public void BuyCarrotSeed()
    {
        BuyItem("Carrot Seed", carrotSeedPrice);
    }

    public void BuyPotatoSeed()
    {
        BuyItem("Potato Seed", potatoSeedPrice);
    }

    private void BuyItem(string itemName, int price)
    {
        if (playerStats == null)
        {
            Debug.LogError("Chưa kéo PlayerStats vào ShopManager!");
            return;
        }

        if (!playerStats.SpendMoney(price))
        {
            Debug.Log("Không đủ tiền mua " + itemName);
            return;
        }

        Debug.Log("Đã mua: " + itemName);

        // Sau này nối Inventory ở đây:
        // inventory.AddItem(itemName, 1);
    }
}