using UnityEngine;

public class NPCPatrol : MonoBehaviour
{
    [Header("Cài Đặt Di Chuyển")]
    public float moveSpeed = 2f;         // Tốc độ đi bộ của NPC
    public float waitTime = 3f;          // Thời gian đứng chờ tại mỗi điểm (giây)

    [Header("Điểm Mốc Lộ Trình")]
    public Transform[] waypoints;        // Kéo thả các điểm mốc vào đây

    private int currentWaypointIndex = 0;
    private bool isWaiting = false;
    private float waitTimer = 0f;
    private Animator animator;

    void Start()
    {
        // Tự động lấy Component Animator trên NPC để xử lý hoạt ảnh sau này
        animator = GetComponent<Animator>();
        
        // Nếu có cài đặt điểm mốc, đặt vị trí xuất phát của NPC tại điểm đầu tiên
        if (waypoints.Length > 0 && waypoints[0] != null)
        {
            transform.position = waypoints[0].position;
        }
    }

    void Update()
    {
        // Nếu quên chưa kéo điểm mốc vào thì không chạy code dưới để tránh lỗi
        if (waypoints.Length == 0) return;

        if (isWaiting)
        {
            // Đếm thời gian đứng chờ
            waitTimer += Time.deltaTime;
            if (waitTimer >= waitTime)
            {
                isWaiting = false;
                waitTimer = 0f;
                
                // Chuyển sang mục tiêu điểm mốc tiếp theo
                currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
                
                SetWalkingAnimation(true); // Bật hoạt ảnh đi bộ khi hết chờ
            }
        }
        else
        {
            MoveNPC();
        }
    }

    void MoveNPC()
    {
        Transform targetTarget = waypoints[currentWaypointIndex];

        if (targetTarget == null) return;

        // Di chuyển vị trí NPC hướng dần tới điểm mốc mục tiêu
        transform.position = Vector3.MoveTowards(transform.position, targetTarget.position, moveSpeed * Time.deltaTime);

        // Tự động xoay mặt (Flip Sprite) theo hướng di chuyển X
        Vector3 direction = targetTarget.position - transform.position;
        if (direction.x > 0.01f)
        {
            // Đi sang phải -> quay mặt sang phải (giữ nguyên scale gốc)
            transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        }
        else if (direction.x < -0.01f)
        {
            // Đi sang trái -> lật scale X sang âm để quay mặt sang trái
            transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        }

        // Kiểm tra xem NPC đã chạm sát vào điểm mốc chưa
        if (Vector3.Distance(transform.position, targetTarget.position) < 0.05f)
        {
            isWaiting = true;
            SetWalkingAnimation(false); // Tắt hoạt ảnh đi bộ, chuyển sang đứng yên (Idle)
        }
    }

    void SetWalkingAnimation(bool isWalking)
    {
        if (animator != null)
        {
            // Bạn có thể tạo một biến Bool tên "isMoving" trong Animator của NPC để kích hoạt animation đi bộ
            animator.SetBool("isMoving", isWalking);
        }
    }
}