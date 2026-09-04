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
        [Tooltip("Thứ tự: 0-Bí ngô (Z), 1-Cà tím (X), 2-Ớt (C), 3-Việt quất (V)")]
        [SerializeField] private GameObject[] cropPrefabs;

        private int selectedCropIndex = 0;
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
            // Chọn mầm cây bằng phím Z, X, C, V
            if (Input.GetKeyDown(KeyCode.Z)) selectedCropIndex = 0;
            if (Input.GetKeyDown(KeyCode.X)) selectedCropIndex = 1;
            if (Input.GetKeyDown(KeyCode.C)) selectedCropIndex = 2;
            if (Input.GetKeyDown(KeyCode.V)) selectedCropIndex = 3;

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
            if (selectedCropIndex < 0 || selectedCropIndex >= cropPrefabs.Length || cropPrefabs[selectedCropIndex] == null) return;

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