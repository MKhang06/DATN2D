using System.Collections;
using TMPro;
using UnityEngine;

public class SaveLoadNotificationUI : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text notificationText;
    [SerializeField] private float showTime = 1.5f;

    private Coroutine hideRoutine;

    private void Awake()
    {
        if (panel != null)
            panel.SetActive(false);
    }

    public void ShowMessage(string message)
    {
        if (panel != null)
            panel.SetActive(true);

        if (notificationText != null)
            notificationText.text = message;

        if (hideRoutine != null)
            StopCoroutine(hideRoutine);

        hideRoutine = StartCoroutine(HideAfterDelay());
    }

    private IEnumerator HideAfterDelay()
    {
        yield return new WaitForSecondsRealtime(showTime);

        if (panel != null)
            panel.SetActive(false);
    }
}
