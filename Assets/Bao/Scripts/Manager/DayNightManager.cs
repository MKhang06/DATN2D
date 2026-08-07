using UnityEngine;
using UnityEngine.Rendering.Universal;

public class DayNightManager : MonoBehaviour
{
    [Header("Light")]
    [SerializeField] private Light2D globalLight;

    [Header("Day Night Settings")]
    [SerializeField] private Gradient dayNightColor;
    [SerializeField] private AnimationCurve intensityCurve;

    [Header("Visibility")]
    [SerializeField, Range(0f, 1f)] private float minimumIntensity = 0.35f;
    [SerializeField] private Color minimumNightColor =
        new Color(0.2f, 0.26f, 0.4f, 1f);

    private GameTimeManager timeManager;

    private void OnEnable()
    {
        if (!Application.isPlaying)
            return;

        BindTimeManager();
        ApplyLighting();
    }

    private void Start()
    {
        BindTimeManager();
        ApplyLighting();
    }

    private void OnDisable()
    {
        UnbindTimeManager();
    }

    private void BindTimeManager()
    {
        GameTimeManager manager = GameTimeManager.Instance;
        if (timeManager == manager)
            return;

        UnbindTimeManager();
        timeManager = manager;

        if (timeManager != null)
            timeManager.OnMinuteChanged += ApplyLighting;
    }

    private void UnbindTimeManager()
    {
        if (timeManager != null)
            timeManager.OnMinuteChanged -= ApplyLighting;

        timeManager = null;
    }

    private void ApplyLighting()
    {
        if (timeManager == null)
            BindTimeManager();

        if (timeManager == null || globalLight == null)
            return;

        float currentMinutes =
            timeManager.CurrentHour * 60f +
            timeManager.CurrentMinute;

        float dayPercent = currentMinutes / 1440f;

        Color evaluatedColor = dayNightColor.Evaluate(dayPercent);
        evaluatedColor.r = Mathf.Max(evaluatedColor.r, minimumNightColor.r);
        evaluatedColor.g = Mathf.Max(evaluatedColor.g, minimumNightColor.g);
        evaluatedColor.b = Mathf.Max(evaluatedColor.b, minimumNightColor.b);
        evaluatedColor.a = 1f;

        globalLight.color = evaluatedColor;

        globalLight.intensity = Mathf.Max(
            minimumIntensity,
            intensityCurve.Evaluate(dayPercent)
        );
    }
}
