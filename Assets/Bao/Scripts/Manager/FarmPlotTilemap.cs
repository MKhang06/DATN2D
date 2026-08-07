using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[DisallowMultipleComponent]
[RequireComponent(typeof(Tilemap))]
public sealed class FarmPlotTilemap : MonoBehaviour
{
    public static FarmPlotTilemap Instance { get; private set; }

    [Header("Tilemap")]
    [SerializeField] private Tilemap farmTilemap;

    [Header("Soil Colors")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color hoedColor =
        new Color(0.68f, 0.5f, 0.32f, 1f);
    [SerializeField] private Color wateredColor =
        new Color(0.42f, 0.5f, 0.58f, 1f);

    private readonly Dictionary<Vector3Int, FarmTile.SoilState>
        changedCells =
            new Dictionary<Vector3Int, FarmTile.SoilState>();

    private readonly List<Vector3Int> reusableCells =
        new List<Vector3Int>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Instance = null;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning(
                "Chỉ nên có một FarmPlotTilemap trong scene.",
                this
            );
            enabled = false;
            return;
        }

        Instance = this;

        if (farmTilemap == null)
            farmTilemap = GetComponent<Tilemap>();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public bool TryGetCell(Vector2 worldPosition, out Vector3Int cell)
    {
        cell = default;

        if (farmTilemap == null)
            return false;

        cell = farmTilemap.WorldToCell(worldPosition);
        return farmTilemap.HasTile(cell);
    }

    public FarmTile.SoilState GetState(Vector3Int cell)
    {
        return changedCells.TryGetValue(
            cell,
            out FarmTile.SoilState state
        )
            ? state
            : FarmTile.SoilState.Normal;
    }

    public bool CanHoe(Vector3Int cell)
    {
        return IsFarmCell(cell) &&
               GetState(cell) == FarmTile.SoilState.Normal;
    }

    public bool CanWater(Vector3Int cell)
    {
        return IsFarmCell(cell) &&
               GetState(cell) == FarmTile.SoilState.Hoed;
    }

    public bool Hoe(Vector3Int cell)
    {
        if (!CanHoe(cell))
            return false;

        FarmTile.SoilState nextState =
            WeatherManager.Instance != null &&
            WeatherManager.Instance.IsRaining
                ? FarmTile.SoilState.Watered
                : FarmTile.SoilState.Hoed;

        SetState(cell, nextState);
        return true;
    }

    public bool Water(Vector3Int cell)
    {
        if (!CanWater(cell))
            return false;

        SetState(cell, FarmTile.SoilState.Watered);
        return true;
    }

    public void WaterHoedTiles()
    {
        reusableCells.Clear();

        foreach (KeyValuePair<Vector3Int, FarmTile.SoilState> entry
                 in changedCells)
        {
            if (entry.Value == FarmTile.SoilState.Hoed)
                reusableCells.Add(entry.Key);
        }

        foreach (Vector3Int cell in reusableCells)
            SetState(cell, FarmTile.SoilState.Watered);

        reusableCells.Clear();
    }

    public void WriteSaveData(List<FarmPlotTileSaveData> target)
    {
        if (target == null)
            return;

        target.Clear();

        foreach (KeyValuePair<Vector3Int, FarmTile.SoilState> entry
                 in changedCells)
        {
            target.Add(new FarmPlotTileSaveData
            {
                x = entry.Key.x,
                y = entry.Key.y,
                z = entry.Key.z,
                state = entry.Value
            });
        }
    }

    public void LoadSaveData(List<FarmPlotTileSaveData> savedCells)
    {
        ResetChangedCellVisuals();
        changedCells.Clear();

        if (savedCells == null)
            return;

        foreach (FarmPlotTileSaveData data in savedCells)
        {
            if (data == null || data.state == FarmTile.SoilState.Normal)
                continue;

            Vector3Int cell = new Vector3Int(data.x, data.y, data.z);
            if (IsFarmCell(cell))
                SetState(cell, data.state);
        }
    }

    private bool IsFarmCell(Vector3Int cell)
    {
        return farmTilemap != null && farmTilemap.HasTile(cell);
    }

    private void SetState(Vector3Int cell, FarmTile.SoilState state)
    {
        if (!IsFarmCell(cell))
            return;

        if (state == FarmTile.SoilState.Normal)
            changedCells.Remove(cell);
        else
            changedCells[cell] = state;

        ApplyCellColor(cell, state);
    }

    private void ResetChangedCellVisuals()
    {
        foreach (Vector3Int cell in changedCells.Keys)
            ApplyCellColor(cell, FarmTile.SoilState.Normal);
    }

    private void ApplyCellColor(
        Vector3Int cell,
        FarmTile.SoilState state)
    {
        farmTilemap.SetTileFlags(cell, TileFlags.None);

        switch (state)
        {
            case FarmTile.SoilState.Hoed:
                farmTilemap.SetColor(cell, hoedColor);
                break;

            case FarmTile.SoilState.Watered:
                farmTilemap.SetColor(cell, wateredColor);
                break;

            default:
                farmTilemap.SetColor(cell, normalColor);
                break;
        }
    }
}
