using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Khang
{
    public class Crop : MonoBehaviour
    {
        [Header("Cấu Hình Phát Triển")]
        [SerializeField] private Sprite[] growthStages;     // Dải ảnh giai đoạn phát triển
        [SerializeField] private float timePerStage = 5f;   // Thời gian lớn lên mỗi giai đoạn (giây)
        [SerializeField] private ItemData cropItemData;     // File ItemData vật phẩm thu hoạch

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
            // Tự động xếp lớp theo trục Y (Cây ở dưới đè cây ở trên)
            if (spriteRenderer != null)
            {
                spriteRenderer.sortingOrder = Mathf.RoundToInt(-transform.position.y * 100);
            }

            UpdateSprite();

            if (growthStages != null && growthStages.Length > 1)
            {
                growthCoroutine = StartCoroutine(GrowthRoutine());
            }
        }

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

        public void Harvest()
        {
            if (!isFullyGrown) return;

            // Thêm nông sản trực tiếp vào Túi Đồ
            if (cropItemData != null && InventoryManager.Instance != null)
            {
                InventoryManager.Instance.AddItem(cropItemData, 1);
            }

            Destroy(gameObject);
        }

        private void OnMouseDown()
        {
            // Bỏ qua nếu click trên UI
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

            Harvest();
        }
    }
}