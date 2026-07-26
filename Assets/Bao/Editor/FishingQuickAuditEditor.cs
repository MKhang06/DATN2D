#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class FishingQuickAuditEditor
{
    private const string Prefix = "[FISHING QUICK AUDIT] ";

    [MenuItem("Tools/Fishing/Run Fishing Audit")]
    public static void RunAudit()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog(
                "Fishing Audit",
                "Hãy thoát Play Mode trước khi chạy.",
                "OK"
            );
            return;
        }

        int errors = 0;
        int warnings = 0;
        int infos = 0;

        Debug.Log(
            "\n====================================\n" +
            Prefix + "START\n" +
            "===================================="
        );

        CheckClass(
            "FishingEquipmentData",
            ref errors,
            ref warnings,
            ref infos
        );

        CheckClass(
            "FishingEquipmentShopItemData",
            ref errors,
            ref warnings,
            ref infos
        );

        CheckClass(
            "FishingGearStats",
            ref errors,
            ref warnings,
            ref infos
        );

        CheckClass(
            "FishingCustomizationUI",
            ref errors,
            ref warnings,
            ref infos
        );

        CheckClass(
            "FishingEquipmentShopUI",
            ref errors,
            ref warnings,
            ref infos
        );

        CheckClass(
            "InventoryManager",
            ref errors,
            ref warnings,
            ref infos
        );

        CheckClass(
            "InventoryItemData",
            ref errors,
            ref warnings,
            ref infos
        );

        CheckClass(
            "ItemDatabase",
            ref errors,
            ref warnings,
            ref infos
        );

        CheckMissingScripts(
            ref errors,
            ref infos
        );

        MonoBehaviour inventory =
            FindSceneComponent(
                "InventoryManager"
            );

        MonoBehaviour gearStats =
            FindSceneComponent(
                "FishingGearStats"
            );

        MonoBehaviour customization =
            FindSceneComponent(
                "FishingCustomizationUI"
            );

        MonoBehaviour shop =
            FindSceneComponent(
                "FishingEquipmentShopUI"
            );

        CheckSceneComponent(
            "InventoryManager",
            inventory,
            ref errors,
            ref infos
        );

        CheckSceneComponent(
            "FishingGearStats",
            gearStats,
            ref errors,
            ref infos
        );

        CheckSceneComponent(
            "FishingCustomizationUI",
            customization,
            ref errors,
            ref infos
        );

        CheckSceneComponent(
            "FishingEquipmentShopUI",
            shop,
            ref errors,
            ref infos
        );

        if (shop != null)
        {
            CheckReference(
                shop,
                "inventoryManager",
                true,
                ref errors,
                ref warnings
            );

            CheckReference(
                shop,
                "content",
                true,
                ref errors,
                ref warnings
            );

            CheckReference(
                shop,
                "equipmentCardPrefab",
                true,
                ref errors,
                ref warnings
            );

            CheckArray(
                shop,
                "availableEquipment",
                true,
                ref errors,
                ref warnings
            );
        }

        if (gearStats != null)
        {
            CheckReference(
                gearStats,
                "inventoryManager",
                true,
                ref errors,
                ref warnings
            );
        }

        if (customization != null)
        {
            CheckReference(
                customization,
                "gearStats",
                true,
                ref errors,
                ref warnings
            );

            CheckReference(
                customization,
                "inventoryManager",
                true,
                ref errors,
                ref warnings
            );

            CheckReference(
                customization,
                "equipmentShopUI",
                false,
                ref errors,
                ref warnings
            );

            CheckReference(
                customization,
                "selectionPanel",
                true,
                ref errors,
                ref warnings
            );

            CheckReference(
                customization,
                "optionContent",
                true,
                ref errors,
                ref warnings
            );

            CheckReference(
                customization,
                "optionButtonPrefab",
                true,
                ref errors,
                ref warnings
            );

            CheckArray(
                customization,
                "shopEquipmentItems",
                false,
                ref errors,
                ref warnings
            );

            CheckArray(
                customization,
                "availableEquipment",
                false,
                ref errors,
                ref warnings
            );
        }

        AuditShopItems(
            ref errors,
            ref warnings,
            ref infos
        );

        Debug.Log(
            "\n====================================\n" +
            Prefix +
            "DONE | Errors=" +
            errors +
            " | Warnings=" +
            warnings +
            " | Info=" +
            infos +
            "\n===================================="
        );

        EditorUtility.DisplayDialog(
            "Fishing Audit",
            "Errors: " +
            errors +
            "\nWarnings: " +
            warnings +
            "\nInfo: " +
            infos +
            "\n\nXem Console và lọc:\n" +
            "FISHING QUICK AUDIT",
            "OK"
        );
    }

    [MenuItem("Tools/Fishing/Fix Fishing References")]
    public static void FixReferences()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog(
                "Fishing Fix",
                "Hãy thoát Play Mode trước khi chạy.",
                "OK"
            );
            return;
        }

        MonoBehaviour inventory =
            FindSceneComponent(
                "InventoryManager"
            );

        MonoBehaviour gearStats =
            FindSceneComponent(
                "FishingGearStats"
            );

        MonoBehaviour customization =
            FindSceneComponent(
                "FishingCustomizationUI"
            );

        MonoBehaviour shop =
            FindSceneComponent(
                "FishingEquipmentShopUI"
            );

        int fixedCount = 0;

        if (shop != null &&
            inventory != null)
        {
            fixedCount +=
                SetReference(
                    shop,
                    "inventoryManager",
                    inventory
                );
        }

        if (gearStats != null &&
            inventory != null)
        {
            fixedCount +=
                SetReference(
                    gearStats,
                    "inventoryManager",
                    inventory
                );
        }

        if (customization != null)
        {
            if (gearStats != null)
            {
                fixedCount +=
                    SetReference(
                        customization,
                        "gearStats",
                        gearStats
                    );
            }

            if (inventory != null)
            {
                fixedCount +=
                    SetReference(
                        customization,
                        "inventoryManager",
                        inventory
                    );
            }

            if (shop != null)
            {
                fixedCount +=
                    SetReference(
                        customization,
                        "equipmentShopUI",
                        shop
                    );
            }

            fixedCount +=
                AutoFindChildReference(
                    customization,
                    "selectionPanel",
                    new string[]
                    {
                        "SelectionPanel"
                    },
                    null
                );

            fixedCount +=
                AutoFindChildReference(
                    customization,
                    "optionContent",
                    new string[]
                    {
                        "Content"
                    },
                    "Transform"
                );

            fixedCount +=
                AutoFindChildReference(
                    customization,
                    "emptyListText",
                    new string[]
                    {
                        "EmptyListText"
                    },
                    "TMP_Text"
                );

            fixedCount +=
                AutoFindChildReference(
                    customization,
                    "unequipButton",
                    new string[]
                    {
                        "UnequipButton"
                    },
                    "Button"
                );

            fixedCount +=
                AutoFindChildReference(
                    customization,
                    "closeButton",
                    new string[]
                    {
                        "CloseButton"
                    },
                    "Button"
                );
        }

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkAllScenesDirty();

        Debug.Log(
            Prefix +
            "Đã sửa " +
            fixedCount +
            " reference. Nhấn Ctrl+S để lưu Scene."
        );

        EditorUtility.DisplayDialog(
            "Fishing Fix",
            "Đã sửa " +
            fixedCount +
            " reference.\n\n" +
            "Nhấn Ctrl+S để lưu Scene.",
            "OK"
        );
    }

    private static void CheckClass(
        string className,
        ref int errors,
        ref int warnings,
        ref int infos)
    {
        List<string> files =
            FindClassFiles(
                className
            );

        if (files.Count == 0)
        {
            errors++;

            Debug.LogError(
                Prefix +
                "Thiếu class: " +
                className
            );

            return;
        }

        if (files.Count > 1)
        {
            errors++;

            Debug.LogError(
                Prefix +
                "Class bị trùng: " +
                className +
                "\n- " +
                string.Join(
                    "\n- ",
                    files.ToArray()
                )
            );

            return;
        }

        infos++;

        Debug.Log(
            Prefix +
            className +
            " -> " +
            files[0]
        );
    }

    private static List<string>
        FindClassFiles(
            string className)
    {
        string[] guids =
            AssetDatabase.FindAssets(
                className +
                " t:MonoScript",
                new string[]
                {
                    "Assets"
                }
            );

        Regex regex =
            new Regex(
                @"\b(class|struct)\s+" +
                Regex.Escape(
                    className
                ) +
                @"\b"
            );

        List<string> result =
            new List<string>();

        foreach (string guid in guids)
        {
            string path =
                AssetDatabase
                    .GUIDToAssetPath(
                        guid
                    );

            if (!path.EndsWith(
                    ".cs",
                    StringComparison
                        .OrdinalIgnoreCase))
            {
                continue;
            }

            try
            {
                string content =
                    File.ReadAllText(
                        path
                    );

                if (regex.IsMatch(content))
                    result.Add(path);
            }
            catch
            {
                // Bỏ qua file không đọc được.
            }
        }

        return result;
    }

    private static void CheckMissingScripts(
        ref int errors,
        ref int infos)
    {
        int missing = 0;

        GameObject[] objects =
            Resources
                .FindObjectsOfTypeAll<
                    GameObject
                >();

        foreach (GameObject go in objects)
        {
            if (go == null ||
                !go.scene.IsValid())
            {
                continue;
            }

            int count =
                GameObjectUtility
                    .GetMonoBehavioursWithMissingScriptCount(
                        go
                    );

            if (count <= 0)
                continue;

            missing += count;
            errors += count;

            Debug.LogError(
                Prefix +
                go.name +
                " có " +
                count +
                " Missing Script.",
                go
            );
        }

        if (missing == 0)
        {
            infos++;

            Debug.Log(
                Prefix +
                "Không có Missing Script."
            );
        }
    }

    private static MonoBehaviour
        FindSceneComponent(
            string typeName)
    {
        MonoBehaviour[] all =
            Resources
                .FindObjectsOfTypeAll<
                    MonoBehaviour
                >();

        foreach (
            MonoBehaviour item
            in all)
        {
            if (item == null ||
                item.gameObject == null ||
                !item.gameObject
                    .scene.IsValid())
            {
                continue;
            }

            if (item.GetType().Name ==
                typeName)
            {
                return item;
            }
        }

        return null;
    }

    private static void CheckSceneComponent(
        string typeName,
        MonoBehaviour component,
        ref int errors,
        ref int infos)
    {
        if (component == null)
        {
            errors++;

            Debug.LogError(
                Prefix +
                "Scene không có component " +
                typeName +
                "."
            );

            return;
        }

        infos++;

        Debug.Log(
            Prefix +
            "Scene component " +
            typeName +
            " -> " +
            component.name,
            component
        );
    }

    private static void CheckReference(
        UnityEngine.Object target,
        string fieldName,
        bool required,
        ref int errors,
        ref int warnings)
    {
        SerializedObject serialized =
            new SerializedObject(
                target
            );

        SerializedProperty property =
            serialized.FindProperty(
                fieldName
            );

        if (property == null)
        {
            warnings++;

            Debug.LogWarning(
                Prefix +
                target.GetType().Name +
                " không có field " +
                fieldName +
                ". Có thể đang dùng script cũ.",
                target
            );

            return;
        }

        if (property.propertyType !=
            SerializedPropertyType
                .ObjectReference)
        {
            return;
        }

        if (property.objectReferenceValue !=
            null)
        {
            return;
        }

        if (required)
        {
            errors++;

            Debug.LogError(
                Prefix +
                target.name +
                "." +
                fieldName +
                " đang None.",
                target
            );
        }
        else
        {
            warnings++;

            Debug.LogWarning(
                Prefix +
                target.name +
                "." +
                fieldName +
                " đang None.",
                target
            );
        }
    }

    private static void CheckArray(
        UnityEngine.Object target,
        string fieldName,
        bool required,
        ref int errors,
        ref int warnings)
    {
        SerializedObject serialized =
            new SerializedObject(
                target
            );

        SerializedProperty property =
            serialized.FindProperty(
                fieldName
            );

        if (property == null)
        {
            warnings++;

            Debug.LogWarning(
                Prefix +
                target.GetType().Name +
                " không có array " +
                fieldName +
                ".",
                target
            );

            return;
        }

        if (!property.isArray)
            return;

        if (property.arraySize > 0)
            return;

        if (required)
        {
            errors++;

            Debug.LogError(
                Prefix +
                target.name +
                "." +
                fieldName +
                " Size=0.",
                target
            );
        }
        else
        {
            warnings++;

            Debug.LogWarning(
                Prefix +
                target.name +
                "." +
                fieldName +
                " Size=0.",
                target
            );
        }
    }

    private static void AuditShopItems(
        ref int errors,
        ref int warnings,
        ref int infos)
    {
        string[] guids =
            AssetDatabase.FindAssets(
                "t:FishingEquipmentShopItemData",
                new string[]
                {
                    "Assets"
                }
            );

        if (guids.Length == 0)
        {
            errors++;

            Debug.LogError(
                Prefix +
                "Không tìm thấy FishingEquipmentShopItemData."
            );

            return;
        }

        foreach (string guid in guids)
        {
            string path =
                AssetDatabase
                    .GUIDToAssetPath(
                        guid
                    );

            UnityEngine.Object shopItem =
                AssetDatabase
                    .LoadMainAssetAtPath(
                        path
                    );

            if (shopItem == null)
                continue;

            SerializedObject serialized =
                new SerializedObject(
                    shopItem
                );

            SerializedProperty equipment =
                serialized.FindProperty(
                    "equipmentData"
                );

            if (equipment == null)
            {
                warnings++;

                Debug.LogWarning(
                    Prefix +
                    shopItem.name +
                    " không có field equipmentData.",
                    shopItem
                );

                continue;
            }

            if (equipment.objectReferenceValue ==
                null)
            {
                errors++;

                Debug.LogError(
                    Prefix +
                    shopItem.name +
                    " chưa gắn Equipment Data.",
                    shopItem
                );

                continue;
            }

            infos++;

            Debug.Log(
                Prefix +
                shopItem.name +
                " -> EquipmentData=" +
                equipment
                    .objectReferenceValue
                    .name,
                shopItem
            );
        }
    }

    private static int SetReference(
        UnityEngine.Object target,
        string fieldName,
        UnityEngine.Object value)
    {
        if (target == null ||
            value == null)
        {
            return 0;
        }

        SerializedObject serialized =
            new SerializedObject(
                target
            );

        SerializedProperty property =
            serialized.FindProperty(
                fieldName
            );

        if (property == null ||
            property.propertyType !=
                SerializedPropertyType
                    .ObjectReference)
        {
            return 0;
        }

        if (property.objectReferenceValue ==
            value)
        {
            return 0;
        }

        Undo.RecordObject(
            target,
            "Fix Fishing Reference"
        );

        property.objectReferenceValue =
            value;

        serialized
            .ApplyModifiedProperties();

        EditorUtility.SetDirty(
            target
        );

        Debug.Log(
            Prefix +
            "[FIXED] " +
            target.name +
            "." +
            fieldName +
            " -> " +
            value.name,
            target
        );

        return 1;
    }

    private static int
        AutoFindChildReference(
            MonoBehaviour owner,
            string fieldName,
            string[] objectNames,
            string componentTypeName)
    {
        if (owner == null)
            return 0;

        SerializedObject serialized =
            new SerializedObject(
                owner
            );

        SerializedProperty property =
            serialized.FindProperty(
                fieldName
            );

        if (property == null ||
            property.propertyType !=
                SerializedPropertyType
                    .ObjectReference ||
            property.objectReferenceValue !=
                null)
        {
            return 0;
        }

        Transform[] children =
            owner.GetComponentsInChildren<
                Transform
            >(true);

        Transform found = null;

        foreach (Transform child in children)
        {
            if (child == null)
                continue;

            foreach (
                string objectName
                in objectNames)
            {
                if (string.Equals(
                        child.name,
                        objectName,
                        StringComparison
                            .OrdinalIgnoreCase))
                {
                    found = child;
                    break;
                }
            }

            if (found != null)
                break;
        }

        if (found == null)
            return 0;

        UnityEngine.Object value = null;

        if (string.IsNullOrWhiteSpace(
                componentTypeName))
        {
            value =
                property.type.Contains(
                    "GameObject")
                    ? found.gameObject
                    : found;
        }
        else
        {
            Component[] components =
                found.GetComponentsInChildren<
                    Component
                >(true);

            foreach (
                Component component
                in components)
            {
                if (component == null)
                    continue;

                Type type =
                    component.GetType();

                if (type.Name ==
                        componentTypeName ||
                    IsSubclassNamed(
                        type,
                        componentTypeName
                    ))
                {
                    value = component;
                    break;
                }
            }
        }

        return SetReference(
            owner,
            fieldName,
            value
        );
    }

    private static bool IsSubclassNamed(
        Type type,
        string baseName)
    {
        Type current = type;

        while (current != null)
        {
            if (current.Name ==
                baseName)
            {
                return true;
            }

            current =
                current.BaseType;
        }

        return false;
    }
}
#endif