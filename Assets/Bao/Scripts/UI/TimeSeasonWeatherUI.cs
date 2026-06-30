using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TimeSeasonWeatherUI : MonoBehaviour
{
    [Header("Text")]
    [SerializeField] private TMP_Text timeText;
    [SerializeField] private TMP_Text dayText;
    [SerializeField] private TMP_Text seasonText;
    [SerializeField] private TMP_Text weatherText;

    [Header("Time Icon")]
    [SerializeField] private Image timeIcon;
    [SerializeField] private RectTransform clockHand;

    [SerializeField] private Sprite sunriseSprite;
    [SerializeField] private Sprite daySprite;
    [SerializeField] private Sprite sunsetSprite;
    [SerializeField] private Sprite nightSprite;

    [Header("Season Icon")]
    [SerializeField] private Image seasonIcon;

    [SerializeField] private Sprite springSprite;
    [SerializeField] private Sprite summerSprite;
    [SerializeField] private Sprite autumnSprite;
    [SerializeField] private Sprite winterSprite;

    private void Update()
    {
        UpdateTimeUI();
        UpdateWeatherUI();
    }

    private void UpdateTimeUI()
    {
        if (GameTimeManager.Instance == null)
            return;

        int hour = GameTimeManager.Instance.CurrentHour;
        int minute = GameTimeManager.Instance.CurrentMinute;

        if (timeText != null)
            timeText.text = hour.ToString("00") + ":" + minute.ToString("00");

        if (dayText != null)
            dayText.text = "Ngày " + GameTimeManager.Instance.CurrentDay;

        if (seasonText != null)
            seasonText.text = GameTimeManager.Instance.GetSeasonText();

        UpdateTimeIcon(hour);
        UpdateClockHand(hour, minute);
        UpdateSeasonIcon();
    }

    private void UpdateTimeIcon(int hour)
    {
        if (timeIcon == null)
            return;

        if (hour >= 5 && hour < 8)
            timeIcon.sprite = sunriseSprite;
        else if (hour >= 8 && hour < 17)
            timeIcon.sprite = daySprite;
        else if (hour >= 17 && hour < 20)
            timeIcon.sprite = sunsetSprite;
        else
            timeIcon.sprite = nightSprite;
    }

    private void UpdateClockHand(int hour, int minute)
    {
        if (clockHand == null)
            return;

        float totalMinutes = hour * 60f + minute;
        float angle = -(totalMinutes / 1440f) * 360f;

        clockHand.localEulerAngles = new Vector3(0f, 0f, angle);
    }

    private void UpdateSeasonIcon()
    {
        if (seasonIcon == null || GameTimeManager.Instance == null)
            return;

        switch (GameTimeManager.Instance.CurrentSeason)
        {
            case GameTimeManager.Season.Spring:
                seasonIcon.sprite = springSprite;
                break;
            case GameTimeManager.Season.Summer:
                seasonIcon.sprite = summerSprite;
                break;
            case GameTimeManager.Season.Autumn:
                seasonIcon.sprite = autumnSprite;
                break;
            case GameTimeManager.Season.Winter:
                seasonIcon.sprite = winterSprite;
                break;
        }
    }

    private void UpdateWeatherUI()
    {
        if (WeatherManager.Instance == null)
            return;

        if (weatherText != null)
        {
            weatherText.text =
                "Thời tiết: " +
                WeatherManager.Instance.GetWeatherText();
        }
    }
}