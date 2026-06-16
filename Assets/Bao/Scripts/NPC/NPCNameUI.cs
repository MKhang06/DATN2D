using TMPro;
using UnityEngine;

public class NPCNameUI : MonoBehaviour
{
    [SerializeField] private Transform npc;
    [SerializeField] private Vector3 offset = new Vector3(0, 1.2f, 0);

    private RectTransform rectTransform;
    private Camera cam;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        cam = Camera.main;
    }

    private void LateUpdate()
    {
        if (npc == null) return;

        Vector3 screenPos =
            cam.WorldToScreenPoint(
                npc.position + offset
            );

        rectTransform.position = screenPos;
    }
}