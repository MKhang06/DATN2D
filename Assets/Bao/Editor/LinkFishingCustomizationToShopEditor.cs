#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class LinkFishingCustomizationToShopEditor
{
    [MenuItem(
        "Tools/Fishing/Customization/Auto Link Shop Source"
    )]
    public static void AutoLink()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog(
                "Fishing Customization",
                "Hãy thoát Play Mode trước khi chạy.",
                "OK"
            );

            return;
        }

        FishingEquipmentShopUI[] shops =
            Resources.FindObjectsOfTypeAll<
                FishingEquipmentShopUI
            >();

        FishingEquipmentShopUI shop =
            null;

        foreach (
            FishingEquipmentShopUI candidate
            in shops)
        {
            if (candidate == null ||
                !candidate.gameObject
                    .scene.IsValid())
            {
                continue;
            }

            if (candidate.AvailableEquipment ==
                    null ||
                candidate.AvailableEquipment
                    .Length == 0)
            {
                continue;
            }

            shop = candidate;
            break;
        }

        if (shop == null)
        {
            EditorUtility.DisplayDialog(
                "Fishing Customization",
                "Không tìm thấy FishingEquipmentShopUI trong Scene " +
                "có Available Equipment.",
                "OK"
            );

            return;
        }

        FishingCustomizationUI[] customizations =
            Resources.FindObjectsOfTypeAll<
                FishingCustomizationUI
            >();

        int linkedCount = 0;

        foreach (
            FishingCustomizationUI customization
            in customizations)
        {
            if (customization == null ||
                !customization.gameObject
                    .scene.IsValid())
            {
                continue;
            }

            SerializedObject serialized =
                new SerializedObject(
                    customization
                );

            SerializedProperty shopProperty =
                serialized.FindProperty(
                    "equipmentShopUI"
                );

            SerializedProperty itemsProperty =
                serialized.FindProperty(
                    "shopEquipmentItems"
                );

            SerializedProperty inventoryProperty =
                serialized.FindProperty(
                    "inventoryManager"
                );

            if (shopProperty != null)
            {
                shopProperty.objectReferenceValue =
                    shop;
            }

            if (inventoryProperty != null &&
                shop.Inventory != null)
            {
                inventoryProperty
                    .objectReferenceValue =
                        shop.Inventory;
            }

            if (itemsProperty != null &&
                itemsProperty.isArray)
            {
                FishingEquipmentShopItemData[]
                    items =
                        shop.AvailableEquipment;

                itemsProperty.arraySize =
                    items.Length;

                for (int i = 0;
                     i < items.Length;
                     i++)
                {
                    itemsProperty
                        .GetArrayElementAtIndex(i)
                        .objectReferenceValue =
                            items[i];
                }
            }

            serialized
                .ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(
                customization
            );

            EditorSceneManager.MarkSceneDirty(
                customization.gameObject.scene
            );

            linkedCount++;
        }

        AssetDatabase.SaveAssets();

        EditorUtility.DisplayDialog(
            "Fishing Customization",
            "Đã liên kết " +
            linkedCount +
            " FishingCustomizationUI với Shop.\n\n" +
            "Shop Items: " +
            shop.AvailableEquipment.Length +
            "\n\nNhấn Ctrl + S.",
            "OK"
        );
    }
}
#endif
