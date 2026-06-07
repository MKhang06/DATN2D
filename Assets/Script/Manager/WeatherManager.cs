using UnityEngine;
using TMPro;

public class WeatherManager : MonoBehaviour
{
    public enum WeatherType
    {
        Sunny,
        Rainy,
        Windy,
        Storm
    }

    [Header("UI")]
    public TextMeshProUGUI weatherText;

    [Header("Effect")]
    public GameObject rainEffect;

    [Header("Settings")]
    public float weatherChangeTime = 10f;

    private float timer;
    private WeatherType currentWeather;

    void Start()
    {
        // Luôn bắt đầu bằng Nắng
        SetWeather(WeatherType.Sunny);
    }

    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= weatherChangeTime)
        {
            timer = 0;

            // Chọn thời tiết ngẫu nhiên
            WeatherType randomWeather =
                (WeatherType)Random.Range(0, 4);

            SetWeather(randomWeather);
        }
    }

    void SetWeather(WeatherType weather)
    {
        currentWeather = weather;

        // Tắt mưa trước
        rainEffect.SetActive(false);

        switch (currentWeather)
        {
            case WeatherType.Sunny:
                weatherText.text = "Sunny";
                break;

            case WeatherType.Rainy:
                weatherText.text = "Rainy";
                rainEffect.SetActive(true);
                break;

            case WeatherType.Windy:
                weatherText.text = "Windy";
                break;

            case WeatherType.Storm:
                weatherText.text = "Storm";
                rainEffect.SetActive(true);
                break;
        }

        Debug.Log("Thời tiết hiện tại: " + currentWeather);
    }
}