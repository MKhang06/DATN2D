#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class AutoAssignFishingIconsEditor
{
    private const int MinimumMatchScore = 650;

    private sealed class SpriteCandidate
    {
        public Sprite sprite;
        public string assetPath;
        public string fileName;
        public string spriteName;
        public HashSet<string> normalizedKeys;
        public HashSet<string> tokens;
    }

    private static readonly Dictionary<
        string,
        string[]
    > ManualAliases =
        new Dictionary<string, string[]>
        {
            {
                "fish_small_salmon",
                new[]
                {
                    "ca hoi nho",
                    "small salmon",
                    "salmon"
                }
            },
            {
                "fish_small_pink",
                new[]
                {
                    "ca hong nho",
                    "small pink fish",
                    "pink fish"
                }
            },
            {
                "fish_small_hong",
                new[]
                {
                    "ca hong nho",
                    "ca hong",
                    "pink fish"
                }
            },
            {
                "fish_small_mackerel",
                new[]
                {
                    "ca thu nho",
                    "small mackerel",
                    "mackerel"
                }
            },
            {
                "fish_sea_bass",
                new[]
                {
                    "ca vuoc",
                    "sea bass",
                    "seabass"
                }
            },

            {
                "bait_bread",
                new[] { "banh mi", "bread" }
            },
            {
                "bait_corn",
                new[] { "bap ngo", "corn" }
            },
            {
                "bait_dough",
                new[] { "bot nhao", "dough" }
            },
            {
                "bait_crab",
                new[] { "cua", "crab" }
            },
            {
                "bait_small_fish",
                new[]
                {
                    "ca moi nho",
                    "small bait fish",
                    "bait fish"
                }
            },
            {
                "bait_technology_feed",
                new[]
                {
                    "cam cong nghe",
                    "technology feed",
                    "fish feed"
                }
            },
            {
                "bait_insect",
                new[] { "con trung", "insect" }
            },
            {
                "bait_earthworm",
                new[]
                {
                    "giun dat",
                    "earthworm",
                    "worm"
                }
            },
            {
                "bait_red_worm",
                new[] { "giun do", "red worm" }
            },
            {
                "bait_artificial_lure",
                new[]
                {
                    "moi gia",
                    "artificial lure",
                    "lure"
                }
            },
            {
                "bait_squid",
                new[] { "muc", "squid" }
            },
            {
                "bait_mealworm",
                new[] { "sau bot", "mealworm" }
            },
            {
                "bait_waxworm",
                new[] { "sau sap", "waxworm" }
            },
            {
                "bait_fish_meat",
                new[] { "thit ca", "fish meat" }
            },
            {
                "bait_tuna_meat",
                new[]
                {
                    "thit ca ngu",
                    "tuna meat"
                }
            },
            {
                "bait_fish_scraps",
                new[]
                {
                    "thit ca vun",
                    "fish scraps"
                }
            },
            {
                "bait_maggot",
                new[]
                {
                    "au trung ruoi",
                    "maggot"
                }
            },

            {
                "equipment_reel_callisto_xsr",
                new[]
                {
                    "callisto xsr",
                    "callistoxsr"
                }
            },
            {
                "equipment_line_braided_noodle",
                new[]
                {
                    "day du noodle",
                    "braided noodle"
                }
            },
            {
                "equipment_line_braided_lightning",
                new[]
                {
                    "day du tia chop",
                    "braided lightning",
                    "lightning line"
                }
            },
            {
                "equipment_line_braided_king",
                new[]
                {
                    "day du vua",
                    "braided king",
                    "king line"
                }
            },
            {
                "equipment_line_mobey_mono",
                new[]
                {
                    "day mobey don",
                    "mobey mono"
                }
            },
            {
                "equipment_line_mono_medium",
                new[]
                {
                    "day don co vua",
                    "medium mono line"
                }
            },
            {
                "equipment_line_mono_cheap",
                new[]
                {
                    "day don re tien",
                    "cheap mono line"
                }
            },
            {
                "equipment_rod_feather_light",
                new[]
                {
                    "eq featherlight",
                    "feather light",
                    "featherlight"
                }
            },
            {
                "equipment_hook_6",
                new[]
                {
                    "moc cau 6",
                    "hook 6"
                }
            },
            {
                "equipment_hook_heavy",
                new[]
                {
                    "moc cau hang nang",
                    "heavy hook"
                }
            },
            {
                "equipment_hook_1",
                new[]
                {
                    "moc cau 1",
                    "hook 1"
                }
            }
        };

    private static readonly HashSet<string>
        IgnoredTokens =
            new HashSet<string>
            {
                "icon",
                "icons",
                "sprite",
                "sprites",
                "item",
                "items",
                "inventory",
                "fishing",
                "fish",
                "bait",
                "equipment",
                "shop",
                "data",
                "asset",
                "ui",
                "2d",
                "png",
                "jpg",
                "jpeg",
                "image",
                "images",
                "new",
                "copy"
            };

    [MenuItem(
        "Tools/Fishing Database/Auto Assign Missing Icons"
    )]
    public static void AutoAssignMissingIcons()
    {
        RunAutoAssign(false);
    }

    [MenuItem(
        "Tools/Fishing Database/Force Reassign All Icons"
    )]
    public static void ForceReassignAllIcons()
    {
        bool confirmed =
            EditorUtility.DisplayDialog(
                "Force Reassign All Icons",
                "Tool sẽ thay lại icon của toàn bộ " +
                "InventoryItemData bằng kết quả tự tìm.\n\n" +
                "Tiếp tục?",
                "Tiếp tục",
                "Hủy"
            );

        if (!confirmed)
            return;

        RunAutoAssign(true);
    }

    [MenuItem(
        "Tools/Fishing Database/Validate Missing Icons"
    )]
    public static void ValidateMissingIcons()
    {
        InventoryItemData[] items =
            LoadAllInventoryItems();

        int missing = 0;

        foreach (InventoryItemData item in items)
        {
            if (item == null ||
                item.Icon != null)
            {
                continue;
            }

            Debug.LogWarning(
                "[Fishing Icons] Chưa có icon: " +
                item.ItemId +
                " | " +
                item.DisplayName,
                item
            );

            missing++;
        }

        EditorUtility.DisplayDialog(
            "Validate Missing Icons",
            missing == 0
                ? "Tất cả InventoryItemData đã có icon."
                : "Còn " +
                  missing +
                  " vật phẩm chưa có icon. " +
                  "Xem Console.",
            "OK"
        );
    }

    private static void RunAutoAssign(
        bool forceReassign)
    {
        List<SpriteCandidate> candidates =
            LoadAllSpriteCandidates();

        if (candidates.Count == 0)
        {
            EditorUtility.DisplayDialog(
                "Auto Assign Icons",
                "Không tìm thấy Sprite nào trong Assets.",
                "OK"
            );

            return;
        }

        InventoryItemData[] items =
            LoadAllInventoryItems();

        int assigned = 0;
        int skipped = 0;
        int unresolved = 0;

        Dictionary<
            InventoryItemData,
            Sprite
        > resolvedIcons =
            new Dictionary<
                InventoryItemData,
                Sprite
            >();

        foreach (InventoryItemData item in items)
        {
            if (item == null)
                continue;

            if (!forceReassign &&
                item.Icon != null)
            {
                resolvedIcons[item] = item.Icon;
                skipped++;
                continue;
            }

            SpriteCandidate best =
                FindBestMatch(
                    item,
                    candidates,
                    out int bestScore
                );

            if (best == null ||
                bestScore < MinimumMatchScore)
            {
                unresolved++;

                Debug.LogWarning(
                    "[Fishing Icons] Không tìm được icon đủ chính xác cho: " +
                    item.ItemId +
                    " | " +
                    item.DisplayName +
                    ". Điểm cao nhất: " +
                    bestScore,
                    item
                );

                continue;
            }

            SetInventoryItemIcon(
                item,
                best.sprite
            );

            resolvedIcons[item] =
                best.sprite;

            assigned++;

            Debug.Log(
                "[Fishing Icons] " +
                item.DisplayName +
                " ← " +
                best.assetPath +
                " (score " +
                bestScore +
                ")",
                item
            );
        }

        int specializedUpdated =
            SynchronizeSpecializedAssets(
                resolvedIcons
            );

        int managersUpdated =
            SynchronizeFishingManagers();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Auto Assign Icons",
            "Hoàn tất.\n\n" +
            "Đã gắn mới: " +
            assigned +
            "\nGiữ icon cũ: " +
            skipped +
            "\nKhông tìm được: " +
            unresolved +
            "\nAsset chuyên dụng đã đồng bộ: " +
            specializedUpdated +
            "\nFishingManager đã đồng bộ: " +
            managersUpdated +
            "\n\nNhững mục chưa tìm được đã ghi trong Console.",
            "OK"
        );
    }

    private static List<SpriteCandidate>
        LoadAllSpriteCandidates()
    {
        string[] guids =
            AssetDatabase.FindAssets(
                "t:Sprite",
                new[] { "Assets" }
            );

        List<SpriteCandidate> result =
            new List<SpriteCandidate>();

        HashSet<int> seenSprites =
            new HashSet<int>();

        foreach (string guid in guids)
        {
            string path =
                AssetDatabase.GUIDToAssetPath(
                    guid
                );

            UnityEngine.Object[] assets =
                AssetDatabase.LoadAllAssetsAtPath(
                    path
                );

            foreach (UnityEngine.Object asset
                     in assets)
            {
                Sprite sprite = asset as Sprite;

                if (sprite == null)
                    continue;

                int instanceId =
                    sprite.GetInstanceID();

                if (!seenSprites.Add(instanceId))
                    continue;

                string fileName =
                    Path.GetFileNameWithoutExtension(
                        path
                    );

                SpriteCandidate candidate =
                    new SpriteCandidate
                    {
                        sprite = sprite,
                        assetPath = path,
                        fileName = fileName,
                        spriteName = sprite.name,
                        normalizedKeys =
                            new HashSet<string>(),
                        tokens =
                            new HashSet<string>()
                    };

                AddCandidateKey(
                    candidate,
                    fileName
                );

                AddCandidateKey(
                    candidate,
                    sprite.name
                );

                result.Add(candidate);
            }
        }

        return result;
    }

    private static void AddCandidateKey(
        SpriteCandidate candidate,
        string value)
    {
        string normalized =
            NormalizeCompact(value);

        if (!string.IsNullOrWhiteSpace(
                normalized))
        {
            candidate.normalizedKeys.Add(
                normalized
            );
        }

        foreach (string token
                 in Tokenize(value))
        {
            candidate.tokens.Add(token);
        }
    }

    private static SpriteCandidate FindBestMatch(
        InventoryItemData item,
        List<SpriteCandidate> candidates,
        out int bestScore)
    {
        bestScore = 0;
        SpriteCandidate best = null;

        List<string> aliases =
            BuildItemAliases(item);

        HashSet<string> targetKeys =
            new HashSet<string>();

        HashSet<string> targetTokens =
            new HashSet<string>();

        foreach (string alias in aliases)
        {
            string normalized =
                NormalizeCompact(alias);

            if (!string.IsNullOrWhiteSpace(
                    normalized))
            {
                targetKeys.Add(normalized);
            }

            foreach (string token
                     in Tokenize(alias))
            {
                targetTokens.Add(token);
            }
        }

        foreach (SpriteCandidate candidate
                 in candidates)
        {
            int score =
                CalculateMatchScore(
                    targetKeys,
                    targetTokens,
                    candidate
                );

            if (score <= bestScore)
                continue;

            bestScore = score;
            best = candidate;
        }

        return best;
    }

    private static List<string> BuildItemAliases(
        InventoryItemData item)
    {
        List<string> aliases =
            new List<string>
            {
                item.ItemId,
                item.DisplayName,
                item.name
            };

        string normalizedId =
            NormalizeId(item.ItemId);

        if (ManualAliases.TryGetValue(
                normalizedId,
                out string[] manualAliases))
        {
            aliases.AddRange(manualAliases);
        }

        /*
         * Bỏ tiền tố category để khớp filename ngắn.
         * Ví dụ equipment_hook_heavy → hook_heavy.
         */
        string[] idParts =
            normalizedId.Split(
                new[] { '_' },
                StringSplitOptions
                    .RemoveEmptyEntries
            );

        if (idParts.Length > 1)
        {
            aliases.Add(
                string.Join(
                    "_",
                    idParts.Skip(1)
                )
            );
        }

        if (idParts.Length > 2)
        {
            aliases.Add(
                string.Join(
                    "_",
                    idParts.Skip(2)
                )
            );
        }

        return aliases;
    }

    private static int CalculateMatchScore(
        HashSet<string> targetKeys,
        HashSet<string> targetTokens,
        SpriteCandidate candidate)
    {
        int score = 0;

        foreach (string targetKey
                 in targetKeys)
        {
            foreach (string candidateKey
                     in candidate.normalizedKeys)
            {
                if (targetKey ==
                    candidateKey)
                {
                    score =
                        Mathf.Max(score, 1200);

                    continue;
                }

                if (targetKey.Length >= 5 &&
                    candidateKey.Length >= 5)
                {
                    if (candidateKey.Contains(
                            targetKey) ||
                        targetKey.Contains(
                            candidateKey))
                    {
                        int containmentScore =
                            780 +
                            Mathf.Min(
                                targetKey.Length,
                                candidateKey.Length
                            );

                        score =
                            Mathf.Max(
                                score,
                                containmentScore
                            );
                    }
                }
            }
        }

        if (targetTokens.Count > 0 &&
            candidate.tokens.Count > 0)
        {
            int intersection =
                targetTokens.Intersect(
                    candidate.tokens
                ).Count();

            int union =
                targetTokens.Union(
                    candidate.tokens
                ).Count();

            float jaccard =
                union > 0
                    ? intersection /
                      (float)union
                    : 0f;

            int tokenScore =
                Mathf.RoundToInt(
                    jaccard * 700f
                );

            if (intersection >= 2)
                tokenScore += 120;

            if (intersection ==
                targetTokens.Count)
            {
                tokenScore += 120;
            }

            score =
                Mathf.Max(
                    score,
                    tokenScore
                );
        }

        return score;
    }

    private static void SetInventoryItemIcon(
        InventoryItemData item,
        Sprite sprite)
    {
        if (item == null ||
            sprite == null)
        {
            return;
        }

        SerializedObject serializedItem =
            new SerializedObject(item);

        SerializedProperty icon =
            serializedItem.FindProperty(
                "icon"
            );

        if (icon == null)
        {
            Debug.LogError(
                "InventoryItemData không có field icon.",
                item
            );

            return;
        }

        icon.objectReferenceValue = sprite;

        serializedItem
            .ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(item);
    }

    private static int
        SynchronizeSpecializedAssets(
            Dictionary<
                InventoryItemData,
                Sprite
            > resolvedIcons)
    {
        string[] specializedTypeNames =
        {
            "FishingBaitData",
            "FishingEquipmentData",
            "FishingEquipmentShopItemData"
        };

        int updated = 0;

        foreach (string typeName
                 in specializedTypeNames)
        {
            Type type =
                FindTypeByName(typeName);

            if (type == null)
                continue;

            string[] guids =
                AssetDatabase.FindAssets(
                    "t:" + typeName
                );

            foreach (string guid in guids)
            {
                string path =
                    AssetDatabase.GUIDToAssetPath(
                        guid
                    );

                UnityEngine.Object asset =
                    AssetDatabase.LoadAssetAtPath(
                        path,
                        type
                    );

                if (asset == null)
                    continue;

                SerializedObject serialized =
                    new SerializedObject(asset);

                InventoryItemData item =
                    ReadInventoryReference(
                        serialized
                    );

                Sprite sprite = null;

                if (item != null)
                {
                    if (!resolvedIcons.TryGetValue(
                            item,
                            out sprite))
                    {
                        sprite = item.Icon;
                    }
                }

                if (sprite == null)
                {
                    string assetName =
                        ReadDisplayName(
                            serialized,
                            asset.name
                        );

                    item =
                        FindInventoryItemByName(
                            assetName
                        );

                    if (item != null)
                        sprite = item.Icon;
                }

                if (sprite == null)
                    continue;

                bool changed =
                    SetFirstObjectReference(
                        serialized,
                        sprite,
                        "icon",
                        "itemIcon",
                        "sprite"
                    );

                if (!changed)
                    continue;

                serialized
                    .ApplyModifiedPropertiesWithoutUndo();

                EditorUtility.SetDirty(asset);
                updated++;
            }
        }

        return updated;
    }

    private static int
        SynchronizeFishingManagers()
    {
        FishingManager[] managers =
            UnityEngine.Object
                .FindObjectsByType<
                    FishingManager
                >(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None
                );

        int updated = 0;

        foreach (FishingManager manager
                 in managers)
        {
            SerializedObject serialized =
                new SerializedObject(manager);

            SerializedProperty fishLoots =
                serialized.FindProperty(
                    "fishLoots"
                );

            if (fishLoots == null ||
                !fishLoots.isArray)
            {
                continue;
            }

            bool changed = false;

            for (int i = 0;
                 i < fishLoots.arraySize;
                 i++)
            {
                SerializedProperty loot =
                    fishLoots
                        .GetArrayElementAtIndex(i);

                SerializedProperty itemProperty =
                    loot.FindPropertyRelative(
                        "inventoryItem"
                    );

                SerializedProperty iconProperty =
                    loot.FindPropertyRelative(
                        "icon"
                    );

                if (itemProperty == null ||
                    iconProperty == null)
                {
                    continue;
                }

                InventoryItemData item =
                    itemProperty
                        .objectReferenceValue
                        as InventoryItemData;

                if (item == null ||
                    item.Icon == null)
                {
                    continue;
                }

                if (iconProperty
                        .objectReferenceValue ==
                    item.Icon)
                {
                    continue;
                }

                iconProperty.objectReferenceValue =
                    item.Icon;

                changed = true;
            }

            if (!changed)
                continue;

            serialized
                .ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(manager);

            if (manager.gameObject.scene.IsValid())
            {
                EditorSceneManager.MarkSceneDirty(
                    manager.gameObject.scene
                );
            }

            updated++;
        }

        return updated;
    }

    private static InventoryItemData
        ReadInventoryReference(
            SerializedObject serialized)
    {
        string[] names =
        {
            "inventoryItem",
            "itemData",
            "inventoryItemData"
        };

        foreach (string name in names)
        {
            SerializedProperty property =
                serialized.FindProperty(name);

            if (property == null ||
                property.propertyType !=
                    SerializedPropertyType
                        .ObjectReference)
            {
                continue;
            }

            InventoryItemData item =
                property.objectReferenceValue
                    as InventoryItemData;

            if (item != null)
                return item;
        }

        return null;
    }

    private static string ReadDisplayName(
        SerializedObject serialized,
        string fallback)
    {
        string[] names =
        {
            "baitName",
            "itemName",
            "equipmentName",
            "displayName"
        };

        foreach (string name in names)
        {
            SerializedProperty property =
                serialized.FindProperty(name);

            if (property == null ||
                property.propertyType !=
                    SerializedPropertyType.String)
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(
                    property.stringValue))
            {
                return property.stringValue;
            }
        }

        return fallback;
    }

    private static InventoryItemData
        FindInventoryItemByName(
            string displayName)
    {
        string target =
            NormalizeCompact(displayName);

        if (string.IsNullOrWhiteSpace(target))
            return null;

        InventoryItemData[] items =
            LoadAllInventoryItems();

        foreach (InventoryItemData item in items)
        {
            if (item == null)
                continue;

            if (NormalizeCompact(
                    item.DisplayName) ==
                target)
            {
                return item;
            }

            if (NormalizeCompact(
                    item.name) ==
                target)
            {
                return item;
            }
        }

        return null;
    }

    private static bool
        SetFirstObjectReference(
            SerializedObject serialized,
            UnityEngine.Object value,
            params string[] propertyNames)
    {
        foreach (string propertyName
                 in propertyNames)
        {
            SerializedProperty property =
                serialized.FindProperty(
                    propertyName
                );

            if (property == null ||
                property.propertyType !=
                    SerializedPropertyType
                        .ObjectReference)
            {
                continue;
            }

            if (property.objectReferenceValue ==
                value)
            {
                return false;
            }

            property.objectReferenceValue = value;
            return true;
        }

        return false;
    }

    private static InventoryItemData[]
        LoadAllInventoryItems()
    {
        string[] guids =
            AssetDatabase.FindAssets(
                "t:InventoryItemData"
            );

        List<InventoryItemData> result =
            new List<InventoryItemData>();

        foreach (string guid in guids)
        {
            string path =
                AssetDatabase.GUIDToAssetPath(
                    guid
                );

            InventoryItemData item =
                AssetDatabase.LoadAssetAtPath<
                    InventoryItemData
                >(path);

            if (item != null)
                result.Add(item);
        }

        return result.ToArray();
    }

    private static Type FindTypeByName(
        string typeName)
    {
        foreach (Assembly assembly
                 in AppDomain.CurrentDomain
                     .GetAssemblies())
        {
            Type direct =
                assembly.GetType(typeName);

            if (direct != null)
                return direct;

            try
            {
                Type found =
                    assembly.GetTypes()
                        .FirstOrDefault(
                            type =>
                                type.Name ==
                                typeName
                        );

                if (found != null)
                    return found;
            }
            catch (
                ReflectionTypeLoadException)
            {
                // Bỏ qua assembly lỗi reflection.
            }
        }

        return null;
    }

    private static string NormalizeId(
        string value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Trim().ToLowerInvariant();
    }

    private static string NormalizeCompact(
        string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        string decomposed =
            value.Trim()
                .Normalize(
                    NormalizationForm.FormD
                );

        StringBuilder builder =
            new StringBuilder();

        foreach (char character in decomposed)
        {
            UnicodeCategory category =
                CharUnicodeInfo.GetUnicodeCategory(
                    character
                );

            if (category ==
                UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetterOrDigit(character))
            {
                builder.Append(
                    char.ToLowerInvariant(character)
                );
            }
        }

        return builder
            .ToString()
            .Normalize(
                NormalizationForm.FormC
            );
    }

    private static IEnumerable<string> Tokenize(
        string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            yield break;

        string decomposed =
            value.Normalize(
                NormalizationForm.FormD
            );

        StringBuilder current =
            new StringBuilder();

        foreach (char character in decomposed)
        {
            UnicodeCategory category =
                CharUnicodeInfo.GetUnicodeCategory(
                    character
                );

            if (category ==
                UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetterOrDigit(character))
            {
                current.Append(
                    char.ToLowerInvariant(character)
                );
            }
            else if (current.Length > 0)
            {
                string token =
                    current.ToString();

                current.Clear();

                if (!IgnoredTokens.Contains(token) &&
                    token.Length > 0)
                {
                    yield return token;
                }
            }
        }

        if (current.Length > 0)
        {
            string token =
                current.ToString();

            if (!IgnoredTokens.Contains(token))
                yield return token;
        }
    }
}
#endif
