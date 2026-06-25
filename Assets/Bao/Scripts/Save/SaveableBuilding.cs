using UnityEngine;

public class SaveableBuilding : MonoBehaviour
{
    [SerializeField] private string buildingID;
    [SerializeField] private int upgradeLevel;

    public string BuildingID => buildingID;

    public BuildingSaveData GetSaveData()
    {
        return new BuildingSaveData
        {
            buildingID = buildingID,
            upgradeLevel = upgradeLevel,
            position = transform.position
        };
    }

    public void LoadSaveData(BuildingSaveData data)
    {
        upgradeLevel = data.upgradeLevel;
        transform.position = data.position;
    }
}