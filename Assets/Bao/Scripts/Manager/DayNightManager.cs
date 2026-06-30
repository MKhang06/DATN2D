using UnityEngine;
using UnityEngine.Rendering.Universal;

public class DayNightManager : MonoBehaviour
{
    [Header("Light")]
    [SerializeField] private Light2D globalLight;

    [Header("Day Night Settings")]
    [SerializeField] private Gradient dayNightColor;
    [SerializeField] private AnimationCurve intensityCurve;

    private void Update()
    {
        if (GameTimeManager.Instance == null)
            return;

        if (globalLight == null)
            return;

        float currentMinutes =
            GameTimeManager.Instance.CurrentHour * 60f +
            GameTimeManager.Instance.CurrentMinute;

        float dayPercent = currentMinutes / 1440f;

        globalLight.color =
            dayNightColor.Evaluate(dayPercent);

        globalLight.intensity =
            intensityCurve.Evaluate(dayPercent);
    }
}