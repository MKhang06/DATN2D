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

    [Header("Notification")]
    [Tooltip("Có thể để trống, code sẽ tự tìm FishingNotificationUI.")]
    [SerializeField] private FishingNotificationUI notificationUI;

    [Header("Fishing Objects")]
    [SerializeField] private GameObject bobberObject;
    [SerializeField] private GameObject biteIconObject;
    [SerializeField] private GameObject splashObject;
    [Tooltip("Có thể để trống. Game sẽ tự tạo bóng cá từ icon của cá.")]
    [SerializeField] private GameObject fishShadowObject;
    [Tooltip("Điểm thu phao về. Có thể để trống để dùng vị trí Player.")]
    [SerializeField] private Transform rodTipPoint;

    [Header("Water Check")]
    [SerializeField] private LayerMask waterLayer;
    [SerializeField, Min(0.05f)] private float waterCheckRadius = 0.55f;

    [Header("Energy")]
    [SerializeField] private float fishingEnergyCost = 8f;

    [Header("Wait Time")]
    [SerializeField, Min(0f)] private float waitMin = 5f;
    [SerializeField, Min(0f)] private float waitMax = 16f;
    [SerializeField, Min(1f)] private float maxWaitDuration = 30f;

    [Header("Fish Approach")]
    [SerializeField, Min(0.1f)] private float fishApproachDuration = 1.6f;
    [SerializeField, Min(0.1f)] private float fishShadowStartDistance = 2.6f;
    [SerializeField, Min(0f)] private float fishShadowDepthOffset = 0.38f;
    [SerializeField, Min(0.05f)] private float fishShadowWorldWidth = 0.85f;
    [SerializeField] private Color fishShadowColor =
        new Color(0.015f, 0.12f, 0.16f, 0.48f);

    [Header("Nibble Animation")]
    [SerializeField, Min(1)] private int minNibbleCount = 2;
    [SerializeField, Min(1)] private int maxNibbleCount = 4;
    [SerializeField, Min(0.01f)] private float nibbleDipDistance = 0.11f;
    [SerializeField, Min(0.01f)] private float nibbleMoveDuration = 0.12f;
    [SerializeField, Min(0f)] private float nibblePauseMin = 0.18f;
    [SerializeField, Min(0f)] private float nibblePauseMax = 0.42f;
    [SerializeField, Min(0.05f)] private float autoRetractDuration = 0.45f;

    [Header("Chance")]
    [SerializeField] private float baseFishChance = 68f;
    [SerializeField] private float noBaitPenalty = 18f;

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
    private Collider2D currentWaterCollider;
    private SpriteRenderer fishShadowRenderer;
    private Vector3 bobberDefaultScale = Vector3.one;
    private bool bobberDefaultsCached;

    private void Awake()
    {
        cam = Camera.main;

        if (playerStats == null)
            playerStats = GetComponent<PlayerStats>();

        if (gearStats == null)
            gearStats = GetComponent<FishingGearStats>();

        if (inventoryManager == null)
            inventoryManager = InventoryManager.Instance;

        FindNotificationUI();
        CacheBobberDefaults();
        FindRodTipPoint();
        HideFishingObjects();
    }

    private void OnDisable()
    {
        if (!isFishing)
            return;

        isFishing = false;
        currentLoot = null;
        currentWaterCollider = null;

        HideFishingObjects();

        if (fishingBarUI != null)
            fishingBarUI.Hide();

        GameLockManager.Instance?.UnlockPlayer();
    }

    public void TryStartFishing()
    {
        if (cam == null)
            cam = Camera.main;

        if (cam == null)
        {
            Debug.LogWarning("Không tìm thấy Camera Main.");
            return;
        }

        Vector3 mouseWorldPos =
            cam.ScreenToWorldPoint(Input.mousePosition);

        mouseWorldPos.z = 0f;
        TryStartFishingAt(mouseWorldPos);
    }

    public void TryStartFishingAt(Vector3 castWorldPosition)
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

        castWorldPosition.z = 0f;

        Collider2D hit = FindWaterCollider(castWorldPosition);

        if (hit == null)
        {
            Debug.Log("Phải quăng cần ở khu vực nước.");
            return;
        }

        currentWaterCollider = hit;

        FishingWaterZone zone =
            hit.GetComponentInParent<FishingWaterZone>();

        float zoneDepth = zone != null
            ? zone.ZoneMaxDepth
            : gearStats.MaxDepth;

        currentMaxDepth = Mathf.Min(
            gearStats.MaxDepth,
            zoneDepth
        );

        fishingPos = castWorldPosition;
        isFishing = true;

        GameLockManager.Instance?.LockPlayer();

        if (fishingBarUI != null)
        {
            fishingBarUI.ShowDepthSelect(
                currentMaxDepth,
                OnDepthSelected
            );
        }
        else
        {
            OnDepthSelected(currentMaxDepth * 0.5f);
        }
    }

    private Collider2D FindWaterCollider(Vector3 castWorldPosition)
    {
        if (waterLayer.value == 0)
            waterLayer = LayerMask.GetMask("Water");

        Collider2D[] hits = Physics2D.OverlapCircleAll(
            castWorldPosition,
            Mathf.Max(0.05f, waterCheckRadius),
            waterLayer
        );

        Collider2D nearest = null;
        float nearestDistance = float.MaxValue;

        foreach (Collider2D candidate in hits)
        {
            if (candidate == null || !candidate.enabled)
                continue;

            Vector2 closestPoint =
                candidate.ClosestPoint(castWorldPosition);

            float distance = Vector2.SqrMagnitude(
                closestPoint - (Vector2)castWorldPosition
            );

            if (distance >= nearestDistance)
                continue;

            nearestDistance = distance;
            nearest = candidate;
        }

        return nearest;
    }

    private void OnDepthSelected(float depth)
    {
        selectedDepth = Mathf.Clamp(
            depth,
            0f,
            currentMaxDepth
        );

        playerStats.UseEnergy(fishingEnergyCost);

        StartCoroutine(FishingRoutine());
    }

    private IEnumerator FishingRoutine()
    {
        if (fishingBarUI != null)
            fishingBarUI.ShowWaitingBite();

        Debug.Log(
            "Thả móc ở độ sâu: " +
            selectedDepth.ToString("0.0") +
            "m"
        );

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
            ResetBobberVisual();
            bobberObject.SetActive(true);
        }

        float castStartedAt = Time.time;
        bool baitAvailable = gearStats.HasBait;

        currentLoot = RollLoot(
            selectedDepth,
            baitAvailable
        );

        if (currentLoot == null)
        {
            yield return WaitAndAutoRetract(castStartedAt);
            yield break;
        }

        if (baitAvailable)
            gearStats.TryConsumeBait();

        float minimumDelay = Mathf.Max(0f, waitMin);
        float maximumDelay = Mathf.Max(minimumDelay, waitMax);
        float preBiteBudget = GetPreBiteAnimationBudget(currentLoot.isFish);
        float latestApproachStart = Mathf.Max(
            0f,
            maxWaitDuration - preBiteBudget
        );

        float encounterDelay = Mathf.Min(
            Random.Range(minimumDelay, maximumDelay),
            latestApproachStart
        );

        yield return new WaitForSeconds(encounterDelay);

        if (!isFishing)
            yield break;

        if (currentLoot.isFish)
        {
            if (fishingBarUI != null)
                fishingBarUI.ShowFishApproaching();

            yield return AnimateFishApproach(currentLoot);

            if (!isFishing)
                yield break;
        }

        if (fishingBarUI != null)
            fishingBarUI.ShowNibbling();

        yield return AnimateNibbleSequence();

        if (!isFishing)
            yield break;

        if (biteIconObject != null)
        {
            biteIconObject.transform.position =
                fishingPos + Vector3.up * 1.1f;

            biteIconObject.SetActive(true);
        }

        PlaySound(biteSound);
        HideFishShadow();

        if (fishingBarUI != null &&
            currentLoot != null)
        {
            if (currentLoot.isFish)
            {
                fishingBarUI.ShowFishBite(
                    currentLoot.icon
                );
            }
            else
            {
                fishingBarUI.ShowJunkBite(
                    currentLoot.icon
                );
            }
        }

        yield return new WaitForSeconds(0.4f);

        if (biteIconObject != null)
            biteIconObject.SetActive(false);

        /*
         * Câu được vật phẩm rác thì không mở
         * minigame kéo cá.
         */
        if (!currentLoot.isFish)
        {
            CatchJunk(currentLoot);
            yield break;
        }

        /*
         * Câu được cá thì bắt đầu minigame kéo cá.
         */
        StartFishBattle(currentLoot);
    }

    private float GetPreBiteAnimationBudget(bool isFish)
    {
        int nibbleCount = Mathf.Max(
            minNibbleCount,
            maxNibbleCount
        );

        float nibbleBudget = nibbleCount *
            (nibbleMoveDuration * 2f +
             Mathf.Max(nibblePauseMin, nibblePauseMax));

        return nibbleBudget +
               (isFish ? fishApproachDuration : 0f) +
               0.5f;
    }

    private IEnumerator WaitAndAutoRetract(float castStartedAt)
    {
        float elapsed = Time.time - castStartedAt;
        float remaining = Mathf.Max(
            0f,
            maxWaitDuration - elapsed
        );

        if (remaining > 0f)
            yield return new WaitForSeconds(remaining);

        if (!isFishing)
            yield break;

        if (fishingBarUI != null)
            fishingBarUI.ShowAutoRetract();

        PlaySound(reelSound);
        yield return AnimateAutoRetract();

        if (!isFishing)
            yield break;

        const string timeoutMessage =
            "Đã chờ 30 giây nhưng không có cá cắn câu. Cần câu đã tự thu lại.";

        Debug.Log(timeoutMessage);
        NotifyFishingTimedOut(timeoutMessage);
        CleanupFishing();
    }

    private IEnumerator AnimateFishApproach(FishingLoot fish)
    {
        if (!PrepareFishShadow(fish))
            yield break;

        float direction = Random.value < 0.5f
            ? -1f
            : 1f;

        Vector3 target =
            fishingPos +
            Vector3.down * fishShadowDepthOffset;

        target = ClampToCurrentWater(target);

        Vector3 start = ClampToCurrentWater(
            target +
            Vector3.right * direction * fishShadowStartDistance
        );

        if (Vector3.Distance(start, target) < 0.3f)
        {
            direction *= -1f;
            start = ClampToCurrentWater(
                target +
                Vector3.right * direction * fishShadowStartDistance
            );
        }

        fishShadowObject.transform.position = start;
        SetFishShadowFacing(direction);
        fishShadowObject.SetActive(true);

        float duration = Mathf.Max(0.05f, fishApproachDuration);
        float elapsed = 0f;

        while (elapsed < duration && isFishing)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / duration);
            float eased = t * t * (3f - 2f * t);

            Vector3 position = Vector3.Lerp(start, target, eased);
            position.y += Mathf.Sin(eased * Mathf.PI) * 0.08f;
            fishShadowObject.transform.position = position;

            Color color = fishShadowColor;
            color.a *= Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t * 3f));
            fishShadowRenderer.color = color;

            yield return null;
        }

        if (fishShadowObject != null)
            fishShadowObject.transform.position = target;
    }

    private IEnumerator AnimateNibbleSequence()
    {
        if (bobberObject == null)
        {
            yield return new WaitForSeconds(0.6f);
            yield break;
        }

        CacheBobberDefaults();

        Vector3 basePosition = fishingPos;
        int low = Mathf.Max(1, minNibbleCount);
        int high = Mathf.Max(low, maxNibbleCount);
        int count = Random.Range(low, high + 1);

        for (int i = 0; i < count && isFishing; i++)
        {
            float strength = Random.Range(0.75f, 1.15f);
            Vector3 dipPosition =
                basePosition +
                Vector3.down * nibbleDipDistance * strength;

            yield return MoveBobber(
                basePosition,
                dipPosition,
                nibbleMoveDuration
            );

            yield return MoveBobber(
                dipPosition,
                basePosition,
                nibbleMoveDuration
            );

            if (!isFishing)
                yield break;

            float pause = Random.Range(
                Mathf.Min(nibblePauseMin, nibblePauseMax),
                Mathf.Max(nibblePauseMin, nibblePauseMax)
            );

            if (pause > 0f)
                yield return new WaitForSeconds(pause);
        }

        if (!isFishing)
            yield break;

        Vector3 finalDip =
            basePosition +
            Vector3.down * nibbleDipDistance * 2.1f;

        yield return MoveBobber(
            basePosition,
            finalDip,
            nibbleMoveDuration * 0.8f
        );

        yield return MoveBobber(
            finalDip,
            basePosition,
            nibbleMoveDuration * 0.65f
        );

        ResetBobberVisual();
    }

    private IEnumerator MoveBobber(
        Vector3 from,
        Vector3 to,
        float duration)
    {
        if (bobberObject == null)
            yield break;

        duration = Mathf.Max(0.01f, duration);
        float elapsed = 0f;

        while (elapsed < duration && isFishing)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = Mathf.SmoothStep(0f, 1f, t);

            bobberObject.transform.position =
                Vector3.Lerp(from, to, t);

            float squash = 1f - Mathf.Sin(t * Mathf.PI) * 0.08f;
            bobberObject.transform.localScale =
                new Vector3(
                    bobberDefaultScale.x * (2f - squash),
                    bobberDefaultScale.y * squash,
                    bobberDefaultScale.z
                );

            yield return null;
        }

        if (bobberObject != null)
            bobberObject.transform.position = to;
    }

    private IEnumerator AnimateAutoRetract()
    {
        if (bobberObject == null)
            yield break;

        Vector3 start = bobberObject.transform.position;
        Vector3 target = rodTipPoint != null
            ? rodTipPoint.position
            : playerStats != null
                ? playerStats.transform.position + Vector3.up * 0.2f
                : start;

        float duration = Mathf.Max(0.05f, autoRetractDuration);
        float elapsed = 0f;

        while (elapsed < duration && isFishing)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);

            Vector3 position = Vector3.Lerp(start, target, eased);
            position.y += Mathf.Sin(t * Mathf.PI) * 0.35f;
            bobberObject.transform.position = position;

            yield return null;
        }

        ResetBobberVisual();
    }

    private bool PrepareFishShadow(FishingLoot fish)
    {
        if (fish == null || fish.icon == null)
            return false;

        if (fishShadowObject == null)
        {
            fishShadowObject = new GameObject("Fish Shadow (Runtime)");
            fishShadowObject.transform.SetParent(transform, true);
        }

        fishShadowRenderer =
            fishShadowObject.GetComponentInChildren<SpriteRenderer>();

        if (fishShadowRenderer == null)
            fishShadowRenderer = fishShadowObject.AddComponent<SpriteRenderer>();

        fishShadowRenderer.sprite = fish.icon;
        fishShadowRenderer.color = fishShadowColor;

        SpriteRenderer bobberRenderer = bobberObject != null
            ? bobberObject.GetComponentInChildren<SpriteRenderer>()
            : null;

        if (bobberRenderer != null)
        {
            fishShadowRenderer.sortingLayerID =
                bobberRenderer.sortingLayerID;
            fishShadowRenderer.sortingOrder =
                bobberRenderer.sortingOrder - 1;
        }

        float spriteWidth = Mathf.Max(
            0.001f,
            fish.icon.bounds.size.x
        );

        float scale = fishShadowWorldWidth / spriteWidth;
        fishShadowObject.transform.localScale =
            new Vector3(scale, scale * 0.62f, 1f);

        return true;
    }

    private Vector3 ClampToCurrentWater(Vector3 position)
    {
        if (currentWaterCollider == null)
            return position;

        Vector2 clamped = currentWaterCollider.ClosestPoint(position);
        return new Vector3(clamped.x, clamped.y, position.z);
    }

    private void SetFishShadowFacing(float approachDirection)
    {
        if (fishShadowObject == null)
            return;

        Vector3 scale = fishShadowObject.transform.localScale;
        scale.x = Mathf.Abs(scale.x) *
                  (approachDirection > 0f ? -1f : 1f);
        fishShadowObject.transform.localScale = scale;
    }

    private void CacheBobberDefaults()
    {
        if (bobberObject == null || bobberDefaultsCached)
            return;

        bobberDefaultScale = bobberObject.transform.localScale;
        bobberDefaultsCached = true;
    }

    private void ResetBobberVisual()
    {
        if (bobberObject == null)
            return;

        CacheBobberDefaults();
        bobberObject.transform.localScale = bobberDefaultScale;
    }

    private void HideFishShadow()
    {
        if (fishShadowObject != null)
            fishShadowObject.SetActive(false);
    }

    private void FindRodTipPoint()
    {
        if (rodTipPoint != null)
            return;

        Transform[] children = GetComponentsInChildren<Transform>(true);

        foreach (Transform child in children)
        {
            if (child != null && child.name == "RodTipPoint")
            {
                rodTipPoint = child;
                return;
            }
        }
    }

    private FishingLoot RollLoot(
        float depth,
        bool baitUsed)
    {
        FishingLoot[] availableFish =
            GetLootByDepth(fishLoots, depth);

        float fishChance = baseFishChance;

        fishChance += gearStats.HookFishChanceBonus;

        if (baitUsed)
        {
            fishChance +=
                gearStats.BaitFishChanceBonus;
        }
        else
        {
            fishChance -= noBaitPenalty;
        }

        if (availableFish.Length <= 0)
            fishChance = 0f;

        fishChance = Mathf.Clamp(
            fishChance,
            0f,
            95f
        );

        float roll = Random.Range(0f, 100f);

        if (roll <= fishChance)
        {
            return PickWeightedLoot(
                availableFish,
                depth,
                baitUsed
            );
        }

        FishingLoot[] availableJunk =
            GetLootByDepth(junkLoots, depth);

        return PickWeightedLoot(
            availableJunk,
            depth,
            false
        );
    }

    private FishingLoot[] GetLootByDepth(
        FishingLoot[] list,
        float depth)
    {
        List<FishingLoot> result =
            new List<FishingLoot>();

        if (list == null)
            return result.ToArray();

        foreach (FishingLoot loot in list)
        {
            if (loot == null)
                continue;

            if (depth >= loot.minDepth &&
                depth <= loot.maxDepth)
            {
                result.Add(loot);
            }
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

            totalWeight += GetLootWeight(
                loot,
                depth,
                baitUsed
            );
        }

        if (totalWeight <= 0f)
            return list[0];

        float roll = Random.Range(
            0f,
            totalWeight
        );

        float current = 0f;

        foreach (FishingLoot loot in list)
        {
            if (loot == null)
                continue;

            current += GetLootWeight(
                loot,
                depth,
                baitUsed
            );

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
        float weight = Mathf.Max(
            0.1f,
            loot.weight
        );

        float middleDepth =
            (loot.minDepth + loot.maxDepth) *
            0.5f;

        float depthDistance =
            Mathf.Abs(depth - middleDepth);

        weight += Mathf.Max(
            0f,
            5f - depthDistance
        );

        if (baitUsed &&
            !string.IsNullOrEmpty(
                loot.preferredBait
            ) &&
            gearStats.CurrentBaitName ==
            loot.preferredBait)
        {
            weight += loot.preferredBaitBonus;
        }

        return weight;
    }

    private void StartFishBattle(
        FishingLoot fish)
    {
        if (fish == null)
        {
            FailFishing(
                "Không tìm thấy dữ liệu cá."
            );

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

    private void OnFishBattleFinished(
        bool success)
    {
        if (success)
        {
            CompleteCatchFish(currentLoot);
        }
        else
        {
            /*
             * Người chơi kéo cá thất bại,
             * thanh FishPower đầy hoặc đứt dây.
             */
            BreakLine();
        }
    }

    private void CompleteCatchFish(
        FishingLoot fish)
    {
        if (fish == null)
        {
            FailFishing(
                "Không tìm thấy cá sau khi kéo thành công."
            );

            return;
        }

        if (fish.isFish)
            GreenFieldQuestEvents.ReportFishCaught();

        PlaySound(catchSound);
        ShowCatchResult(fish);
    }

    private void CatchJunk(
        FishingLoot junk)
    {
        if (junk == null)
        {
            FailFishing(
                "Không câu được vật phẩm nào."
            );

            return;
        }

        PlaySound(junkSound);
        ShowCatchResult(junk);
    }

    /*
     * Có cá cắn nhưng người chơi kéo thất bại.
     */
    private void BreakLine()
    {
        PlaySound(lineBreakSound);

        Debug.Log(
            "Kéo cá thất bại! Cá đã thoát hoặc dây câu bị đứt."
        );

        NotifyFishEscaped();
        CleanupFishing();
    }

    /*
     * Thả dây nhưng không câu được cá
     * hoặc không câu được vật phẩm nào.
     */
    private void FailFishing(string reason)
    {
        Debug.Log(reason);

        NotifyNoFish();
        CleanupFishing();
    }

    private void CleanupFishing()
    {
        isFishing = false;
        currentLoot = null;
        currentWaterCollider = null;

        HideFishingObjects();

        if (fishingBarUI != null)
            fishingBarUI.Hide();

        GameLockManager.Instance?.UnlockPlayer();
    }

    private void HideFishingObjects()
    {
        ResetBobberVisual();

        if (bobberObject != null)
            bobberObject.SetActive(false);

        if (biteIconObject != null)
            biteIconObject.SetActive(false);

        if (splashObject != null)
            splashObject.SetActive(false);

        HideFishShadow();
    }

    private void PlaySound(AudioClip clip)
    {
        if (fishingAudio == null ||
            clip == null)
        {
            return;
        }

        fishingAudio.Stop();
        fishingAudio.clip = clip;
        fishingAudio.loop = false;
        fishingAudio.Play();
    }

    private void ShowCatchResult(
        FishingLoot loot)
    {
        HideFishingObjects();

        if (fishingBarUI != null)
            fishingBarUI.Hide();

        if (inventoryManager == null)
            inventoryManager =
                InventoryManager.Instance;

        float resultWeight = 0f;

        if (loot.isFish)
        {
            resultWeight = Random.Range(
                loot.minWeightKg,
                loot.maxWeightKg
            );
        }

        int resultSellPrice =
            Mathf.RoundToInt(
                loot.baseSellPrice * 0.7f
            );

        if (catchResultUI != null)
        {
            catchResultUI.ShowResult(
                loot.itemName,
                loot.icon,
                loot.isFish,
                resultWeight,
                loot.expReward,
                resultSellPrice,
                playerStats,
                inventoryManager,
                OnCatchResultClosed
            );
        }
        else
        {
            inventoryManager?.AddItem(
                loot.itemName,
                loot.icon,
                1
            );

            CleanupFishing();
        }
    }

    private void OnCatchResultClosed()
    {
        isFishing = false;
        currentLoot = null;
        currentWaterCollider = null;

        GameLockManager.Instance?.UnlockPlayer();
    }

    private void FindNotificationUI()
    {
        if (notificationUI != null)
            return;

        if (FishingNotificationUI.Instance != null)
        {
            notificationUI =
                FishingNotificationUI.Instance;

            return;
        }

        notificationUI =
            FindFirstObjectByType<
                FishingNotificationUI
            >();
    }

    private void NotifyNoFish()
    {
        FindNotificationUI();

        Debug.Log(
            "Hiển thị thông báo: không câu được gì."
        );

        if (notificationUI == null)
        {
            Debug.LogError(
                "Không tìm thấy FishingNotificationUI trong Scene."
            );

            return;
        }

        notificationUI.ShowNoFish();
    }

    private void NotifyFishingTimedOut(string message)
    {
        FindNotificationUI();

        if (notificationUI == null)
        {
            Debug.LogError(
                "Không tìm thấy FishingNotificationUI trong Scene."
            );

            return;
        }

        notificationUI.ShowNotification(
            message,
            FishingNotificationUI.NotificationType.Warning,
            title: "TỰ THU CẦN"
        );
    }

    private void NotifyFishEscaped()
    {
        FindNotificationUI();

        Debug.Log(
            "Hiển thị thông báo: kéo cá thất bại."
        );

        if (notificationUI == null)
        {
            Debug.LogError(
                "Không tìm thấy FishingNotificationUI trong Scene."
            );

            return;
        }

        notificationUI.ShowFishEscaped();
    }
}
