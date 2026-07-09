using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance {get; private set;}
    public List<Item> items = new List<Item>();
    public Transform itemHolder;
    public GameObject intemPrefab;
    private void Awake()
    {
        if(Instance != null || Instance != this)
        {
            Destroy(Instance);
        }
        
            Instance = this;
    }
    public void Add(Item item)
    {
        items.Add(item);
    }
    public void DisplayInventory()
{
    foreach (Item item in items)
    {
        GameObject obj = Instantiate(intemPrefab, itemHolder);

        var itemName = obj.transform.Find("ItemName").GetComponent<TextMeshProUGUI>();
        var itemImage = obj.transform.Find("ItemImage").GetComponent<Image>();

        itemName.text = item.itemName;
        itemImage.sprite = item.imgage;
    }
}
}