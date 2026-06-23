using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerToolController : MonoBehaviour
{
    public enum ToolType
    {
        None,
        Hoe,
        WateringCan
    }

    [Header("Tool")]
    public ToolType currentTool = ToolType.None;

    [Header("Settings")]
    [SerializeField] private float interactRadius = 0.25f;
    [SerializeField] private LayerMask farmLayer;

    [Header("References")]
    [SerializeField] private PlayerToolAnimation toolAnimation;
    [SerializeField] private PlayerStats playerStats;

    private Camera cam;

    private void Awake()
    {
        cam = Camera.main;

        if (toolAnimation == null)
            toolAnimation = GetComponent<PlayerToolAnimation>();

        if (playerStats == null)
            playerStats = GetComponent<PlayerStats>();
    }

    private void Update()
    {
        if (toolAnimation != null && toolAnimation.IsBusy())
            return;

        ChangeTool();
        RotateToolToMouse();

        if (Input.GetMouseButtonDown(0))
            UseToolAtMouse();
    }

    private void ChangeTool()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            if (currentTool == ToolType.Hoe)
            {
                currentTool = ToolType.None;
                toolAnimation?.HideHoe();
                CursorManager.Instance?.SetDefaultCursor();
            }
            else
            {
                currentTool = ToolType.Hoe;
                toolAnimation?.ShowHoe();
                CursorManager.Instance?.SetHoeCursor();
            }
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            if (currentTool == ToolType.WateringCan)
            {
                currentTool = ToolType.None;
                toolAnimation?.HideWateringCan();
                CursorManager.Instance?.SetDefaultCursor();
            }
            else
            {
                currentTool = ToolType.WateringCan;
                toolAnimation?.ShowWateringCan();
                CursorManager.Instance?.SetWateringCursor();
            }
        }
    }

    private void RotateToolToMouse()
    {
        if (currentTool == ToolType.None) return;
        if (toolAnimation == null) return;

        if (cam == null)
            cam = Camera.main;

        if (cam == null) return;

        Vector3 mousePos = cam.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0f;

        toolAnimation.LookAtMouse(mousePos);
    }

    private void UseToolAtMouse()
    {
        if (currentTool == ToolType.None) return;

        if (cam == null)
            cam = Camera.main;

        if (cam == null) return;

        if (EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject())
            return;

        Vector2 mouseWorldPos =
            cam.ScreenToWorldPoint(Input.mousePosition);

        Collider2D hit = Physics2D.OverlapCircle(
            mouseWorldPos,
            interactRadius,
            farmLayer
        );

        if (hit == null) return;

        FarmTile tile = hit.GetComponent<FarmTile>();
        if (tile == null) return;

        if (currentTool == ToolType.Hoe)
            UseHoe(tile);
        else if (currentTool == ToolType.WateringCan)
            UseWateringCan(tile);
    }

    private void UseHoe(FarmTile tile)
    {
        if (playerStats == null) return;

        if (!playerStats.HasEnergy(15f))
        {
            Debug.Log("Không đủ Energy để cuốc đất!");
            return;
        }

        playerStats.UseEnergy(15f);
        playerStats.AddXP(2);

        if (toolAnimation != null)
            toolAnimation.UseHoe(() => tile.Hoe());
        else
            tile.Hoe();
    }

    private void UseWateringCan(FarmTile tile)
    {
        if (playerStats == null) return;

        if (!playerStats.HasEnergy(10f))
        {
            Debug.Log("Không đủ Energy để tưới nước!");
            return;
        }

        playerStats.UseEnergy(10f);
        playerStats.AddXP(1);

        if (toolAnimation != null)
            toolAnimation.UseWateringCan(() => tile.Water());
        else
            tile.Water();
    }

    private void OnDisable()
    {
        CursorManager.Instance?.SetDefaultCursor();
    }
}