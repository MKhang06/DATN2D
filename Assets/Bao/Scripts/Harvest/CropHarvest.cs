using System.Collections;
using UnityEngine;

public class CropHarvest : MonoBehaviour
{
    [Header("Crop Info")]
    [SerializeField] private string cropName = "Blueberry";
    [SerializeField] private Sprite cropIcon;
    [SerializeField] private int amount = 3;

    [Header("References")]
    [SerializeField] private CropGrowth cropGrowth;
    [SerializeField] private GameObject harvestFlyPrefab;
    [SerializeField] private ParticleSystem leafParticle;
    [SerializeField] private Transform flyTarget;
    [SerializeField] private PlayerToolAnimation toolAnimation;
    [SerializeField] private ToolProgressUI progressUI;

    [Header("Settings")]
    [SerializeField] private float harvestTime = 0.8f;

    private bool isHarvesting;

    private void Awake()
    {
        if (cropGrowth == null)
            cropGrowth = GetComponent<CropGrowth>();
    }

    public void Harvest()
    {
        if (isHarvesting)
            return;

        if (progressUI != null)
            progressUI.Hide();

        if (cropGrowth == null)
        {
            Debug.LogWarning("Thiếu CropGrowth.");
            return;
        }

        if (!cropGrowth.IsReadyToHarvest)
        {
            Debug.Log("Cây chưa chín!");
            return;
        }

        StartCoroutine(HarvestRoutine());
    }

    private IEnumerator HarvestRoutine()
    {
        isHarvesting = true;

        if (progressUI != null)
            progressUI.Show("Đang thu hoạch...");

        bool sickleFinished = false;

        if (toolAnimation != null)
        {
            toolAnimation.UseSickle(() =>
            {
                sickleFinished = true;
            });
        }
        else
        {
            sickleFinished = true;
        }

        float timer = 0f;

        while (timer < harvestTime)
        {
            timer += Time.deltaTime;

            if (progressUI != null)
                progressUI.SetProgress(1f - timer / harvestTime);

            yield return null;
        }

        if (progressUI != null)
            progressUI.Hide();

        while (!sickleFinished)
            yield return null;

        if (leafParticle != null)
            leafParticle.Play();

        SpawnHarvestEffect();

        cropGrowth.AfterHarvest();

        isHarvesting = false;
    }

    private void SpawnHarvestEffect()
    {
        if (harvestFlyPrefab == null || flyTarget == null)
        {
            InventoryManager.Instance?.AddItem(cropName, cropIcon, amount);
            return;
        }

        GameObject obj = Instantiate(
            harvestFlyPrefab,
            transform.position + Vector3.up * 0.4f,
            Quaternion.identity
        );

        SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();

        if (sr != null)
            sr.sprite = cropIcon;

        HarvestFlyEffect effect = obj.GetComponent<HarvestFlyEffect>();

        if (effect != null)
        {
            effect.Play(
                transform.position + Vector3.up * 0.4f,
                flyTarget,
                cropName,
                cropIcon,
                amount
            );
        }
    }

    private void OnDisable()
    {
        if (progressUI != null)
            progressUI.Hide();
    }
}