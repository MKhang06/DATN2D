using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

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

    [Header("Water Blocking")]
    [SerializeField] private LayerMask waterLayer;
    [SerializeField, Min(0.05f)] private float waterPathStep = 0.2f;
    [SerializeField, Min(0.01f)] private float waterFootInset = 0.08f;

    public bool canMove = true;

    private Rigidbody2D rb;
    private Collider2D bodyCollider;
    private Animator animator;
    private PlayerInput playerInput;
    private Tilemap[] waterTilemaps;
    private Vector2 lastDryPosition;
    private bool hasLastDryPosition;

    private Vector2 moveInput;
    private Vector2 lastInput = Vector2.down;
    private bool isRunning;
    private bool movementLocked;

    public Vector2 LastDirection => lastInput;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        bodyCollider = GetComponent<Collider2D>();
        animator = GetComponent<Animator>();
        playerInput = GetComponent<PlayerInput>();

        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        if (waterLayer.value == 0)
            waterLayer = LayerMask.GetMask("Water");

        if (playerStats == null)
            playerStats = GetComponent<PlayerStats>();
    }

    private void Start()
    {
        RefreshWaterTilemaps();

        if (!IsStandingInWater(rb.position))
        {
            lastDryPosition = rb.position;
            hasLastDryPosition = true;
        }
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
        Vector2 desiredVelocity = moveInput.normalized * speed;

        if (IsStandingInWater(rb.position))
        {
            if (hasLastDryPosition)
                rb.position = lastDryPosition;

            rb.linearVelocity = Vector2.zero;
            return;
        }

        lastDryPosition = rb.position;
        hasLastDryPosition = true;

        rb.linearVelocity = GetWaterSafeVelocity(desiredVelocity);
    }

    private Vector2 GetWaterSafeVelocity(Vector2 desiredVelocity)
    {
        if (desiredVelocity.sqrMagnitude <= 0.0001f ||
            waterTilemaps == null ||
            waterTilemaps.Length == 0)
        {
            return desiredVelocity;
        }

        Vector2 start = rb.position;
        Vector2 movement = desiredVelocity * Time.fixedDeltaTime;

        if (!CrossesWater(start, start + movement))
            return desiredVelocity;

        Vector2 horizontalMovement =
            new Vector2(movement.x, 0f);
        Vector2 verticalMovement =
            new Vector2(0f, movement.y);

        bool canMoveHorizontal =
            Mathf.Abs(horizontalMovement.x) > 0.0001f &&
            !CrossesWater(start, start + horizontalMovement);

        bool canMoveVertical =
            Mathf.Abs(verticalMovement.y) > 0.0001f &&
            !CrossesWater(start, start + verticalMovement);

        if (canMoveHorizontal && canMoveVertical)
        {
            return Mathf.Abs(movement.x) >= Mathf.Abs(movement.y)
                ? new Vector2(desiredVelocity.x, 0f)
                : new Vector2(0f, desiredVelocity.y);
        }

        if (canMoveHorizontal)
            return new Vector2(desiredVelocity.x, 0f);

        if (canMoveVertical)
            return new Vector2(0f, desiredVelocity.y);

        return Vector2.zero;
    }

    private bool CrossesWater(Vector2 start, Vector2 target)
    {
        float distance = Vector2.Distance(start, target);
        int steps = Mathf.Max(
            1,
            Mathf.CeilToInt(distance / Mathf.Max(0.05f, waterPathStep))
        );

        for (int i = 1; i <= steps; i++)
        {
            Vector2 samplePosition = Vector2.Lerp(
                start,
                target,
                i / (float)steps
            );

            if (IsStandingInWater(samplePosition))
                return true;
        }

        return false;
    }

    private bool IsStandingInWater(Vector2 playerPosition)
    {
        if (waterTilemaps == null || waterTilemaps.Length == 0)
            return false;

        if (bodyCollider == null)
            return IsWaterTileAt(playerPosition);

        Bounds bounds = bodyCollider.bounds;
        Vector2 positionOffset = playerPosition - rb.position;
        float inset = Mathf.Min(
            waterFootInset,
            bounds.extents.x * 0.45f
        );

        float left = bounds.min.x + inset + positionOffset.x;
        float center = bounds.center.x + positionOffset.x;
        float right = bounds.max.x - inset + positionOffset.x;
        float footY = bounds.min.y + waterFootInset + positionOffset.y;

        return IsWaterTileAt(new Vector2(left, footY)) ||
               IsWaterTileAt(new Vector2(center, footY)) ||
               IsWaterTileAt(new Vector2(right, footY));
    }

    private bool IsWaterTileAt(Vector2 worldPosition)
    {
        foreach (Tilemap tilemap in waterTilemaps)
        {
            if (tilemap == null || !tilemap.isActiveAndEnabled)
                continue;

            int tileLayerMask = 1 << tilemap.gameObject.layer;

            if ((waterLayer.value & tileLayerMask) == 0)
                continue;

            if (tilemap.HasTile(tilemap.WorldToCell(worldPosition)))
                return true;
        }

        return false;
    }

    private void RefreshWaterTilemaps()
    {
        Tilemap[] allTilemaps = FindObjectsByType<Tilemap>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        int count = 0;

        foreach (Tilemap tilemap in allTilemaps)
        {
            int tileLayerMask = 1 << tilemap.gameObject.layer;

            if ((waterLayer.value & tileLayerMask) != 0)
                count++;
        }

        waterTilemaps = new Tilemap[count];
        int index = 0;

        foreach (Tilemap tilemap in allTilemaps)
        {
            int tileLayerMask = 1 << tilemap.gameObject.layer;

            if ((waterLayer.value & tileLayerMask) == 0)
                continue;

            waterTilemaps[index] = tilemap;
            index++;
        }
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
                playerInput.enabled = false;
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
