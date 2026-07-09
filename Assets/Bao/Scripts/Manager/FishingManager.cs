using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FishingManager : MonoBehaviour
{
    [System.Serializable]
    public class FishingLoot
    {
        public string itemName;
        public Sprite icon;

        [Header("Type")]
        public bool isFish = true;

        [Header("Depth")]
        public float minDepth = 0f;
        public float maxDepth = 10f;

        [Header("Chance")]
        public float weight = 10f;

        [Header("Fish Power")]
        public float fishPower = 1f;

        [Header("Bait Bonus")]
        public string preferredBait;
        public float preferredBaitBonus = 10f;
        [Header("Result Info")]
        public float minWeightKg = 1f;
        public float maxWeightKg = 5f;
        public float expReward = 0.49f;
        public int baseSellPrice = 100;
    }

    [Header("References")]
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private InventoryManager inventoryManager;
    [SerializeField] private FishingGearStats gearStats;
    [SerializeField] private FishingBarMiniGameUI fishingBarUI;
    [SerializeField] private FishingCatchResultUI catchResultUI;

    [Header("Fishing Objects")]
    [SerializeField] private GameObject bobberObject;
    [SerializeField] private GameObject biteIconObject;
    [SerializeField] private GameObject splashObject;

    [Header("Water Check")]
    [SerializeField] private LayerMask waterLayer;
    [SerializeField] private float waterCheckRadius = 0.35f;

    [Header("Energy")]
    [SerializeField] private float fishingEnergyCost = 8f;

    [Header("Wait Time")]
    [SerializeField] private float waitMin = 2f;
    [SerializeField] private float waitMax = 5f;

    [Header("Chance")]
    [SerializeField] private float baseFishChance = 55f;
    [SerializeField] private float noBaitPenalty = 20f;

    [Header("Loot")]
    [SerializeField] private FishingLoot[] fishLoots;
    [SerializeField] private FishingLoot[] junkLoots;

    [Header("Audio")]
    [SerializeField] private AudioSource fishingAudio;
    [SerializeField] private AudioClip castSound;
    [SerializeField] private AudioClip splashSound;
    [SerializeField] private AudioClip biteSound;
    [SerializeField] private AudioClip reelSound;
    [SerializeField] private AudioClip catchSound;
    [SerializeField] private AudioClip lineBreakSound;
    [SerializeField] private AudioClip junkSound;

    private Camera cam;
    private bool isFishing;

    private Vector3 fishingPos;
    private float selectedDepth;
    private float currentMaxDepth;

    private FishingLoot currentLoot;

    private void Awake()
    {
        cam = Camera.main;

        if (playerStats == null)
            playerStats = GetComponent<PlayerStats>();

        if (gearStats == null)
            gearStats = GetComponent<FishingGearStats>();

        if (inventoryManager == null)
            inventoryManager = InventoryManager.Instance;

        HideFishingObjects();
    }

    public void TryStartFishing()
    {
        if (isFishing)
            return;

        if (playerStats == null)
        {
            Debug.LogWarning("Thiếu PlayerStats.");
            return;
        }

        if (gearStats == null)
        {
            Debug.LogWarning("Thiếu FishingGearStats.");
            return;
        }

        if (!playerStats.HasEnergy(fishingEnergyCost))
        {
            Debug.Log("Không đủ Energy để câu cá.");
            return;
        }

        if (cam == null)
            cam = Camera.main;

        if (cam == null)
        {
            Debug.LogWarning("Không tìm thấy Camera Main.");
            return;
        }

        Vector3 mouseWorldPos = cam.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0f;

        Collider2D hit = Physics2D.OverlapCircle(
            mouseWorldPos,
            waterCheckRadius,
            waterLayer
        );

        if (hit == null)
        {
            Debug.Log("Phải quăng cần ở khu vực nước.");
            return;
        }

        FishingWaterZone zone = hit.GetComponentInParent<FishingWaterZone>();

        float zoneDepth = zone != null
            ? zone.ZoneMaxDepth
            : gearStats.MaxDepth;

        currentMaxDepth = Mathf.Min(gearStats.MaxDepth, zoneDepth);

        fishingPos = mouseWorldPos;
        isFishing = true;

        GameLockManager.Instance?.LockPlayer();

        if (fishingBarUI != null)
            fishingBarUI.ShowDepthSelect(currentMaxDepth, OnDepthSelected);
        else
            OnDepthSelected(currentMaxDepth * 0.5f);
    }

    private void OnDepthSelected(float depth)
    {
        selectedDepth = Mathf.Clamp(depth, 0f, currentMaxDepth);

        playerStats.UseEnergy(fishingEnergyCost);

        StartCoroutine(FishingRoutine());
    }

    private IEnumerator FishingRoutine()
    {
        if (fishingBarUI != null)
            fishingBarUI.ShowWaitingBite();

        Debug.Log("Thả móc ở độ sâu: " + selectedDepth.ToString("0.0") + "m");

        PlaySound(castSound);

        if (splashObject != null)
        {
            splashObject.transform.position = fishingPos;
            splashObject.SetActive(true);
        }

        PlaySound(splashSound);

        yield return new WaitForSeconds(0.3f);

        if (splashObject != null)
            splashObject.SetActive(false);

        if (bobberObject != null)
        {
            bobberObject.transform.position = fishingPos;
            bobberObject.SetActive(true);
        }

        yield return new WaitForSeconds(Random.Range(waitMin, waitMax));

        if (!isFishing)
            yield break;

        bool baitUsed = false;

        if (gearStats.HasBait)
            baitUsed = gearStats.TryConsumeBait();

        if (biteIconObject != null)
        {
            biteIconObject.transform.position = fishingPos + Vector3.up * 1.1f;
            biteIconObject.SetActive(true);
        }

        PlaySound(biteSound);

        currentLoot = RollLoot(selectedDepth, baitUsed);

        if (fishingBarUI != null && currentLoot != null)
        {
            if (currentLoot.isFish)
                fishingBarUI.ShowFishBite(currentLoot.icon);
            else
                fishingBarUI.ShowJunkBite(currentLoot.icon);
        }

        yield return new WaitForSeconds(0.4f);

        if (biteIconObject != null)
            biteIconObject.SetActive(false);

        if (currentLoot == null)
        {
            FailFishing("Không câu được gì.");
            yield break;
        }

        if (!currentLoot.isFish)
        {
            CatchJunk(currentLoot);
            yield break;
        }

        StartFishBattle(currentLoot);
    }

    private FishingLoot RollLoot(float depth, bool baitUsed)
    {
        FishingLoot[] availableFish = GetLootByDepth(fishLoots, depth);

        float fishChance = baseFishChance;

        fishChance += gearStats.HookFishChanceBonus;

        if (baitUsed)
            fishChance += gearStats.BaitFishChanceBonus;
        else
            fishChance -= noBaitPenalty;

        if (availableFish.Length <= 0)
            fishChance = 0f;

        fishChance = Mathf.Clamp(fishChance, 0f, 95f);

        float roll = Random.Range(0f, 100f);

        if (roll <= fishChance)
            return PickWeightedLoot(availableFish, depth, baitUsed);

        FishingLoot[] availableJunk = GetLootByDepth(junkLoots, depth);
        return PickWeightedLoot(availableJunk, depth, false);
    }

    private FishingLoot[] GetLootByDepth(FishingLoot[] list, float depth)
    {
        List<FishingLoot> result = new List<FishingLoot>();

        if (list == null)
            return result.ToArray();

        foreach (FishingLoot loot in list)
        {
            if (loot == null)
                continue;

            if (depth >= loot.minDepth && depth <= loot.maxDepth)
                result.Add(loot);
        }

        return result.ToArray();
    }

    private FishingLoot PickWeightedLoot(
        FishingLoot[] list,
        float depth,
        bool baitUsed)
    {
        if (list == null || list.Length == 0)
            return null;

        float totalWeight = 0f;

        foreach (FishingLoot loot in list)
        {
            if (loot == null)
                continue;

            totalWeight += GetLootWeight(loot, depth, baitUsed);
        }

        if (totalWeight <= 0f)
            return list[0];

        float roll = Random.Range(0f, totalWeight);
        float current = 0f;

        foreach (FishingLoot loot in list)
        {
            if (loot == null)
                continue;

            current += GetLootWeight(loot, depth, baitUsed);

            if (roll <= current)
                return loot;
        }

        return list[0];
    }

    private float GetLootWeight(
        FishingLoot loot,
        float depth,
        bool baitUsed)
    {
        float weight = Mathf.Max(0.1f, loot.weight);

        float middleDepth = (loot.minDepth + loot.maxDepth) * 0.5f;
        float depthDistance = Mathf.Abs(depth - middleDepth);

        weight += Mathf.Max(0f, 5f - depthDistance);

        if (baitUsed &&
            !string.IsNullOrEmpty(loot.preferredBait) &&
            gearStats.CurrentBaitName == loot.preferredBait)
        {
            weight += loot.preferredBaitBonus;
        }

        return weight;
    }

    private void StartFishBattle(FishingLoot fish)
    {
        if (fish == null)
        {
            CleanupFishing();
            return;
        }

        if (fishingBarUI == null)
        {
            CompleteCatchFish(fish);
            return;
        }

        PlaySound(reelSound);

        fishingBarUI.StartReelGame(
            fish.fishPower,
            OnFishBattleFinished
        );
    }

    private void OnFishBattleFinished(bool success)
    {
        if (success)
            CompleteCatchFish(currentLoot);
        else
            BreakLine();
    }

    private void CompleteCatchFish(FishingLoot fish)
    {
        if (fish == null)
        {
            CleanupFishing();
            return;
        }

        ShowCatchResult(fish);
    }

    private void CatchJunk(FishingLoot junk)
    {
        if (junk == null)
        {
            CleanupFishing();
            return;
        }

        ShowCatchResult(junk);
    }

    private void BreakLine()
    {
        PlaySound(lineBreakSound);

        Debug.Log("FishPower đầy! Đứt dây câu, mất cá.");

        CleanupFishing();
    }

    private void FailFishing(string reason)
    {
        Debug.Log(reason);
        CleanupFishing();
    }

    private void CleanupFishing()
    {
        isFishing = false;
        currentLoot = null;

        HideFishingObjects();

        if (fishingBarUI != null)
            fishingBarUI.Hide();

        GameLockManager.Instance?.UnlockPlayer();
    }

    private void HideFishingObjects()
    {
        if (bobberObject != null)
            bobberObject.SetActive(false);

        if (biteIconObject != null)
            biteIconObject.SetActive(false);

        if (splashObject != null)
            splashObject.SetActive(false);
    }

    private void PlaySound(AudioClip clip)
    {
        if (fishingAudio == null || clip == null)
            return;

        fishingAudio.Stop();
        fishingAudio.clip = clip;
        fishingAudio.loop = false;
        fishingAudio.Play();
    }
    private void ShowCatchResult(FishingLoot loot)
    {
        HideFishingObjects();

        if (fishingBarUI != null)
            fishingBarUI.Hide();

        if (inventoryManager == null)
            inventoryManager = InventoryManager.Instance;

        float resultWeight = 0f;

        if (loot.isFish)
        {
            resultWeight = Random.Range(
                loot.minWeightKg,
                loot.maxWeightKg
            );
        }

        int sellPrice = Mathf.RoundToInt(loot.baseSellPrice * 0.7f);

        if (catchResultUI != null)
        {
            catchResultUI.ShowResult(
                loot.itemName,
                loot.icon,
                loot.isFish,
                resultWeight,
                loot.expReward,
                sellPrice,
                playerStats,
                inventoryManager,
                OnCatchResultClosed
            );
        }
        else
        {
            inventoryManager?.AddItem(loot.itemName, loot.icon, 1);
            CleanupFishing();
        }
    }

    private void OnCatchResultClosed()
    {
        isFishing = false;
        currentLoot = null;

        GameLockManager.Instance?.UnlockPlayer();
    }
}