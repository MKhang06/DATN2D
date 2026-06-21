using TMPro;
using UnityEngine;

public class LoanNPC : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private LoanManager loanManager;
    [SerializeField] private NPCMood npcMood;
    [SerializeField] private NPCRelationship relationship;

    [Header("NPC Info")]
    [SerializeField] private string npcName = "Victor";

    [Header("Floating UI")]
    [SerializeField] private GameObject npcNameUI;
    [SerializeField] private TMP_Text npcNameText;
    [SerializeField] private GameObject interactPromptUI;
    [SerializeField] private TMP_Text promptText;

    private bool playerInRange;

    private void Awake()
    {
        if (npcMood == null)
            npcMood = GetComponent<NPCMood>();

        if (relationship == null)
            relationship = GetComponent<NPCRelationship>();

        if (npcNameText != null)
            npcNameText.text = npcName;

        if (promptText != null)
            promptText.text = "[F] Vay tiền";

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
                    promptText.text = "NPC đang giận. Quay lại sau.";
                return;
            }

            if (loanManager != null)
                loanManager.OpenLoanPanel(npcMood, relationship);
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
            promptText.text = "[F] Vay tiền";
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        playerInRange = false;

        if (npcNameUI != null)
            npcNameUI.SetActive(false);

        if (interactPromptUI != null)
            interactPromptUI.SetActive(false);

        if (loanManager != null)
            loanManager.CloseLoanPanel();
    }
}