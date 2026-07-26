using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[DisallowMultipleComponent]
[DefaultExecutionOrder(40000)]
public class FishingRodAimByTip : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Object Pivot xoay tại tay nhân vật.")]
    [SerializeField]
    private Transform rodPivot;

    [Tooltip("Empty Object đặt tại phần cán/tay cầm của cần câu.")]
    [SerializeField]
    private Transform rodHandlePoint;

    [Tooltip("Empty Object đặt tại đầu trên cùng của cần câu.")]
    [SerializeField]
    private Transform rodTipPoint;

    [SerializeField]
    private Camera worldCamera;

    [Header("Rotation")]
    [Tooltip("Bật để xoay mượt.")]
    [SerializeField]
    private bool smoothRotation = true;

    [Tooltip("Tốc độ bám theo chuột. 20-35 thường hợp lý.")]
    [Min(0.01f)]
    [SerializeField]
    private float followSpeed = 28f;

    [Tooltip("Tốc độ xoay tối đa theo độ/giây.")]
    [Min(1f)]
    [SerializeField]
    private float maximumDegreesPerSecond = 2160f;

    [Tooltip("Chuột quá gần tay cầm thì giữ nguyên hướng để tránh rung.")]
    [Min(0f)]
    [SerializeField]
    private float minimumAimDistance = 0.12f;

    [Tooltip("Sai số góc nhỏ hơn mức này sẽ bỏ qua.")]
    [Range(0f, 3f)]
    [SerializeField]
    private float angleDeadZone = 0.1f;

    [Header("Emergency Fix")]
    [Tooltip(
        "Chỉ bật khi RodTip và RodHandle đã đặt đúng nhưng đầu cần vẫn quay ngược."
    )]
    [SerializeField]
    private bool reverseTipDirection;

    [Header("Debug")]
    [SerializeField]
    private bool drawDebugLines;

    private Plane aimPlane;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        ResolveReferences();
    }

    private void LateUpdate()
    {
        ResolveReferences();

        if (rodPivot == null ||
            rodHandlePoint == null ||
            rodTipPoint == null ||
            worldCamera == null)
        {
            return;
        }

        Vector2 screenPosition;

#if ENABLE_INPUT_SYSTEM
        if (Mouse.current == null)
            return;

        screenPosition =
            Mouse.current.position.ReadValue();
#else
        screenPosition =
            Input.mousePosition;
#endif

        aimPlane.SetNormalAndPosition(
            Vector3.forward,
            rodHandlePoint.position
        );

        Ray ray =
            worldCamera.ScreenPointToRay(
                screenPosition
            );

        if (!aimPlane.Raycast(
                ray,
                out float enter))
        {
            return;
        }

        Vector3 mouseWorld =
            ray.GetPoint(enter);

        Vector2 targetDirection =
            mouseWorld -
            rodHandlePoint.position;

        if (targetDirection.sqrMagnitude <
            minimumAimDistance *
            minimumAimDistance)
        {
            return;
        }

        Vector2 currentTipDirection =
            rodTipPoint.position -
            rodHandlePoint.position;

        if (currentTipDirection.sqrMagnitude <
            0.000001f)
        {
            return;
        }

        if (reverseTipDirection)
        {
            currentTipDirection =
                -currentTipDirection;
        }

        float remainingAngle =
            Vector2.SignedAngle(
                currentTipDirection,
                targetDirection
            );

        if (Mathf.Abs(remainingAngle) <=
            angleDeadZone)
        {
            return;
        }

        float step;

        if (smoothRotation)
        {
            float smoothFactor =
                1f -
                Mathf.Exp(
                    -followSpeed *
                    Time.deltaTime
                );

            step =
                remainingAngle *
                smoothFactor;
        }
        else
        {
            step =
                remainingAngle;
        }

        float maximumStep =
            maximumDegreesPerSecond *
            Time.deltaTime;

        step =
            Mathf.Clamp(
                step,
                -maximumStep,
                maximumStep
            );

        /*
         * Xoay theo phần chênh lệch thật giữa:
         * Handle -> Tip và Handle -> Mouse.
         *
         * Không cần Angle Offset, không cần đoán sprite đang hướng bên nào.
         */
        rodPivot.Rotate(
            0f,
            0f,
            step,
            Space.World
        );

        if (drawDebugLines)
        {
            Debug.DrawLine(
                rodHandlePoint.position,
                rodTipPoint.position,
                Color.green
            );

            Debug.DrawLine(
                rodHandlePoint.position,
                mouseWorld,
                Color.yellow
            );
        }
    }

    private void ResolveReferences()
    {
        if (rodPivot == null)
            rodPivot = transform;

        if (worldCamera == null)
            worldCamera = Camera.main;
    }

    [ContextMenu("TEST - Print Setup")]
    private void PrintSetup()
    {
        ResolveReferences();

        Debug.Log(
            "[FishingRodAimByTip] " +
            "Pivot=" +
            (rodPivot != null
                ? rodPivot.name
                : "NULL") +
            " | Handle=" +
            (rodHandlePoint != null
                ? rodHandlePoint.name
                : "NULL") +
            " | Tip=" +
            (rodTipPoint != null
                ? rodTipPoint.name
                : "NULL") +
            " | Camera=" +
            (worldCamera != null
                ? worldCamera.name
                : "NULL"),
            this
        );
    }
}
