using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FishingBarMiniGameUI : MonoBehaviour
{
    public enum State
    {
        Hidden,
        SelectingDepth,
        WaitingBite,
        Reeling
    }

    [Header("Root")]
    [SerializeField] private GameObject root;

    [Header("Left Bar - Lực người chơi")]
    [SerializeField] private Image playerPullFill;

    [Header("Middle Bar - Chọn độ sâu")]
    [SerializeField] private RectTransform waterMaskRect;
    [SerializeField] private RectTransform greenZoneRect;
    [SerializeField] private RectTransform fishIconRect;
    [SerializeField] private Image fishIconImage;

    [Header("Right Bar - Lực cá kéo")]
    [SerializeField] private Image fishForceFill;

    [Header("Text")]
    [SerializeField] private TMP_Text depthText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text hintText;

    [Header("Depth Settings")]
    [SerializeField] private float depthMoveSpeed = 4f;
    [SerializeField] private float fishIconOffsetX = 0f;

    [Header("Power Settings")]
    [SerializeField] private float playerPullGainSpeed = 0.35f;
    [SerializeField] private float playerPullLoseSpeed = 0.04f;

    [SerializeField] private float fishForceGainSpeed = 0.25f;
    [SerializeField] private float fishForceRecoverSpeed = 0.35f;

    [SerializeField] private float fishRandomMin = 0.6f;
    [SerializeField] private float fishRandomMax = 1.5f;

    [Header("Fish Icon Shake")]
    [SerializeField] private float fishIconShakeAmount = 10f;
    [SerializeField] private float fishIconShakeSpeed = 6f;

    private State state = State.Hidden;

    private float maxDepth = 10f;
    private float currentDepth = 5f;

    private float playerPull;
    private float fishForce;

    private float fishDifficulty = 1f;
    private float currentFishPull = 1f;
    private float nextPullChangeTime;

    private Action<float> onDepthSelected;
    private Action<bool> onReelFinished;

    private void Awake()
    {
        Hide();
    }

    private void Update()
    {
        if (state == State.SelectingDepth)
        {
            UpdateDepthSelect();
        }
        else if (state == State.Reeling)
        {
            UpdateReeling();
        }

        UpdateVisual();
    }

    public void ShowDepthSelect(float maxDepthValue, Action<float> callback)
    {
        maxDepth = Mathf.Max(1f, maxDepthValue);
        currentDepth = maxDepth * 0.5f;

        playerPull = 0f;
        fishForce = 0f;

        onDepthSelected = callback;
        state = State.SelectingDepth;

        if (root != null)
            root.SetActive(true);

        HideFishIcon();

        if (statusText != null)
            statusText.text = "CHỌN ĐỘ SÂU";

        if (hintText != null)
            hintText.text = "W/S hoặc ↑/↓ để chỉnh độ sâu - SPACE để thả móc";

        UpdateVisual();
    }

    private void UpdateDepthSelect()
    {
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            currentDepth -= depthMoveSpeed * Time.deltaTime;

        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            currentDepth += depthMoveSpeed * Time.deltaTime;

        currentDepth = Mathf.Clamp(currentDepth, 0f, maxDepth);

        if (Input.GetKeyDown(KeyCode.Space) ||
            Input.GetKeyDown(KeyCode.Return))
        {
            state = State.WaitingBite;

            if (statusText != null)
                statusText.text = "ĐÃ THẢ MÓC";

            if (hintText != null)
                hintText.text = "Đang chờ cá cắn ở độ sâu " +
                                currentDepth.ToString("0.0") + "m";

            onDepthSelected?.Invoke(currentDepth);
        }
    }

    public void ShowWaitingBite()
    {
        state = State.WaitingBite;

        if (root != null)
            root.SetActive(true);

        HideFishIcon();

        if (statusText != null)
            statusText.text = "ĐANG ĐỢI CÁ CẮN...";

        if (hintText != null)
            hintText.text = "Mồi đang ở độ sâu " +
                            currentDepth.ToString("0.0") + "m";

        UpdateVisual();
    }

    public void ShowFishBite(Sprite fishSprite)
    {
        if (fishIconRect != null)
            fishIconRect.gameObject.SetActive(true);

        if (fishIconImage != null && fishSprite != null)
            fishIconImage.sprite = fishSprite;

        UpdateFishIconPosition(false);

        if (statusText != null)
            statusText.text = "CÁ CẮN CÂU!";

        if (hintText != null)
            hintText.text = "Giữ SPACE để kéo cá - thả SPACE khi lực cá quá cao!";
    }

    public void ShowJunkBite(Sprite junkSprite)
    {
        if (fishIconRect != null)
            fishIconRect.gameObject.SetActive(true);

        if (fishIconImage != null && junkSprite != null)
            fishIconImage.sprite = junkSprite;

        UpdateFishIconPosition(false);

        if (statusText != null)
            statusText.text = "CÓ VẬT PHẨM LẠ!";

        if (hintText != null)
            hintText.text = "Bạn câu được vật phẩm lạ.";
    }

    public void StartReelGame(float fishDifficultyValue, Action<bool> callback)
    {
        fishDifficulty = Mathf.Max(0.1f, fishDifficultyValue);

        playerPull = 0f;
        fishForce = 0f;

        onReelFinished = callback;
        state = State.Reeling;

        currentFishPull = 1f;
        nextPullChangeTime = Time.time + 0.5f;

        if (root != null)
            root.SetActive(true);

        if (statusText != null)
            statusText.text = "CÁ ĐANG KÉO!";

        if (hintText != null)
            hintText.text = "Giữ SPACE để kéo - thả SPACE khi lực cá quá cao";

        UpdateVisual();
    }

    private void UpdateReeling()
    {
        UpdateFishPull();

        bool holdingSpace = Input.GetKey(KeyCode.Space);

        if (holdingSpace)
        {
            playerPull += playerPullGainSpeed * Time.deltaTime;

            fishForce +=
                fishForceGainSpeed *
                fishDifficulty *
                currentFishPull *
                Time.deltaTime;
        }
        else
        {
            playerPull -= playerPullLoseSpeed * Time.deltaTime;
            fishForce -= fishForceRecoverSpeed * Time.deltaTime;
        }

        playerPull = Mathf.Clamp01(playerPull);
        fishForce = Mathf.Clamp01(fishForce);

        UpdateFishIconPosition(true);

        if (fishForce >= 1f)
        {
            FinishReel(false);
            return;
        }

        if (playerPull >= 1f)
        {
            FinishReel(true);
        }
    }

    private void UpdateFishPull()
    {
        if (Time.time < nextPullChangeTime)
            return;

        currentFishPull = UnityEngine.Random.Range(fishRandomMin, fishRandomMax);

        nextPullChangeTime =
            Time.time + UnityEngine.Random.Range(0.35f, 1f);
    }

    private void FinishReel(bool success)
    {
        state = State.Hidden;

        if (statusText != null)
            statusText.text = success ? "KÉO THÀNH CÔNG!" : "ĐỨT DÂY CÂU!";

        if (hintText != null)
            hintText.text = success ? "Bắt được cá!" : "Lực cá quá cao, dây bị đứt.";

        onReelFinished?.Invoke(success);

        Invoke(nameof(Hide), 0.5f);
    }

    private void UpdateVisual()
    {
        if (playerPullFill != null)
            playerPullFill.fillAmount = playerPull;

        if (fishForceFill != null)
            fishForceFill.fillAmount = fishForce;

        UpdateGreenZonePosition();
        UpdateDepthText();
    }

    private void UpdateGreenZonePosition()
    {
        if (waterMaskRect == null || greenZoneRect == null)
            return;

        float y = GetGreenZoneY();

        greenZoneRect.anchoredPosition =
            new Vector2(0f, y);
    }

    private float GetGreenZoneY()
    {
        if (waterMaskRect == null || greenZoneRect == null)
            return 0f;

        float t = maxDepth <= 0f ? 0f : currentDepth / maxDepth;

        float waterHeight = waterMaskRect.rect.height;
        float greenHeight = greenZoneRect.rect.height;

        float topY = waterHeight * 0.5f - greenHeight * 0.5f;
        float bottomY = -waterHeight * 0.5f + greenHeight * 0.5f;

        return Mathf.Lerp(topY, bottomY, t);
    }

    private void UpdateFishIconPosition(bool shake)
    {
        if (fishIconRect == null || greenZoneRect == null)
            return;

        float y = greenZoneRect.anchoredPosition.y;

        if (shake && state == State.Reeling)
        {
            y += Mathf.Sin(Time.time * fishIconShakeSpeed) *
                 fishIconShakeAmount;
        }

        fishIconRect.anchoredPosition =
            new Vector2(fishIconOffsetX, y);
    }

    private void UpdateDepthText()
    {
        if (depthText == null)
            return;

        depthText.text =
            currentDepth.ToString("0.0") +
            "m / " +
            maxDepth.ToString("0.0") +
            "m";
    }

    private void HideFishIcon()
    {
        if (fishIconRect != null)
            fishIconRect.gameObject.SetActive(false);
    }

    public void Hide()
    {
        state = State.Hidden;

        if (root != null)
            root.SetActive(false);

        HideFishIcon();
    }
}