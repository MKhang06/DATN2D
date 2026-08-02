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

    private bool playerInside;

    private void Awake()
    {
        Collider2D trigger =
            GetComponent<Collider2D>();

        trigger.isTrigger = true;

        SetPromptVisible(false);
    }

    private void Update()
    {
        if (!playerInside ||
            marketUI == null)
        {
            return;
        }

        if (!Input.GetKeyDown(
                interactionKey))
        {
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

        playerInside = true;

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

        playerInside = false;
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
}
