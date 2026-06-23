using TMPro;
using UnityEngine;

public class LoanManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject loanPanel;
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private DayManager dayManager;

    private NPCMood npcMood;
    private NPCRelationship relationship;

    [Header("UI")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text moneyText;
    [SerializeField] private TMP_Text relationshipText;
    [SerializeField] private TMP_Text debtText;
    [SerializeField] private TMP_Text limitText;
    [SerializeField] private TMP_Text interestText;
    [SerializeField] private TMP_Text dueDateText;
    [SerializeField] private TMP_Text penaltyText;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private TMP_InputField amountInput;

    [Header("Loan Settings")]
    [SerializeField] private int baseLoanLimit = 500;
    [SerializeField] private int loanDurationDays = 7;
    [SerializeField] private float baseInterestPercent = 10f;
    [SerializeField] private float latePenaltyPercentPerDay = 5f;
    [SerializeField] private int maxOverdueDaysBeforeAngry = 5;

    private int currentDebt;
    private int dueDay;
    private int overdueDays;
    private bool hasActiveLoan;

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

        if (npcMood != null && npcMood.IsAngry)
        {
            SetMessage("Victor đang giận. Hãy quay lại sau.");
            return;
        }

        if (hasActiveLoan && overdueDays >= maxOverdueDaysBeforeAngry)
        {
            SetMessage("Cậu nợ quá lâu rồi. Trả hết nợ cũ rồi hãy nói tiếp.");
            return;
        }

        if (loanPanel != null)
            loanPanel.SetActive(true);

        SetMessage("Victor: Cần vốn xoay vòng à? Tôi có thể giúp, nhưng nhớ trả đúng hạn.");
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

        if (hasActiveLoan && currentDebt > 0)
        {
            SetMessage("Cậu vẫn còn nợ cũ. Trả hết rồi mới được vay tiếp.");
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

        if (amount > limit)
        {
            SetMessage("Số tiền này vượt quá hạn mức của cậu.");
            ReduceTrust(5f, 1);
            RefreshUI();
            return;
        }

        float interest = GetInterestRate();

        int debtWithInterest = Mathf.CeilToInt(
            amount * (1f + interest / 100f)
        );

        playerStats.AddMoney(amount);

        currentDebt = debtWithInterest;
        hasActiveLoan = true;
        overdueDays = 0;

        if (dayManager != null)
            dueDay = dayManager.currentDay + loanDurationDays;
        else
            dueDay = loanDurationDays;

        if (relationship != null)
            relationship.AddFriendship(1);

        SetMessage(
            "Đã vay " + amount + "G. " +
            "Cậu cần trả " + currentDebt + "G trong " + loanDurationDays + " ngày."
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

        if (currentDebt <= 0)
        {
            currentDebt = 0;
            hasActiveLoan = false;
            overdueDays = 0;
            dueDay = 0;

            if (relationship != null)
                relationship.AddFriendship(5);

            if (npcMood != null)
                npcMood.IncreaseMood(10f);

            SetMessage("Đã trả hết nợ. Uy tín của cậu tăng lên.");
        }
        else
        {
            if (relationship != null)
                relationship.AddFriendship(2);

            SetMessage("Đã trả " + amount + "G. Còn nợ " + currentDebt + "G.");
        }

        amountInput.text = "";
        RefreshUI();
    }

    public void RefusePayment()
    {
        if (currentDebt <= 0)
        {
            SetMessage("Không có nợ thì không cần né tránh tôi đâu.");
            return;
        }

        ReduceTrust(10f, 3);

        SetMessage("Victor: Đừng đùa với tiền bạc. Tôi sẽ nhớ chuyện này.");

        RefreshUI();
    }

    public void CheckLoanPenalty()
    {
        if (!hasActiveLoan) return;
        if (currentDebt <= 0) return;
        if (dayManager == null) return;
        if (dayManager.currentDay <= dueDay) return;

        overdueDays++;

        int penalty = Mathf.CeilToInt(
            currentDebt * (latePenaltyPercentPerDay / 100f)
        );

        currentDebt += penalty;

        ReduceTrust(10f, 3);

        if (overdueDays >= maxOverdueDaysBeforeAngry)
        {
            if (npcMood != null)
                npcMood.ReduceMood(100f);

            SetMessage("Cậu đã nợ quá lâu. Victor sẽ không cho vay tiếp cho đến khi cậu trả hết.");
        }
        else
        {
            SetMessage("Bạn đã trễ hạn. Tiền phạt hôm nay: +" + penalty + "G.");
        }

        RefreshUI();
    }

    private int GetLoanLimit()
    {
        int limit = baseLoanLimit;

        if (relationship == null)
            return limit;

        if (relationship.friendship >= 10)
            limit += 250;

        if (relationship.friendship >= 25)
            limit += 500;

        if (relationship.friendship >= 50)
            limit += 1000;

        if (relationship.friendship >= 100)
            limit += 2000;

        return limit;
    }

    private float GetInterestRate()
    {
        float rate = baseInterestPercent;

        if (relationship == null)
            return rate;

        if (relationship.friendship >= 10)
            rate = 9f;

        if (relationship.friendship >= 25)
            rate = 8f;

        if (relationship.friendship >= 50)
            rate = 6f;

        if (relationship.friendship >= 100)
            rate = 4f;

        return rate;
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

    private void ReduceTrust(float moodAmount, int friendshipAmount)
    {
        if (npcMood != null)
            npcMood.ReduceMood(moodAmount);

        if (relationship != null)
            relationship.RemoveFriendship(friendshipAmount);
    }

    private void RefreshUI()
    {
        if (titleText != null)
            titleText.text = "DỊCH VỤ VAY TIỀN";

        if (moneyText != null && playerStats != null)
            moneyText.text = "Tiền hiện có:\n" + playerStats.Money + "G";

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

        if (debtText != null)
            debtText.text = "Nợ hiện tại:\n" + currentDebt + "G";

        if (limitText != null)
            limitText.text = "Hạn mức:\n" + GetLoanLimit() + "G";

        if (interestText != null)
            interestText.text = "Lãi suất:\n" + GetInterestRate().ToString("0") + "%";

        if (dueDateText != null)
        {
            if (hasActiveLoan && dayManager != null)
            {
                int daysLeft = dueDay - dayManager.currentDay;

                if (daysLeft >= 0)
                    dueDateText.text = "Hạn trả:\n" + daysLeft + " ngày";
                else
                    dueDateText.text = "Trễ hạn:\n" + overdueDays + " ngày";
            }
            else
            {
                dueDateText.text = "Hạn trả:\nKhông có";
            }
        }

        if (penaltyText != null)
            penaltyText.text = "Phạt trễ:\n" + latePenaltyPercentPerDay.ToString("0") + "% / ngày";
    }

    private void SetMessage(string message)
    {
        if (messageText != null)
            messageText.text = message;
    }
}