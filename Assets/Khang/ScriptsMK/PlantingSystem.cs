using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;

namespace Khang
{
    public class PlantingSystem : MonoBehaviour
    {
        [Header("Cấu Hình Tilemap")]
        [SerializeField] private Tilemap farmTilemap;           // Tilemap 'FarmPlots'

        [Header("Danh Sách Các Prefab Cây Trồng")]
        [SerializeField] private GameObject[] cropPrefabs;      // Mảng chứa các loại cây

        private int selectedCropIndex = 0;                      // Cây đang chọn
        private Camera mainCamera;

        private Dictionary<Vector3Int, GameObject> plantedCrops = new Dictionary<Vector3Int, GameObject>();

        private void Awake()
        {
            mainCamera = Camera.main;
        }

        private void Update()
        {
            HandleInputSelection();

            // Click chuột trái để trồng
            if (Input.GetMouseButtonDown(0))
            {
                TryPlantCrop();
            }
        }

        private void HandleInputSelection()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) selectedCropIndex = 0;
            if (Input.GetKeyDown(KeyCode.Alpha2)) selectedCropIndex = 1;
            if (Input.GetKeyDown(KeyCode.Alpha3)) selectedCropIndex = 2;
            if (Input.GetKeyDown(KeyCode.Alpha4)) selectedCropIndex = 3;
            if (Input.GetKeyDown(KeyCode.Alpha5)) selectedCropIndex = 4;
        }

        private void TryPlantCrop()
        {
            // Bỏ qua nếu đang tương tác trên UI
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

            if (farmTilemap == null || cropPrefabs == null || cropPrefabs.Length == 0) return;
            if (selectedCropIndex >= cropPrefabs.Length || cropPrefabs[selectedCropIndex] == null) return;

            Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            Vector3Int cellPosition = farmTilemap.WorldToCell(mouseWorldPos);

            if (farmTilemap.HasTile(cellPosition))
            {
                // Dọn dẹp ô đất nếu cây đã bị thu hoạch/xóa
                if (plantedCrops.ContainsKey(cellPosition) && plantedCrops[cellPosition] == null)
                {
                    plantedCrops.Remove(cellPosition);
                }

                if (!plantedCrops.ContainsKey(cellPosition))
                {
                    Vector3 spawnPosition = farmTilemap.GetCellCenterWorld(cellPosition);
                    GameObject newCrop = Instantiate(cropPrefabs[selectedCropIndex], spawnPosition, Quaternion.identity);
                    plantedCrops.Add(cellPosition, newCrop);
                }
            }
        }
    }
}