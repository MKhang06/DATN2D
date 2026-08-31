using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;

public class PlantingSystem : MonoBehaviour
{
    [Header("Cấu Hình Tilemap")]
    [SerializeField] private Tilemap farmTilemap;           // Kéo Tilemap 'FarmPlots' vào đây

    [Header("Danh Sách 4 Cây Trồng")]
    [Tooltip("Gồm 4 Prefab: 1-Bí ngô, 2-Cà tím, 3-Ớt, 4-Việt quất")]
    [SerializeField] private GameObject[] cropPrefabs;      // Mảng chứa 4 loại cây

    private int selectedCropIndex = 0;                      // Vị trí cây đang chọn (Mặc định: 0 - Bí ngô)
    private Camera mainCamera;

    // TỐI ƯU: Lưu danh sách tọa độ các ô đã trồng cây vào Dictionary (Nhanh và chính xác hơn kiểm tra Collider)
    private Dictionary<Vector3Int, GameObject> plantedCrops = new Dictionary<Vector3Int, GameObject>();

    private void Awake()
    {
        // Cache camera chính từ đầu
        mainCamera = Camera.main;
    }

    private void Update()
    {
        // Chọn loại hạt giống bằng phím 1, 2, 3, 4
        HandleInputSelection();

        // Click chuột trái để trồng
        if (Input.GetMouseButtonDown(0))
        {
            TryPlantCrop();
        }
    }

    private void HandleInputSelection()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) selectedCropIndex = 0; // Bí ngô
        if (Input.GetKeyDown(KeyCode.Alpha2)) selectedCropIndex = 1; // Cà tím
        if (Input.GetKeyDown(KeyCode.Alpha3)) selectedCropIndex = 2; // Ớt
        if (Input.GetKeyDown(KeyCode.Alpha4)) selectedCropIndex = 3; // Việt quất
    }

    private void TryPlantCrop()
    {
        // 1. Không trồng cây nếu người chơi đang bấm vào menu / nút UI
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        if (farmTilemap == null || cropPrefabs == null || cropPrefabs.Length == 0) return;
        if (selectedCropIndex >= cropPrefabs.Length || cropPrefabs[selectedCropIndex] == null) return;

        // 2. Chuyển vị trí chuột sang tọa độ ô lưới Tilemap
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        Vector3Int cellPosition = farmTilemap.WorldToCell(mouseWorldPos);

        // 3. Kiểm tra ô click vào có phải là ô đất ruộng FarmPlots không
        if (farmTilemap.HasTile(cellPosition))
        {
            // Tự động dọn dẹp các cây đã bị xóa/thu hoạch khỏi danh sách lưu trữ
            if (plantedCrops.ContainsKey(cellPosition) && plantedCrops[cellPosition] == null)
            {
                plantedCrops.Remove(cellPosition);
            }

            // 4. Nếu ô đất này trống chưa trồng cây nào
            if (!plantedCrops.ContainsKey(cellPosition))
            {
                // Lấy tọa độ chính giữa ô vuông đất
                Vector3 spawnPosition = farmTilemap.GetCellCenterWorld(cellPosition);
                
                // Sinh ra cây trồng mới
                GameObject newCrop = Instantiate(cropPrefabs[selectedCropIndex], spawnPosition, Quaternion.identity);

                // Lưu vết ô đất đã được trồng
                plantedCrops.Add(cellPosition, newCrop);

                GreenFieldQuestEvents.ReportCropPlanted();
            }
        }
    }
}
