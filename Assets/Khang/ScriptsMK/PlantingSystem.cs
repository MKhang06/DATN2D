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

        public void SetSelectedCropIndex(int index)
        {
            if (cropPrefabs == null || cropPrefabs.Length == 0) return;
            selectedCropIndex = Mathf.Clamp(index, 0, cropPrefabs.Length - 1);
        }

        private void Awake()
        {
            mainCamera = Camera.main;
        }

        private void Update()
        {
            HandleInputSelection();

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

            if (cropPrefabs != null && cropPrefabs.Length > 0)
            {
                selectedCropIndex = Mathf.Clamp(selectedCropIndex, 0, cropPrefabs.Length - 1);
            }
            else
            {
                selectedCropIndex = 0;
            }
        }

        private void TryPlantCrop()
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
            if (farmTilemap == null || cropPrefabs == null || cropPrefabs.Length == 0) return;
            if (selectedCropIndex < 0 || selectedCropIndex >= cropPrefabs.Length) return;

            Vector3 mouseWorldPos = mainCamera != null
                ? mainCamera.ScreenToWorldPoint(Input.mousePosition)
                : Camera.main.ScreenToWorldPoint(Input.mousePosition);

            Vector3Int cellPosition = farmTilemap.WorldToCell(mouseWorldPos);
            if (!farmTilemap.HasTile(cellPosition)) return;

            if (plantedCrops.TryGetValue(cellPosition, out var existingCrop) && existingCrop != null)
            {
                return;
            }

            Vector3 spawnPosition = farmTilemap.GetCellCenterWorld(cellPosition);
            GameObject newCrop = Instantiate(cropPrefabs[selectedCropIndex], spawnPosition, Quaternion.identity);
            plantedCrops[cellPosition] = newCrop;
        }
    }
}