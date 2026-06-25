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

    private Vector2 moveInput;
    private Vector2 lastInput = Vector2.down;
    private bool isRunning;

    public Vector2 LastDirection => lastInput;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();

        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        if (playerStats == null)
            playerStats = GetComponent<PlayerStats>();
    }

    private void Update()
    {
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
        if (!canMove)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        float passiveSpeed = 1f;

        if (CharacterPassiveManager.Instance != null)
            passiveSpeed = CharacterPassiveManager.Instance.MoveSpeedMultiplier;

        float speed = (isRunning ? runSpeed : walkSpeed) * passiveSpeed;
        rb.linearVelocity = moveInput.normalized * speed;
    }

    private void UpdateAnimation()
    {
        bool isMoving =
            moveInput.sqrMagnitude > 0.01f &&
            canMove;

        animator.SetFloat("InputX", moveInput.x);
        animator.SetFloat("InputY", moveInput.y);

        animator.SetFloat("LastInputX", lastInput.x);
        animator.SetFloat("LastInputY", lastInput.y);

        animator.SetBool("IsWalking", isMoving);
        animator.SetBool("IsRunning", isMoving && isRunning);
    }
}