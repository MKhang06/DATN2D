using UnityEngine;

public class NPCMood : MonoBehaviour
{
    public enum NPCPersonality
    {
        Friendly,
        Normal,
        Merchant,
        Greedy,
        Grumpy
    }

    [Header("Personality")]
    public NPCPersonality personality = NPCPersonality.Normal;

    [Range(0, 100)]
    public float friendship = 20f;

    [Header("Mood")]
    public float maxMood = 100f;
    public float currentMood = 100f;

    [Header("Angry")]
    public float angryLimit = 20f;
    public float minAngryTime = 30f;
    public float maxAngryTime = 120f;

    private bool isAngry;
    private float angryTimer;

    public bool IsAngry => isAngry;

    private void Update()
    {
        if (!isAngry) return;

        angryTimer -= Time.deltaTime;

        if (angryTimer <= 0f)
        {
            isAngry = false;
            currentMood = 50f;

            Debug.Log("NPC is no longer angry.");
        }
    }

    public void ReduceMood(float amount)
    {
        currentMood = Mathf.Clamp(currentMood - amount, 0f, maxMood);

        if (currentMood <= angryLimit)
            BecomeAngry();
    }

    public void IncreaseMood(float amount)
    {
        if (isAngry) return;

        currentMood = Mathf.Clamp(currentMood + amount, 0f, maxMood);
    }

    private void BecomeAngry()
    {
        if (isAngry) return;

        isAngry = true;
        angryTimer = Random.Range(minAngryTime, maxAngryTime);

        Debug.Log("NPC is angry!");
    }
}