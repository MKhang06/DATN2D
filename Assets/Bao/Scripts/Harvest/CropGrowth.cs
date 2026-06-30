using UnityEngine;

public class CropGrowth : MonoBehaviour
{
    [Header("Sprites")]
    [SerializeField] private Sprite[] growthSprites;

    [Header("Growth")]
    [SerializeField] private int daysPerStage = 1;
    [SerializeField] private bool isRegrowCrop = true;
    [SerializeField] private int regrowStageIndex = 4;
    [SerializeField] private int regrowDays = 2;

    private SpriteRenderer spriteRenderer;
    private int currentStage;
    private int dayCounter;
    private bool readyToHarvest;

    public bool IsReadyToHarvest => readyToHarvest;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        currentStage = 0;
        readyToHarvest = false;
        UpdateVisual();
    }

    private void OnEnable()
    {
        if (GameTimeManager.Instance != null)
            GameTimeManager.Instance.OnDayChanged += GrowOneDay;
    }

    private void OnDisable()
    {
        if (GameTimeManager.Instance != null)
            GameTimeManager.Instance.OnDayChanged -= GrowOneDay;
    }

    public void GrowOneDay()
    {
        if (readyToHarvest) return;

        dayCounter++;

        int requiredDays =
            isRegrowCrop && currentStage == regrowStageIndex
                ? regrowDays
                : daysPerStage;

        if (dayCounter < requiredDays) return;

        dayCounter = 0;
        currentStage++;

        if (currentStage >= growthSprites.Length - 1)
        {
            currentStage = growthSprites.Length - 1;
            readyToHarvest = true;
        }

        UpdateVisual();

        Debug.Log("Crop stage: " + currentStage + " Ready: " + readyToHarvest);
    }

    public void AfterHarvest()
    {
        if (isRegrowCrop)
        {
            currentStage = regrowStageIndex;
            readyToHarvest = false;
            dayCounter = 0;
            UpdateVisual();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void UpdateVisual()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (growthSprites == null || growthSprites.Length == 0)
            return;

        currentStage = Mathf.Clamp(currentStage, 0, growthSprites.Length - 1);
        spriteRenderer.sprite = growthSprites[currentStage];
    }

    [ContextMenu("TEST/Grow One Day")]
    private void TestGrowOneDay()
    {
        GrowOneDay();
    }

    [ContextMenu("TEST/Set Ready To Harvest")]
    private void TestSetReady()
    {
        currentStage = growthSprites.Length - 1;
        readyToHarvest = true;
        UpdateVisual();
    }
}