using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerInput))]
[RequireComponent(typeof(Animator))]
public class PlayerController : MonoBehaviour
{
    [Header("Speed")]
    [SerializeField] private float walkSpeed = 4f;
    [SerializeField] private float runSpeed = 7f;

    [Header("Stats")]
    [SerializeField] private PlayerStats playerStats;

    public bool canMove = true;

    private Rigidbody2D rb;
    private Animator animator;
    private PlayerInput playerInput;

    private Vector2 moveInput;
    private Vector2 lastInput = Vector2.down;
    private bool isRunning;
    private bool movementLocked;

    public Vector2 LastDirection => lastInput;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        playerInput = GetComponent<PlayerInput>();

        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        if (playerStats == null)
            playerStats = GetComponent<PlayerStats>();
    }

    private void Update()
    {
        if (movementLocked)
        {
            StopMovement();
            UpdateAnimation();
            return;
        }

        bool wantsRun =
            Keyboard.current != null &&
            Keyboard.current.leftShiftKey.isPressed;

        isRunning =
            wantsRun &&
            moveInput.sqrMagnitude > 0.01f &&
            playerStats != null &&
            playerStats.HasStamina();

        if (playerStats != null)
        {
            if (isRunning)
                playerStats.DrainStamina();
            else
                playerStats.RegenStamina();
        }

        UpdateAnimation();
    }

    public void OnMove(InputValue value)
    {
        if (movementLocked || !canMove)
        {
            StopMovement();
            UpdateAnimation();
            return;
        }

        moveInput = value.Get<Vector2>();

        if (moveInput.sqrMagnitude > 0.01f)
        {
            if (Mathf.Abs(moveInput.x) > Mathf.Abs(moveInput.y))
                lastInput = new Vector2(Mathf.Sign(moveInput.x), 0f);
            else
                lastInput = new Vector2(0f, Mathf.Sign(moveInput.y));
        }

        UpdateAnimation();
    }

    private void FixedUpdate()
    {
        if (movementLocked || !canMove)
        {
            StopMovement();
            return;
        }

        float passiveSpeed = 1f;

        if (CharacterPassiveManager.Instance != null)
            passiveSpeed = CharacterPassiveManager.Instance.MoveSpeedMultiplier;

        float speed = (isRunning ? runSpeed : walkSpeed) * passiveSpeed;
        rb.linearVelocity = moveInput.normalized * speed;
    }

    private void StopMovement()
    {
        moveInput = Vector2.zero;
        isRunning = false;

        if (rb != null)
            rb.linearVelocity = Vector2.zero;
    }

    private void UpdateAnimation()
    {
        if (animator == null)
            return;

        bool isMoving =
            moveInput.sqrMagnitude > 0.01f &&
            canMove &&
            !movementLocked;

        animator.SetFloat("InputX", moveInput.x);
        animator.SetFloat("InputY", moveInput.y);

        animator.SetFloat("LastInputX", lastInput.x);
        animator.SetFloat("LastInputY", lastInput.y);

        animator.SetBool("IsWalking", isMoving);
        animator.SetBool("IsRunning", isMoving && isRunning);
    }

    public void SetMovementLocked(bool locked)
{
    movementLocked = locked;
    canMove = !locked;

    moveInput = Vector2.zero;
    isRunning = false;

    if (rb != null)
    {
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeAll;
    }

    if (playerInput != null)
    {
        if (locked)
            playerInput.enabled = false;   // Tắt hoàn toàn Input
        else
            playerInput.enabled = true;
    }

    if (!locked && rb != null)
    {
        rb.constraints =
            RigidbodyConstraints2D.FreezeRotation;
    }

    UpdateAnimation();
}
}