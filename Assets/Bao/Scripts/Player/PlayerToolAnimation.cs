using System.Collections;
using UnityEngine;

public class PlayerToolAnimation : MonoBehaviour
{
    public enum PlayerActionState
    {
        Idle,
        Walking,
        Hoeing,
        Watering
    }

    [Header("State")]
    public PlayerActionState currentState = PlayerActionState.Idle;

    [Header("References")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private ToolProgressUI toolProgressUI;

    [Header("Hoe")]
    [SerializeField] private GameObject hoeSprite;
    [SerializeField] private Transform hoeTransform;
    [SerializeField] private SpriteRenderer hoeRenderer;

    [Header("Watering Can")]
    [SerializeField] private GameObject wateringCanSprite;
    [SerializeField] private Transform wateringCanTransform;
    [SerializeField] private SpriteRenderer wateringCanRenderer;
    [SerializeField] private ParticleSystem waterParticle;
    [SerializeField] private Transform waterSpawnPoint;

    [Header("Sound")]
    [SerializeField] private AudioSource hoeAudioSource;
    [SerializeField] private AudioSource waterAudioSource;
    [SerializeField] private AudioClip hoeSound;
    [SerializeField] private AudioClip waterSound;

    [Header("Camera Shake")]
[SerializeField] private CameraShake cameraShake;
[SerializeField] private float hoeShakeDuration = 0.08f;
[SerializeField] private float hoeShakeStrength = 0.04f;
[SerializeField] private float waterShakeDuration = 0.03f;
[SerializeField] private float waterShakeStrength = 0.01f;

    [Header("Settings")]
    [SerializeField] private float hoeDuration = 0.7f;
    [SerializeField] private float wateringDuration = 1.0f;

    [SerializeField] private float hoeWindupAngle = 30f;
    [SerializeField] private float hoeHitAngle = 65f;
    [SerializeField] private float wateringTiltAngle = 22f;

    [SerializeField] private float hoeWindupTime = 0.06f;
    [SerializeField] private float hoeHitTime = 0.09f;
    [SerializeField] private float hoeReturnTime = 0.06f;

    [SerializeField] private float wateringTiltTime = 0.18f;
    [SerializeField] private float wateringReturnTime = 0.18f;

    private bool isUsingTool;
    private float currentAngle;
    private Coroutine activeToolLoop;

    private void Awake()
    {
        if (playerController == null)
            playerController = GetComponent<PlayerController>();

        if (hoeAudioSource == null)
            hoeAudioSource = GetComponent<AudioSource>();

        if (waterAudioSource == null)
            waterAudioSource = GetComponent<AudioSource>();

        if (hoeSprite != null)
            hoeSprite.SetActive(false);

        if (wateringCanSprite != null)
            wateringCanSprite.SetActive(false);
    }

    public bool IsBusy()
    {
        return isUsingTool;
    }

    public void SetWalkingState(bool isWalking)
    {
        if (isUsingTool) return;

        currentState = isWalking
            ? PlayerActionState.Walking
            : PlayerActionState.Idle;
    }

    public void ShowHoe()
{
    if (isUsingTool) return;

    HideWateringCan();

    if (hoeSprite != null)
        hoeSprite.SetActive(true);

    if (hoeTransform != null)
        hoeTransform.localRotation = Quaternion.identity;
}
    public void ShowWateringCan()
{
    if (isUsingTool) return;

    HideHoe();

    if (wateringCanSprite != null)
        wateringCanSprite.SetActive(true);

    if (wateringCanTransform != null)
        wateringCanTransform.localRotation = Quaternion.identity;
}

    public void HideHoe()
    {
        if (hoeSprite != null)
            hoeSprite.SetActive(false);
    }

    public void HideWateringCan()
    {
        if (wateringCanSprite != null)
            wateringCanSprite.SetActive(false);
    }

    private void RotateToolToMouse(
        Transform tool,
        SpriteRenderer renderer,
        Vector3 mouseWorldPos)
    {
        Vector2 dir = mouseWorldPos - tool.position;

        currentAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        tool.rotation = Quaternion.Euler(0, 0, currentAngle);

        if (renderer != null)
            renderer.flipY = dir.x < 0;
    }

    public void UseHoe(System.Action onComplete)
    {
        if (isUsingTool) return;

        StartCoroutine(HoeRoutine(onComplete));
    }

    public void UseWateringCan(System.Action onComplete)
    {
        if (isUsingTool) return;

        StartCoroutine(WateringRoutine(onComplete));
    }

    private IEnumerator HoeRoutine(System.Action onComplete)
    {
        BeginAction(PlayerActionState.Hoeing, "Đang cuốc đất...");

        float startAngle = currentAngle;

        PlayHoeSound();

        activeToolLoop = StartCoroutine(
            HoeSwingLoop(startAngle)
        );

        yield return RunProgress(hoeDuration);

        StopHoeSound();
        StopActiveLoop();

        if (hoeTransform != null)
            hoeTransform.rotation = Quaternion.Euler(0, 0, startAngle);

        onComplete?.Invoke();

        FinishAction();
    }

    private IEnumerator WateringRoutine(System.Action onComplete)
    {
        BeginAction(PlayerActionState.Watering, "Đang tưới nước...");

        float startAngle = currentAngle;

        PlayWaterParticle();
        PlayWaterSoundLoop();

        activeToolLoop = StartCoroutine(
            WateringSwingLoop(startAngle)
        );

        yield return RunProgress(wateringDuration);

        StopActiveLoop();
        StopWaterParticle();
        StopWaterSound();

        if (wateringCanTransform != null)
            wateringCanTransform.rotation = Quaternion.Euler(0, 0, startAngle);

        onComplete?.Invoke();

        FinishAction();
    }

    private void BeginAction(PlayerActionState state, string message)
    {
        isUsingTool = true;
        currentState = state;

        if (playerController != null)
            playerController.canMove = false;

        if (toolProgressUI != null)
            toolProgressUI.Show(message);
    }

    private void FinishAction()
    {
        if (playerController != null)
            playerController.canMove = true;

        isUsingTool = false;
        currentState = PlayerActionState.Idle;
    }

    private IEnumerator RunProgress(float duration)
    {
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;

            if (toolProgressUI != null)
                toolProgressUI.SetProgress(1f - timer / duration);

            yield return null;
        }

        if (toolProgressUI != null)
            toolProgressUI.Hide();
    }

    private IEnumerator HoeSwingLoop(float baseAngle)
{
    while (isUsingTool)
    {
        yield return RotateTool(
            hoeTransform,
            baseAngle,
            baseAngle + hoeWindupAngle,
            hoeWindupTime
        );

        if (cameraShake != null)
            cameraShake.Shake(hoeShakeDuration, hoeShakeStrength);

        yield return RotateTool(
            hoeTransform,
            baseAngle + hoeWindupAngle,
            baseAngle - hoeHitAngle,
            hoeHitTime
        );

        yield return RotateTool(
            hoeTransform,
            baseAngle - hoeHitAngle,
            baseAngle,
            hoeReturnTime
        );
    }
}

    private IEnumerator WateringSwingLoop(float baseAngle)
{
    while (isUsingTool)
    {
        if (cameraShake != null)
            cameraShake.Shake(waterShakeDuration, waterShakeStrength);

        yield return RotateTool(
            wateringCanTransform,
            baseAngle,
            baseAngle - wateringTiltAngle,
            wateringTiltTime
        );

        yield return RotateTool(
            wateringCanTransform,
            baseAngle - wateringTiltAngle,
            baseAngle,
            wateringReturnTime
        );
    }
}

    private IEnumerator RotateTool(
        Transform tool,
        float fromAngle,
        float toAngle,
        float duration)
    {
        if (tool == null)
            yield break;

        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;

            float t = Mathf.Clamp01(timer / duration);
            float angle = Mathf.LerpAngle(fromAngle, toAngle, t);

            tool.rotation = Quaternion.Euler(0, 0, angle);

            yield return null;
        }

        tool.rotation = Quaternion.Euler(0, 0, toAngle);
    }

    private void StopActiveLoop()
    {
        if (activeToolLoop == null) return;

        StopCoroutine(activeToolLoop);
        activeToolLoop = null;
    }

    private void PlayWaterParticle()
    {
        if (waterParticle == null) return;

        if (waterSpawnPoint != null)
        {
            waterParticle.transform.position = waterSpawnPoint.position;
            waterParticle.transform.rotation = waterSpawnPoint.rotation;
        }

        waterParticle.Stop();
        waterParticle.Clear();
        waterParticle.Play();
    }

    private void StopWaterParticle()
    {
        if (waterParticle == null) return;

        waterParticle.Stop();
        waterParticle.Clear();
    }

    private void PlayHoeSound()
    {
        if (hoeAudioSource == null || hoeSound == null)
            return;

        hoeAudioSource.Stop();
        hoeAudioSource.clip = hoeSound;
        hoeAudioSource.loop = true;
        hoeAudioSource.Play();
    }

    private void StopHoeSound()
    {
        if (hoeAudioSource == null)
            return;

        hoeAudioSource.Stop();
        hoeAudioSource.loop = false;
        hoeAudioSource.clip = null;
    }

    private void PlayWaterSoundLoop()
    {
        if (waterAudioSource == null || waterSound == null)
            return;

        waterAudioSource.Stop();
        waterAudioSource.clip = waterSound;
        waterAudioSource.loop = true;
        waterAudioSource.Play();
    }

    private void StopWaterSound()
    {
        if (waterAudioSource == null) return;

        waterAudioSource.Stop();
        waterAudioSource.loop = false;
        waterAudioSource.clip = null;
    }

    public void UpdateToolDirection(Vector2 dir)
{
    float angle = 0f;

    if (dir == Vector2.up)
        angle = 90f;
    else if (dir == Vector2.down)
        angle = -90f;
    else if (dir == Vector2.left)
        angle = 180f;
    else if (dir == Vector2.right)
        angle = 0f;

    if (hoeTransform != null && hoeSprite.activeSelf)
    {
        hoeTransform.rotation = Quaternion.Euler(0, 0, angle);

        if (hoeRenderer != null)
            hoeRenderer.flipY = dir == Vector2.left;
    }

    if (wateringCanTransform != null && wateringCanSprite.activeSelf)
    {
        wateringCanTransform.rotation = Quaternion.Euler(0, 0, angle);

        if (wateringCanRenderer != null)
            wateringCanRenderer.flipY = dir == Vector2.left;
    }
}
}