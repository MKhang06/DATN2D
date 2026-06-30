using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance;

    [Header("Inventory")]
    [SerializeField] private List<InventoryItem> items = new List<InventoryItem>();
    [SerializeField] private InventorySlotUI[] slots;

    [Header("Money UI")]
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private TMP_Text moneyText;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        RefreshUI();
    }

    private void Update()
    {
        UpdateMoneyUI();

        // Test nhanh
        if (Input.GetKeyDown(KeyCode.Alpha1))
            AddItem("Cá xanh", null, 1);

        if (Input.GetKeyDown(KeyCode.Alpha2))
            AddItem("Cá vàng", null, 1);
    }

    public void AddItem(string itemName, Sprite icon, int amount)
    {
        if (amount <= 0) return;

        foreach (InventoryItem item in items)
        {
            if (item.itemName == itemName)
            {
                item.amount += amount;
                RefreshUI();
                return;
            }
        }

        items.Add(new InventoryItem(itemName, icon, amount));
        RefreshUI();
    }

    public bool RemoveItem(string itemName, int amount)
    {
        InventoryItem item = items.Find(x => x.itemName == itemName);

        if (item == null || item.amount < amount)
            return false;

        item.amount -= amount;

        if (item.amount <= 0)
            items.Remove(item);

        RefreshUI();
        return true;
    }

    private void RefreshUI()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null) continue;

            if (i < items.Count)
                slots[i].SetItem(items[i].icon, items[i].amount);
            else
                slots[i].Clear();
        }
    }

    private void UpdateMoneyUI()
    {
        if (moneyText == null || playerStats == null)
            return;

        moneyText.text = "Gold: " + playerStats.Money;
    }
}