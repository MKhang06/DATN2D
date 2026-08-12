using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SaveData
{
    public int saveVersion;
    public string savedAtUtc;
    public string sceneName;

    public int gold;
    public float health;
    public float maxHealth;
    public float energy;
    public float maxEnergy;
    public float stamina;
    public float maxStamina;
    public int level;
    public int currentXP;
    public int requiredXP;

    public Vector3 playerPosition;

    public int currentDay;
    public int currentHour;
    public int currentMinute;
    public GameTimeManager.Season currentSeason;
    public WeatherManager.WeatherType currentWeather;
    public bool hasWeatherData;

    public int selectedHotbarIndex;
    public List<InventorySlotSaveData> hotbarSlots = new();
    public List<InventorySlotSaveData> bagSlots = new();

    public string equippedReelId;
    public string equippedLineId;
    public string equippedHookId;
    public string equippedBaitId;

    public List<FarmTileSaveData> farmTiles = new();
    public List<FarmPlotTileSaveData> farmPlotTiles = new();
    public List<AnimalSaveData> animals = new();
    public List<BuildingSaveData> buildings = new();
}

[Serializable]
public class FarmTileSaveData
{
    public int gridX;
    public int gridY;
    public FarmTile.SoilState state;
    public int growthStage;
    public bool hasCrop;
}

[Serializable]
public class FarmPlotTileSaveData
{
    public int x;
    public int y;
    public int z;
    public FarmTile.SoilState state;
}

[Serializable]
public class AnimalSaveData
{
    public string animalID;
    public Vector3 position;
    public int hunger;
    public bool hasProduct;
}

[Serializable]
public class BuildingSaveData
{
    public string buildingID;
    public int upgradeLevel;
    public Vector3 position;
}

[Serializable]
public class InventorySlotSaveData
{
    public string itemId;
    public string itemName;
    public int amount;
    public int maxStack = 99;
}
