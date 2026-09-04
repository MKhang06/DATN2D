#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class ToolShopStandaloneFinalEditor
{
    private const string RootFolder =
        "Assets/Bao/ToolShopStandalone";

    private const string ItemFolder =
        RootFolder + "/Items";

    private const string ResourcesFolder =
        RootFolder + "/Resources";

    private const string ProductFolder =
        ResourcesFolder +
        "/ToolShopProducts";

    [MenuItem(
        "Tools/Tool Shop/FINAL - Rebuild Standalone Shop"
    )]
    public static void Rebuild()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog(
                "Tool Shop FINAL",
                "Hãy thoát Play Mode trước.",
                "OK"
            );
            return;
        }

        EnsureFolders();

        DisableOldShopObjects();

        InventoryItemData wateringCan =
            CreateItem(
                "watering_can",
                "Bình tưới nước",
                "Dụng cụ dùng để tưới cây.",
                150
            );

        InventoryItemData hoe =
            CreateItem(
                "hoe",
                "Cuốc đất",
                "Dụng cụ dùng để cuốc đất.",
                120
            );

        InventoryItemData axe =
            CreateItem(
                "axe",
                "Rìu",
                "Dụng cụ dùng để chặt cây.",
                300
            );

        InventoryItemData pickaxe =
            CreateItem(
                "pickaxe",
                "Cuốc chim",
                "Dụng cụ dùng để khai thác đá.",
                350
            );

        InventoryItemData sickle =
            CreateItem(
                "sickle",
                "Liềm",
                "Dụng cụ dùng để thu hoạch.",
                180
            );

        ToolShopItemData[] products =
        {
            CreateProduct(
                "Product_WateringCan",
                wateringCan
            ),
            CreateProduct(
                "Product_Hoe",
                hoe
            ),
            CreateProduct(
                "Product_Axe",
                axe
            ),
            CreateProduct(
                "Product_Pickaxe",
                pickaxe
            ),
            CreateProduct(
                "Product_Sickle",
                sickle
            )
        };

        GameObject existing =
            GameObject.Find(
                "ToolSupplyShopStandaloneCanvas"
            );

        if (existing != null)
            Undo.DestroyObjectImmediate(
                existing
            );

        GameObject shopObject =
            new GameObject(
                "ToolSupplyShopStandaloneCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster),
                typeof(ToolSupplyShopStandalone)
            );

        Undo.RegisterCreatedObjectUndo(
            shopObject,
            "Create Standalone Tool Shop"
        );

        Canvas canvas =
            shopObject.GetComponent<
                Canvas
            >();

        canvas.renderMode =
            RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 32767;

        CanvasScaler scaler =
            shopObject.GetComponent<
                CanvasScaler
            >();

        scaler.uiScaleMode =
            CanvasScaler.ScaleMode
                .ScaleWithScreenSize;
        scaler.referenceResolution =
            new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        ToolSupplyShopStandalone shop =
            shopObject.GetComponent<
                ToolSupplyShopStandalone
            >();

        SerializedObject serialized =
            new SerializedObject(shop);

        SerializedProperty productArray =
            serialized.FindProperty(
                "products"
            );

        productArray.arraySize =
            products.Length;

        for (int index = 0;
             index < products.Length;
             index++)
        {
            productArray
                .GetArrayElementAtIndex(
                    index
                )
                .objectReferenceValue =
                    products[index];
        }

        InventoryManager inventory =
            UnityEngine.Object
                .FindFirstObjectByType<
                    InventoryManager
                >(
                    FindObjectsInactive.Include
                );

        serialized
            .FindProperty("inventoryManager")
            .objectReferenceValue =
                inventory;

        serialized
            .FindProperty("walletSource")
            .objectReferenceValue =
                FindPlayerStats();

        serialized.ApplyModifiedProperties();

        FixEventSystem();

        EditorUtility.SetDirty(shop);
        EditorSceneManager.MarkAllScenesDirty();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeGameObject =
            shopObject;

        EditorUtility.DisplayDialog(
            "Tool Shop FINAL",
            "Đã tạo shop độc lập hoàn toàn.\n\n" +
            "Shop không dùng prefab cũ.\n" +
            "Shop tự tạo card lúc chạy.\n" +
            "Shop tự vô hiệu hóa raycaster UI khác khi mở.\n\n" +
            "Gắn icon trong:\n" +
            ItemFolder +
            "\n\nPlay và nhấn F8.",
            "OK"
        );
    }

    private static void DisableOldShopObjects()
    {
        MonoBehaviour[] behaviours =
            UnityEngine.Object
                .FindObjectsByType<
                    MonoBehaviour
                >(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None
                );

        foreach (MonoBehaviour behaviour
                 in behaviours)
        {
            if (behaviour == null)
                continue;

            string typeName =
                behaviour.GetType().Name;

            if (typeName ==
                    "ToolSupplyShopUI" ||
                typeName ==
                    "ToolShopPurchasePopupUI")
            {
                Undo.RecordObject(
                    behaviour.gameObject,
                    "Disable Old Tool Shop"
                );

                behaviour.gameObject
                    .SetActive(false);
            }
        }

        GameObject oldNamed =
            GameObject.Find(
                "ToolSupplyShopCanvas"
            );

        if (oldNamed != null)
        {
            Undo.RecordObject(
                oldNamed,
                "Disable Old Shop Canvas"
            );

            oldNamed.SetActive(false);
        }
    }

    private static void FixEventSystem()
    {
        EventSystem eventSystem =
            UnityEngine.Object
                .FindFirstObjectByType<
                    EventSystem
                >(
                    FindObjectsInactive.Include
                );

        if (eventSystem == null)
        {
            GameObject eventObject =
                new GameObject(
                    "EventSystem",
                    typeof(EventSystem)
                );

            Undo.RegisterCreatedObjectUndo(
                eventObject,
                "Create EventSystem"
            );

            eventSystem =
                eventObject.GetComponent<
                    EventSystem
                >();
        }

        eventSystem.gameObject
            .SetActive(true);

        Type moduleType =
            Type.GetType(
                "UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem"
            );

        if (moduleType != null)
        {
            Component module =
                eventSystem.gameObject
                    .GetComponent(
                        moduleType
                    );

            if (module == null)
            {
                module =
                    Undo.AddComponent(
                        eventSystem.gameObject,
                        moduleType
                    );
            }

            MethodInfo assignDefaults =
                moduleType.GetMethod(
                    "AssignDefaultActions",
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic
                );

            assignDefaults?.Invoke(
                module,
                null
            );

            if (module is Behaviour behaviour)
                behaviour.enabled = true;

            StandaloneInputModule standalone =
                eventSystem.GetComponent<
                    StandaloneInputModule
                >();

            if (standalone != null)
                standalone.enabled = false;
        }
        else
        {
            StandaloneInputModule standalone =
                eventSystem.GetComponent<
                    StandaloneInputModule
                >();

            if (standalone == null)
            {
                standalone =
                    Undo.AddComponent<
                        StandaloneInputModule
                    >(eventSystem.gameObject);
            }

            standalone.enabled = true;
        }

        EditorUtility.SetDirty(
            eventSystem.gameObject
        );
    }

    private static InventoryItemData CreateItem(
        string itemId,
        string displayName,
        string description,
        int buyPrice)
    {
        string path =
            ItemFolder +
            "/" +
            itemId +
            ".asset";

        InventoryItemData item =
            AssetDatabase.LoadAssetAtPath<
                InventoryItemData
            >(path);

        if (item == null)
        {
            item =
                ScriptableObject
                    .CreateInstance<
                        InventoryItemData
                    >();

            AssetDatabase.CreateAsset(
                item,
                path
            );
        }

        SerializedObject serialized =
            new SerializedObject(item);

        SetString(
            serialized,
            itemId,
            "itemId",
            "id"
        );

        SetString(
            serialized,
            displayName,
            "displayName",
            "itemName"
        );

        SetString(
            serialized,
            description,
            "description"
        );

        SetInteger(
            serialized,
            1,
            "maxStack"
        );

        SetBoolean(
            serialized,
            true,
            "uniqueOwnership",
            "isUnique"
        );

        SetInteger(
            serialized,
            buyPrice,
            "buyPrice"
        );

        SetInteger(
            serialized,
            Mathf.Max(1, buyPrice / 2),
            "sellPrice"
        );

        serialized.ApplyModifiedProperties();
        EditorUtility.SetDirty(item);

        return item;
    }

    private static ToolShopItemData CreateProduct(
        string fileName,
        InventoryItemData item)
    {
        string path =
            ProductFolder +
            "/" +
            fileName +
            ".asset";

        ToolShopItemData product =
            AssetDatabase.LoadAssetAtPath<
                ToolShopItemData
            >(path);

        if (product == null)
        {
            product =
                ScriptableObject
                    .CreateInstance<
                        ToolShopItemData
                    >();

            AssetDatabase.CreateAsset(
                product,
                path
            );
        }

        product.inventoryItem = item;
        product.category = "Dụng cụ";
        product.priceOverride = -1;
        product.maxPurchaseQuantity = 1;

        EditorUtility.SetDirty(product);

        return product;
    }

    private static MonoBehaviour FindPlayerStats()
    {
        return UnityEngine.Object
            .FindObjectsByType<
                MonoBehaviour
            >(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            )
            .FirstOrDefault(
                behaviour =>
                    behaviour != null &&
                    behaviour.GetType().Name ==
                        "PlayerStats"
            );
    }

    private static void EnsureFolders()
    {
        EnsureFolder("Assets/Bao");
        EnsureFolder(RootFolder);
        EnsureFolder(ItemFolder);
        EnsureFolder(ResourcesFolder);
        EnsureFolder(ProductFolder);
    }

    private static void EnsureFolder(
        string path)
    {
        if (AssetDatabase.IsValidFolder(
                path))
        {
            return;
        }

        string[] parts =
            path.Split('/');

        string current =
            parts[0];

        for (int index = 1;
             index < parts.Length;
             index++)
        {
            string next =
                current +
                "/" +
                parts[index];

            if (!AssetDatabase
                    .IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(
                    current,
                    parts[index]
                );
            }

            current = next;
        }
    }

    private static void SetString(
        SerializedObject serialized,
        string value,
        params string[] names)
    {
        SerializedProperty property =
            FindProperty(
                serialized,
                names
            );

        if (property != null &&
            property.propertyType ==
                SerializedPropertyType.String)
        {
            property.stringValue = value;
        }
    }

    private static void SetInteger(
        SerializedObject serialized,
        int value,
        params string[] names)
    {
        SerializedProperty property =
            FindProperty(
                serialized,
                names
            );

        if (property != null &&
            property.propertyType ==
                SerializedPropertyType.Integer)
        {
            property.intValue = value;
        }
    }

    private static void SetBoolean(
        SerializedObject serialized,
        bool value,
        params string[] names)
    {
        SerializedProperty property =
            FindProperty(
                serialized,
                names
            );

        if (property != null &&
            property.propertyType ==
                SerializedPropertyType.Boolean)
        {
            property.boolValue = value;
        }
    }

    private static SerializedProperty FindProperty(
        SerializedObject serialized,
        params string[] names)
    {
        foreach (string name in names)
        {
            SerializedProperty property =
                serialized.FindProperty(name);

            if (property != null)
                return property;
        }

        return null;
    }
}
#endif
