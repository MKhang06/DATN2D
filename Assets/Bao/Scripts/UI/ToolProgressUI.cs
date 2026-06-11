using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ToolProgressUI : MonoBehaviour
{
    [SerializeField] private GameObject progressRoot;
    [SerializeField] private Image fillImage;
    [SerializeField] private TMP_Text actionText;

    [Header("Follow Player")]
    public Transform player;

    private Camera cam;

    private void Start()
    {
        cam = Camera.main;
        Hide();
    }

    private void LateUpdate()
    {
        if (player == null || cam == null) return;

        Vector3 worldPos = player.position + new Vector3(0f, 2.3f, 0f);

        transform.position =
            cam.WorldToScreenPoint(worldPos);
    }

    public void Show(string message)
    {
        progressRoot.SetActive(true);

        fillImage.fillAmount = 1f;

        actionText.gameObject.SetActive(true);
        actionText.text = message;
    }

    public void SetProgress(float value)
    {
        fillImage.fillAmount = Mathf.Clamp01(value);
    }

    public void Hide()
    {
        progressRoot.SetActive(false);

        if (actionText != null)
            actionText.gameObject.SetActive(false);
    }
}