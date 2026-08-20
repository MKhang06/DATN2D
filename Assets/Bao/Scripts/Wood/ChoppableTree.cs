using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[DisallowMultipleComponent]
public class ChoppableTree : MonoBehaviour
{
    [Header("Minigame")]
    [SerializeField, Min(1)]
    private int requiredSuccessfulHits = 8;

    [SerializeField, Range(1, 10)]
    private int maximumMisses = 3;

    [SerializeField]
    private WoodChopMinigameUI minigameUI;

    [Header("Interaction")]
    [SerializeField]
    private string playerTag = "Player";

    [SerializeField]
    private bool requireAxeSelected;

    [SerializeField]
    private string[] axeKeywords =
    {
        "Axe",
        "Hatchet",
        "Rìu",
        "Riu",
        "Chop"
    };

    [SerializeField]
    private GameObject interactionPrompt;

    [SerializeField]
    private bool createPromptAutomatically = true;

    [SerializeField]
    private Vector3 promptLocalPosition =
        new Vector3(0f, 1.35f, 0f);

    [Header("Wood Reward")]
    [Tooltip("Kéo InventoryItemData của vật phẩm Gỗ vào đây.")]
    [SerializeField]
    private InventoryItemData woodItem;

    [Tooltip(
        "Tên dự phòng khi chưa kéo Wood Item. " +
        "Tên này phải tồn tại trong ItemDatabase."
    )]
    [SerializeField]
    private string fallbackWoodItemName = "Gỗ";

    [SerializeField]
    private Sprite fallbackWoodIcon;

    [SerializeField, Min(1)]
    private int minimumWoodReward = 3;

    [SerializeField, Min(1)]
    private int maximumWoodReward = 6;

    [Header("Tree Behaviour")]
    [SerializeField]
    private Animator treeAnimator;

    [SerializeField]
    private string chopTriggerName = "Chop";

    [SerializeField]
    private string fallTriggerName = "Fall";

    [SerializeField, Min(0f)]
    private float destroyDelay = 0.15f;

    [SerializeField]
    private bool respawnTree;

    [SerializeField, Min(1f)]
    private float respawnSeconds = 60f;

    [Header("Audio")]
    [SerializeField]
    private AudioSource audioSource;

    [SerializeField]
    private AudioClip hitSound;

    [SerializeField]
    private AudioClip successSound;

    [SerializeField]
    private AudioClip failSound;

    [Header("Events")]
    [SerializeField]
    private UnityEvent onSuccessfulHit;

    [SerializeField]
    private UnityEvent onMiss;

    [SerializeField]
    private UnityEvent onTreeCompleted;

    [SerializeField]
    private UnityEvent onMinigameFailed;

    private bool playerInRange;
    private bool treeCompleted;
    private Collider2D interactionTrigger;
    private SpriteRenderer[] cachedRenderers;
    private Collider2D[] cachedColliders;
    private Coroutine interactionRoutine;

    public int RequiredSuccessfulHits =>
        Mathf.Max(1, requiredSuccessfulHits);

    public int MaximumMisses =>
        Mathf.Max(1, maximumMisses);

    private void Awake()
    {
        ResolveReferences();
        CacheTreeComponents();
        SetPromptVisible(false);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsPlayer(other))
            return;

        playerInRange = true;

        if (!treeCompleted)
        {
            CreatePromptIfNeeded();
            SetPromptVisible(true);
            StartInteractionPolling();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!IsPlayer(other))
            return;

        playerInRange = false;
        SetPromptVisible(false);
        StopInteractionPolling();
    }

    public void TryStartMinigame()
    {
        if (treeCompleted)
            return;

        ResolveReferences();

        if (requireAxeSelected &&
            !HasAxeSelected())
        {
            ShowNotification(
                "Bạn cần cầm rìu để chặt cây.",
                false
            );

            return;
        }

        if (minigameUI == null)
        {
            Debug.LogError(
                "Không tìm thấy WoodChopMinigameUI.",
                this
            );

            return;
        }

        bool started =
            minigameUI.StartGame(this);

        if (started)
            SetPromptVisible(false);
    }

    /// <summary>
    /// Copies gameplay settings from a configured source tree while keeping
    /// scene-object references local to this tree.
    /// </summary>
    public void CopySettingsFrom(ChoppableTree source)
    {
        if (source == null || source == this)
            return;

        requiredSuccessfulHits = source.requiredSuccessfulHits;
        maximumMisses = source.maximumMisses;
        minigameUI = source.minigameUI;

        playerTag = source.playerTag;
        requireAxeSelected = source.requireAxeSelected;
        axeKeywords = source.axeKeywords != null
            ? (string[])source.axeKeywords.Clone()
            : Array.Empty<string>();

        // A prompt belongs to one tree only. Sharing the source reference
        // would make one tree hide/show an unrelated UI object.
        interactionPrompt = null;
        createPromptAutomatically = source.createPromptAutomatically;
        promptLocalPosition = source.promptLocalPosition;

        woodItem = source.woodItem;
        fallbackWoodItemName = source.fallbackWoodItemName;
        fallbackWoodIcon = source.fallbackWoodIcon;
        minimumWoodReward = source.minimumWoodReward;
        maximumWoodReward = source.maximumWoodReward;

        chopTriggerName = source.chopTriggerName;
        fallTriggerName = source.fallTriggerName;
        destroyDelay = source.destroyDelay;
        respawnTree = source.respawnTree;
        respawnSeconds = source.respawnSeconds;

        hitSound = source.hitSound;
        successSound = source.successSound;
        failSound = source.failSound;

        ResolveReferences();
        CacheTreeComponents();
    }

    public void NotifySuccessfulHit()
    {
        if (treeAnimator != null &&
            !string.IsNullOrWhiteSpace(chopTriggerName))
        {
            treeAnimator.ResetTrigger(chopTriggerName);
            treeAnimator.SetTrigger(chopTriggerName);
        }

        PlaySound(hitSound);
        onSuccessfulHit?.Invoke();
    }

    public void NotifyMiss()
    {
        onMiss?.Invoke();
    }

    public int RollWoodReward()
    {
        int min = Mathf.Max(1, minimumWoodReward);
        int max = Mathf.Max(min, maximumWoodReward);

        return UnityEngine.Random.Range(min, max + 1);
    }

    public bool TryCompleteTree(
        int rewardAmount,
        out string reason)
    {
        reason = string.Empty;

        if (treeCompleted)
        {
            reason = "Cây đã được chặt.";
            return false;
        }

        InventoryManager inventory =
            InventoryManager.Instance;

        if (inventory == null)
        {
            inventory =
                FindFirstObjectByType<InventoryManager>(
                    FindObjectsInactive.Include
                );
        }

        if (inventory == null)
        {
            reason = "Không tìm thấy túi đồ.";
            ShowNotification(reason, false);
            return false;
        }

        bool added;

        if (woodItem != null)
        {
            added =
                inventory.TryAddItem(
                    woodItem,
                    rewardAmount,
                    out reason
                );
        }
        else
        {
            added =
                inventory.AddItem(
                    fallbackWoodItemName,
                    fallbackWoodIcon,
                    rewardAmount
                );

            if (!added &&
                string.IsNullOrWhiteSpace(reason))
            {
                reason = "Không thể thêm Gỗ vào túi.";
            }
        }

        if (!added)
        {
            ShowNotification(
                string.IsNullOrWhiteSpace(reason)
                    ? "Balo không đủ chỗ."
                    : reason,
                false
            );

            return false;
        }

        treeCompleted = true;
        StopInteractionPolling();
        PlaySound(successSound);

        if (treeAnimator != null &&
            !string.IsNullOrWhiteSpace(fallTriggerName))
        {
            treeAnimator.ResetTrigger(fallTriggerName);
            treeAnimator.SetTrigger(fallTriggerName);
        }

        onTreeCompleted?.Invoke();

        ShowNotification(
            "Bạn nhận được " +
            rewardAmount +
            " Gỗ.",
            true
        );

        SetPromptVisible(false);
        StartCoroutine(RemoveOrRespawnRoutine());

        return true;
    }

    public void NotifyMinigameFailed()
    {
        PlaySound(failSound);
        onMinigameFailed?.Invoke();

        ShowNotification(
            "Bạn đã sai 3 lần. Hãy thử lại.",
            false
        );

        if (playerInRange)
            SetPromptVisible(true);
    }

    public void NotifyMinigameCancelled()
    {
        if (playerInRange &&
            !treeCompleted)
        {
            SetPromptVisible(true);
        }
    }

    private IEnumerator RemoveOrRespawnRoutine()
    {
        if (destroyDelay > 0f)
            yield return new WaitForSeconds(destroyDelay);

        if (!respawnTree)
        {
            Destroy(gameObject);
            yield break;
        }

        SetTreeVisible(false);

        yield return new WaitForSeconds(respawnSeconds);

        treeCompleted = false;
        SetTreeVisible(true);

        if (playerInRange)
        {
            SetPromptVisible(true);
            StartInteractionPolling();
        }
    }

    private IEnumerator WaitForInteractionRoutine()
    {
        while (playerInRange && !treeCompleted)
        {
            if (WoodChopMinigameUI.Instance?.IsPlaying != true &&
                WasInteractPressed())
            {
                TryStartMinigame();
            }

            yield return null;
        }

        interactionRoutine = null;
    }

    private void StartInteractionPolling()
    {
        if (interactionRoutine == null)
        {
            interactionRoutine =
                StartCoroutine(WaitForInteractionRoutine());
        }
    }

    private void StopInteractionPolling()
    {
        if (interactionRoutine == null)
            return;

        StopCoroutine(interactionRoutine);
        interactionRoutine = null;
    }

    private void SetTreeVisible(bool visible)
    {
        if (cachedRenderers != null)
        {
            foreach (SpriteRenderer renderer in cachedRenderers)
            {
                if (renderer != null)
                    renderer.enabled = visible;
            }
        }

        if (cachedColliders != null)
        {
            foreach (Collider2D collider in cachedColliders)
            {
                if (collider == null)
                    continue;

                if (collider == interactionTrigger)
                {
                    collider.enabled = true;
                    continue;
                }

                collider.enabled = visible;
            }
        }
    }

    private bool HasAxeSelected()
    {
        InventoryManager inventory =
            InventoryManager.Instance;

        if (inventory == null)
            return false;

        InventoryManager.InventorySlot slot =
            inventory.SelectedSlot;

        if (slot == null ||
            slot.IsEmpty ||
            string.IsNullOrWhiteSpace(slot.itemName))
        {
            return false;
        }

        string selectedName =
            Normalize(slot.itemName);

        if (axeKeywords == null)
            return false;

        foreach (string keyword in axeKeywords)
        {
            string normalizedKeyword =
                Normalize(keyword);

            if (string.IsNullOrWhiteSpace(normalizedKeyword))
                continue;

            if (selectedName.Contains(normalizedKeyword))
                return true;
        }

        return false;
    }

    private bool IsPlayer(Collider2D other)
    {
        if (other == null)
            return false;

        if (!string.IsNullOrWhiteSpace(playerTag) &&
            other.CompareTag(playerTag))
        {
            return true;
        }

        Transform current = other.transform;

        while (current != null)
        {
            if (string.Equals(
                    current.name,
                    "Player",
                    StringComparison.OrdinalIgnoreCase
                ))
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    private bool WasInteractPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null &&
               Keyboard.current.eKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.E);
#endif
    }

    private void ResolveReferences()
    {
        if (minigameUI == null)
            minigameUI = WoodChopMinigameUI.Instance;

        if (minigameUI == null)
        {
            minigameUI =
                FindFirstObjectByType<WoodChopMinigameUI>(
                    FindObjectsInactive.Include
                );
        }

        if (treeAnimator == null)
            treeAnimator = GetComponentInChildren<Animator>(true);

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (interactionTrigger == null)
        {
            Collider2D[] colliders =
                GetComponents<Collider2D>();

            foreach (Collider2D collider in colliders)
            {
                if (collider != null && collider.isTrigger)
                {
                    interactionTrigger = collider;
                    break;
                }
            }
        }
    }

    private void CacheTreeComponents()
    {
        cachedRenderers =
            GetComponentsInChildren<SpriteRenderer>(true);

        cachedColliders =
            GetComponentsInChildren<Collider2D>(true);
    }

    private void CreatePromptIfNeeded()
    {
        if (interactionPrompt != null ||
            !createPromptAutomatically)
        {
            return;
        }

        GameObject prompt =
            new GameObject("ChopPrompt");

        prompt.transform.SetParent(transform, false);
        prompt.transform.localPosition = promptLocalPosition;

        TextMeshPro text =
            prompt.AddComponent<TextMeshPro>();

        text.text = "[E] CHẶT CÂY";
        text.fontSize = 3.5f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.sortingOrder = 200;

        interactionPrompt = prompt;
    }

    private void SetPromptVisible(bool visible)
    {
        if (interactionPrompt != null)
            interactionPrompt.SetActive(visible);
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip == null)
            return;

        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.PlayOneShot(clip);
    }

    private void ShowNotification(
        string message,
        bool success)
    {
        if (FishingNotificationUI.Instance != null)
        {
            FishingNotificationUI.Instance.ShowNotification(
                message,
                success
                    ? FishingNotificationUI.NotificationType.Success
                    : FishingNotificationUI.NotificationType.Warning
            );

            return;
        }

        if (success)
            Debug.Log(message, this);
        else
            Debug.LogWarning(message, this);
    }

    private static string Normalize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        return value.Trim()
            .ToLowerInvariant()
            .Replace("ì", "i")
            .Replace("í", "i")
            .Replace("ỉ", "i")
            .Replace("ĩ", "i")
            .Replace("ị", "i")
            .Replace("ư", "u")
            .Replace("ù", "u")
            .Replace("ú", "u")
            .Replace("ủ", "u")
            .Replace("ũ", "u")
            .Replace("ụ", "u");
    }
}
