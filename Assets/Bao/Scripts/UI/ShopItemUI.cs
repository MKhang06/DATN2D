using TMPro;
using UnityEngine;

public class ShopItemUI : MonoBehaviour
{
    [Header("Item")]
    public string itemName;
    public int basePrice = 5;

    [Header("Runtime")]
    public int price;
    public int quantity;

    [Header("UI")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private TMP_Text quantityText;

    public int Quantity => quantity;
    public int TotalPrice => price * quantity;

    private void Start()
    {
        price = basePrice;
        RefreshUI();
    }

    public void RandomizePrice()
    {
        float multiplier = Random.Range(0.8f, 1.3f);
        price = Mathf.Max(1, Mathf.RoundToInt(basePrice * multiplier));

        RefreshUI();
    }

    public void Add()
    {
        quantity++;
        RefreshUI();
    }

    public void Remove()
    {
        quantity = Mathf.Max(0, quantity - 1);
        RefreshUI();
    }

    public void Clear()
    {
        quantity = 0;
        RefreshUI();
    }

    public void RefreshUI()
    {
        if (nameText != null)
            nameText.text = itemName;

        if (priceText != null)
            priceText.text = price + "G";

        if (quantityText != null)
            quantityText.text = quantity.ToString();
    }
}