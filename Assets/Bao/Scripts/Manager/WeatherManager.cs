using UnityEngine;
using System.Collections;
using UnityEngine.Rendering.Universal;

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

    [Header("Lightning")]
    [SerializeField] private Light2D lightningLight;
    [SerializeField] private float minFlashDelay = 4f;
    [SerializeField] private float maxFlashDelay = 10f;
    [SerializeField] private float minFlashIntensity = 1.5f;
    [SerializeField] private float maxFlashIntensity = 3.5f;
    [SerializeField] private float flashDuration = 0.12f;

    private Coroutine thunderRoutine;

    public WeatherType CurrentWeather => currentWeather;

    public bool IsRaining =>
        currentWeather == WeatherType.Rainy ||
        currentWeather == WeatherType.Stormy;

    public bool IsSnowing =>
        currentWeather == WeatherType.Snowy;

    private void Awake()
    {
        Instance = this;

        if (lightningLight != null)
        {
            lightningLight.enabled = false;
            lightningLight.intensity = 0f;
        }
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

        StopThunder();
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
                StartThunder();
                break;

            case WeatherType.Snowy:
                PlayParticle(snowParticle);
                PlayAudio(windAudio);
                break;
        }
    }

    private void StopAll()
    {
        StopParticle(rainParticle);
        StopParticle(snowParticle);

        StopAudio(birdAudio);
        StopAudio(windAudio);
        StopAudio(rainAudio);
        StopAudio(stormAudio);

        StopThunder();
    }

    private void StartThunder()
    {
        StopThunder();
        thunderRoutine = StartCoroutine(ThunderRoutine());
    }

    private void StopThunder()
    {
        if (thunderRoutine != null)
        {
            StopCoroutine(thunderRoutine);
            thunderRoutine = null;
        }

        if (lightningLight != null)
        {
            lightningLight.enabled = false;
            lightningLight.intensity = 0f;
        }
    }

    private IEnumerator ThunderRoutine()
    {
        while (currentWeather == WeatherType.Stormy)
        {
            yield return new WaitForSeconds(
                Random.Range(minFlashDelay, maxFlashDelay)
            );

            yield return StartCoroutine(FlashLightning());

            if (stormAudio != null)
                stormAudio.Play();
        }
    }

    private IEnumerator FlashLightning()
{
    if (lightningLight == null)
        yield break;

    lightningLight.enabled = true;

    lightningLight.intensity =
        Random.Range(minFlashIntensity, maxFlashIntensity);

    lightningLight.pointLightOuterRadius =
        Random.Range(25f, 35f);

    lightningLight.pointLightInnerRadius =
        Random.Range(10f, 15f);

    float safeFlashDuration = Mathf.Max(0.01f, flashDuration);

    yield return new WaitForSeconds(safeFlashDuration * (2f / 3f));

    lightningLight.intensity = 0;

    yield return new WaitForSeconds(safeFlashDuration / 3f);

    lightningLight.intensity =
        Random.Range(minFlashIntensity * 0.6f,
                     maxFlashIntensity * 0.8f);

    yield return new WaitForSeconds(safeFlashDuration * (5f / 12f));

    lightningLight.intensity = 0;
    lightningLight.enabled = false;
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
    FarmTile[] tiles =
        FindObjectsByType<FarmTile>(FindObjectsSortMode.None);

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
