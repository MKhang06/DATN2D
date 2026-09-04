using UnityEngine;

namespace Khang
{
    public class InventoryButton : MonoBehaviour
    {
        public void ToggleInventory()
        {
            if (InventoryManager.Instance == null)
            {
                Debug.LogWarning("InventoryManager chưa khởi tạo. Vui lòng kiểm tra scene và gán component.");
                return;
            }

            InventoryManager.Instance.ToggleInventory();
        }

        public void OpenInventory()
        {
            if (InventoryManager.Instance == null)
            {
                Debug.LogWarning("InventoryManager chưa khởi tạo. Vui lòng kiểm tra scene và gán component.");
                return;
            }

            InventoryManager.Instance.OpenInventory();
        }

        public void CloseInventory()
        {
            if (InventoryManager.Instance == null)
            {
                Debug.LogWarning("InventoryManager chưa khởi tạo. Vui lòng kiểm tra scene và gán component.");
                return;
            }

            InventoryManager.Instance.CloseInventory();
        }
    }
}
