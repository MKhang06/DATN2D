using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class FarmManager : MonoBehaviour
{
    public Tilemap farmTilemap;

    public TileBase hoeTile;
    public TileBase wateredTile;

    private Dictionary<Vector3Int, FarmState> tileStates =
        new Dictionary<Vector3Int, FarmState>();

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            HoeTile();
        }

        if (Input.GetMouseButtonDown(1))
        {
            WaterTile();
        }
    }

    void HoeTile()
    {
        Vector3 worldPos =
            Camera.main.ScreenToWorldPoint(Input.mousePosition);

        Vector3Int cellPos =
            farmTilemap.WorldToCell(worldPos);

        tileStates[cellPos] = FarmState.Hoed;

        farmTilemap.SetTile(cellPos, hoeTile);
    }

    void WaterTile()
    {
        Vector3 worldPos =
            Camera.main.ScreenToWorldPoint(Input.mousePosition);

        Vector3Int cellPos =
            farmTilemap.WorldToCell(worldPos);

        if (!tileStates.ContainsKey(cellPos))
            return;

        if (tileStates[cellPos] != FarmState.Hoed)
            return;

        tileStates[cellPos] = FarmState.Watered;

        farmTilemap.SetTile(cellPos, wateredTile);
    }
}

public enum FarmState
{
    Normal,
    Hoed,
    Watered
}