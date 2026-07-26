using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[DisallowMultipleComponent]
[DefaultExecutionOrder(60000)]
public class FishingRodAimExact : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Object Fishing Rod sẽ được xoay.")]
    [SerializeField]
    private Transform rodRoot;

    [Tooltip("Điểm nằm tại cán cần, nơi nhân vật cầm.")]
    [SerializeField]
    private Transform handlePoint;

    [Tooltip("Điểm nằm tại đầu cần câu.")]
    [SerializeField]
    private Transform tipPoint;

    [SerializeField]
    private Camera worldCamera;

    [Header("Aim")]
    [Tooltip("0 = xoay ngay lập tức.")]
    [Min(0f)]
    [SerializeField]
    private float maximumDegreesPerSecond = 2880f;

    [Tooltip("Chuột quá gần cán thì giữ nguyên góc để tránh rung.")]
    [Min(0f)]
    [SerializeField]
    private float minimumMouseDistance = 0.12f;

    [Tooltip("Bỏ qua sai số góc rất nhỏ.")]
    [Range(0f, 3f)]
    [SerializeField]
    private float angleDeadZone = 0.05f;

    [Header("Safety")]
    [Tooltip("Tự tắt Flip X/Y để không làm đảo hướng cần câu.")]
    [SerializeField]
    private bool forceDisableSpriteFlip = true;

    [SerializeField]
    private SpriteRenderer rodRenderer;

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

        if (rodRoot == null ||
            handlePoint == null ||
            tipPoint == null ||
            worldCamera == null)
        {
            return;
        }

        if (!TryGetMouseWorld(out Vector3 mouseWorld))
            return;

        Vector2 currentRodDirection =
            tipPoint.position -
            handlePoint.position;

        Vector2 targetDirection =
            mouseWorld -
            handlePoint.position;

        if (currentRodDirection.sqrMagnitude <
                0.000001f ||
            targetDirection.sqrMagnitude <
                minimumMouseDistance *
                minimumMouseDistance)
        {
            return;
        }

        float remainingAngle =
            Vector2.SignedAngle(
                currentRodDirection,
                targetDirection
            );

        if (Mathf.Abs(remainingAngle) <=
            angleDeadZone)
        {
            return;
        }

        float step = remainingAngle;

        if (maximumDegreesPerSecond > 0f)
        {
            float maximumStep =
                maximumDegreesPerSecond *
                Time.deltaTime;

            step =
                Mathf.Clamp(
                    remainingAngle,
                    -maximumStep,
                    maximumStep
                );
        }

        /*
         * Điểm khác biệt quan trọng:
         * RotateAround xoay object quanh đúng vị trí cán cần,
         * không xoay quanh tâm giữa của sprite.
         *
         * Hướng được đo trực tiếp từ Handle -> Tip,
         * nên không cần Angle Offset, 135 hay -135.
         */
        Vector3 fixedHandlePosition =
            handlePoint.position;

        rodRoot.RotateAround(
            fixedHandlePosition,
            Vector3.forward,
            step
        );

        if (forceDisableSpriteFlip &&
            rodRenderer != null)
        {
            rodRenderer.flipX = false;
            rodRenderer.flipY = false;
        }

        if (drawDebugLines)
        {
            Debug.DrawLine(
                handlePoint.position,
                tipPoint.position,
                Color.green
            );

            Debug.DrawLine(
                handlePoint.position,
                mouseWorld,
                Color.yellow
            );
        }
    }

    private bool TryGetMouseWorld(
        out Vector3 mouseWorld)
    {
        mouseWorld = Vector3.zero;

        Vector2 screenPosition;

#if ENABLE_INPUT_SYSTEM
        if (Mouse.current == null)
            return false;

        screenPosition =
            Mouse.current.position.ReadValue();
#else
        screenPosition =
            Input.mousePosition;
#endif

        aimPlane.SetNormalAndPosition(
            Vector3.forward,
            handlePoint.position
        );

        Ray ray =
            worldCamera.ScreenPointToRay(
                screenPosition
            );

        if (!aimPlane.Raycast(
                ray,
                out float distance))
        {
            return false;
        }

        mouseWorld =
            ray.GetPoint(distance);

        mouseWorld.z =
            handlePoint.position.z;

        return true;
    }

    private void ResolveReferences()
    {
        if (rodRoot == null)
            rodRoot = transform;

        if (worldCamera == null)
            worldCamera = Camera.main;

        if (rodRenderer == null &&
            rodRoot != null)
        {
            rodRenderer =
                rodRoot.GetComponentInChildren<
                    SpriteRenderer
                >(true);
        }
    }

    [ContextMenu("TEST - Print Points")]
    private void PrintPoints()
    {
        ResolveReferences();

        Debug.Log(
            "[FishingRodAimExact] Root=" +
            (rodRoot != null
                ? rodRoot.name
                : "NULL") +
            " | Handle=" +
            (handlePoint != null
                ? handlePoint.name
                : "NULL") +
            " | Tip=" +
            (tipPoint != null
                ? tipPoint.name
                : "NULL") +
            " | Camera=" +
            (worldCamera != null
                ? worldCamera.name
                : "NULL"),
            this
        );
    }
}
