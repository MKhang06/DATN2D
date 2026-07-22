using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public class FishingSupplyShopNPCInteraction :
    MonoBehaviour
{
    [Header("Cửa hàng")]
    [SerializeField]
    private FishingSupplyShopUI shopUI;

    [Header("Prompt")]
    [SerializeField]
    private GameObject promptRoot;

    [SerializeField]
    private TMP_Text promptText;

    [SerializeField]
    private string promptMessage =
        "[E] MUA ĐỒ CÂU";

    [Header("Tương tác")]
    [SerializeField]
    private KeyCode interactionKey =
        KeyCode.E;

    [SerializeField]
    private string playerTag =
        "Player";

    [SerializeField]
    private Transform playerTransform;

    [SerializeField, Min(0.1f)]
    private float interactionDistance =
        2.5f;

    [SerializeField]
    private bool closeWhenLeaving =
        true;

    [Header("Debug")]
    [SerializeField]
    private bool showDebugLogs = true;

    private bool playerInTrigger;
    private bool playerInRange;

    private void Awake()
    {
        Collider2D trigger =
            GetComponent<Collider2D>();

        trigger.isTrigger = true;

        ResolveShop();
        ResolvePlayer();
        SetPrompt(false);
    }

    private void Update()
    {
        ResolveShop();
        ResolvePlayer();
        UpdateRange();

        SetPrompt(
            playerInRange &&
            shopUI != null &&
            !shopUI.IsOpen
        );

        if (!playerInRange)
            return;

        if (!Input.GetKeyDown(
                interactionKey))
        {
            return;
        }

        if (shopUI == null)
        {
            Debug.LogError(
                "[FishingSupplyNPC] Shop UI đang NULL. " +
                "Kéo object có FishingSupplyShopUI vào trường Shop UI.",
                this
            );

            return;
        }

        if (showDebugLogs)
        {
            Debug.Log(
                "[FishingSupplyNPC] Đã nhận phím E. " +
                "IsOpen trước khi toggle = " +
                shopUI.IsOpen,
                this
            );
        }

        shopUI.ToggleShop();
    }

    private void ResolveShop()
    {
        if (shopUI != null)
            return;

        FishingSupplyShopUI[] all =
            Resources
                .FindObjectsOfTypeAll<
                    FishingSupplyShopUI
                >();

        foreach (
            FishingSupplyShopUI candidate
            in all)
        {
            if (candidate == null)
                continue;

            if (!candidate.gameObject
                    .scene.IsValid())
            {
                continue;
            }

            shopUI = candidate;

            if (showDebugLogs)
            {
                Debug.Log(
                    "[FishingSupplyNPC] Tự tìm thấy Shop UI: " +
                    candidate.name,
                    candidate
                );
            }

            return;
        }
    }

    private void ResolvePlayer()
    {
        if (playerTransform != null)
            return;

        try
        {
            GameObject player =
                GameObject
                    .FindGameObjectWithTag(
                        playerTag
                    );

            if (player != null)
            {
                playerTransform =
                    player.transform;
            }
        }
        catch (UnityException)
        {
            Debug.LogError(
                "[FishingSupplyNPC] Tag Player chưa tồn tại.",
                this
            );
        }
    }

    private void UpdateRange()
    {
        if (playerTransform == null)
        {
            playerInRange =
                playerInTrigger;

            return;
        }

        float distance =
            Vector2.Distance(
                transform.position,
                playerTransform.position
            );

        playerInRange =
            playerInTrigger ||
            distance <= interactionDistance;

        if (!playerInRange &&
            closeWhenLeaving &&
            shopUI != null &&
            shopUI.IsOpen)
        {
            shopUI.CloseShop();
        }
    }

    private void OnTriggerEnter2D(
        Collider2D other)
    {
        if (!IsPlayer(other))
            return;

        playerInTrigger = true;
        playerInRange = true;

        if (showDebugLogs)
        {
            Debug.Log(
                "[FishingSupplyNPC] Player vào vùng NPC.",
                this
            );
        }
    }

    private void OnTriggerStay2D(
        Collider2D other)
    {
        if (!IsPlayer(other))
            return;

        playerInTrigger = true;
        playerInRange = true;
    }

    private void OnTriggerExit2D(
        Collider2D other)
    {
        if (!IsPlayer(other))
            return;

        playerInTrigger = false;

        if (showDebugLogs)
        {
            Debug.Log(
                "[FishingSupplyNPC] Player rời vùng NPC.",
                this
            );
        }
    }

    private bool IsPlayer(
        Collider2D other)
    {
        if (other == null)
            return false;

        if (other.CompareTag(playerTag))
            return true;

        Transform root =
            other.transform.root;

        return root != null &&
               root.CompareTag(playerTag);
    }

    private void SetPrompt(
        bool visible)
    {
        if (promptText != null)
            promptText.text = promptMessage;

        if (promptRoot != null &&
            promptRoot.activeSelf != visible)
        {
            promptRoot.SetActive(visible);
        }
    }

    [ContextMenu(
        "TEST - Force Open From NPC"
    )]
    private void TestForceOpen()
    {
        ResolveShop();
        shopUI?.OpenShop();
    }

    private void OnDisable()
    {
        SetPrompt(false);
    }
}
