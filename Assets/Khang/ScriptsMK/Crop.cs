using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class Crop : MonoBehaviour
{
    [Header("Cấu Hình Phát Triển")]
    [SerializeField] private Sprite[] growthStages;     // Dải ảnh các giai đoạn phát triển
    [SerializeField] private float timePerStage = 5f;   // Thời gian lớn lên mỗi giai đoạn (giây)
    [SerializeField] private GameObject harvestDrop;    // Asset củ/quả/Prefab rơi ra khi thu hoạch

    private int currentStage = 0;
    private bool isFullyGrown = false;
    private SpriteRenderer spriteRenderer;
    private Coroutine growthCoroutine;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        UpdateSprite();

        // TỐI ƯU: Sử dụng Coroutine đếm giờ thay vì Update() đếm mỗi frame
        if (growthStages != null && growthStages.Length > 1)
        {
            growthCoroutine = StartCoroutine(GrowthRoutine());
        }
    }

    // Coroutine đếm thời gian: Tiết kiệm tài nguyên CPU tuyệt đối
    private IEnumerator GrowthRoutine()
    {
        while (currentStage < growthStages.Length - 1)
        {
            yield return new WaitForSeconds(timePerStage);

            currentStage++;
            UpdateSprite();

            if (currentStage == growthStages.Length - 1)
            {
                isFullyGrown = true;
                break;
            }
        }
    }

    private void UpdateSprite()
    {
        if (growthStages != null && currentStage < growthStages.Length && spriteRenderer != null)
        {
            spriteRenderer.sprite = growthStages[currentStage];
        }
    }

    // Thu hoạch nông sản
    public void Harvest()
    {
        if (!isFullyGrown) return;

        // Khóa ngay để không thể tính/nhả vật phẩm hai lần trước cuối frame.
        isFullyGrown = false;

        if (growthCoroutine != null)
        {
            StopCoroutine(growthCoroutine);
        }

        // Rơi củ/quả ra đất
        if (harvestDrop != null)
        {
            Instantiate(harvestDrop, transform.position, Quaternion.identity);
        }

        GreenFieldQuestEvents.ReportCropHarvested();

        // Xóa cây khỏi ô ruộng
        Destroy(gameObject);
    }

    private void OnMouseDown()
    {
        // Nếu click trúng giao diện UI (Túi đồ, Menu) thì không thu hoạch
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        Harvest();
    }
}
