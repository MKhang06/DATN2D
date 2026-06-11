using UnityEngine;

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
    [SerializeField] private float interactDistance = 1f;
    [SerializeField] private float interactRadius = 0.5f;
    [SerializeField] private LayerMask farmLayer;

    [Header("References")]
    [SerializeField] private PlayerToolAnimation toolAnimation;
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private PlayerController playerController;

    private void Awake()
    {
        if (toolAnimation == null)
            toolAnimation = GetComponent<PlayerToolAnimation>();

        if (playerStats == null)
            playerStats = GetComponent<PlayerStats>();

        if (playerController == null)
            playerController = GetComponent<PlayerController>();
    }

    private void Update()
    {
        if (toolAnimation != null && playerController != null)
        {
            toolAnimation.UpdateToolDirection(playerController.LastDirection);
        }

        if (toolAnimation != null && toolAnimation.IsBusy())
            return;

        ChangeTool();
        UseTool();
    }

    private void ChangeTool()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            if (currentTool == ToolType.Hoe)
            {
                currentTool = ToolType.None;
                toolAnimation.HideHoe();
            }
            else
            {
                currentTool = ToolType.Hoe;
                toolAnimation.ShowHoe();
            }
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            if (currentTool == ToolType.WateringCan)
            {
                currentTool = ToolType.None;
                toolAnimation.HideWateringCan();
            }
            else
            {
                currentTool = ToolType.WateringCan;
                toolAnimation.ShowWateringCan();
            }
        }
    }

   private void UseTool()
{
    if (!Input.GetMouseButtonDown(0)) return;
    if (currentTool == ToolType.None) return;
    if (toolAnimation == null || playerController == null) return;

    Vector2 checkPos =
        (Vector2)transform.position +
        playerController.LastDirection.normalized * interactDistance;
    Collider2D hit = Physics2D.OverlapCircle(
        checkPos,
        interactRadius,
        farmLayer
    );

    if (hit == null) return;

    FarmTile tile = hit.GetComponent<FarmTile>();
    if (tile == null) return;

    if (playerStats == null)
    {
        Debug.LogError("PlayerStats bị null!");
        return;
    }

    if (currentTool == ToolType.Hoe)
    {
        if (!playerStats.HasEnergy(15f))
        {
            Debug.Log("Không đủ Energy để cuốc đất!");
            return;
        }

        playerStats.UseEnergy(15f);
        playerStats.AddXP(2);

        toolAnimation.UseHoe(() =>
        {
            tile.Hoe();
        });

        Debug.Log("Energy sau khi cuốc: " + playerStats.currentEnergy);
    }
    else if (currentTool == ToolType.WateringCan)
    {
        if (!playerStats.HasEnergy(10f))
        {
            Debug.Log("Không đủ Energy để tưới nước!");
            return;
        }

        playerStats.UseEnergy(10f);
        playerStats.AddXP(1);

        toolAnimation.UseWateringCan(() =>
        {
            tile.Water();
        });

        Debug.Log("Energy sau khi tưới: " + playerStats.currentEnergy);
    }
}


    private void OnDrawGizmosSelected()
    {
        if (playerController == null) return;

        Vector2 checkPos =
            (Vector2)transform.position +
            playerController.LastDirection.normalized * interactDistance;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(checkPos, interactRadius);
    }
}