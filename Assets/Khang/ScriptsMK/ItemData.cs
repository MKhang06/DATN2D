using UnityEngine;

namespace Khang
{
    [CreateAssetMenu(fileName = "ItemData_New", menuName = "Khang/Item Data")]
    public class ItemData : ScriptableObject
    {
        public string itemID;             // ID vật phẩm (VD: pumpkin, eggplant)
        public string itemName;           // Tên hiển thị
        public Sprite icon;               // Icon hình ảnh quả / hạt giống
        public bool isStackable = true;   // Cho phép cộng dồn số lượng

        [Header("Liên Kết Trồng Cây")]
        [Tooltip("Kéo Prefab cây trồng tương ứng vào đây (chỉ điền nếu là hạt giống)")]
        public GameObject cropPrefab;     
    }
}