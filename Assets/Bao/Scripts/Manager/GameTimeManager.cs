using System;
using UnityEngine;

public class GameTimeManager : MonoBehaviour
{
    public static GameTimeManager Instance;

    public enum Season
    {
        Spring,
        Summer,
        Autumn,
        Winter
    }

    [Header("Time Settings")]
    [SerializeField] private float realMinutesPerGameDay = 24f;

    [Header("Current Time")]
    [SerializeField] private int currentDay = 1;
    [SerializeField] private int currentHour = 6;
    [SerializeField] private int currentMinute = 0;

    [Header("Season Settings")]
    [SerializeField] private int daysPerSeason = 28;
    [SerializeField] private Season currentSeason = Season.Spring;

    public int CurrentDay => currentDay;
    public int CurrentHour => currentHour;
    public int CurrentMinute => currentMinute;
    public Season CurrentSeason => currentSeason;

    public event Action OnMinuteChanged;
    public event Action OnHourChanged;
    public event Action OnDayChanged;
    public event Action<Season> OnSeasonChanged;

    private float timer;
    private const int TotalMinutesInDay = 24 * 60;

    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        TickTime();

        // Test nhanh: bấm N để sang ngày
        if (Input.GetKeyDown(KeyCode.N))
            NextDay();
    }

    private void TickTime()
    {
        float realSecondsPerGameDay = realMinutesPerGameDay * 60f;
        float gameMinutesPerRealSecond = TotalMinutesInDay / realSecondsPerGameDay;

        timer += Time.deltaTime * gameMinutesPerRealSecond;

        while (timer >= 1f)
        {
            timer -= 1f;
            AddMinute();
        }
    }

    private void AddMinute()
    {
        currentMinute++;

        if (currentMinute >= 60)
        {
            currentMinute = 0;
            currentHour++;

            OnHourChanged?.Invoke();

            if (currentHour >= 24)
            {
                currentHour = 0;
                NextDay();
            }
        }

        OnMinuteChanged?.Invoke();
    }

    public void NextDay()
    {
        currentDay++;

        UpdateSeason();

        OnDayChanged?.Invoke();
    }

    private void UpdateSeason()
    {
        int seasonIndex = ((currentDay - 1) / daysPerSeason) % 4;
        Season newSeason = (Season)seasonIndex;

        if (newSeason != currentSeason)
        {
            currentSeason = newSeason;
            OnSeasonChanged?.Invoke(currentSeason);
        }
    }

    public string GetTimeText()
    {
        return currentHour.ToString("00") + ":" + currentMinute.ToString("00");
    }

    public string GetSeasonText()
    {
        switch (currentSeason)
        {
            case Season.Spring:
                return "Mùa xuân";
            case Season.Summer:
                return "Mùa hè";
            case Season.Autumn:
                return "Mùa thu";
            case Season.Winter:
                return "Mùa đông";
            default:
                return "";
        }
    }
    public void SetTime(
    int day,
    int hour,
    int minute,
    Season season)
    {
        currentDay = day;
        currentHour = hour;
        currentMinute = minute;
        currentSeason = season;

        OnMinuteChanged?.Invoke();
    }
}
