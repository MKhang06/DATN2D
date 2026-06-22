using TMPro;
using UnityEngine;

public class ToolShopManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject toolShopPanel;
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private NPCMood npcMood;
    [SerializeField] private NPCRelationship relationship;

    [Header("Items")]
    [SerializeField] private ShopItemUI[] toolItems;

    [Header("UI")]
    [SerializeField] private TMP_Text totalText;
    [SerializeField] private TMP_Text discountText;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private TMP_Text mainButtonText;
    [SerializeField] private TMP_Text relationshipText;
    [SerializeField] private TMP_Text moneyText;

    [Header("Discount")]
    [SerializeField] private float maxDiscountPercent = 30f;

    private bool dealAccepted;
    private int acceptedPrice;
    private float discountPercent;
    private int canceledDeals;

    private readonly string[] cancelDealLines =
    {
        "Này này, đừng đùa với tôi chứ.",
        "Mất công tôi đồng ý rồi đấy.",
        "Lần sau suy nghĩ kỹ trước khi trả giá nhé.",
        "Tôi không thích kiểu giao dịch như vậy.",
        "Cậu đang làm mất thời gian của tôi đấy."
    };

    private void Start()
    {
        if (toolShopPanel != null)
            toolShopPanel.SetActive(false);

        FindRelationship();

        RandomizeAllPrices();
        ResetDeal();
        RefreshUI();
    }

    private void Update()
    {
        RefreshUI();
    }

    public void OpenToolShop(NPCMood mood = null)
    {
        if (mood != null)
            npcMood = mood;

        FindRelationship();

        if (npcMood != null && npcMood.IsAngry)
        {
            SetMessage("Tôi đang giận. Quay lại sau đi.");
            return;
        }

        if (toolShopPanel != null)
            toolShopPanel.SetActive(true);

        ResetDeal();
        SetMessage("Cần dụng cụ mới cho nông trại à?");
        RefreshUI();
    }

    public void CloseToolShop()
    {
        if (toolShopPanel != null)
            toolShopPanel.SetActive(false);
    }

    public void MainButton()
    {
        int total = GetTotalPrice();

        if (dealAccepted)
        {
            Checkout();
            return;
        }

        if (playerStats != null && playerStats.Money >= total)
        {
            Checkout();
        }
        else
        {
            NegotiateDiscount();
        }
    }

    public void RedButton()
    {
        if (dealAccepted)
        {
            ResetDeal();
            canceledDeals++;

            if (npcMood != null)
            {
                npcMood.ReduceMood(3f);
                CheckNPCAngry();
            }

            if (relationship != null)
                relationship.RemoveFriendship(1);

            if (canceledDeals >= 3)
            {
                if (npcMood != null)
                {
                    npcMood.ReduceMood(15f);
                    CheckNPCAngry();
                }

                SetMessage("Đủ rồi. Tôi không muốn giao dịch với cậu hôm nay.");
                CloseToolShop();
                return;
            }

            SetMessage(cancelDealLines[Random.Range(0, cancelDealLines.Length)]);
            RefreshUI();
            return;
        }

        CloseToolShop();
    }

    private void NegotiateDiscount()
    {
        int rawTotal = GetRawTotal();

        if (rawTotal <= 0)
        {
            SetMessage("Bạn chưa chọn món nào.");
            return;
        }

        if (playerStats == null)
        {
            SetMessage("Không tìm thấy thông tin người chơi.");
            return;
        }

        int missingMoney = rawTotal - playerStats.Money;

        if (missingMoney <= 0)
        {
            Checkout();
            return;
        }

        float neededDiscount = ((float)missingMoney / rawTotal) * 100f;
        neededDiscount = Mathf.Ceil(neededDiscount);

        if (neededDiscount > maxDiscountPercent)
        {
            if (npcMood != null)
            {
                npcMood.ReduceMood(20f);
                CheckNPCAngry();
            }

            SetMessage("Cậu thiếu quá nhiều tiền. Tôi không thể giảm sâu như vậy.");
            return;
        }

        float chance = CalculateChance();
        float roll = Random.Range(0f, 100f);

        if (roll <= chance)
        {
            dealAccepted = true;
            discountPercent = neededDiscount;

            acceptedPrice = Mathf.RoundToInt(
                rawTotal * (1f - discountPercent / 100f)
            );

            if (npcMood != null)
                npcMood.ReduceMood(Random.Range(2f, 8f));

            SetMessage(
                "Được rồi. Tôi giảm " +
                discountPercent.ToString("0") +
                "% để cậu vừa đủ mua."
            );
        }
        else
        {
            if (npcMood != null)
            {
                npcMood.ReduceMood(Random.Range(10f, 25f));
                CheckNPCAngry();
            }

            SetMessage("Không được. Giá đó đã rất sát rồi.");
        }

        RefreshUI();
    }

    private float CalculateChance()
    {
        if (npcMood == null)
            return 60f;

        float chance = npcMood.currentMood * 0.5f;

        if (relationship != null)
        {
            chance += relationship.friendship * 0.3f;

            if (relationship.friendship >= 10)
                chance += 5f;

            if (relationship.friendship >= 25)
                chance += 10f;

            if (relationship.friendship >= 50)
                chance += 15f;

            if (relationship.friendship >= 100)
                chance += 25f;
        }

        switch (npcMood.personality)
        {
            case NPCMood.NPCPersonality.Friendly:
                chance += 20f;
                break;

            case NPCMood.NPCPersonality.Merchant:
                chance -= 10f;
                break;

            case NPCMood.NPCPersonality.Greedy:
                chance -= 20f;
                break;

            case NPCMood.NPCPersonality.Grumpy:
                chance -= 30f;
                break;
        }

        return Mathf.Clamp(chance, 5f, 95f);
    }

    private void Checkout()
    {
        int total = GetTotalPrice();

        if (total <= 0)
        {
            SetMessage("Bạn chưa chọn món nào.");
            return;
        }

        if (playerStats == null || !playerStats.SpendMoney(total))
        {
            SetMessage("Không đủ tiền.");
            return;
        }

        foreach (ShopItemUI item in toolItems)
        {
            if (item == null || item.Quantity <= 0)
                continue;

            Debug.Log("Đã mua: " + item.itemName + " x" + item.Quantity);
        }

        ClearCart();

        if (relationship != null)
            relationship.AddFriendship(2);

        ResetDeal();
        RandomizeAllPrices();

        SetMessage("Mua hàng thành công!");
        RefreshUI();
    }

    private int GetRawTotal()
    {
        int total = 0;

        foreach (ShopItemUI item in toolItems)
        {
            if (item != null)
                total += item.TotalPrice;
        }

        return total;
    }

    private int GetTotalPrice()
    {
        if (dealAccepted)
            return acceptedPrice;

        return GetRawTotal();
    }

    private void ClearCart()
    {
        foreach (ShopItemUI item in toolItems)
        {
            if (item != null)
                item.Clear();
        }
    }

    private void RandomizeAllPrices()
    {
        foreach (ShopItemUI item in toolItems)
        {
            if (item != null)
                item.RandomizePrice();
        }
    }

    private void ResetDeal()
    {
        dealAccepted = false;
        acceptedPrice = 0;
        discountPercent = 0f;
    }

    private void RefreshUI()
    {
        int rawTotal = GetRawTotal();

        if (totalText != null)
        {
            totalText.text =
                rawTotal <= 0
                    ? "Total:"
                    : "Total:\n" + GetTotalPrice() + "G";
        }

        if (discountText != null)
        {
            discountText.text =
                "Discount:\n" +
                discountPercent.ToString("0") +
                "%";
        }

        if (mainButtonText != null)
        {
            if (dealAccepted)
                mainButtonText.text = "Thanh toán";
            else if (playerStats != null && playerStats.Money >= rawTotal)
                mainButtonText.text = "Mua";
            else
                mainButtonText.text = "Thương lượng";
        }

        UpdateRelationshipUI();
        if (moneyText != null && playerStats != null)
        {
            moneyText.text =
                "Tiền hiện có:\n" +
                playerStats.Money +
                "G";
        }
    }

    private void UpdateRelationshipUI()
    {
        if (relationshipText == null)
            return;

        if (relationship == null)
        {
            relationshipText.text =
                "Quan hệ:\nNgười lạ\n0%";
            return;
        }

        relationshipText.text =
            "Quan hệ:\n" +
            GetRelationshipName() +
            "\n" +
            relationship.friendship +
            "%";
    }

    private string GetRelationshipName()
    {
        if (relationship == null)
            return "Người lạ";

        if (relationship.friendship >= 100)
            return "Bạn thân";

        if (relationship.friendship >= 50)
            return "Bạn tốt";

        if (relationship.friendship >= 25)
            return "Thân thiện";

        if (relationship.friendship >= 10)
            return "Quen biết";

        return "Người lạ";
    }

    private void FindRelationship()
    {
        if (relationship == null && npcMood != null)
            relationship = npcMood.GetComponent<NPCRelationship>();
    }

    private void SetMessage(string message)
    {
        if (messageText != null)
            messageText.text = message;
    }

    private void CheckNPCAngry()
    {
        if (npcMood == null) return;

        if (npcMood.IsAngry)
        {
            ResetDeal();

            SetMessage("Đủ rồi. Tôi không muốn giao dịch với cậu lúc này.");

            if (toolShopPanel != null)
                toolShopPanel.SetActive(false);
        }
    }
}