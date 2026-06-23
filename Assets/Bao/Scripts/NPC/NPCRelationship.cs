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

    public enum RelationshipLevel
{
    Stranger,      // Người lạ
    Acquaintance,  // Quen biết
    Friendly,      // Thân thiện
    GoodFriend,    // Bạn tốt
    BestFriend     // Bạn thân
}

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

    public void RemoveFriendship(int amount)
{
    friendship -= amount;
    friendship = Mathf.Clamp(friendship, 0, 100);
}
public RelationshipLevel CurrentLevel
{
    get
    {
        if (friendship >= 100)
            return RelationshipLevel.BestFriend;

        if (friendship >= 50)
            return RelationshipLevel.GoodFriend;

        if (friendship >= 25)
            return RelationshipLevel.Friendly;

        if (friendship >= 10)
            return RelationshipLevel.Acquaintance;

        return RelationshipLevel.Stranger;
    }
}
}