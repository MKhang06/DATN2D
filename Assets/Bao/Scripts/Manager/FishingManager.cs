using System.Collections;
using UnityEngine;

public class FishingManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private InventoryManager inventoryManager;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private PlayerToolAnimation toolAnimation;

    [Header("Fishing Objects")]
    [SerializeField] private GameObject bobberObject;
    [SerializeField] private GameObject biteIconObject;
    [SerializeField] private GameObject splashObject;

    [Header("Fish Data")]
    [SerializeField] private Sprite[] fishSprites;
    [SerializeField] private string[] fishNames;

    [Header("Settings")]
    [SerializeField] private LayerMask waterLayer;
    [SerializeField] private float fishingEnergyCost = 8f;
    [SerializeField] private float waitMin = 2f;
    [SerializeField] private float waitMax = 5f;
    [SerializeField] private float catchTimeLimit = 2f;
    [SerializeField] private float waterCheckRadius = 0.35f;

    [Header("Audio Source")]
    [SerializeField] private AudioSource fishingAudio;

    [Header("Fishing Audio Clips")]
    [SerializeField] private AudioClip castSound;
    [SerializeField] private AudioClip splashSound;
    [SerializeField] private AudioClip fishBiteSound;
    [SerializeField] private AudioClip reelSound;
    [SerializeField] private AudioClip fishCatchSound;
    [SerializeField] private AudioClip inventoryPopSound;

    [Header("Catch Panel")]
    [SerializeField] private FishCatchPanelUI fishCatchPanelUI;
    [SerializeField] private float minFishKg = 0.5f;
    [SerializeField] private float maxFishKg = 8f;

    private Camera cam;
    private bool isFishing;
    private bool fishBiting;
    private Coroutine fishingRoutine;
    private Coroutine audioRoutine;
    private Vector3 fishingPos;

    private void Awake()
    {
        cam = Camera.main;

        if (playerStats == null)
            playerStats = GetComponent<PlayerStats>();

        if (inventoryManager == null)
            inventoryManager = InventoryManager.Instance;

        if (playerController == null)
            playerController = GetComponent<PlayerController>();

        if (toolAnimation == null)
            toolAnimation = GetComponent<PlayerToolAnimation>();

        if (bobberObject != null)
            bobberObject.SetActive(false);

        if (biteIconObject != null)
            biteIconObject.SetActive(false);

        if (splashObject != null)
            splashObject.SetActive(false);
    }

    private void Update()
    {
        if (fishBiting && Input.GetKeyDown(KeyCode.Space))
            CatchFish();
    }

    public void TryStartFishing()
    {
        if (isFishing)
            return;

        if (playerStats == null)
        {
            Debug.LogError("Thiếu PlayerStats.");
            return;
        }

        if (!playerStats.HasEnergy(fishingEnergyCost))
        {
            Debug.Log("Không đủ Energy để câu cá!");
            return;
        }

        if (cam == null)
            cam = Camera.main;

        Vector3 mouseWorldPos = cam.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0f;

        Collider2D hit = Physics2D.OverlapCircle(
            mouseWorldPos,
            waterCheckRadius,
            waterLayer
        );

        if (hit == null)
        {
            Debug.Log("Phải quăng ở khu vực có nước!");
            return;
        }

        playerStats.UseEnergy(fishingEnergyCost);

        fishingPos = mouseWorldPos;

        LockPlayerFishing();

        PlayFishingSound(castSound);

        fishingRoutine = StartCoroutine(FishingRoutine());
    }

    private IEnumerator FishingRoutine()
    {
        isFishing = true;
        fishBiting = false;

        Debug.Log("Quăng cần câu.");

        yield return new WaitForSeconds(0.2f);

        if (splashObject != null)
        {
            splashObject.transform.position = fishingPos;
            splashObject.SetActive(true);
        }

        PlayFishingSound(splashSound);

        yield return new WaitForSeconds(0.25f);

        if (splashObject != null)
            splashObject.SetActive(false);

        if (bobberObject != null)
        {
            bobberObject.transform.position = fishingPos;
            bobberObject.SetActive(true);
        }

        float waitTime = Random.Range(waitMin, waitMax);
        yield return new WaitForSeconds(waitTime);

        if (!isFishing)
            yield break;

        fishBiting = true;

        if (biteIconObject != null)
        {
            biteIconObject.transform.position =
                fishingPos + Vector3.up * 1.1f;

            biteIconObject.SetActive(true);
        }

        PlayFishingSound(fishBiteSound);

        Debug.Log("Cá cắn câu! Bấm SPACE.");

        yield return new WaitForSeconds(catchTimeLimit);

        if (fishBiting)
            FailFishing();
    }

    private void CatchFish()
    {
        if (!isFishing)
            return;

        fishBiting = false;
        isFishing = false;

        if (fishingRoutine != null)
            StopCoroutine(fishingRoutine);

        StartCoroutine(CatchFishRoutine());
    }

    private IEnumerator CatchFishRoutine()
    {
        if (biteIconObject != null)
            biteIconObject.SetActive(false);

        PlayFishingSound(reelSound);

        yield return new WaitForSeconds(0.35f);

        if (bobberObject != null)
            bobberObject.SetActive(false);

        PlayFishingSound(fishCatchSound);

        if (fishSprites == null || fishSprites.Length == 0)
        {
            Debug.LogWarning("Chưa gắn Fish Sprites.");
            UnlockPlayerFishing();
            yield break;
        }

        int index = Random.Range(0, fishSprites.Length);

        string fishName = "Cá";

        if (fishNames != null &&
            fishNames.Length > index &&
            !string.IsNullOrEmpty(fishNames[index]))
        {
            fishName = fishNames[index];
        }

        Sprite fishSprite = fishSprites[index];

        yield return new WaitForSeconds(0.15f);

        float fishKg = Random.Range(minFishKg, maxFishKg);

        if (fishCatchPanelUI != null)
        {
            fishCatchPanelUI.ShowFish(fishName, fishSprite, fishKg);
            PlayFishingSound(inventoryPopSound);
        }
        else if (inventoryManager != null)
        {
            inventoryManager.AddItem(fishName, fishSprite, 1);
            PlayFishingSound(inventoryPopSound);
        }
        else
        {
            Debug.LogWarning("Thiếu InventoryManager, cá chưa vào túi.");
        }
        Debug.Log("Bắt được cá: " + fishName);

        UnlockPlayerFishing();
    }

    private void FailFishing()
    {
        if (!isFishing)
            return;

        fishBiting = false;
        isFishing = false;

        if (biteIconObject != null)
            biteIconObject.SetActive(false);

        if (bobberObject != null)
            bobberObject.SetActive(false);

        if (splashObject != null)
            splashObject.SetActive(false);

        Debug.Log("Cá chạy mất!");

        UnlockPlayerFishing();
    }

    private void PlayFishingSound(AudioClip clip)
    {
        if (fishingAudio == null || clip == null)
            return;

        if (audioRoutine != null)
            StopCoroutine(audioRoutine);

        fishingAudio.Stop();
        fishingAudio.clip = clip;
        fishingAudio.loop = false;
        fishingAudio.Play();

        audioRoutine = StartCoroutine(StopFishingSoundAfterClip(clip.length));
    }

    private IEnumerator StopFishingSoundAfterClip(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (fishingAudio != null)
        {
            fishingAudio.Stop();
            fishingAudio.clip = null;
        }

        audioRoutine = null;
    }

    private void LockPlayerFishing()
    {
        if (playerController != null)
            playerController.canMove = false;

        if (toolAnimation != null)
            toolAnimation.SetFishingLock(true);
    }

    private void UnlockPlayerFishing()
    {
        if (playerController != null)
            playerController.canMove = true;

        if (toolAnimation != null)
            toolAnimation.SetFishingLock(false);
    }
}