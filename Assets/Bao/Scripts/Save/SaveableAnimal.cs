using UnityEngine;

public class SaveableAnimal : MonoBehaviour
{
    [SerializeField] private string animalID;

    public string AnimalID => animalID;

    [SerializeField] private int hunger;
    [SerializeField] private bool hasProduct;

    public AnimalSaveData GetSaveData()
    {
        return new AnimalSaveData
        {
            animalID = animalID,
            position = transform.position,
            hunger = hunger,
            hasProduct = hasProduct
        };
    }

    public void LoadSaveData(AnimalSaveData data)
    {
        transform.position = data.position;
        hunger = data.hunger;
        hasProduct = data.hasProduct;
    }
}