using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class ToolSupplyShopNPCInteraction : MonoBehaviour
{
    [SerializeField] private ToolSupplyShopUI shopUI;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private GameObject promptRoot;
    [SerializeField] private TMP_Text promptText;

    private bool playerInside;

    private void Awake()
    {
        if (shopUI == null)
        {
            shopUI = FindFirstObjectByType<ToolSupplyShopUI>(
                FindObjectsInactive.Include
            );
        }

        SetPrompt(false);
    }

    private void Update()
    {
        if (playerInside &&
            Input.GetKeyDown(interactKey))
        {
            shopUI?.OpenShop();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsPlayer(other))
            return;

        playerInside = true;
        SetPrompt(true);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!IsPlayer(other))
            return;

        playerInside = false;
        SetPrompt(false);
    }

    private bool IsPlayer(Collider2D other)
    {
        return other != null &&
               (
                   other.CompareTag(playerTag) ||
                   other.transform.root.name == "Player"
               );
    }

    private void SetPrompt(bool visible)
    {
        if (promptRoot != null)
            promptRoot.SetActive(visible);

        if (promptText != null)
            promptText.text = "[E] MUA DỤNG CỤ";
    }
}
