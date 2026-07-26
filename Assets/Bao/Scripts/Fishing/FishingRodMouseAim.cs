using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[DisallowMultipleComponent]
[DefaultExecutionOrder(31000)]
public class FishingRodMouseAim : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Object sẽ xoay. Để trống nếu gắn script trực tiếp lên Fishing Rod.")]
    [SerializeField]
    private Transform rodTransform;

    [Tooltip("Tâm xoay đặt tại tay nhân vật. Để trống sẽ dùng vị trí của Fishing Rod.")]
    [SerializeField]
    private Transform aimPivot;

    [SerializeField]
    private Camera worldCamera;

    [Header("Aim")]
    [Tooltip(
        "Góc bù theo hướng gốc của sprite.\n" +
        "Sprite hướng phải: 0\n" +
        "Sprite hướng lên: -90\n" +
        "Sprite hướng trái: 180\n" +
        "Sprite hướng xuống: 90"
    )]
    [SerializeField]
    private float angleOffset;

    [Tooltip("Bật để xoay mượt thay vì xoay ngay lập tức.")]
    [SerializeField]
    private bool smoothRotation = true;

    [Min(0.01f)]
    [SerializeField]
    private float rotationSpeed = 20f;

    [Header("Optional Sprite Flip")]
    [Tooltip("Bật khi sprite bị lộn ngược lúc chuột nằm bên trái nhân vật.")]
    [SerializeField]
    private bool flipWhenMouseIsLeft;

    [SerializeField]
    private SpriteRenderer rodRenderer;

    [Tooltip("Dùng Flip Y thường hợp với sprite cần câu hướng sang phải.")]
    [SerializeField]
    private bool useFlipY = true;

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

        if (rodTransform == null ||
            aimPivot == null ||
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

        Vector3 mouseWorld =
            worldCamera.ScreenToWorldPoint(
                new Vector3(
                    screenPosition.x,
                    screenPosition.y,
                    Mathf.Abs(
                        worldCamera.transform.position.z -
                        aimPivot.position.z
                    )
                )
            );

        mouseWorld.z =
            aimPivot.position.z;

        Vector2 direction =
            mouseWorld -
            aimPivot.position;

        if (direction.sqrMagnitude <
            0.0001f)
        {
            return;
        }

        float targetAngle =
            Mathf.Atan2(
                direction.y,
                direction.x
            ) *
            Mathf.Rad2Deg +
            angleOffset;

        Quaternion targetRotation =
            Quaternion.Euler(
                0f,
                0f,
                targetAngle
            );

        if (smoothRotation)
        {
            rodTransform.rotation =
                Quaternion.Slerp(
                    rodTransform.rotation,
                    targetRotation,
                    1f -
                    Mathf.Exp(
                        -rotationSpeed *
                        Time.deltaTime
                    )
                );
        }
        else
        {
            rodTransform.rotation =
                targetRotation;
        }

        if (flipWhenMouseIsLeft &&
            rodRenderer != null)
        {
            bool mouseOnLeft =
                direction.x < 0f;

            if (useFlipY)
            {
                rodRenderer.flipY =
                    mouseOnLeft;
            }
            else
            {
                rodRenderer.flipX =
                    mouseOnLeft;
            }
        }
    }

    private void ResolveReferences()
    {
        if (rodTransform == null)
            rodTransform = transform;

        if (aimPivot == null)
            aimPivot = transform;

        if (worldCamera == null)
            worldCamera = Camera.main;

        if (rodRenderer == null &&
            rodTransform != null)
        {
            rodRenderer =
                rodTransform
                    .GetComponentInChildren<
                        SpriteRenderer
                    >(true);
        }
    }

    [ContextMenu("TEST - Point Right")]
    private void TestPointRight()
    {
        ResolveReferences();

        if (rodTransform != null)
        {
            rodTransform.rotation =
                Quaternion.Euler(
                    0f,
                    0f,
                    angleOffset
                );
        }
    }
}
