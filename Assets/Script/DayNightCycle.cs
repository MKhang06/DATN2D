using UnityEngine;
using UnityEngine.Rendering.Universal;

public class DayNightCycle : MonoBehaviour
{
    [Header("Time")]
    public float dayLength = 300f; // 5 phút = 1 ngày

    [Range(0, 1)]
    public float timeOfDay;

    [Header("Light")]
    public Light2D globalLight;

    public Gradient lightColor;
    public AnimationCurve lightIntensity;

    private void Update()
    {
        timeOfDay += Time.deltaTime / dayLength;

        if (timeOfDay >= 1f)
        {
            timeOfDay = 0f;
            NewDay();
        }

        UpdateLighting();
    }

    void UpdateLighting()
    {
        globalLight.color = lightColor.Evaluate(timeOfDay);
        globalLight.intensity = lightIntensity.Evaluate(timeOfDay);
    }

    void NewDay()
    {
        Debug.Log("New Day");
    }
}