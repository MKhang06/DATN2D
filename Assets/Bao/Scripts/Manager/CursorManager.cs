using UnityEngine;

[DisallowMultipleComponent]
[DefaultExecutionOrder(32760)]
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

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Instance = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RestoreCursorBeforeSceneLoad()
    {
        EnsureCursorAvailable();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        SetDefaultCursor();
    }

    private void OnEnable()
    {
        EnsureCursorAvailable();
    }

    private void Start()
    {
        SetDefaultCursor();
    }

    private void LateUpdate()
    {
        EnsureCursorAvailable();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus)
            SetDefaultCursor();
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (!pauseStatus)
            SetDefaultCursor();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public static void EnsureCursorAvailable()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void SetDefaultCursor()
    {
        ApplyCursor(defaultCursor);
    }

    public void SetHoeCursor()
    {
        ApplyCursor(hoeCursor);
    }

    public void SetWateringCursor()
    {
        ApplyCursor(wateringCursor);
    }

    private void ApplyCursor(Texture2D cursorTexture)
    {
        EnsureCursorAvailable();
        Cursor.SetCursor(cursorTexture, hotspot, cursorMode);
    }
}
