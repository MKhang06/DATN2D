using UnityEngine;

public class NPCPatrol : MonoBehaviour
{
    [Header("Cấu Hình Lộ Trình")]
    public Transform[] waypoints;        // Danh sách các điểm mốc cố định NPC sẽ đi qua
    public float moveSpeed = 2f;         // Tốc độ di chuyển của NPC
    public float startWaitTime = 1.5f;   // Thời gian NPC đứng nghỉ khi đến mỗi điểm mốc

    private int currentWaypointIndex = 0;
    private float waitTimer;
    private bool isWaiting = false;

    private Rigidbody2D rb;
    private Animator anim;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        
        // Đóng băng trục Z và tắt trọng lực để phù hợp game Top-down 2D
        if (rb != null)
        {
            rb.freezeRotation = true;
            rb.gravityScale = 0f;
        }

        waitTimer = startWaitTime;
    }

    void Update()
    {
        if (waypoints.Length == 0) return;

        // Xử lý logic đếm ngược thời gian chờ tại điểm mốc
        if (isWaiting)
        {
            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0)
            {
                isWaiting = false;
                // Chuyển mục tiêu sang điểm tiếp theo (vòng lặp)
                currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
                waitTimer = startWaitTime;
            }
        }
    }

    void FixedUpdate()
    {
        // Nếu không có điểm mốc hoặc NPC đang đứng chờ, bắt NPC dừng lại
        if (waypoints.Length == 0 || isWaiting)
        {
            StopMovement();
            return;
        }

        MoveTowardsWaypoint();
    }

    void MoveTowardsWaypoint()
    {
        Vector2 targetPosition = waypoints[currentWaypointIndex].position;
        Vector2 currentPosition = rb.position;

        // Tính toán hướng di chuyển từ vị trí hiện tại đến điểm mốc
        Vector2 direction = (targetPosition - currentPosition).normalized;
        
        // Tính khoảng cách còn lại tới điểm mốc
        float distance = Vector2.Distance(currentPosition, targetPosition);

        if (distance > 0.1f)
        {
            // Unity 6: Sử dụng linearVelocity thay cho velocity cũ của Rigidbody2D
            rb.linearVelocity = direction * moveSpeed;

            // Kích hoạt animation chạy
            if (anim != null) anim.SetBool("isMoving", true);

            // Tự động xoay mặt NPC theo hướng đi Trái/Phải
            FlipSprite(direction.x);
        }
        else
        {
            // Đã chạm đến điểm mốc, dừng lại kích hoạt thời gian chờ
            isWaiting = true;
        }
    }

    void StopMovement()
    {
        rb.linearVelocity = Vector2.zero;
        if (anim != null) anim.SetBool("isMoving", false);
    }

    void FlipSprite(float horizontalMove)
    {
        // Lật scale của trục X để quay đầu nhân vật
        if (horizontalMove > 0.01f)
        {
            transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        }
        else if (horizontalMove < -0.01f)
        {
            transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        }
    }
}