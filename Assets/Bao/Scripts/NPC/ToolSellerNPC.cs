using TMPro;
using UnityEngine;

public class ToolSellerNPC : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ToolShopManager toolShopManager;

    [Header("UI")]
    [SerializeField] private GameObject npcNameUI;
    [SerializeField] private TMP_Text npcNameText;

    [SerializeField] private GameObject interactPromptUI;
    [SerializeField] private TMP_Text promptText;

    [Header("NPC Info")]
    [SerializeField] private string npcName = "Bob";

    private bool playerInRange;

    private void Awake()
    {
        if (npcNameText != null)
            npcNameText.text = npcName;

        if (promptText != null)
            promptText.text = "[F] Tương tác";

        if (npcNameUI != null)
            npcNameUI.SetActive(false);

        if (interactPromptUI != null)
            interactPromptUI.SetActive(false);
    }

    private void Update()
    {
        if (!playerInRange)
            return;

        if (Input.GetKeyDown(KeyCode.F))
        {
            if (toolShopManager != null)
                toolShopManager.OpenToolShop();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerInRange = true;

        if (npcNameUI != null)
            npcNameUI.SetActive(true);

        if (interactPromptUI != null)
            interactPromptUI.SetActive(true);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerInRange = false;

        if (npcNameUI != null)
            npcNameUI.SetActive(false);

        if (interactPromptUI != null)
            interactPromptUI.SetActive(false);

        if (toolShopManager != null)
            toolShopManager.CloseToolShop();
    }
}