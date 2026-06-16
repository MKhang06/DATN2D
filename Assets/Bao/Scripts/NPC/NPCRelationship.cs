using UnityEngine;

public class NPCRelationship : MonoBehaviour
{
    [Header("Friendship")]
    [Range(0,100)]
    public int friendship = 0;

    public bool IsFriendly => friendship >= 10;
    public bool Discount5 => friendship >= 25;
    public bool Discount10 => friendship >= 50;
    public bool SpecialDialogue => friendship >= 100;

    public void AddFriendship(int amount)
    {
        int oldValue = friendship;

        friendship += amount;
        friendship = Mathf.Clamp(friendship, 0, 100);

        if (oldValue < 10 && friendship >= 10)
        {
            Debug.Log("NPC đã trở nên thân thiện hơn.");
        }

        if (oldValue < 25 && friendship >= 25)
        {
            Debug.Log("Mở khóa giảm giá 5%");
        }

        if (oldValue < 50 && friendship >= 50)
        {
            Debug.Log("Mở khóa giảm giá 10%");
        }

        if (oldValue < 100 && friendship >= 100)
        {
            Debug.Log("Mở khóa hội thoại đặc biệt");
        }
    }
}