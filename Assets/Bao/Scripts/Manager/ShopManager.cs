using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject shopPanel;
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private NPCMood npcMood;

    [Header("Items")]
    [SerializeField] private ShopItemUI[] shopItems;

    [Header("UI")]
    [SerializeField] private TMP_Text totalText;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private TMP_Text discountText;
    [SerializeField] private TMP_Text mainButtonText;
    [SerializeField] private TMP_InputField offerInput;
    [SerializeField] private Image moodFill;

    [Header("Negotiation")]
    [SerializeField] private float maxDiscountPercent = 30f;

    private bool dealAccepted;
    private int acceptedPrice;
    private float discountPercent;

    private readonly string[] greetings =
    {
        "Welcome, farmer!",
        "Fresh seeds just arrived.",
        "Take your time browsing.",
        "Quality seeds, fair prices.",
        "Every great harvest starts with a seed."
    };

    private readonly string[] rejectLines =
    {
        "No way.",
        "That's too low.",
        "You're joking, right?",
        "I can't accept that.",
        "Stop wasting my time."
    };

    private void Start()
    {
        if (shopPanel != null)
            shopPanel.SetActive(false);

        RandomizeAllPrices();
        ResetDeal();
        RefreshUI();
    }

    private void Update()
    {
        RefreshUI();
        UpdateMoodUI();
    }

    public void OpenShop()
    {
        if (npcMood != null && npcMood.IsAngry)
        {
            SetMessage("I'm still angry. Come back later.");
            return;
        }

        if (shopPanel != null)
            shopPanel.SetActive(true);

        SetMessage(greetings[Random.Range(0, greetings.Length)]);
        RefreshUI();
    }

    public void OpenShop(NPCMood mood)
    {
        npcMood = mood;
        OpenShop();
    }

    public void CloseShop()
    {
        if (shopPanel != null)
            shopPanel.SetActive(false);
    }

    public void RandomizeAllPrices()
    {
        foreach (ShopItemUI item in shopItems)
        {
            if (item != null)
                item.RandomizePrice();
        }

        RefreshUI();
    }

    public void MainShopButton()
    {
        if (!dealAccepted)
            NegotiateOffer();
        else
            Checkout();
    }

    private void NegotiateOffer()
    {
        if (npcMood == null)
        {
            SetMessage("No NPC to negotiate with.");
            return;
        }

        if (npcMood.IsAngry)
        {
            SetMessage("I'm not talking to you right now.");
            CloseShop();
            return;
        }

        int rawTotal = GetRawTotal();

        if (rawTotal <= 0)
        {
            SetMessage("You haven't selected anything.");
            return;
        }

        if (offerInput == null)
        {
            SetMessage("Offer input is missing.");
            return;
        }

        string input = offerInput.text.Replace("%", "").Trim();

        if (!float.TryParse(input, out float offerDiscount))
        {
            SetMessage("Enter a valid discount percent.");
            return;
        }

        if (offerDiscount <= 0f)
        {
            SetMessage("Please enter a discount above 0%.");
            return;
        }

        if (offerDiscount > maxDiscountPercent)
        {
            npcMood.ReduceMood(25f);
            SetMessage("That's way too much! I can't accept " + offerDiscount.ToString("0") + "% off.");
            CheckNPCAngry();
            RefreshUI();
            return;
        }

        float chance = CalculateNegotiationChance();
        float roll = Random.Range(0f, 100f);

        if (roll <= chance)
        {
            dealAccepted = true;
            discountPercent = offerDiscount;

            acceptedPrice = Mathf.RoundToInt(
                rawTotal * (1f - discountPercent / 100f)
            );

            npcMood.ReduceMood(Random.Range(2f, 8f));

            SetMessage("Deal accepted! -" + discountPercent.ToString("0") + "%");
        }
        else
        {
            npcMood.ReduceMood(Random.Range(10f, 25f));
            SetMessage(rejectLines[Random.Range(0, rejectLines.Length)]);
        }

        CheckNPCAngry();
        RefreshUI();
    }

    private void CheckNPCAngry()
    {
        if (npcMood != null && npcMood.IsAngry)
        {
            ResetDeal();
            SetMessage("I've had enough. Come back later.");
            CloseShop();
        }
    }

    private float CalculateNegotiationChance()
    {
        if (npcMood == null) return 0f;

        float chance = 0f;

        chance += npcMood.currentMood * 0.5f;
        chance += npcMood.friendship * 0.3f;

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

    public void Checkout()
    {
        if (playerStats == null)
        {
            Debug.LogError("ShopManager is missing PlayerStats.");
            return;
        }

        if (npcMood != null && npcMood.IsAngry)
        {
            SetMessage("I'm not selling to you right now.");
            CloseShop();
            return;
        }

        int total = GetTotalPrice();

        if (total <= 0)
        {
            SetMessage("You haven't selected anything.");
            return;
        }

        if (!playerStats.SpendMoney(total))
        {
            SetMessage("Not enough money.");
            return;
        }

        foreach (ShopItemUI item in shopItems)
        {
            if (item == null || item.Quantity <= 0)
                continue;

            Debug.Log("Bought " + item.itemName + " x" + item.Quantity);
        }

        ClearCart();

        if (npcMood != null)
            npcMood.IncreaseMood(5f);

        ResetDeal();

        if (totalText != null)
            totalText.text = "";

        SetMessage("Purchase complete!");
        RefreshUI();
    }

    private int GetRawTotal()
    {
        int total = 0;

        foreach (ShopItemUI item in shopItems)
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
        foreach (ShopItemUI item in shopItems)
        {
            if (item != null)
                item.Clear();
        }
    }

    private void ResetDeal()
    {
        dealAccepted = false;
        acceptedPrice = 0;
        discountPercent = 0f;

        if (offerInput != null)
            offerInput.text = "";
    }

    private void RefreshUI()
    {
        int rawTotal = GetRawTotal();

        if (totalText != null)
        {
            if (rawTotal <= 0)
                totalText.text = "";
            else
                totalText.text = "Total:\n" + GetTotalPrice() + "G";
        }

        if (discountText != null)
            discountText.text = "Discount: " + discountPercent.ToString("0") + "%";

        if (mainButtonText != null)
            mainButtonText.text = dealAccepted ? "Checkout" : "Negotiate";
    }

    private void UpdateMoodUI()
    {
        if (npcMood == null || moodFill == null)
            return;

        moodFill.fillAmount = npcMood.currentMood / npcMood.maxMood;

        if (npcMood.currentMood > 60)
            moodFill.color = new Color(0.2f, 0.9f, 0.2f);
        else if (npcMood.currentMood > 20)
            moodFill.color = Color.yellow;
        else
            moodFill.color = Color.red;
    }

    private void SetMessage(string message)
    {
        if (messageText != null)
            messageText.text = message;
    }
}