using UnityEngine;

public class SeasonManager : MonoBehaviour
{
    public enum Season
    {
        Spring,
        Summer,
        Fall,
        Winter
    }

    public Season currentSeason = Season.Spring;
}