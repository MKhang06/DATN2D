using System;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[DisallowMultipleComponent]
[DefaultExecutionOrder(50000)]
public class FishingRodMouseAimFinal : MonoBehaviour
{
    [Header("References")]
    [Tooltip(
        "Object xoay quanh tay nhân vật. " +
        "Có thể là RodPivot hoặc chính Fishing Rod."
    )]
    [SerializeField]
    private Transform rodPivot;

    [Tooltip(
        "Transform chứa SpriteRenderer của cần câu. " +
        "Dùng để biết đầu cần trong asset đang hướng về đâu."
    )]
    [SerializeField]
    private Transform rodVisual;

    [SerializeField]
    private SpriteRenderer rodRenderer;

    [SerializeField]
    private Camera worldCamera;

    [Header("Asset Forward Direction")]
    [Tooltip(
        "Hướng từ cán tới đầu cần trong chính sprite gốc.\n" +
        "Asset của bạn đang hướng chéo lên trái nên dùng (-1, 1)."
    )]
    [SerializeField]
    private Vector2 spriteForwardLocal =
        new Vector2(-1f, 1f);

    [Tooltip(
        "Bật duy nhất khi đầu cần vẫn quay ngược chuột."
    )]
    [SerializeField]
    private bool reverseForward;

    [Header("Aim")]
    [Tooltip(
        "0 = xoay ngay. Giá trị 1440-2880 cho chuyển động nhanh và mượt."
    )]
    [Min(0f)]
    [SerializeField]
    private float maximumDegreesPerSecond =
        2160f;

    [Tooltip(
        "Chuột quá gần tâm xoay thì giữ góc cũ để tránh rung."
    )]
    [Min(0f)]
    [SerializeField]
    private float minimumAimDistance =
        0.12f;

    [Tooltip(
        "Bỏ qua sai số góc rất nhỏ để tránh giật."
    )]
    [Range(0f, 3f)]
    [SerializeField]
    private float angleDeadZone =
        0.08f;

    [Tooltip(
        "Góc tinh chỉnh cuối cùng. Thường để 0."
    )]
    [SerializeField]
    private float extraAngleOffset;

    [Header("Important")]
    [Tooltip(
        "Tự tắt các component xoay cần câu cũ trên cùng Player."
    )]
    [SerializeField]
    private bool disableKnownOldAimScripts =
        true;

    [Tooltip(
        "Không dùng Flip X/Y khi cần câu đã xoay đủ 360 độ."
    )]
    [SerializeField]
    private bool forceDisableSpriteFlip =
        true;

    [Header("Debug")]
    [SerializeField]
    private bool drawDebugLines;

    [SerializeField]
    private bool showSetupWarnings =
        true;

    private Plane aimPlane;
    private Vector3 mouseWorld;
    private bool setupCleaned;

    private void Awake()
    {
        ResolveReferences();
        CleanConflictingScripts();
    }

    private void OnEnable()
    {
        ResolveReferences();
        CleanConflictingScripts();
    }

    private void LateUpdate()
    {
        ResolveReferences();

        if (!IsSetupValid())
            return;

        if (!TryGetMouseWorld(
                out mouseWorld))
        {
            return;
        }

        Vector2 targetDirection =
            mouseWorld -
            rodPivot.position;

        if (targetDirection.sqrMagnitude <
            minimumAimDistance *
            minimumAimDistance)
        {
            return;
        }

        Vector2 localForward =
            spriteForwardLocal;

        if (localForward.sqrMagnitude <
            0.000001f)
        {
            localForward =
                new Vector2(-1f, 1f);
        }

        localForward.Normalize();

        if (reverseForward)
        {
            localForward =
                -localForward;
        }

        /*
         * TransformVector tính luôn rotation và scale/mirror của cha.
         * Vì vậy Player hoặc ToolHolder có scale âm vẫn không làm
         * hướng cần câu bị đảo trái/phải.
         */
        Vector3 actualForward3 =
            rodVisual.TransformVector(
                new Vector3(
                    localForward.x,
                    localForward.y,
                    0f
                )
            );

        Vector2 actualForward =
            new Vector2(
                actualForward3.x,
                actualForward3.y
            );

        if (actualForward.sqrMagnitude <
            0.000001f)
        {
            return;
        }

        actualForward.Normalize();
        targetDirection.Normalize();

        float remainingAngle =
            Vector2.SignedAngle(
                actualForward,
                targetDirection
            ) +
            extraAngleOffset;

        if (Mathf.Abs(remainingAngle) <=
            angleDeadZone)
        {
            return;
        }

        float rotationStep =
            remainingAngle;

        if (maximumDegreesPerSecond > 0f)
        {
            float maximumStep =
                maximumDegreesPerSecond *
                Time.deltaTime;

            rotationStep =
                Mathf.Clamp(
                    remainingAngle,
                    -maximumStep,
                    maximumStep
                );
        }

        /*
         * Xoay bằng phần chênh lệch thật giữa hướng đầu cần
         * và hướng chuột. Không còn phụ thuộc Angle Offset 135/-135.
         */
        rodPivot.Rotate(
            0f,
            0f,
            rotationStep,
            Space.World
        );

        if (forceDisableSpriteFlip &&
            rodRenderer != null)
        {
            rodRenderer.flipX = false;
            rodRenderer.flipY = false;
        }

        if (drawDebugLines)
        {
            Debug.DrawRay(
                rodPivot.position,
                actualForward * 2f,
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
        out Vector3 result)
    {
        result = Vector3.zero;

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
                out float enter))
        {
            return false;
        }

        result =
            ray.GetPoint(enter);

        result.z =
            rodPivot.position.z;

        return true;
    }

    private bool IsSetupValid()
    {
        if (rodPivot != null &&
            rodVisual != null &&
            worldCamera != null)
        {
            return true;
        }

        if (showSetupWarnings)
        {
            Debug.LogWarning(
                "[FishingRodMouseAimFinal] Thiếu reference: " +
                "Rod Pivot, Rod Visual hoặc World Camera.",
                this
            );

            showSetupWarnings = false;
        }

        return false;
    }

    private void ResolveReferences()
    {
        if (rodPivot == null)
        {
            rodPivot =
                transform;
        }

        if (rodVisual == null)
        {
            rodVisual =
                rodRenderer != null
                    ? rodRenderer.transform
                    : transform;
        }

        if (rodRenderer == null &&
            rodVisual != null)
        {
            rodRenderer =
                rodVisual
                    .GetComponentInChildren<
                        SpriteRenderer
                    >(true);
        }

        if (rodRenderer != null &&
            rodVisual == null)
        {
            rodVisual =
                rodRenderer.transform;
        }

        if (worldCamera == null)
        {
            worldCamera =
                Camera.main;
        }
    }

    private void CleanConflictingScripts()
    {
        if (setupCleaned ||
            !disableKnownOldAimScripts)
        {
            return;
        }

        setupCleaned = true;

        Transform root =
            transform.root;

        MonoBehaviour[] behaviours =
            root.GetComponentsInChildren<
                MonoBehaviour
            >(true);

        string[] conflictingTypes =
        {
            "FishingRodMouseAim",
            "FishingRodMouseAimStable",
            "FishingRodAutoRotate",
            "FishingRodAimByTip"
        };

        foreach (
            MonoBehaviour behaviour
            in behaviours)
        {
            if (behaviour == null ||
                behaviour == this)
            {
                continue;
            }

            string typeName =
                behaviour.GetType().Name;

            foreach (
                string conflictingType
                in conflictingTypes)
            {
                if (!string.Equals(
                        typeName,
                        conflictingType,
                        StringComparison
                            .Ordinal))
                {
                    continue;
                }

                behaviour.enabled =
                    false;

                Debug.Log(
                    "[FishingRodMouseAimFinal] Đã tắt script xoay cũ: " +
                    typeName +
                    " trên " +
                    behaviour.name +
                    ".",
                    behaviour
                );

                break;
            }
        }

        /*
         * Tắt phần xoay theo hướng nhân vật trong
         * FishingRodHotbarController bằng Serialized field không khả dụng
         * ở runtime. Component này phải được bỏ chọn
         * Rotate With Player Direction trong Inspector.
         */
    }

    [ContextMenu(
        "TEST - Print Current Forward"
    )]
    private void PrintCurrentForward()
    {
        ResolveReferences();

        if (rodVisual == null)
            return;

        Vector2 localForward =
            spriteForwardLocal.sqrMagnitude >
                0.000001f
                ? spriteForwardLocal.normalized
                : new Vector2(-1f, 1f);

        Vector3 worldForward =
            rodVisual.TransformVector(
                new Vector3(
                    localForward.x,
                    localForward.y,
                    0f
                )
            );

        Debug.Log(
            "[FishingRodMouseAimFinal] " +
            "LocalForward=" +
            localForward +
            " | WorldForward=" +
            worldForward,
            this
        );
    }
}
