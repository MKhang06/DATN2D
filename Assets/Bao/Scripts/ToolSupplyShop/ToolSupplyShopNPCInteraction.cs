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
        if (other == null)
            return false;

        if (other.CompareTag(playerTag))
            return true;

        Transform current = other.transform;

        while (current != null)
        {
            if (current.name == "Player")
                return true;

            current = current.parent;
        }

        return false;
    }

    private void SetPrompt(bool visible)
    {
        if (promptRoot != null)
            promptRoot.SetActive(visible);

        if (promptText != null)
            promptText.text = "[E] MUA DỤNG CỤ";
    }
}
