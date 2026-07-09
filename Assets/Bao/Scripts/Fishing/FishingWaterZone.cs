using UnityEngine;

public class FishingWaterZone : MonoBehaviour
{
    public enum WaterType
    {
        River,
        Lake,
        Sea
    }

    [Header("Water Zone")]
    [SerializeField] private WaterType waterType = WaterType.River;
    [SerializeField] private float zoneMaxDepth = 10f;

    public WaterType Type => waterType;
    public float ZoneMaxDepth => zoneMaxDepth;
}