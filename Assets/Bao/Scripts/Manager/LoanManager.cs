using TMPro;
using UnityEngine;

public class LoanManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject loanPanel;
    [SerializeField] private PlayerStats playerStats;

    private NPCMood npcMood;
    private NPCRelationship relationship;

    [Header("UI")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text debtText;
    [SerializeField] private TMP_Text limitText;
    [SerializeField] private TMP_Text interestText;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private TMP_InputField amountInput;
    [SerializeField] private TMP_Text moneyText;
    [SerializeField] private TMP_Text relationshipText;

    [Header("Loan Settings")]
    [SerializeField] private int baseLoanLimit = 500;
    [SerializeField] private float interestPercent = 10f;

    private int currentDebt;
    private int totalBorrowed;

    private void Start()
    {
        if (loanPanel != null)
            loanPanel.SetActive(false);

        RefreshUI();
    }

    public void OpenLoanPanel(NPCMood mood, NPCRelationship rel)
    {
        npcMood = mood;
        relationship = rel;

        if (loanPanel != null)
            loanPanel.SetActive(true);

        SetMessage("Cần vốn xoay vòng à? Tôi có thể giúp, nhưng nhớ trả đúng hạn.");
        RefreshUI();
    }

    public void CloseLoanPanel()
    {
        if (loanPanel != null)
            loanPanel.SetActive(false);
    }

    public void BorrowMoney()
    {
        if (playerStats == null)
        {
            SetMessage("Không tìm thấy PlayerStats.");
            return;
        }

        if (!int.TryParse(amountInput.text, out int amount))
        {
            SetMessage("Nhập số tiền hợp lệ.");
            return;
        }

        if (amount <= 0)
        {
            SetMessage("Số tiền vay phải lớn hơn 0.");
            return;
        }

        int limit = GetLoanLimit();

        if (currentDebt + amount > limit)
        {
            SetMessage("Khoản vay vượt quá hạn mức của cậu.");
            ReduceTrust();
            RefreshUI();
            return;
        }

        int debtWithInterest = Mathf.CeilToInt(amount * (1f + interestPercent / 100f));

        playerStats.AddMoney(amount);

        currentDebt += debtWithInterest;
        totalBorrowed += amount;

        if (relationship != null)
            relationship.AddFriendship(1);

        SetMessage(
            "Đã vay " + amount + "G. " +
            "Cậu cần trả lại " + debtWithInterest + "G."
        );

        amountInput.text = "";
        RefreshUI();
    }

    public void RepayDebt()
    {
        if (playerStats == null)
        {
            SetMessage("Không tìm thấy PlayerStats.");
            return;
        }

        if (currentDebt <= 0)
        {
            SetMessage("Cậu không còn khoản nợ nào.");
            return;
        }

        if (!int.TryParse(amountInput.text, out int amount))
        {
            SetMessage("Nhập số tiền muốn trả.");
            return;
        }

        if (amount <= 0)
        {
            SetMessage("Số tiền trả phải lớn hơn 0.");
            return;
        }

        amount = Mathf.Min(amount, currentDebt);

        if (!playerStats.SpendMoney(amount))
        {
            SetMessage("Cậu không đủ tiền để trả khoản này.");
            return;
        }

        currentDebt -= amount;

        if (relationship != null)
            relationship.AddFriendship(2);

        if (currentDebt <= 0)
        {
            currentDebt = 0;
            SetMessage("Đã trả hết nợ. Uy tín của cậu tăng lên.");
            if (relationship != null)
                relationship.AddFriendship(5);
        }
        else
        {
            SetMessage("Đã trả " + amount + "G. Còn nợ " + currentDebt + "G.");
        }

        amountInput.text = "";
        RefreshUI();
    }

    public void RefusePayment()
    {
        if (currentDebt <= 0)
        {
            SetMessage("Không có nợ thì khỏi né tránh tôi.");
            return;
        }

        ReduceTrust();
        SetMessage("Đừng đùa với tiền bạc. Tôi sẽ nhớ chuyện này.");
        RefreshUI();
    }

    private int GetLoanLimit()
    {
        int bonus = 0;

        if (relationship != null)
        {
            if (relationship.friendship >= 10)
                bonus += 250;

            if (relationship.friendship >= 25)
                bonus += 500;

            if (relationship.friendship >= 50)
                bonus += 1000;

            if (relationship.friendship >= 100)
                bonus += 2000;
        }

        return baseLoanLimit + bonus;
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

    private void ReduceTrust()
    {
        if (npcMood != null)
            npcMood.ReduceMood(10f);

        if (relationship != null)
            relationship.RemoveFriendship(2);
    }

    private void RefreshUI()
    {
        if (titleText != null)
            titleText.text = "Dịch vụ vay tiền";

        if (debtText != null)
            debtText.text = "Nợ hiện tại:\n" + currentDebt + "G";

        if (limitText != null)
            limitText.text =
                "Hạn mức:\n" +
                GetLoanLimit() + "G\n" +
                GetRelationshipName();

        if (interestText != null)
            interestText.text = "Lãi suất:\n" + interestPercent.ToString("0") + "%";

        if (moneyText != null && playerStats != null)
        {
            moneyText.text =
                "Tiền hiện có:\n" +
                playerStats.Money + "G";
        }
        if (relationshipText != null)
        {
            if (relationship != null)
            {
                relationshipText.text =
                    "Quan hệ:\n" +
                    GetRelationshipName() +
                    "\n" +
                    relationship.friendship + "%";
            }
            else
            {
                relationshipText.text =
                    "Quan hệ:\nNgười lạ\n0%";
            }
        }
    }

    private void SetMessage(string message)
    {
        if (messageText != null)
            messageText.text = message;
    }
}