using System.Collections.Generic;
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public class FishBuyerNPCInteraction :
    MonoBehaviour
{
    [Header("Cửa hàng")]
    [SerializeField]
    private FishMarketUI marketUI;

    [Header("Thông báo tương tác")]
    [SerializeField]
    private GameObject promptRoot;

    [SerializeField]
    private TMP_Text promptText;

    [SerializeField]
    private string promptMessage =
        "[E] BÁN CÁ";

    [Header("Tương tác")]
    [SerializeField]
    private KeyCode interactionKey =
        KeyCode.E;

    [SerializeField]
    private string playerTag =
        "Player";

    [SerializeField]
    private bool closeWhenPlayerLeaves =
        true;

    private readonly HashSet<Collider2D>
        playerColliders =
            new HashSet<Collider2D>();

    private float nextMarketResolveTime;

    private bool playerInside =>
        playerColliders.Count > 0;

    private void Awake()
    {
        ResolveMarketUI();

        Collider2D trigger =
            GetComponent<Collider2D>();

        trigger.isTrigger = true;

        SetPromptVisible(false);
    }

    private void Update()
    {
        if (marketUI == null &&
            Time.unscaledTime >= nextMarketResolveTime)
        {
            ResolveMarketUI();
            nextMarketResolveTime = Time.unscaledTime + 1f;
        }

        if (!playerInside ||
            marketUI == null)
        {
            return;
        }

        if (!Input.GetKeyDown(interactionKey))
        {
            if (!marketUI.IsOpen &&
                promptRoot != null &&
                !promptRoot.activeSelf)
            {
                SetPromptVisible(true);
            }

            return;
        }

        if (marketUI.IsOpen)
        {
            marketUI.CloseShop();
            SetPromptVisible(true);
        }
        else
        {
            marketUI.OpenShop();
            SetPromptVisible(false);
        }
    }

    private void OnTriggerEnter2D(
        Collider2D other)
    {
        if (!other.CompareTag(playerTag))
            return;

        playerColliders.Add(other);

        if (marketUI == null ||
            !marketUI.IsOpen)
        {
            SetPromptVisible(true);
        }
    }

    private void OnTriggerExit2D(
        Collider2D other)
    {
        if (!other.CompareTag(playerTag))
            return;

        playerColliders.Remove(other);

        if (playerInside)
            return;

        SetPromptVisible(false);

        if (closeWhenPlayerLeaves)
        {
            marketUI?.CloseShop();
        }
    }

    private void SetPromptVisible(
        bool visible)
    {
        if (promptText != null)
            promptText.text = promptMessage;

        if (promptRoot != null)
            promptRoot.SetActive(visible);
    }

    private void ResolveMarketUI()
    {
        if (marketUI == null)
            marketUI = FindFirstObjectByType<FishMarketUI>();
    }

    private void OnDisable()
    {
        playerColliders.Clear();
        SetPromptVisible(false);

        if (closeWhenPlayerLeaves)
            marketUI?.CloseShop();
    }
}
