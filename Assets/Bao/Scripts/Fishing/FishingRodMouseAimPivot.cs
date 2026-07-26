using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[DisallowMultipleComponent]
[DefaultExecutionOrder(65000)]
public class FishingRodMouseAimPivot : MonoBehaviour
{
    [Header("Required")]
    [Tooltip("Empty Object đặt tại tay nhân vật. Script nên gắn trên object này.")]
    [SerializeField]
    private Transform rodPivot;

    [Tooltip("Object chứa SpriteRenderer của cây cần.")]
    [SerializeField]
    private Transform rodVisual;

    [Tooltip("Điểm đặt đúng tại cán cần.")]
    [SerializeField]
    private Transform handlePoint;

    [Tooltip("Điểm đặt đúng tại đầu cần.")]
    [SerializeField]
    private Transform tipPoint;

    [SerializeField]
    private Camera worldCamera;

    [Header("Aim")]
    [Tooltip("0 = xoay ngay. 1440-2880 = xoay mượt nhưng vẫn bám chuột.")]
    [Min(0f)]
    [SerializeField]
    private float degreesPerSecond = 2160f;

    [Tooltip("Chuột quá gần tay thì giữ nguyên hướng để tránh rung.")]
    [Min(0f)]
    [SerializeField]
    private float minimumMouseDistance = 0.08f;

    [Tooltip("Sai số góc nhỏ sẽ bị bỏ qua.")]
    [Range(0f, 2f)]
    [SerializeField]
    private float angleDeadZone = 0.05f;

    [Header("Safety")]
    [Tooltip("Không dùng SpriteRenderer Flip khi cây cần đã xoay đủ 360 độ.")]
    [SerializeField]
    private bool clearSpriteFlip = true;

    [SerializeField]
    private SpriteRenderer rodRenderer;

    [Tooltip(
        "Bật khi parent của RodPivot có Scale X hoặc Y âm. " +
        "Tốt nhất vẫn nên để Scale parent dương."
    )]
    [SerializeField]
    private bool compensateNegativeParentScale = true;

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

        if (!ValidSetup())
            return;

        if (!TryGetMouseWorld(out Vector3 mouseWorld))
            return;

        Vector2 mouseDirection =
            mouseWorld -
            rodPivot.position;

        if (mouseDirection.sqrMagnitude <
            minimumMouseDistance *
            minimumMouseDistance)
        {
            return;
        }

        /*
         * Đọc hướng thật của asset trong LOCAL SPACE của RodVisual.
         * Không dùng 135, -135 hoặc Angle Offset.
         */
        Vector3 handleLocal =
            rodVisual.InverseTransformPoint(
                handlePoint.position
            );

        Vector3 tipLocal =
            rodVisual.InverseTransformPoint(
                tipPoint.position
            );

        Vector2 assetForwardLocal =
            tipLocal -
            handleLocal;

        if (assetForwardLocal.sqrMagnitude <
            0.000001f)
        {
            return;
        }

        float assetForwardAngle =
            Mathf.Atan2(
                assetForwardLocal.y,
                assetForwardLocal.x
            ) *
            Mathf.Rad2Deg;

        float visualLocalAngle =
            NormalizeSignedAngle(
                rodVisual.localEulerAngles.z
            );

        float mouseWorldAngle =
            Mathf.Atan2(
                mouseDirection.y,
                mouseDirection.x
            ) *
            Mathf.Rad2Deg;

        float parentWorldAngle =
            rodPivot.parent != null
                ? NormalizeSignedAngle(
                    rodPivot.parent.eulerAngles.z
                  )
                : 0f;

        float targetLocalAngle;

        bool parentIsMirrored =
            rodPivot.parent != null &&
            (
                rodPivot.parent.lossyScale.x *
                rodPivot.parent.lossyScale.y
            ) < 0f;

        if (compensateNegativeParentScale &&
            parentIsMirrored)
        {
            /*
             * Reflection làm chiều quay local bị đảo.
             * Công thức này bù lại khi parent có determinant âm.
             */
            targetLocalAngle =
                parentWorldAngle -
                mouseWorldAngle -
                visualLocalAngle -
                assetForwardAngle;
        }
        else
        {
            targetLocalAngle =
                mouseWorldAngle -
                parentWorldAngle -
                visualLocalAngle -
                assetForwardAngle;
        }

        float currentLocalAngle =
            NormalizeSignedAngle(
                rodPivot.localEulerAngles.z
            );

        float difference =
            Mathf.DeltaAngle(
                currentLocalAngle,
                targetLocalAngle
            );

        if (Mathf.Abs(difference) <=
            angleDeadZone)
        {
            return;
        }

        float nextAngle;

        if (degreesPerSecond <= 0f)
        {
            nextAngle =
                targetLocalAngle;
        }
        else
        {
            nextAngle =
                Mathf.MoveTowardsAngle(
                    currentLocalAngle,
                    targetLocalAngle,
                    degreesPerSecond *
                    Time.deltaTime
                );
        }

        rodPivot.localRotation =
            Quaternion.Euler(
                0f,
                0f,
                nextAngle
            );

        if (clearSpriteFlip &&
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
                rodPivot.position,
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
            rodPivot.position
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
            rodPivot.position.z;

        return true;
    }

    private bool ValidSetup()
    {
        return rodPivot != null &&
               rodVisual != null &&
               handlePoint != null &&
               tipPoint != null &&
               worldCamera != null;
    }

    private void ResolveReferences()
    {
        if (rodPivot == null)
            rodPivot = transform;

        if (worldCamera == null)
            worldCamera = Camera.main;

        if (rodRenderer == null &&
            rodVisual != null)
        {
            rodRenderer =
                rodVisual.GetComponentInChildren<
                    SpriteRenderer
                >(true);
        }
    }

    private static float NormalizeSignedAngle(
        float angle)
    {
        return Mathf.DeltaAngle(
            0f,
            angle
        );
    }

    [ContextMenu("TEST - Print Aim Setup")]
    private void PrintSetup()
    {
        ResolveReferences();

        Debug.Log(
            "[FishingRodMouseAimPivot] " +
            "Pivot=" +
            (rodPivot != null
                ? rodPivot.name
                : "NULL") +
            " | Visual=" +
            (rodVisual != null
                ? rodVisual.name
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
