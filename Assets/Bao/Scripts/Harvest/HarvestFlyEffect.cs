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
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip harvestPopSound;
    [SerializeField] private AudioClip itemFlySound;
    [SerializeField] private AudioClip inventoryPopSound;

    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    public void Play(Vector3 startPos, Transform target, string itemName, Sprite itemIcon, int amount)
    {
        transform.position = startPos;
        transform.localScale = Vector3.one;
        StartCoroutine(PlayRoutine(target, itemName, itemIcon, amount));
    }

    private IEnumerator PlayRoutine(Transform target, string itemName, Sprite itemIcon, int amount)
    {
        PlayOneShot(harvestPopSound);

        Vector3 start = transform.position;
        Vector3 jumpTarget = start + Vector3.up * jumpHeight;

        float timer = 0f;

        while (timer < jumpTime)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / jumpTime);

            transform.position = Vector3.Lerp(start, jumpTarget, t);
            transform.Rotate(0f, 0f, rotateSpeed * Time.deltaTime);

            yield return null;
        }

        PlayOneShot(itemFlySound);

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
            transform.localScale = Vector3.Lerp(originalScale, Vector3.zero, t);
            transform.Rotate(0f, 0f, rotateSpeed * Time.deltaTime);

            yield return null;
        }

        InventoryManager.Instance?.AddItem(itemName, itemIcon, amount);

        if (spriteRenderer != null)
            spriteRenderer.enabled = false;

        PlayOneShot(inventoryPopSound);

        float wait = inventoryPopSound != null ? inventoryPopSound.length : 0.15f;
        yield return new WaitForSeconds(wait);

        Destroy(gameObject);
    }

    private void PlayOneShot(AudioClip clip)
    {
        if (audioSource == null || clip == null)
            return;

        audioSource.PlayOneShot(clip);
    }
}