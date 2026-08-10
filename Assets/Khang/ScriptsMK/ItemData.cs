using UnityEngine;

namespace Khang
{
    [CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item Data")]
    public class ItemData : ScriptableObject
    {
        public string itemID;             // ID vật phẩm (VD: tomato, pumpkin)
        public string itemName;           // Tên hiển thị
        public Sprite icon;               // Icon hình ảnh quả
        public bool isStackable = true;   // Cho phép cộng dồn số lượng
    }
}