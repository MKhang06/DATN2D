#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class ToolShopVisibleCardsFixEditor
{
    [MenuItem(
        "Tools/Tool Shop/FIX - Product Cards Invisible"
    )]
    public static void FixInvisibleCards()
    {
        ToolSupplyShopStandalone shop =
            Object.FindFirstObjectByType<
                ToolSupplyShopStandalone
            >(
                FindObjectsInactive.Include
            );

        if (shop == null)
        {
            EditorUtility.DisplayDialog(
                "Tool Shop",
                "Không tìm thấy ToolSupplyShopStandalone trong Scene.",
                "OK"
            );
            return;
        }

        Transform[] children =
            shop.GetComponentsInChildren<
                Transform
            >(true);

        int removedMasks = 0;
        int addedRectMasks = 0;

        foreach (Transform child in children)
        {
            if (child == null ||
                child.name != "Viewport")
            {
                continue;
            }

            Mask oldMask =
                child.GetComponent<Mask>();

            if (oldMask != null)
            {
                Undo.DestroyObjectImmediate(
                    oldMask
                );

                removedMasks++;
            }

            Image oldImage =
                child.GetComponent<Image>();

            if (oldImage != null)
            {
                Undo.DestroyObjectImmediate(
                    oldImage
                );
            }

            RectMask2D rectMask =
                child.GetComponent<
                    RectMask2D
                >();

            if (rectMask == null)
            {
                rectMask =
                    Undo.AddComponent<
                        RectMask2D
                    >(child.gameObject);

                addedRectMasks++;
            }

            rectMask.padding =
                Vector4.zero;
        }

        EditorUtility.SetDirty(
            shop.gameObject
        );

        EditorSceneManager
            .MarkAllScenesDirty();

        EditorUtility.DisplayDialog(
            "Tool Shop",
            "Đã sửa Viewport Mask.\n\n" +
            "Mask cũ đã xóa: " +
            removedMasks +
            "\nRectMask2D đã thêm: " +
            addedRectMasks +
            "\n\nNhấn Ctrl+S, Play rồi F8.",
            "OK"
        );
    }
}
#endif
