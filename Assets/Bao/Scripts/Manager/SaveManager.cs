using System.IO;
using UnityEngine;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance;

    [Header("References")]
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private Transform player;

    private string SavePath =>
        Path.Combine(Application.persistentDataPath, "save.json");

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F5))
            SaveGame();

        if (Input.GetKeyDown(KeyCode.F9))
            LoadGame();
    }

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        LoadGame();
    }

    private void OnApplicationQuit()
    {
        SaveGame();
    }

    public void SaveGame()
    {
        SaveData data = new SaveData();

        SavePlayer(data);
        SaveTime(data);
        SaveFarmTiles(data);
        SaveAnimals(data);
        SaveBuildings(data);

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(SavePath, json);

        Debug.Log("Đã lưu game: " + SavePath);
    }

    public void LoadGame()
    {
        if (!File.Exists(SavePath))
        {
            Debug.Log("Chưa có file save.");
            return;
        }

        string json = File.ReadAllText(SavePath);
        SaveData data = JsonUtility.FromJson<SaveData>(json);

        LoadPlayer(data);
        LoadTime(data);
        LoadFarmTiles(data);
        LoadAnimals(data);
        LoadBuildings(data);

        Debug.Log("Đã tải game.");
    }

    private void SavePlayer(SaveData data)
    {
        if (playerStats != null)
        {
            data.gold = playerStats.Money;
            data.stamina = playerStats.CurrentStamina;
            data.level = playerStats.Level;
        }

        if (player != null)
            data.playerPosition = player.position;
    }

    private void LoadPlayer(SaveData data)
    {
        if (playerStats != null)
        {
            playerStats.SetMoney(data.gold);
            playerStats.SetStamina(data.stamina);
            playerStats.SetLevel(data.level);
        }

        if (player != null)
            player.position = data.playerPosition;
    }

    private void SaveTime(SaveData data)
    {
        if (GameTimeManager.Instance == null) return;

        data.currentDay = GameTimeManager.Instance.CurrentDay;
        data.currentHour = GameTimeManager.Instance.CurrentHour;
        data.currentMinute = GameTimeManager.Instance.CurrentMinute;
        data.currentSeason = GameTimeManager.Instance.CurrentSeason;
    }

    private void LoadTime(SaveData data)
    {
        if (GameTimeManager.Instance == null) return;

        GameTimeManager.Instance.SetTime(
            data.currentDay,
            data.currentHour,
            data.currentMinute,
            data.currentSeason
        );
    }

    private void SaveFarmTiles(SaveData data)
    {
        FarmTile[] tiles = FindObjectsOfType<FarmTile>();

        foreach (FarmTile tile in tiles)
        {
            data.farmTiles.Add(tile.GetSaveData());
        }
    }

    private void LoadFarmTiles(SaveData data)
    {
        FarmTile[] tiles = FindObjectsOfType<FarmTile>();

        foreach (FarmTile tile in tiles)
        {
            foreach (FarmTileSaveData tileData in data.farmTiles)
            {
                if (tile.gridX == tileData.gridX &&
                    tile.gridY == tileData.gridY)
                {
                    tile.LoadSaveData(tileData);
                    break;
                }
            }
        }
    }

    private void SaveAnimals(SaveData data)
    {
        SaveableAnimal[] animals = FindObjectsOfType<SaveableAnimal>();

        foreach (SaveableAnimal animal in animals)
            data.animals.Add(animal.GetSaveData());
    }

    private void LoadAnimals(SaveData data)
    {
        SaveableAnimal[] animals = FindObjectsOfType<SaveableAnimal>();

        foreach (SaveableAnimal animal in animals)
        {
            foreach (AnimalSaveData animalData in data.animals)
            {
                if (animal.AnimalID == animalData.animalID)
                {
                    animal.LoadSaveData(animalData);
                    break;
                }
            }
        }
    }

    private void SaveBuildings(SaveData data)
    {
        SaveableBuilding[] buildings = FindObjectsOfType<SaveableBuilding>();

        foreach (SaveableBuilding building in buildings)
            data.buildings.Add(building.GetSaveData());
    }

    private void LoadBuildings(SaveData data)
    {
        SaveableBuilding[] buildings = FindObjectsOfType<SaveableBuilding>();

        foreach (SaveableBuilding building in buildings)
        {
            foreach (BuildingSaveData buildingData in data.buildings)
            {
                if (building.BuildingID == buildingData.buildingID)
                {
                    building.LoadSaveData(buildingData);
                    break;
                }
            }
        }
    }
}