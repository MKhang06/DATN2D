using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SaveData
{
    public int gold;
    public float stamina;
    public int level;

    public Vector3 playerPosition;

    public int currentDay;
    public int currentHour;
    public int currentMinute;
    public GameTimeManager.Season currentSeason;

    public List<FarmTileSaveData> farmTiles = new();
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