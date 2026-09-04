using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;
using Khang;

public class PlantingSystem : MonoBehaviour
{
    [Header("Cấu Hình Tilemap")]
    [SerializeField] private Tilemap farmTilemap;           // Kéo Tilemap 'FarmPlots' vào đây

    [Header("Danh Sách Dự Phòng (Nếu ItemData chưa gắn Prefab)")]
    [Tooltip("Thứ tự: 0-Bí ngô, 1-Cà tím, 2-Ớt, 3-Việt quất")]
    [SerializeField] private GameObject[] cropPrefabs;

    private Camera mainCamera;
    private Dictionary<Vector3Int, GameObject> plantedCrops = new Dictionary<Vector3Int, GameObject>();

    private void Awake()
    {
        mainCamera = Camera.main;
    }

    private void Update()
    {
        // Click chuột trái để gieo trồng theo ô Hotbar đang chọn
        if (Input.GetMouseButtonDown(0))
        {
            TryPlantCrop();
        }
    }

    private void TryPlantCrop()
    {
        // 1. Nếu click trúng UI thì không trồng
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
        if (farmTilemap == null) return;

        // 2. Lấy ô đang chọn từ Hotbar của InventoryManager
        if (InventoryManager.Instance == null) return;
        InventorySlot currentSlot = InventoryManager.Instance.SelectedSlot;

        if (currentSlot == null || currentSlot.IsEmpty || currentSlot.Item == null) return;

        // 3. Nhận diện Prefab cây trồng từ ItemData
        GameObject cropToPlant = GetCropPrefab(currentSlot.Item);
        if (cropToPlant == null) return; // Không phải hạt giống (VD: là rìu/cuốc) -> Không gieo

        // 4. Chuyển vị trí chuột sang ô Tilemap
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        Vector3Int cellPosition = farmTilemap.WorldToCell(mouseWorldPos);

        if (farmTilemap.HasTile(cellPosition))
        {
            // Dọn dẹp cây cũ đã bị thu hoạch
            if (plantedCrops.ContainsKey(cellPosition) && plantedCrops[cellPosition] == null)
            {
                plantedCrops.Remove(cellPosition);
            }

            // Nếu ô đất còn trống
            if (!plantedCrops.ContainsKey(cellPosition))
            {
                Vector3 spawnPosition = farmTilemap.GetCellCenterWorld(cellPosition);
                GameObject newCrop = Instantiate(cropToPlant, spawnPosition, Quaternion.identity);

                plantedCrops.Add(cellPosition, newCrop);

                // Trừ 1 hạt giống khỏi Hotbar
                currentSlot.RemoveAmount(1);

                GreenFieldQuestEvents.ReportCropPlanted();
            }
        }
    }

    // Cơ chế thông minh: Tự tìm Prefab từ ItemData hoặc so khớp tên
    private GameObject GetCropPrefab(ItemData item)
    {
        if (item == null) return null;

        // Ưu tiên 1: Lấy trực tiếp từ trường cropPrefab trong ItemData
        if (item.cropPrefab != null) return item.cropPrefab;

        // Ưu tiên 2: Tự động so khớp theo tên với danh sách cropPrefabs
        if (cropPrefabs != null && cropPrefabs.Length > 0)
        {
            string id = (item.itemID + " " + item.itemName + " " + item.name).ToLower();

            if (id.Contains("pumpkin") || id.Contains("bi")) return cropPrefabs[0];
            if (cropPrefabs.Length > 1 && (id.Contains("eggplant") || id.Contains("tim"))) return cropPrefabs[1];
            if (cropPrefabs.Length > 2 && (id.Contains("pepper") || id.Contains("papper") || id.Contains("ot"))) return cropPrefabs[2];
            if (cropPrefabs.Length > 3 && (id.Contains("blue") || id.Contains("quat"))) return cropPrefabs[3];
        }

        return null;
    }
}