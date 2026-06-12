using TMPro;
using UnityEngine;

public class ShopNPC : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ShopManager shopManager;
    [SerializeField] private NPCMood npcMood;

    [Header("NPC Info")]
    [SerializeField] private string npcName = "Maya";

    [Header("Name UI")]
    [SerializeField] private GameObject npcNameUI;
    [SerializeField] private TMP_Text npcNameText;

    [Header("Prompt UI")]
    [SerializeField] private GameObject interactPromptUI;
    [SerializeField] private TMP_Text promptText;

    private bool playerInRange;

    private void Awake()
    {
        if (npcMood == null)
            npcMood = GetComponent<NPCMood>();

        if (npcNameText != null)
            npcNameText.text = npcName;

        if (promptText != null)
            promptText.text = "[F] Interact";

        if (npcNameUI != null)
            npcNameUI.SetActive(false);

        if (interactPromptUI != null)
            interactPromptUI.SetActive(false);
    }

    private void Update()
    {
        if (!playerInRange) return;

        if (Input.GetKeyDown(KeyCode.F))
        {
            if (npcMood != null && npcMood.IsAngry)
            {
                if (promptText != null)
                    promptText.text = "NPC is angry. Come back later.";

                return;
            }

            if (shopManager != null)
                shopManager.OpenShop(npcMood);

            if (interactPromptUI != null)
                interactPromptUI.SetActive(false);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        playerInRange = true;

        if (npcNameUI != null)
            npcNameUI.SetActive(true);

        if (interactPromptUI != null)
            interactPromptUI.SetActive(true);

        if (promptText != null)
            promptText.text = "[F] Interact";
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        playerInRange = false;

        if (npcNameUI != null)
            npcNameUI.SetActive(false);

        if (interactPromptUI != null)
            interactPromptUI.SetActive(false);

        if (shopManager != null)
            shopManager.CloseShop();
    }
}