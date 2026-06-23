using UnityEngine;

public class FarmGridGenerator : MonoBehaviour
{
    [Header("Farm Settings")]
    [SerializeField] private GameObject farmTilePrefab;

    [SerializeField] private int width = 8;
    [SerializeField] private int height = 8;

    [SerializeField] private float spacing = 1f;

    [Header("Center Grid")]
    [SerializeField] private bool centerGrid = true;

    [ContextMenu("Generate Farm")]
    public void GenerateFarm()
    {
        ClearFarm();

        if (farmTilePrefab == null)
        {
            Debug.LogError("Chưa kéo FarmTile Prefab vào FarmGridGenerator!");
            return;
        }

        float offsetX = centerGrid ? (width - 1) * spacing / 2f : 0f;
        float offsetY = centerGrid ? (height - 1) * spacing / 2f : 0f;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3 spawnPos = new Vector3(
                    x * spacing - offsetX,
                    y * spacing - offsetY,
                    0f
                );

                GameObject tile = Instantiate(
                    farmTilePrefab,
                    spawnPos,
                    Quaternion.identity,
                    transform
                );

                tile.name = $"FarmTile_{x}_{y}";

                FarmTile farmTile = tile.GetComponent<FarmTile>();

                if (farmTile != null)
                {
                    farmTile.gridX = x;
                    farmTile.gridY = y;
                }
            }
        }
    }

    [ContextMenu("Clear Farm")]
    public void ClearFarm()
    {
        while (transform.childCount > 0)
        {
#if UNITY_EDITOR
            DestroyImmediate(transform.GetChild(0).gameObject);
#else
            Destroy(transform.GetChild(0).gameObject);
#endif
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;

        float offsetX = centerGrid ? (width - 1) * spacing / 2f : 0f;
        float offsetY = centerGrid ? (height - 1) * spacing / 2f : 0f;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3 pos = transform.position + new Vector3(
                    x * spacing - offsetX,
                    y * spacing - offsetY,
                    0f
                );

                Gizmos.DrawWireCube(
                    pos,
                    Vector3.one * spacing * 0.9f
                );
            }
        }
    }

    private void Start()
    {
        GenerateFarm();
    }
}