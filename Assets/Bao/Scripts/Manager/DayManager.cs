using UnityEngine;

public class DayManager : MonoBehaviour
{
    public int currentDay = 1;

    [SerializeField] private LoanManager loanManager;

    public void NextDay()
    {
        currentDay++;

        if (loanManager != null)
            loanManager.CheckLoanPenalty();

        Debug.Log("Ngày hiện tại: " + currentDay);
    }
}