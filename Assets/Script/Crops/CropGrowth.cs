using UnityEngine;

public class CropGrowth : MonoBehaviour
{
    [SerializeField] private Sprite[] growthSprites;
    [SerializeField] private float growthTime = 3f;

    private SpriteRenderer sr;
    private int stage;
    private float timer;

    private void Start()
    {
        sr = GetComponent<SpriteRenderer>();

        if (growthSprites.Length > 0)
        {
            sr.sprite = growthSprites[0];
        }
    }

    private void Update()
    {
        if (stage >= growthSprites.Length - 1)
            return;

        timer += Time.deltaTime;

        if (timer >= growthTime)
        {
            timer = 0;
            stage++;

            sr.sprite = growthSprites[stage];
        }
    }
}