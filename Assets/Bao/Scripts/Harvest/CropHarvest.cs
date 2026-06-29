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
    [SerializeField] private HarvestMiniGameUI harvestMiniGameUI;

    [Header("Audio")]
    [SerializeField] private AudioSource leafAudio;
    [SerializeField] private AudioClip leafRustleSound;

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
            return;

        if (!cropGrowth.IsReadyToHarvest)
        {
            Debug.Log("Cây chưa chín!");
            return;
        }

        isHarvesting = true;

        if (harvestMiniGameUI != null)
            harvestMiniGameUI.StartMiniGame(OnHarvestMiniGameFinished);
        else
            StartCoroutine(HarvestRoutine());
    }

    private void OnHarvestMiniGameFinished(bool success)
    {
        if (success)
        {
            StartCoroutine(HarvestRoutine());
        }
        else
        {
            isHarvesting = false;

            if (progressUI != null)
                progressUI.Hide();

            Debug.Log("Thu hoạch thất bại!");
        }
    }

    private IEnumerator HarvestRoutine()
    {
        if (progressUI != null)
            yield return progressUI.PlayProgress("Đang thu hoạch...", harvestTime);

        bool sickleFinished = false;

        if (toolAnimation != null)
        {
            toolAnimation.UseSickle(() =>
            {
                sickleFinished = true;
            });

            while (!sickleFinished)
                yield return null;
        }

        if (leafParticle != null)
            leafParticle.Play();

        if (leafAudio != null && leafRustleSound != null)
            leafAudio.PlayOneShot(leafRustleSound);

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