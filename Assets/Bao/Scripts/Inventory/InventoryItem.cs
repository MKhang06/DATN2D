using UnityEngine;

[System.Serializable]
public class InventoryItem
{
    public string itemName;
    public Sprite icon;
    public int amount;

    public InventoryItem(string itemName, Sprite icon, int amount)
    {
        this.itemName = itemName;
        this.icon = icon;
        this.amount = amount;
    }
}