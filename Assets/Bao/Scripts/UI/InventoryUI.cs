using UnityEngine;

public class InventoryUI : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject bagPanel;

    [Header("Hotbar UI")]
    [SerializeField] private InventorySlotUI[] hotbarUISlots;

    [Header("Bag UI")]
    [SerializeField] private InventorySlotUI[] bagUISlots;

    private bool bagOpen;

    private void Start()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged += RefreshUI;
            InventoryManager.Instance.OnSelectedHotbarChanged += OnHotbarChanged;
        }

        CloseBag();
        RefreshUI();
    }

    private void OnDestroy()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged -= RefreshUI;
            InventoryManager.Instance.OnSelectedHotbarChanged -= OnHotbarChanged;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.I))
        {
            ToggleBag();
        }
    }

    private void OnHotbarChanged(int index)
    {
        RefreshUI();
    }

    public void ToggleBag()
    {
        bagOpen = !bagOpen;

        if (bagPanel != null)
            bagPanel.SetActive(bagOpen);
    }

    public void OpenBag()
    {
        bagOpen = true;

        if (bagPanel != null)
            bagPanel.SetActive(true);
    }

    public void CloseBag()
    {
        bagOpen = false;

        if (bagPanel != null)
            bagPanel.SetActive(false);
    }

    public void RefreshUI()
    {
        if (InventoryManager.Instance == null)
            return;

        InventoryManager.InventorySlot[] hotbarSlots =
            InventoryManager.Instance.HotbarSlots;

        InventoryManager.InventorySlot[] bagSlots =
            InventoryManager.Instance.BagSlots;

        int selectedIndex =
            InventoryManager.Instance.SelectedHotbarIndex;

        for (int i = 0; i < hotbarUISlots.Length; i++)
        {
            if (i >= hotbarSlots.Length)
                continue;

            bool selected = i == selectedIndex;

            hotbarUISlots[i].SetSlot(
                hotbarSlots[i],
                selected
            );
        }

        for (int i = 0; i < bagUISlots.Length; i++)
        {
            if (i >= bagSlots.Length)
                continue;

            bagUISlots[i].SetSlot(
                bagSlots[i],
                false
            );
        }
    }

    public void MoveSelectedToBagButton()
    {
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.MoveSelectedHotbarToBag();
    }

    public void MoveBagSlotToHotbar(int bagIndex)
    {
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.MoveBagToHotbar(bagIndex);
    }
}