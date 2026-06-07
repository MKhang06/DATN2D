using UnityEngine;
using UnityEngine.Tilemaps;

public class FarmManager : MonoBehaviour
{
    public Tilemap farmTilemap;

    public TileBase hoeTile;
    public TileBase wateredTile;

    public void HoeTile(Vector3 worldPos)
    {
        Vector3Int cellPos =
            farmTilemap.WorldToCell(worldPos);

        farmTilemap.SetTile(cellPos, hoeTile);
    }

    public void WaterTile(Vector3 worldPos)
    {
        Vector3Int cellPos =
            farmTilemap.WorldToCell(worldPos);

        if (farmTilemap.GetTile(cellPos) == hoeTile)
        {
            farmTilemap.SetTile(cellPos, wateredTile);
        }
    }
}   