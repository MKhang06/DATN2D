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
        private bool isHarvested = false;
        private SpriteRenderer spriteRenderer;
        private Coroutine growthCoroutine;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        private void Start()
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.sortingOrder = Mathf.RoundToInt(-transform.position.y * 100);
            }

            UpdateSprite();

            if (growthStages != null && growthStages.Length > 1)
            {
                growthCoroutine = StartCoroutine(GrowthRoutine());
            }
            else
            {
                isFullyGrown = true;
            }
        }

        private IEnumerator GrowthRoutine()
        {
            while (!isFullyGrown)
            {
                yield return new WaitForSeconds(timePerStage);
                currentStage++;
                UpdateSprite();

                if (growthStages != null && currentStage >= growthStages.Length - 1)
                {
                    currentStage = growthStages.Length - 1;
                    isFullyGrown = true;
                }
            }
        }

        private void UpdateSprite()
        {
            if (spriteRenderer == null || growthStages == null || growthStages.Length == 0) return;

            currentStage = Mathf.Clamp(currentStage, 0, growthStages.Length - 1);
            spriteRenderer.sprite = growthStages[currentStage];
        }

        public void Harvest()
        {
            if (!isFullyGrown || isHarvested) return;
            if (cropItemData == null || InventoryManager.Instance == null) return;

            bool added = InventoryManager.Instance.AddItem(cropItemData, 1);
            if (!added)
            {
                Debug.LogWarning("Túi đồ đã đầy. Không thể thu hoạch nông sản.");
                return;
            }

            isHarvested = true;
            global::GreenFieldQuestEvents.ReportCropHarvested();
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
