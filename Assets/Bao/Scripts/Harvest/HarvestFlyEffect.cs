using System.Collections;
using UnityEngine;

public class HarvestFlyEffect : MonoBehaviour
{
    [Header("Move")]
    [SerializeField] private float jumpHeight = 1.2f;
    [SerializeField] private float jumpTime = 0.25f;
    [SerializeField] private float flyTime = 0.55f;

    [Header("Rotate")]
    [SerializeField] private float rotateSpeed = 720f;

    [Header("Audio")]
    [SerializeField] private AudioSource harvestAudio;
    [SerializeField] private AudioClip popSound;

    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (harvestAudio == null)
            harvestAudio = GetComponent<AudioSource>();
    }

    public void Play(
        Vector3 startPos,
        Transform target,
        string itemName,
        Sprite itemIcon,
        int amount)
    {
        transform.position = startPos;
        transform.localScale = Vector3.one;

        StartCoroutine(
            PlayRoutine(
                target,
                itemName,
                itemIcon,
                amount
            )
        );
    }

    private IEnumerator PlayRoutine(
        Transform target,
        string itemName,
        Sprite itemIcon,
        int amount)
    {
        PlayHarvestSound();

        Vector3 start = transform.position;
        Vector3 jumpTarget = start + Vector3.up * jumpHeight;

        float timer = 0f;

        while (timer < jumpTime)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / jumpTime);

            transform.position =
                Vector3.Lerp(start, jumpTarget, t);

            transform.Rotate(
                0f,
                0f,
                rotateSpeed * Time.deltaTime
            );

            yield return null;
        }

        Vector3 p0 = transform.position;
        Vector3 p2 = target.position + Vector3.up * 0.5f;
        Vector3 p1 = (p0 + p2) * 0.5f + Vector3.up * 1.5f;

        timer = 0f;
        Vector3 originalScale = transform.localScale;

        while (timer < flyTime)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / flyTime);

            Vector3 a = Vector3.Lerp(p0, p1, t);
            Vector3 b = Vector3.Lerp(p1, p2, t);

            transform.position = Vector3.Lerp(a, b, t);

            transform.localScale =
                Vector3.Lerp(originalScale, Vector3.zero, t);

            transform.Rotate(
                0f,
                0f,
                rotateSpeed * Time.deltaTime
            );

            yield return null;
        }

        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.AddItem(
                itemName,
                itemIcon,
                amount
            );
        }

        if (spriteRenderer != null)
            spriteRenderer.enabled = false;

        if (popSound != null)
            yield return new WaitForSeconds(popSound.length);

        Destroy(gameObject);
    }

    private void PlayHarvestSound()
    {
        if (harvestAudio == null)
        {
            Debug.LogWarning("Thiếu Harvest AudioSource.");
            return;
        }

        if (popSound == null)
        {
            Debug.LogWarning("Thiếu Pop Sound.");
            return;
        }

        harvestAudio.Stop();
        harvestAudio.clip = popSound;
        harvestAudio.loop = false;
        harvestAudio.Play();

        Debug.Log("Phát âm thanh thu hoạch ngay từ đầu.");
    }
}