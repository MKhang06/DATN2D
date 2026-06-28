using UnityEngine;
using UnityEngine.EventSystems;

public class TestHarvestInput : MonoBehaviour
{
    [SerializeField] private LayerMask cropLayer;
    [SerializeField] private PlayerToolController toolController;

    private Camera cam;

    private void Awake()
    {
        cam = Camera.main;

        if (toolController == null)
            toolController = GetComponent<PlayerToolController>();
    }

    private void Update()
    {
        if (!Input.GetMouseButtonDown(0))
            return;

        if (EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject())
            return;

        if (toolController == null)
            return;

        if (toolController.currentTool != PlayerToolController.ToolType.Sickle)
        {
            Debug.Log("Phải cầm liềm mới thu hoạch được!");
            return;
        }

        if (cam == null)
            cam = Camera.main;

        Vector2 mousePos = cam.ScreenToWorldPoint(Input.mousePosition);

        Collider2D hit = Physics2D.OverlapCircle(
            mousePos,
            0.3f,
            cropLayer
        );

        if (hit == null)
        {
            FindFirstObjectByType<ToolProgressUI>()?.Hide();
            return;
        }

        CropHarvest crop = hit.GetComponent<CropHarvest>();

        if (crop != null)
            crop.Harvest();
    }
}