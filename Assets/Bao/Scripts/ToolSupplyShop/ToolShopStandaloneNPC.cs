using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[DisallowMultipleComponent]
public class ToolShopStandaloneNPC : MonoBehaviour
{
    [SerializeField]
    private ToolSupplyShopStandalone shop;

    [SerializeField]
    private string playerTag = "Player";

    [SerializeField]
    private GameObject promptRoot;

    private GameObject interactingPlayer;

    private void Awake()
    {
        if (shop == null)
        {
            shop =
                FindFirstObjectByType<
                    ToolSupplyShopStandalone
                >(
                    FindObjectsInactive.Include
                );
        }

        if (promptRoot != null)
            promptRoot.SetActive(false);
    }

    private void Update()
    {
        if (interactingPlayer == null ||
            shop == null)
        {
            return;
        }

        if (WasInteractPressed())
        {
            /*
             * Truyền đúng Player vừa đi vào trigger.
             * Không còn tự tìm nhầm object khác trong Scene.
             */
            shop.OpenShop(
                interactingPlayer
            );
        }
    }

    private void OnTriggerEnter2D(
        Collider2D other)
    {
        if (!IsPlayer(other))
            return;

        interactingPlayer =
            FindPlayerRoot(other.gameObject);

        if (promptRoot != null)
            promptRoot.SetActive(true);
    }

    private void OnTriggerStay2D(
        Collider2D other)
    {
        if (interactingPlayer != null ||
            !IsPlayer(other))
        {
            return;
        }

        interactingPlayer =
            FindPlayerRoot(other.gameObject);
    }

    private void OnTriggerExit2D(
        Collider2D other)
    {
        GameObject exitingPlayer =
            FindPlayerRoot(other.gameObject);

        if (exitingPlayer !=
            interactingPlayer)
        {
            return;
        }

        interactingPlayer = null;

        if (promptRoot != null)
            promptRoot.SetActive(false);
    }

    private bool IsPlayer(
        Collider2D other)
    {
        if (other == null)
            return false;

        try
        {
            if (other.CompareTag(playerTag) ||
                other.transform.root
                    .CompareTag(playerTag))
            {
                return true;
            }
        }
        catch (UnityException)
        {
        }

        Transform current =
            other.transform;

        while (current != null)
        {
            if (current.name == "Player")
                return true;

            current = current.parent;
        }

        return false;
    }

    private static GameObject FindPlayerRoot(
        GameObject candidate)
    {
        if (candidate == null)
            return null;

        Transform current =
            candidate.transform;

        GameObject result =
            candidate;

        while (current != null)
        {
            bool isPlayer = false;

            if (current.name == "Player")
                isPlayer = true;

            try
            {
                if (current.CompareTag(
                        "Player"))
                {
                    isPlayer = true;
                }
            }
            catch (UnityException)
            {
            }

            if (isPlayer)
                result = current.gameObject;

            current = current.parent;
        }

        return result;
    }

    private static bool WasInteractPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null &&
               Keyboard.current.eKey
                   .wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.E);
#endif
    }
}
