using UnityEngine;
using System.Collections;

public class WeatherManager : MonoBehaviour
{
    public static WeatherManager Instance;

    public enum WeatherType
    {
        Sunny,
        Cloudy,
        Rainy,
        Stormy,
        Snowy
    }

    [Header("Current Weather")]
    [SerializeField] private WeatherType currentWeather = WeatherType.Sunny;

    [Header("Weather Chance")]
    [Range(0, 100)][SerializeField] private int springRainChance = 35;
    [Range(0, 100)][SerializeField] private int summerRainChance = 20;
    [Range(0, 100)][SerializeField] private int autumnRainChance = 25;
    [Range(0, 100)][SerializeField] private int winterRainChance = 10;

    [Range(0, 100)][SerializeField] private int cloudyChance = 25;
    [Range(0, 100)][SerializeField] private int stormChance = 15;
    [Range(0, 100)][SerializeField] private int snowChanceInWinter = 45;

    [Header("Particles")]
    [SerializeField] private ParticleSystem rainParticle;
    [SerializeField] private ParticleSystem snowParticle;

    [Header("Audio")]
    [SerializeField] private AudioSource birdAudio;
    [SerializeField] private AudioSource windAudio;
    [SerializeField] private AudioSource rainAudio;
    [SerializeField] private AudioSource stormAudio;

    public WeatherType CurrentWeather => currentWeather;

    public bool IsRaining =>
        currentWeather == WeatherType.Rainy ||
        currentWeather == WeatherType.Stormy;

    public bool IsSnowing =>
        currentWeather == WeatherType.Snowy;

    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
        if (GameTimeManager.Instance != null)
            GameTimeManager.Instance.OnDayChanged += GenerateWeatherForNewDay;
    }

    private void OnDisable()
    {
        if (GameTimeManager.Instance != null)
            GameTimeManager.Instance.OnDayChanged -= GenerateWeatherForNewDay;
    }

    private void Start()
    {
        GenerateWeatherForNewDay();
    }

    public void GenerateWeatherForNewDay()
    {
        GameTimeManager.Season season =
            GameTimeManager.Instance != null
                ? GameTimeManager.Instance.CurrentSeason
                : GameTimeManager.Season.Spring;

        int roll = Random.Range(0, 100);

        if (season == GameTimeManager.Season.Winter)
        {
            if (roll < snowChanceInWinter)
                currentWeather = WeatherType.Snowy;
            else if (roll < snowChanceInWinter + winterRainChance)
                currentWeather = WeatherType.Rainy;
            else if (roll < snowChanceInWinter + winterRainChance + cloudyChance)
                currentWeather = WeatherType.Cloudy;
            else
                currentWeather = WeatherType.Sunny;
        }
        else
        {
            int rainChance = GetRainChanceBySeason();

            if (roll < rainChance)
            {
                int stormRoll = Random.Range(0, 100);

                currentWeather =
                    stormRoll < stormChance
                        ? WeatherType.Stormy
                        : WeatherType.Rainy;
            }
            else if (roll < rainChance + cloudyChance)
            {
                currentWeather = WeatherType.Cloudy;
            }
            else
            {
                currentWeather = WeatherType.Sunny;
            }
        }

        ApplyWeatherEffect();

        if (IsRaining)
            WaterAllFarmTiles();

        Debug.Log("Thời tiết hôm nay: " + GetWeatherText());
    }

    private int GetRainChanceBySeason()
    {
        if (GameTimeManager.Instance == null)
            return springRainChance;

        switch (GameTimeManager.Instance.CurrentSeason)
        {
            case GameTimeManager.Season.Spring:
                return springRainChance;

            case GameTimeManager.Season.Summer:
                return summerRainChance;

            case GameTimeManager.Season.Autumn:
                return autumnRainChance;

            case GameTimeManager.Season.Winter:
                return winterRainChance;

            default:
                return springRainChance;
        }
    }

    private void ApplyWeatherEffect()
    {
        StopAll();

        switch (currentWeather)
        {
            case WeatherType.Sunny:

                PlayAudio(birdAudio);

                break;

            case WeatherType.Cloudy:

                PlayAudio(windAudio);

                break;

            case WeatherType.Rainy:

                PlayParticle(rainParticle);

                PlayAudio(rainAudio);

                break;

            case WeatherType.Stormy:

                PlayParticle(rainParticle);

                PlayAudio(rainAudio);

                PlayAudio(windAudio);

                StartCoroutine(ThunderRoutine());

                break;
        }
    }

    private void StopAllWeatherEffects()
    {
        StopParticle(rainParticle);
        StopParticle(snowParticle);

        StopAudio(rainAudio);
        StopAudio(stormAudio);
        StopAudio(windAudio);
    }
    private void StopAll()
    {
        StopParticle(rainParticle);

        StopAudio(birdAudio);
        StopAudio(windAudio);
        StopAudio(rainAudio);
        StopAudio(stormAudio);
    }
    private IEnumerator ThunderRoutine()
    {
        while (currentWeather == WeatherType.Stormy)
        {
            yield return new WaitForSeconds(Random.Range(6f, 15f));

            if (stormAudio != null)
                stormAudio.Play();
        }
    }

    private void PlayParticle(ParticleSystem particle)
    {
        if (particle == null) return;

        if (!particle.isPlaying)
            particle.Play();
    }

    private void StopParticle(ParticleSystem particle)
    {
        if (particle == null) return;

        particle.Stop();
        particle.Clear();
    }

    private void PlayAudio(AudioSource audio)
    {
        if (audio == null) return;

        if (!audio.isPlaying)
            audio.Play();
    }

    private void StopAudio(AudioSource audio)
    {
        if (audio == null) return;

        audio.Stop();
    }

    private void WaterAllFarmTiles()
    {
        FarmTile[] tiles = FindObjectsOfType<FarmTile>();

        foreach (FarmTile tile in tiles)
            tile.WaterByRain();
    }

    public string GetWeatherText()
    {
        switch (currentWeather)
        {
            case WeatherType.Sunny:
                return "Nắng";

            case WeatherType.Cloudy:
                return "Nhiều mây";

            case WeatherType.Rainy:
                return "Mưa";

            case WeatherType.Stormy:
                return "Bão";

            case WeatherType.Snowy:
                return "Tuyết";

            default:
                return "";
        }
    }
    [ContextMenu("TEST/Set Sunny")]
    public void TestSetSunny()
    {
        currentWeather = WeatherType.Sunny;
        ApplyWeatherEffect();
    }

    [ContextMenu("TEST/Set Cloudy")]
    public void TestSetCloudy()
    {
        currentWeather = WeatherType.Cloudy;
        ApplyWeatherEffect();
    }

    [ContextMenu("TEST/Set Rainy")]
    public void TestSetRainy()
    {
        currentWeather = WeatherType.Rainy;
        ApplyWeatherEffect();
        WaterAllFarmTiles();
    }

    [ContextMenu("TEST/Set Stormy")]
    public void TestSetStormy()
    {
        currentWeather = WeatherType.Stormy;
        ApplyWeatherEffect();
        WaterAllFarmTiles();
    }

    [ContextMenu("TEST/Set Snowy")]
    public void TestSetSnowy()
    {
        currentWeather = WeatherType.Snowy;
        ApplyWeatherEffect();
    }
}