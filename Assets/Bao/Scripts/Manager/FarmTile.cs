using UnityEngine;

public class FarmTile : MonoBehaviour
{
    public int gridX;
    public int gridY;

    public enum SoilState
    {
        Normal,
        Hoed,
        Watered
    }

    public SoilState currentState = SoilState.Normal;

    private SpriteRenderer sr;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        UpdateTileVisual();
    }

    public void Hoe()
{
    if (currentState != SoilState.Normal) return;

    if (WeatherManager.Instance != null && WeatherManager.Instance.IsRaining)
        currentState = SoilState.Watered;
    else
        currentState = SoilState.Hoed;

    UpdateTileVisual();
}

    public void Water()
    {
        if (currentState != SoilState.Hoed) return;

        currentState = SoilState.Watered;
        UpdateTileVisual();
    }

    public void WaterByRain()
    {
        if (currentState == SoilState.Hoed)
        {
            currentState = SoilState.Watered;
            UpdateTileVisual();
        }
    }

    private void UpdateTileVisual()
    {
        if (sr == null) return;

        switch (currentState)
        {
            case SoilState.Normal:
                sr.color = new Color(0.55f, 0.38f, 0.23f);
                break;

            case SoilState.Hoed:
                sr.color = new Color(0.35f, 0.20f, 0.10f);
                break;

            case SoilState.Watered:
                sr.color = new Color(0.18f, 0.12f, 0.08f);
                break;
        }
    }

    public void Axe()
    {
        Debug.Log("Chặt cây");
    }

    public void Pickaxe()
    {
        Debug.Log("Đập đá");
    }

    public void Sickle()
    {
        Debug.Log("Cắt cỏ");
    }
}