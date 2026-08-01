#if UNITY_EDITOR
using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class ToolSupplyShopRepairEditor
{
    private const string ProductFolder =
        "Assets/Bao/ToolShop/Products";

    private const string CardPrefabPath =
        "Assets/Bao/ToolShop/Prefabs/ToolShopItemCard.prefab";

    [MenuItem(
        "Tools/Tool Shop/Repair Empty Product List"
    )]
    public static void RepairEmptyProductList()
    {
        ToolSupplyShopUI shop =
            UnityEngine.Object
                .FindFirstObjectByType<
                    ToolSupplyShopUI
                >(
                    FindObjectsInactive.Include
                );

        if (shop == null)
        {
            EditorUtility.DisplayDialog(
                "Tool Shop Repair",
                "Không tìm thấy ToolSupplyShopUI trong Scene.\n" +
                "Hãy chạy Create Full Tool Shop trước.",
                "OK"
            );

            return;
        }

        Transform root =
            shop.transform;

        RectTransform content =
            FindChild<RectTransform>(
                root,
                "Content"
            );

        TMP_InputField searchInput =
            FindChild<TMP_InputField>(
                root,
                "SearchInput"
            );

        TMP_Text moneyText =
            FindText(
                root,
                "MoneyText"
            );

        TMP_Text messageText =
            FindText(
                root,
                "MessageText"
            );

        Button closeButton =
            FindChild<Button>(
                root,
                "CloseButton"
            );

        ToolShopPurchasePopupUI popup =
            shop.GetComponent<
                ToolShopPurchasePopupUI
            >();

        if (popup == null)
        {
            popup =
                shop.gameObject
                    .AddComponent<
                        ToolShopPurchasePopupUI
                    >();
        }

        ToolShopItemCardUI cardPrefab =
            AssetDatabase
                .LoadAssetAtPath<
                    ToolShopItemCardUI
                >(
                    CardPrefabPath
                );

        string[] productGuids =
            AssetDatabase.FindAssets(
                "t:ToolShopItemData",
                new[]
                {
                    ProductFolder
                }
            );

        ToolShopItemData[] products =
            productGuids
                .Select(
                    guid =>
                        AssetDatabase
                            .LoadAssetAtPath<
                                ToolShopItemData
                            >(
                                AssetDatabase
                                    .GUIDToAssetPath(
                                        guid
                                    )
                            )
                )
                .Where(
                    product =>
                        product != null &&
                        product.inventoryItem != null
                )
                .OrderBy(
                    product =>
                        product.DisplayName
                )
                .ToArray();

        SerializedObject serialized =
            new SerializedObject(shop);

        SetObject(
            serialized,
            "content",
            content
        );

        SetObject(
            serialized,
            "itemCardPrefab",
            cardPrefab
        );

        SetObject(
            serialized,
            "purchasePopup",
            popup
        );

        SetObject(
            serialized,
            "moneyText",
            moneyText
        );

        SetObject(
            serialized,
            "messageText",
            messageText
        );

        SetObject(
            serialized,
            "searchInput",
            searchInput
        );

        SetObject(
            serialized,
            "closeButton",
            closeButton
        );

        SerializedProperty items =
            serialized.FindProperty(
                "availableItems"
            );

        if (items != null)
        {
            items.arraySize =
                products.Length;

            for (int index = 0;
                 index < products.Length;
                 index++)
            {
                items
                    .GetArrayElementAtIndex(
                        index
                    )
                    .objectReferenceValue =
                        products[index];
            }
        }

        serialized.ApplyModifiedProperties();

        if (content != null)
        {
            Undo.RecordObject(
                content,
                "Repair Tool Shop Content"
            );

            content.gameObject
                .SetActive(true);

            content.anchoredPosition =
                Vector2.zero;

            content.localScale =
                Vector3.one;

            GridLayoutGroup grid =
                content.GetComponent<
                    GridLayoutGroup
                >();

            if (grid == null)
            {
                grid =
                    content.gameObject
                        .AddComponent<
                            GridLayoutGroup
                        >();
            }

            grid.cellSize =
                new Vector2(
                    245f,
                    325f
                );

            grid.spacing =
                new Vector2(
                    24f,
                    24f
                );

            grid.constraint =
                GridLayoutGroup
                    .Constraint
                    .Flexible;

            grid.childAlignment =
                TextAnchor.UpperLeft;

            ContentSizeFitter fitter =
                content.GetComponent<
                    ContentSizeFitter
                >();

            if (fitter == null)
            {
                fitter =
                    content.gameObject
                        .AddComponent<
                            ContentSizeFitter
                        >();
            }

            fitter.horizontalFit =
                ContentSizeFitter
                    .FitMode
                    .Unconstrained;

            fitter.verticalFit =
                ContentSizeFitter
                    .FitMode
                    .PreferredSize;
        }

        if (cardPrefab != null)
        {
            GameObject prefabRoot =
                cardPrefab.gameObject;

            if (!prefabRoot.activeSelf)
            {
                prefabRoot.SetActive(true);
                EditorUtility.SetDirty(
                    prefabRoot
                );
            }
        }

        EditorUtility.SetDirty(shop);
        EditorSceneManager.MarkAllScenesDirty();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        string result =
            "Content: " +
            (content != null ? "OK" : "THIẾU") +
            "\nCard Prefab: " +
            (cardPrefab != null ? "OK" : "THIẾU") +
            "\nProducts hợp lệ: " +
            products.Length +
            "\nPurchase Popup: " +
            (popup != null ? "OK" : "THIẾU");

        Debug.Log(
            "[Tool Shop Repair]\n" +
            result,
            shop
        );

        EditorUtility.DisplayDialog(
            "Tool Shop Repair",
            result +
            "\n\nNhấn Play rồi F8 để kiểm tra lại.",
            "OK"
        );

        Selection.activeGameObject =
            shop.gameObject;
    }

    private static T FindChild<T>(
        Transform root,
        string objectName)
        where T : Component
    {
        if (root == null)
            return null;

        T[] components =
            root.GetComponentsInChildren<T>(
                true
            );

        foreach (T component in components)
        {
            if (component != null &&
                string.Equals(
                    component.gameObject.name,
                    objectName,
                    StringComparison.OrdinalIgnoreCase
                ))
            {
                return component;
            }
        }

        return null;
    }

    private static TMP_Text FindText(
        Transform root,
        string objectName)
    {
        TMP_Text[] texts =
            root.GetComponentsInChildren<
                TMP_Text
            >(true);

        foreach (TMP_Text text in texts)
        {
            if (text != null &&
                string.Equals(
                    text.gameObject.name,
                    objectName,
                    StringComparison.OrdinalIgnoreCase
                ))
            {
                return text;
            }
        }

        return null;
    }

    private static void SetObject(
        SerializedObject serialized,
        string propertyName,
        UnityEngine.Object value)
    {
        SerializedProperty property =
            serialized.FindProperty(
                propertyName
            );

        if (property != null)
        {
            property.objectReferenceValue =
                value;
        }
    }
}
#endif