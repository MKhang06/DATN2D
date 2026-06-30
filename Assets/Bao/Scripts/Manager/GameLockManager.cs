using UnityEngine;

public class GameLockManager : MonoBehaviour
{
    public static GameLockManager Instance;

    [SerializeField] private PlayerController playerController;
    [SerializeField] private PlayerToolController toolController;
    [SerializeField] private PlayerToolAnimation toolAnimation;
    [SerializeField] private Rigidbody2D playerRb;

    private Vector3 lockedPos;
    private bool locked;

    private void Awake()
    {
        Instance = this;
    }

    private void LateUpdate()
    {
        if (!locked) return;

        if (playerController != null)
            playerController.transform.position = lockedPos;

        if (playerRb != null)
            playerRb.linearVelocity = Vector2.zero;
    }

    public void LockPlayer()
    {
        if (playerController == null)
            playerController = FindFirstObjectByType<PlayerController>();

        if (toolController == null)
            toolController = FindFirstObjectByType<PlayerToolController>();

        if (toolAnimation == null)
            toolAnimation = FindFirstObjectByType<PlayerToolAnimation>();

        if (playerController != null && playerRb == null)
            playerRb = playerController.GetComponent<Rigidbody2D>();

        if (playerController != null)
        {
            lockedPos = playerController.transform.position;
            playerController.SetMovementLocked(true);
        }

        if (toolAnimation != null)
            toolAnimation.SetFishingLock(true);

        if (toolController != null)
            toolController.enabled = false;

        if (playerRb != null)
            playerRb.linearVelocity = Vector2.zero;

        locked = true;
    }

    public void UnlockPlayer()
    {
        locked = false;

        if (playerRb != null)
            playerRb.linearVelocity = Vector2.zero;

        if (playerController != null)
            playerController.SetMovementLocked(false);

        if (toolAnimation != null)
            toolAnimation.SetFishingLock(false);

        if (toolController != null)
            toolController.enabled = true;
    }
}