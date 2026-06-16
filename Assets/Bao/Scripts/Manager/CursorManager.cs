using UnityEngine;

public class CursorManager : MonoBehaviour
{
    public static CursorManager Instance;

    [Header("Cursor Textures")]
    [SerializeField] private Texture2D defaultCursor;
    [SerializeField] private Texture2D hoeCursor;
    [SerializeField] private Texture2D wateringCursor;

    [Header("Settings")]
    [SerializeField] private Vector2 hotspot = Vector2.zero;
    [SerializeField] private CursorMode cursorMode = CursorMode.Auto;

    private void Awake()
    {
        Instance = this;
        SetDefaultCursor();
    }

    public void SetDefaultCursor()
    {
        Cursor.SetCursor(defaultCursor, hotspot, cursorMode);
    }

    public void SetHoeCursor()
    {
        Cursor.SetCursor(hoeCursor, hotspot, cursorMode);
    }

    public void SetWateringCursor()
    {
        Cursor.SetCursor(wateringCursor, hotspot, cursorMode);
    }
}