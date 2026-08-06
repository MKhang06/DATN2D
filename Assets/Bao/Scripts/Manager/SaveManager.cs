using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SaveManager : MonoBehaviour
{
    private const int CurrentSaveVersion = 2;
    private const string SaveFileName = "save.json";
    private const string BackupFileName = "save.backup.json";
    private const string TemporaryFileName = "save.tmp.json";

    public static SaveManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private Transform player;
    [SerializeField] private SaveLoadNotificationUI notificationUI;

    [Header("Auto Save")]
    [SerializeField] private bool saveOnApplicationQuit = true;

    private static bool loadOnNextGameplayScene;
    private bool readyForAutoSave;

    private static string SavePath =>
        Path.Combine(Application.persistentDataPath, SaveFileName);

    private static string BackupPath =>
        Path.Combine(Application.persistentDataPath, BackupFileName);

    private static string TemporaryPath =>
        Path.Combine(Application.persistentDataPath, TemporaryFileName);

    public static string SaveFilePath => SavePath;

    public static bool HasSaveGame =>
        File.Exists(SavePath) || File.Exists(BackupPath);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRuntimeState()
    {
        loadOnNextGameplayScene = false;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        ResolveReferences();
    }

    private IEnumerator Start()
    {
        if (loadOnNextGameplayScene)
        {
            loadOnNextGameplayScene = false;

            // Chờ toàn bộ Awake/Start của gameplay hoàn tất trước khi phục hồi dữ liệu.
            yield return null;
            LoadGame();
        }

        readyForAutoSave = true;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void OnApplicationQuit()
    {
        if (saveOnApplicationQuit &&
            readyForAutoSave &&
            gameObject.scene.isLoaded)
        {
            TrySaveGame(out _);
        }
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused && saveOnApplicationQuit && readyForAutoSave)
            TrySaveGame(out _);
    }

    public static void RequestLoadOnNextGameplayScene()
    {
        loadOnNextGameplayScene = HasSaveGame;
    }

    public static void BeginNewGame()
    {
        loadOnNextGameplayScene = false;
        DeleteSaveGame();
        ClearGameplayPlayerPrefs();
    }

    public static void DeleteSaveGame()
    {
        DeleteIfExists(SavePath);
        DeleteIfExists(BackupPath);
        DeleteIfExists(TemporaryPath);
    }

    public void SaveGame()
    {
        TrySaveGame(out string message);
        ShowStatus(message);
    }

    public void LoadGame()
    {
        TryLoadGame(out string message);
        ShowStatus(message);
    }

    public bool TrySaveGame(out string message)
    {
        ResolveReferences();

        try
        {
            SaveData data = new SaveData
            {
                saveVersion = CurrentSaveVersion,
                savedAtUtc = DateTime.UtcNow.ToString("O"),
                sceneName = SceneManager.GetActiveScene().name
            };

            SavePlayer(data);
            SaveTime(data);
            SaveWeather(data);
            SaveInventory(data);
            SaveFishingLoadout(data);
            SaveFarmTiles(data);
            SaveAnimals(data);
            SaveBuildings(data);

            string json = JsonUtility.ToJson(data, true);
            WriteSaveAtomically(json);

            message = "Đã lưu game.";
            Debug.Log("<color=green>[SaveManager] " + message + "</color>");
            return true;
        }
        catch (Exception exception)
        {
            message = "Lưu game thất bại: " + exception.Message;
            Debug.LogError("[SaveManager] " + message, this);
            return false;
        }
    }

    public bool TryLoadGame(out string message)
    {
        ResolveReferences();

        if (!TryReadSaveData(out SaveData data, out message))
            return false;

        try
        {
            NormalizeSaveData(data);
            LoadPlayer(data);
            LoadTime(data);
            LoadWeather(data);
            LoadInventory(data);
            LoadFarmTiles(data);
            LoadAnimals(data);
            LoadBuildings(data);
            LoadFishingLoadout(data);

            message = "Đã tải game.";
            Debug.Log("<color=cyan>[SaveManager] " + message + "</color>");
            return true;
        }
        catch (Exception exception)
        {
            message = "Tải game thất bại: " + exception.Message;
            Debug.LogError("[SaveManager] " + message, this);
            return false;
        }
    }

    public void ShowStatus(string message)
    {
        if (notificationUI != null && !string.IsNullOrWhiteSpace(message))
            notificationUI.ShowMessage(message);
    }

    private void ResolveReferences()
    {
        if (playerStats == null)
            playerStats = FindFirstObjectByType<PlayerStats>();

        if (player == null && playerStats != null)
            player = playerStats.transform;

        if (notificationUI == null)
            notificationUI = FindFirstObjectByType<SaveLoadNotificationUI>();
    }

    private void SavePlayer(SaveData data)
    {
        if (playerStats != null)
        {
            data.gold = playerStats.Money;
            data.health = playerStats.currentHealth;
            data.maxHealth = playerStats.maxHealth;
            data.energy = playerStats.currentEnergy;
            data.maxEnergy = playerStats.maxEnergy;
            data.stamina = playerStats.CurrentStamina;
            data.maxStamina = playerStats.maxStamina;
            data.level = playerStats.Level;
            data.currentXP = playerStats.CurrentXP;
            data.requiredXP = playerStats.RequiredXP;
        }

        if (player != null)
            data.playerPosition = player.position;
    }

    private void LoadPlayer(SaveData data)
    {
        if (playerStats != null)
        {
            playerStats.SetMoney(data.gold);
            playerStats.SetLevel(data.level);
            playerStats.SetStamina(data.stamina);

            if (data.saveVersion >= 2)
            {
                if (data.maxHealth > 0f)
                    playerStats.maxHealth = data.maxHealth;

                if (data.maxEnergy > 0f)
                    playerStats.maxEnergy = data.maxEnergy;

                if (data.maxStamina > 0f)
                    playerStats.maxStamina = data.maxStamina;

                playerStats.currentHealth = Mathf.Clamp(
                    data.health,
                    0f,
                    playerStats.maxHealth
                );
                playerStats.SetEnergy(data.energy);
                playerStats.SetStamina(data.stamina);
                playerStats.CurrentXP = Mathf.Max(0, data.currentXP);
                playerStats.RequiredXP = Mathf.Max(1, data.requiredXP);
            }
        }

        if (player != null)
        {
            player.position = data.playerPosition;

            Rigidbody2D body = player.GetComponent<Rigidbody2D>();
            if (body != null)
                body.linearVelocity = Vector2.zero;

            GameLockManager.Instance?.RefreshLockedPosition();
        }
    }

    private static void SaveTime(SaveData data)
    {
        if (GameTimeManager.Instance == null)
            return;

        data.currentDay = GameTimeManager.Instance.CurrentDay;
        data.currentHour = GameTimeManager.Instance.CurrentHour;
        data.currentMinute = GameTimeManager.Instance.CurrentMinute;
        data.currentSeason = GameTimeManager.Instance.CurrentSeason;
    }

    private static void LoadTime(SaveData data)
    {
        if (GameTimeManager.Instance == null)
            return;

        GameTimeManager.Instance.SetTime(
            Mathf.Max(1, data.currentDay),
            Mathf.Clamp(data.currentHour, 0, 23),
            Mathf.Clamp(data.currentMinute, 0, 59),
            data.currentSeason
        );
    }

    private static void SaveWeather(SaveData data)
    {
        if (WeatherManager.Instance == null)
            return;

        data.hasWeatherData = true;
        data.currentWeather = WeatherManager.Instance.CurrentWeather;
    }

    private static void LoadWeather(SaveData data)
    {
        if (WeatherManager.Instance == null || !data.hasWeatherData)
            return;

        WeatherManager.Instance.SetWeather(data.currentWeather);
    }

    private static void SaveInventory(SaveData data)
    {
        InventoryManager inventory = InventoryManager.Instance != null
            ? InventoryManager.Instance
            : FindFirstObjectByType<InventoryManager>();

        inventory?.WriteSaveData(data);
    }

    private static void LoadInventory(SaveData data)
    {
        if (data.saveVersion < 2)
            return;

        InventoryManager inventory = InventoryManager.Instance != null
            ? InventoryManager.Instance
            : FindFirstObjectByType<InventoryManager>();

        inventory?.LoadSaveData(data);
    }

    private static void SaveFishingLoadout(SaveData data)
    {
        FishingRodLoadout loadout =
            FindFirstObjectByType<FishingRodLoadout>();
        loadout?.WriteSaveData(data);
    }

    private static void LoadFishingLoadout(SaveData data)
    {
        FishingRodLoadout loadout =
            FindFirstObjectByType<FishingRodLoadout>();
        loadout?.LoadSaveData(data);
    }

    private static void SaveFarmTiles(SaveData data)
    {
        FarmTile[] tiles = FindObjectsByType<FarmTile>(
            FindObjectsSortMode.None
        );

        foreach (FarmTile tile in tiles)
            data.farmTiles.Add(tile.GetSaveData());
    }

    private static void LoadFarmTiles(SaveData data)
    {
        FarmTile[] tiles = FindObjectsByType<FarmTile>(
            FindObjectsSortMode.None
        );

        Dictionary<Vector2Int, FarmTileSaveData> savedTiles =
            new Dictionary<Vector2Int, FarmTileSaveData>();

        foreach (FarmTileSaveData tileData in data.farmTiles)
        {
            if (tileData != null)
            {
                savedTiles[new Vector2Int(tileData.gridX, tileData.gridY)] =
                    tileData;
            }
        }

        foreach (FarmTile tile in tiles)
        {
            if (savedTiles.TryGetValue(
                    new Vector2Int(tile.gridX, tile.gridY),
                    out FarmTileSaveData tileData))
            {
                tile.LoadSaveData(tileData);
            }
        }
    }

    private static void SaveAnimals(SaveData data)
    {
        SaveableAnimal[] animals = FindObjectsByType<SaveableAnimal>(
            FindObjectsSortMode.None
        );

        foreach (SaveableAnimal animal in animals)
            data.animals.Add(animal.GetSaveData());
    }

    private static void LoadAnimals(SaveData data)
    {
        Dictionary<string, AnimalSaveData> savedAnimals =
            new Dictionary<string, AnimalSaveData>(
                StringComparer.OrdinalIgnoreCase
            );

        foreach (AnimalSaveData animalData in data.animals)
        {
            if (animalData != null &&
                !string.IsNullOrWhiteSpace(animalData.animalID))
            {
                savedAnimals[animalData.animalID] = animalData;
            }
        }

        foreach (SaveableAnimal animal in FindObjectsByType<SaveableAnimal>(
                     FindObjectsSortMode.None))
        {
            if (savedAnimals.TryGetValue(
                    animal.AnimalID,
                    out AnimalSaveData animalData))
            {
                animal.LoadSaveData(animalData);
            }
        }
    }

    private static void SaveBuildings(SaveData data)
    {
        SaveableBuilding[] buildings = FindObjectsByType<SaveableBuilding>(
            FindObjectsSortMode.None
        );

        foreach (SaveableBuilding building in buildings)
            data.buildings.Add(building.GetSaveData());
    }

    private static void LoadBuildings(SaveData data)
    {
        Dictionary<string, BuildingSaveData> savedBuildings =
            new Dictionary<string, BuildingSaveData>(
                StringComparer.OrdinalIgnoreCase
            );

        foreach (BuildingSaveData buildingData in data.buildings)
        {
            if (buildingData != null &&
                !string.IsNullOrWhiteSpace(buildingData.buildingID))
            {
                savedBuildings[buildingData.buildingID] = buildingData;
            }
        }

        foreach (SaveableBuilding building in
                 FindObjectsByType<SaveableBuilding>(FindObjectsSortMode.None))
        {
            if (savedBuildings.TryGetValue(
                    building.BuildingID,
                    out BuildingSaveData buildingData))
            {
                building.LoadSaveData(buildingData);
            }
        }
    }

    private static bool TryReadSaveData(
        out SaveData data,
        out string message)
    {
        data = null;

        if (!HasSaveGame)
        {
            message = "Chưa có dữ liệu lưu.";
            return false;
        }

        Exception primaryException = null;

        if (File.Exists(SavePath))
        {
            try
            {
                data = JsonUtility.FromJson<SaveData>(
                    File.ReadAllText(SavePath)
                );

                if (data != null)
                {
                    message = string.Empty;
                    return true;
                }
            }
            catch (Exception exception)
            {
                primaryException = exception;
            }
        }

        if (File.Exists(BackupPath))
        {
            try
            {
                data = JsonUtility.FromJson<SaveData>(
                    File.ReadAllText(BackupPath)
                );

                if (data != null)
                {
                    message = string.Empty;
                    Debug.LogWarning(
                        "[SaveManager] Đã phục hồi từ file save dự phòng."
                    );
                    return true;
                }
            }
            catch (Exception backupException)
            {
                message = "File save và bản dự phòng đều bị lỗi: " +
                          backupException.Message;
                return false;
            }
        }

        message = primaryException != null
            ? "Không đọc được file save: " + primaryException.Message
            : "File save không hợp lệ.";
        return false;
    }

    private static void NormalizeSaveData(SaveData data)
    {
        data.hotbarSlots ??= new List<InventorySlotSaveData>();
        data.bagSlots ??= new List<InventorySlotSaveData>();
        data.farmTiles ??= new List<FarmTileSaveData>();
        data.animals ??= new List<AnimalSaveData>();
        data.buildings ??= new List<BuildingSaveData>();
    }

    private static void WriteSaveAtomically(string json)
    {
        Directory.CreateDirectory(Application.persistentDataPath);
        File.WriteAllText(TemporaryPath, json);

        if (File.Exists(SavePath))
        {
            DeleteIfExists(BackupPath);
            File.Replace(TemporaryPath, SavePath, BackupPath);
        }
        else
        {
            File.Move(TemporaryPath, SavePath);
        }
    }

    private static void DeleteIfExists(string path)
    {
        if (File.Exists(path))
            File.Delete(path);
    }

    private static void ClearGameplayPlayerPrefs()
    {
        PlayerPrefs.DeleteKey("FishingRodLoadout_Reel");
        PlayerPrefs.DeleteKey("FishingRodLoadout_Line");
        PlayerPrefs.DeleteKey("FishingRodLoadout_Hook");
        PlayerPrefs.DeleteKey("FishingRodLoadout_Bait");
        PlayerPrefs.Save();
    }

}
