using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerToolController : MonoBehaviour
{
    public enum ToolType
    {
        None,
        Hoe,
        WateringCan,
        Axe,
        Pickaxe,
        Sickle
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
            ToggleTool(ToolType.Hoe);

        if (Input.GetKeyDown(KeyCode.E))
            ToggleTool(ToolType.WateringCan);

        if (Input.GetKeyDown(KeyCode.R))
            ToggleTool(ToolType.Axe);

        if (Input.GetKeyDown(KeyCode.T))
            ToggleTool(ToolType.Pickaxe);

        if (Input.GetKeyDown(KeyCode.Y))
            ToggleTool(ToolType.Sickle);
    }

    private void ToggleTool(ToolType tool)
    {
        if (toolAnimation == null) return;

        if (currentTool == tool)
        {
            currentTool = ToolType.None;
            toolAnimation.HideAllTools();
            CursorManager.Instance?.SetDefaultCursor();
            return;
        }

        currentTool = tool;
        toolAnimation.ShowTool(tool);
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

        switch (currentTool)
        {
            case ToolType.Hoe:
                UseHoe(tile);
                break;

            case ToolType.WateringCan:
                UseWateringCan(tile);
                break;

            case ToolType.Axe:
                UseAxe(tile);
                break;

            case ToolType.Pickaxe:
                UsePickaxe(tile);
                break;

            case ToolType.Sickle:
                UseSickle(tile);
                break;
        }
    }

    private void UseHoe(FarmTile tile)
    {
        if (playerStats == null) return;

        float cost = GetEnergyCost(15f);

        if (!playerStats.HasEnergy(cost))
        {
            Debug.Log("Không đủ Energy để cuốc đất!");
            return;
        }

        playerStats.UseEnergy(cost);
        playerStats.AddXP(2);

        if (toolAnimation != null)
            toolAnimation.UseHoe(() => tile.Hoe());
        else
            tile.Hoe();
    }

    private void UseWateringCan(FarmTile tile)
    {
        if (playerStats == null) return;

        float cost = GetEnergyCost(10f);

        if (!playerStats.HasEnergy(cost))
        {
            Debug.Log("Không đủ Energy để tưới nước!");
            return;
        }

        playerStats.UseEnergy(cost);
        playerStats.AddXP(1);

        if (toolAnimation != null)
            toolAnimation.UseWateringCan(() => tile.Water());
        else
            tile.Water();
    }

    private void UseAxe(FarmTile tile)
    {
        if (playerStats == null) return;

        float cost = GetEnergyCost(20f);

        if (!playerStats.HasEnergy(cost))
        {
            Debug.Log("Không đủ Energy để dùng rìu!");
            return;
        }

        playerStats.UseEnergy(cost);
        playerStats.AddXP(3);

        if (toolAnimation != null)
            toolAnimation.UseAxe(() => tile.Axe());
        else
            tile.Axe();
    }

    private void UsePickaxe(FarmTile tile)
    {
        if (playerStats == null) return;

        float cost = GetEnergyCost(20f);

        if (!playerStats.HasEnergy(cost))
        {
            Debug.Log("Không đủ Energy để dùng cuốc chim!");
            return;
        }

        playerStats.UseEnergy(cost);
        playerStats.AddXP(3);

        if (toolAnimation != null)
            toolAnimation.UsePickaxe(() => tile.Pickaxe());
        else
            tile.Pickaxe();
    }

    private void UseSickle(FarmTile tile)
    {
        if (playerStats == null) return;

        float cost = GetEnergyCost(8f);

        if (!playerStats.HasEnergy(cost))
        {
            Debug.Log("Không đủ Energy để dùng liềm!");
            return;
        }

        playerStats.UseEnergy(cost);
        playerStats.AddXP(2);

        if (toolAnimation != null)
            toolAnimation.UseSickle(() => tile.Sickle());
        else
            tile.Sickle();
    }

    private float GetEnergyCost(float baseCost)
    {
        float multiplier = 1f;

        if (CharacterPassiveManager.Instance != null)
            multiplier = CharacterPassiveManager.Instance.EnergyCostMultiplier;

        return baseCost * multiplier;
    }

    private void OnDisable()
    {
        CursorManager.Instance?.SetDefaultCursor();
    }
}