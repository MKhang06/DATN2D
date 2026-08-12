#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class RebuildAllFishingDatabaseEditor
{
    private const string InventoryRoot =
        "Assets/Bao/Data/InventoryItems";

    private const string FishInventoryFolder =
        InventoryRoot + "/Fishes";

    private const string BaitInventoryFolder =
        InventoryRoot + "/Baits";

    private const string EquipmentInventoryFolder =
        InventoryRoot + "/Equipment";

    private const string FishingDataRoot =
        "Assets/Bao/Data/Fishing";

    private const string BaitDataFolder =
        FishingDataRoot + "/Baits";

    private const string EquipmentDataFolder =
        FishingDataRoot + "/Equipment";

    private const string EquipmentShopFolder =
        FishingDataRoot + "/EquipmentShopItems";

    private sealed class FishDefinition
    {
        public readonly string fileName;
        public readonly string itemId;
        public readonly string[] legacyIds;
        public readonly string displayName;
        public readonly string[] legacyNames;
        public readonly float minDepth;
        public readonly float maxDepth;
        public readonly float lootWeight;
        public readonly float fishPower;
        public readonly string preferredBait;
        public readonly float preferredBaitBonus;
        public readonly float minWeightKg;
        public readonly float maxWeightKg;
        public readonly float expReward;
        public readonly int baseSellPrice;

        public FishDefinition(
            string fileName,
            string itemId,
            string[] legacyIds,
            string displayName,
            string[] legacyNames,
            float minDepth,
            float maxDepth,
            float lootWeight,
            float fishPower,
            string preferredBait,
            float preferredBaitBonus,
            float minWeightKg,
            float maxWeightKg,
            float expReward,
            int baseSellPrice)
        {
            this.fileName = fileName;
            this.itemId = itemId;
            this.legacyIds = legacyIds ?? Array.Empty<string>();
            this.displayName = displayName;
            this.legacyNames = legacyNames ?? Array.Empty<string>();
            this.minDepth = minDepth;
            this.maxDepth = maxDepth;
            this.lootWeight = lootWeight;
            this.fishPower = fishPower;
            this.preferredBait = preferredBait;
            this.preferredBaitBonus = preferredBaitBonus;
            this.minWeightKg = minWeightKg;
            this.maxWeightKg = maxWeightKg;
            this.expReward = expReward;
            this.baseSellPrice = baseSellPrice;
        }
    }

    private sealed class BaitDefinition
    {
        public readonly string fileName;
        public readonly string itemId;
        public readonly string displayName;
        public readonly int buyPrice;
        public readonly int sellPrice;
        public readonly int requiredLevel;
        public readonly int amountPerPurchase;
        public readonly float fishChanceBonus;
        public readonly float rareFishChanceBonus;

        public BaitDefinition(
            string fileName,
            string itemId,
            string displayName,
            int buyPrice,
            int requiredLevel,
            int amountPerPurchase = 1,
            float fishChanceBonus = 5f,
            float rareFishChanceBonus = 0f)
        {
            this.fileName = fileName;
            this.itemId = itemId;
            this.displayName = displayName;
            this.buyPrice = Mathf.Max(0, buyPrice);
            this.sellPrice = Mathf.Max(0, buyPrice / 2);
            this.requiredLevel = Mathf.Max(1, requiredLevel);
            this.amountPerPurchase = Mathf.Max(1, amountPerPurchase);
            this.fishChanceBonus = Mathf.Max(0f, fishChanceBonus);
            this.rareFishChanceBonus =
                Mathf.Max(0f, rareFishChanceBonus);
        }
    }

    private sealed class EquipmentDefinition
    {
        public readonly string fileName;
        public readonly string itemId;
        public readonly string displayName;
        public readonly string category;
        public readonly int buyPrice;
        public readonly int sellPrice;
        public readonly int requiredLevel;

        public EquipmentDefinition(
            string fileName,
            string itemId,
            string displayName,
            string category,
            int buyPrice,
            int requiredLevel)
        {
            this.fileName = fileName;
            this.itemId = itemId;
            this.displayName = displayName;
            this.category = category;
            this.buyPrice = Mathf.Max(0, buyPrice);
            this.sellPrice = Mathf.Max(0, buyPrice / 2);
            this.requiredLevel = Mathf.Max(1, requiredLevel);
        }
    }

    private static readonly FishDefinition[] Fishes =
    {
        new FishDefinition(
            "SmallSalmon",
            "fish_small_salmon",
            Array.Empty<string>(),
            "Cá Hồi Nhỏ",
            Array.Empty<string>(),
            0f,
            4f,
            22f,
            0.85f,
            "Giun Đất",
            10f,
            0.3f,
            1.2f,
            3f,
            80
        ),

        new FishDefinition(
            "SmallPinkFish",
            "fish_small_pink",
            new[]
            {
                "fish_small_hong"
            },
            "Cá Hồng Nhỏ",
            new[]
            {
                "Cá hồng nhỏ",
                "Cá Hổng Nhỏ",
                "Cá hổng nhỏ"
            },
            0f,
            3.5f,
            26f,
            0.75f,
            "Bột Nhão",
            10f,
            0.2f,
            0.9f,
            2f,
            60
        ),

        new FishDefinition(
            "SmallMackerel",
            "fish_small_mackerel",
            Array.Empty<string>(),
            "Cá Thu Nhỏ",
            Array.Empty<string>(),
            2f,
            7f,
            14f,
            1.2f,
            "Cá Mồi Nhỏ",
            12f,
            0.5f,
            2f,
            5f,
            150
        ),

        new FishDefinition(
            "SeaBass",
            "fish_sea_bass",
            Array.Empty<string>(),
            "Cá Vược",
            Array.Empty<string>(),
            3f,
            10f,
            9f,
            1.6f,
            "Mồi Giả",
            15f,
            1f,
            5f,
            8f,
            300
        )
    };

    private static readonly BaitDefinition[] Baits =
    {
        new BaitDefinition(
            "Bread",
            "bait_bread",
            "Bánh Mì",
            10,
            1
        ),

        new BaitDefinition(
            "Corn",
            "bait_corn",
            "Bắp Ngô",
            15,
            1
        ),

        new BaitDefinition(
            "Dough",
            "bait_dough",
            "Bột Nhão",
            20,
            1
        ),

        new BaitDefinition(
            "Crab",
            "bait_crab",
            "Cua",
            100,
            6,
            1,
            9f,
            2f
        ),

        new BaitDefinition(
            "SmallBaitFish",
            "bait_small_fish",
            "Cá Mồi Nhỏ",
            80,
            5,
            1,
            8f,
            2f
        ),

        new BaitDefinition(
            "TechnologyFeed",
            "bait_technology_feed",
            "Cám Công Nghệ",
            200,
            10,
            1,
            12f,
            5f
        ),

        new BaitDefinition(
            "Insect",
            "bait_insect",
            "Côn Trùng",
            25,
            2
        ),

        new BaitDefinition(
            "Earthworm",
            "bait_earthworm",
            "Giun Đất",
            30,
            2,
            1,
            7f,
            1f
        ),

        new BaitDefinition(
            "RedWorm",
            "bait_red_worm",
            "Giun Đỏ",
            40,
            3,
            1,
            7f,
            1.5f
        ),

        new BaitDefinition(
            "ArtificialLure",
            "bait_artificial_lure",
            "Mồi Giả",
            300,
            12,
            1,
            15f,
            8f
        ),

        new BaitDefinition(
            "Squid",
            "bait_squid",
            "Mực",
            120,
            7,
            1,
            10f,
            3f
        ),

        new BaitDefinition(
            "Mealworm",
            "bait_mealworm",
            "Sâu Bột",
            50,
            4
        ),

        new BaitDefinition(
            "Waxworm",
            "bait_waxworm",
            "Sâu Sáp",
            65,
            5
        ),

        new BaitDefinition(
            "FishMeat",
            "bait_fish_meat",
            "Thịt Cá",
            110,
            6,
            1,
            9f,
            3f
        ),

        new BaitDefinition(
            "TunaMeat",
            "bait_tuna_meat",
            "Thịt Cá Ngừ",
            160,
            8,
            1,
            11f,
            4f
        ),

        new BaitDefinition(
            "FishScraps",
            "bait_fish_scraps",
            "Thịt Cá Vụn",
            70,
            4
        ),

        new BaitDefinition(
            "Maggot",
            "bait_maggot",
            "Ấu Trùng Ruồi",
            45,
            3
        )
    };

    private static readonly EquipmentDefinition[] Equipment =
    {
        new EquipmentDefinition(
            "CallistoXSR",
            "equipment_reel_callisto_xsr",
            "Callisto XSR",
            "Reel",
            2500,
            7
        ),

        new EquipmentDefinition(
            "BraidedLineNoodle",
            "equipment_line_braided_noodle",
            "Dây Dù Noodle",
            "Line",
            700,
            4
        ),

        new EquipmentDefinition(
            "BraidedLineLightning",
            "equipment_line_braided_lightning",
            "Dây Dù Tia Chớp",
            "Line",
            1200,
            6
        ),

        new EquipmentDefinition(
            "BraidedLineKing",
            "equipment_line_braided_king",
            "Dây Dù Vua",
            "Line",
            2000,
            8
        ),

        new EquipmentDefinition(
            "MobeyMonoLine",
            "equipment_line_mobey_mono",
            "Dây Mobey Đơn",
            "Line",
            400,
            3
        ),

        new EquipmentDefinition(
            "MediumMonoLine",
            "equipment_line_mono_medium",
            "Dây Đơn Cỡ Vừa",
            "Line",
            250,
            2
        ),

        new EquipmentDefinition(
            "CheapMonoLine",
            "equipment_line_mono_cheap",
            "Dây Đơn Rẻ Tiền",
            "Line",
            100,
            1
        ),

        new EquipmentDefinition(
            "FeatherLight",
            "equipment_rod_feather_light",
            "EQ_FeatherLight",
            "Rod",
            1800,
            5
        ),

        new EquipmentDefinition(
            "Hook6",
            "equipment_hook_6",
            "Móc Câu #6",
            "Hook",
            150,
            1
        ),

        new EquipmentDefinition(
            "HeavyHook",
            "equipment_hook_heavy",
            "Móc Câu Hạng Nặng",
            "Hook",
            900,
            6
        ),

        new EquipmentDefinition(
            "Hook1",
            "equipment_hook_1",
            "Móc Câu #1",
            "Hook",
            400,
            3
        )
    };

    [MenuItem(
        "Tools/Fishing Database/Rebuild Everything"
    )]
    public static void RebuildEverything()
    {
        EnsureAllFolders();

        List<InventoryItemData> createdItems =
            new List<InventoryItemData>();

        Dictionary<string, InventoryItemData>
            fishItems =
                BuildFishInventoryItems(
                    createdItems
                );

        List<ScriptableObject> baitAssets =
            BuildBaitData(
                createdItems
            );

        List<ScriptableObject> equipmentDataAssets;
        List<ScriptableObject> equipmentShopAssets;

        BuildEquipmentData(
            createdItems,
            out equipmentDataAssets,
            out equipmentShopAssets
        );

        ItemDatabase database =
            FindOrCreateItemDatabase();

        List<InventoryItemData> allItems =
            FindAllInventoryItems();

        AssignDatabaseItems(
            database,
            allItems
        );

        int managersUpdated =
            RebuildFishingManagers(
                fishItems
            );

        int baitShopsUpdated =
            AssignBaitShopArrays(
                baitAssets
            );

        int equipmentShopsUpdated =
            AssignEquipmentShopArrays(
                equipmentShopAssets
            );

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Fishing Database",
            "Đã xây lại toàn bộ dữ liệu câu cá.\n\n" +
            "Cá: " +
            Fishes.Length +
            "\nMồi câu: " +
            Baits.Length +
            "\nTrang bị: " +
            Equipment.Length +
            "\nTổng InventoryItemData trong database: " +
            allItems.Count +
            "\nFishingManager cập nhật: " +
            managersUpdated +
            "\nBait Shop cập nhật: " +
            baitShopsUpdated +
            "\nEquipment Shop cập nhật: " +
            equipmentShopsUpdated +
            "\n\nHãy gắn icon rồi chạy lại tool.",
            "OK"
        );

        Debug.Log(
            "[Fishing Database] Rebuild Everything hoàn tất."
        );
    }

    [MenuItem(
        "Tools/Fishing Database/Validate Everything"
    )]
    public static void ValidateEverything()
    {
        int errors = 0;

        errors += ValidateInventoryItems();
        errors += ValidateSpecializedAssets(
            "FishingBaitData",
            "inventoryItem"
        );

        errors += ValidateSpecializedAssets(
            "FishingEquipmentShopItemData",
            "inventoryItem",
            "equipmentData"
        );

        errors += ValidateFishingManagers();
        errors += ValidateShopArrays(
            "FishingBaitShopUI",
            "availableBaits"
        );

        errors += ValidateShopArrays(
            "FishingEquipmentShopUI",
            "availableEquipment"
        );

        ItemDatabase database =
            UnityEngine.Object
                .FindFirstObjectByType<
                    ItemDatabase
                >();

        if (database == null)
        {
            Debug.LogError(
                "Scene chưa có ItemDatabase."
            );

            errors++;
        }
        else
        {
            database.RebuildLookup();
        }

        EditorUtility.DisplayDialog(
            "Validate Fishing Database",
            errors == 0
                ? "Toàn bộ database hợp lệ."
                : "Tìm thấy " +
                  errors +
                  " lỗi. Xem Console.",
            "OK"
        );
    }

    private static void EnsureAllFolders()
    {
        EnsureFolder(FishInventoryFolder);
        EnsureFolder(BaitInventoryFolder);
        EnsureFolder(EquipmentInventoryFolder);
        EnsureFolder(BaitDataFolder);
        EnsureFolder(EquipmentDataFolder);
        EnsureFolder(EquipmentShopFolder);
    }

    private static Dictionary<
        string,
        InventoryItemData
    > BuildFishInventoryItems(
        List<InventoryItemData> createdItems)
    {
        Dictionary<
            string,
            InventoryItemData
        > result =
            new Dictionary<
                string,
                InventoryItemData
            >();

        foreach (FishDefinition definition
                 in Fishes)
        {
            string path =
                FishInventoryFolder +
                "/Item_Fish_" +
                definition.fileName +
                ".asset";

            InventoryItemData item =
                FindOrCreateInventoryItem(
                    path,
                    definition.itemId,
                    definition.displayName,
                    definition.legacyIds,
                    definition.legacyNames
                );

            ConfigureInventoryItem(
                item,
                definition.itemId,
                definition.displayName,
                InventoryItemType.Fish,
                InventoryStackMode.Separate,
                1,
                false,
                0,
                definition.baseSellPrice
            );

            if (item != null)
            {
                result[definition.itemId] = item;
                createdItems.Add(item);
            }
        }

        return result;
    }

    private static List<ScriptableObject>
        BuildBaitData(
            List<InventoryItemData> createdItems)
    {
        Type baitType =
            FindTypeByName("FishingBaitData");

        List<ScriptableObject> baitAssets =
            new List<ScriptableObject>();

        if (baitType == null)
        {
            Debug.LogError(
                "Không tìm thấy class FishingBaitData. " +
                "InventoryItemData của mồi vẫn được tạo."
            );
        }

        foreach (BaitDefinition definition
                 in Baits)
        {
            string itemPath =
                BaitInventoryFolder +
                "/Item_Bait_" +
                definition.fileName +
                ".asset";

            InventoryItemData item =
                FindOrCreateInventoryItem(
                    itemPath,
                    definition.itemId,
                    definition.displayName
                );

            ConfigureInventoryItem(
                item,
                definition.itemId,
                definition.displayName,
                InventoryItemType.Bait,
                InventoryStackMode.Stackable,
                999,
                false,
                definition.buyPrice,
                definition.sellPrice
            );

            if (item != null)
                createdItems.Add(item);

            if (baitType == null)
                continue;

            string baitPath =
                BaitDataFolder +
                "/Bait_" +
                definition.fileName +
                ".asset";

            ScriptableObject baitAsset =
                FindOrCreateScriptableAsset(
                    baitPath,
                    baitType
                );

            if (baitAsset == null)
                continue;

            SerializedObject serializedBait =
                new SerializedObject(baitAsset);

            SetFirstString(
                serializedBait,
                definition.displayName,
                "baitName",
                "itemName",
                "displayName"
            );

            SetFirstInt(
                serializedBait,
                definition.requiredLevel,
                "requiredLevel",
                "requiredPlayerLevel"
            );

            SetFirstInt(
                serializedBait,
                definition.buyPrice,
                "price",
                "buyPrice",
                "shopPrice"
            );

            SetFirstInt(
                serializedBait,
                definition.amountPerPurchase,
                "amountPerPurchase",
                "purchaseAmount"
            );

            SetFirstFloat(
                serializedBait,
                definition.fishChanceBonus,
                "fishChanceBonus"
            );

            SetFirstFloat(
                serializedBait,
                definition.rareFishChanceBonus,
                "rareFishChanceBonus"
            );

            SetFirstBool(
                serializedBait,
                true,
                "consumeOnCast"
            );

            SetFirstObjectReference(
                serializedBait,
                item,
                "inventoryItem",
                "itemData",
                "inventoryItemData"
            );

            serializedBait
                .ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(baitAsset);

            SynchronizeIcons(
                item,
                baitAsset
            );

            baitAssets.Add(baitAsset);
        }

        return baitAssets;
    }

    private static void BuildEquipmentData(
        List<InventoryItemData> createdItems,
        out List<ScriptableObject>
            equipmentDataAssets,
        out List<ScriptableObject>
            equipmentShopAssets)
    {
        Type equipmentDataType =
            FindTypeByName(
                "FishingEquipmentData"
            );

        Type equipmentShopItemType =
            FindTypeByName(
                "FishingEquipmentShopItemData"
            );

        equipmentDataAssets =
            new List<ScriptableObject>();

        equipmentShopAssets =
            new List<ScriptableObject>();

        if (equipmentDataType == null)
        {
            Debug.LogError(
                "Không tìm thấy class FishingEquipmentData. " +
                "InventoryItemData của trang bị vẫn được tạo."
            );
        }

        if (equipmentShopItemType == null)
        {
            Debug.LogError(
                "Không tìm thấy class " +
                "FishingEquipmentShopItemData. " +
                "Shop item chưa thể tạo."
            );
        }

        foreach (
            EquipmentDefinition definition
            in Equipment)
        {
            string inventoryCategoryFolder =
                EquipmentInventoryFolder +
                "/" +
                definition.category;

            string equipmentCategoryFolder =
                EquipmentDataFolder +
                "/" +
                definition.category;

            EnsureFolder(
                inventoryCategoryFolder
            );

            EnsureFolder(
                equipmentCategoryFolder
            );

            string itemPath =
                inventoryCategoryFolder +
                "/Item_Equipment_" +
                definition.fileName +
                ".asset";

            InventoryItemData item =
                FindOrCreateInventoryItem(
                    itemPath,
                    definition.itemId,
                    definition.displayName
                );

            ConfigureInventoryItem(
                item,
                definition.itemId,
                definition.displayName,
                InventoryItemType.Equipment,
                InventoryStackMode.Separate,
                1,
                true,
                definition.buyPrice,
                definition.sellPrice
            );

            if (item != null)
                createdItems.Add(item);

            ScriptableObject equipmentData = null;

            if (equipmentDataType != null)
            {
                string dataPath =
                    equipmentCategoryFolder +
                    "/Equipment_" +
                    definition.fileName +
                    ".asset";

                equipmentData =
                    FindOrCreateScriptableAsset(
                        dataPath,
                        equipmentDataType
                    );

                ConfigureEquipmentData(
                    equipmentData,
                    definition,
                    item
                );

                if (equipmentData != null)
                {
                    SynchronizeIcons(
                        item,
                        equipmentData
                    );

                    equipmentDataAssets.Add(
                        equipmentData
                    );
                }
            }

            if (equipmentShopItemType == null)
                continue;

            string shopPath =
                EquipmentShopFolder +
                "/ShopItem_" +
                definition.fileName +
                ".asset";

            ScriptableObject shopItem =
                FindOrCreateScriptableAsset(
                    shopPath,
                    equipmentShopItemType
                );

            if (shopItem == null)
                continue;

            SerializedObject serializedShop =
                new SerializedObject(shopItem);

            SetFirstObjectReference(
                serializedShop,
                equipmentData,
                "equipmentData",
                "data"
            );

            SetFirstObjectReference(
                serializedShop,
                item,
                "inventoryItem",
                "itemData",
                "inventoryItemData"
            );

            SetFirstInt(
                serializedShop,
                definition.requiredLevel,
                "requiredPlayerLevel",
                "requiredLevel",
                "levelRequired"
            );

            SetFirstInt(
                serializedShop,
                definition.buyPrice,
                "price",
                "buyPrice",
                "shopPrice"
            );

            SetFirstString(
                serializedShop,
                definition.displayName,
                "itemName",
                "displayName",
                "equipmentName"
            );

            SetCategoryIfAvailable(
                serializedShop,
                definition.category
            );

            serializedShop
                .ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(shopItem);

            SynchronizeIcons(
                item,
                shopItem
            );

            equipmentShopAssets.Add(
                shopItem
            );
        }
    }

    private static void ConfigureEquipmentData(
        ScriptableObject equipmentData,
        EquipmentDefinition definition,
        InventoryItemData item)
    {
        if (equipmentData == null)
            return;

        SerializedObject serializedEquipment =
            new SerializedObject(equipmentData);

        SetFirstString(
            serializedEquipment,
            definition.displayName,
            "itemName",
            "equipmentName",
            "displayName"
        );

        SetFirstInt(
            serializedEquipment,
            definition.buyPrice,
            "price",
            "buyPrice",
            "shopPrice"
        );

        SetFirstInt(
            serializedEquipment,
            definition.requiredLevel,
            "requiredLevel",
            "requiredPlayerLevel",
            "levelRequired"
        );

        SetFirstObjectReference(
            serializedEquipment,
            item,
            "inventoryItem",
            "itemData",
            "inventoryItemData"
        );

        SetCategoryIfAvailable(
            serializedEquipment,
            definition.category
        );

        serializedEquipment
            .ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(equipmentData);
    }

    private static InventoryItemData
        FindOrCreateInventoryItem(
            string preferredPath,
            string canonicalId,
            string canonicalName,
            string[] legacyIds = null,
            string[] legacyNames = null)
    {
        InventoryItemData item =
            FindInventoryItem(
                canonicalId,
                canonicalName,
                legacyIds,
                legacyNames
            );

        if (item != null)
            return item;

        item =
            AssetDatabase.LoadAssetAtPath<
                InventoryItemData
            >(preferredPath);

        if (item != null)
            return item;

        item =
            ScriptableObject.CreateInstance<
                InventoryItemData
            >();

        AssetDatabase.CreateAsset(
            item,
            preferredPath
        );

        return item;
    }

    private static InventoryItemData
        FindInventoryItem(
            string canonicalId,
            string canonicalName,
            string[] legacyIds,
            string[] legacyNames)
    {
        HashSet<string> acceptedIds =
            new HashSet<string>
            {
                NormalizeId(canonicalId)
            };

        if (legacyIds != null)
        {
            foreach (string legacyId
                     in legacyIds)
            {
                acceptedIds.Add(
                    NormalizeId(legacyId)
                );
            }
        }

        HashSet<string> acceptedNames =
            new HashSet<string>
            {
                NormalizeName(canonicalName)
            };

        if (legacyNames != null)
        {
            foreach (string legacyName
                     in legacyNames)
            {
                acceptedNames.Add(
                    NormalizeName(legacyName)
                );
            }
        }

        string[] guids =
            AssetDatabase.FindAssets(
                "t:InventoryItemData"
            );

        foreach (string guid in guids)
        {
            string path =
                AssetDatabase.GUIDToAssetPath(
                    guid
                );

            InventoryItemData candidate =
                AssetDatabase.LoadAssetAtPath<
                    InventoryItemData
                >(path);

            if (candidate == null)
                continue;

            if (acceptedIds.Contains(
                    NormalizeId(
                        candidate.ItemId)))
            {
                return candidate;
            }

            if (acceptedNames.Contains(
                    NormalizeName(
                        candidate.DisplayName)))
            {
                return candidate;
            }
        }

        return null;
    }

    private static void ConfigureInventoryItem(
        InventoryItemData item,
        string itemId,
        string displayName,
        InventoryItemType itemType,
        InventoryStackMode stackMode,
        int maxStack,
        bool uniqueOwnership,
        int buyPrice,
        int sellPrice)
    {
        if (item == null)
            return;

        SerializedObject serializedItem =
            new SerializedObject(item);

        SetString(
            serializedItem,
            "itemId",
            itemId
        );

        SetString(
            serializedItem,
            "displayName",
            displayName
        );

        SetEnumIndex(
            serializedItem,
            "itemType",
            (int)itemType
        );

        SetEnumIndex(
            serializedItem,
            "stackMode",
            (int)stackMode
        );

        SetInt(
            serializedItem,
            "maxStack",
            maxStack
        );

        SetBool(
            serializedItem,
            "uniqueOwnership",
            uniqueOwnership
        );

        SetInt(
            serializedItem,
            "buyPrice",
            buyPrice
        );

        SetInt(
            serializedItem,
            "sellPrice",
            sellPrice
        );

        serializedItem
            .ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(item);
    }

    private static ScriptableObject
        FindOrCreateScriptableAsset(
            string path,
            Type type)
    {
        if (type == null)
            return null;

        ScriptableObject asset =
            AssetDatabase.LoadAssetAtPath(
                path,
                type
            ) as ScriptableObject;

        if (asset != null)
            return asset;

        asset =
            ScriptableObject.CreateInstance(type);

        if (asset == null)
        {
            Debug.LogError(
                "Không tạo được asset type " +
                type.FullName
            );

            return null;
        }

        AssetDatabase.CreateAsset(
            asset,
            path
        );

        return asset;
    }

    private static List<InventoryItemData>
        FindAllInventoryItems()
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

            if (item != null &&
                !result.Contains(item))
            {
                result.Add(item);
            }
        }

        return result
            .OrderBy(
                item => item.ItemId
            )
            .ToList();
    }

    private static ItemDatabase
        FindOrCreateItemDatabase()
    {
        ItemDatabase database =
            UnityEngine.Object
                .FindFirstObjectByType<
                    ItemDatabase
                >();

        if (database != null)
            return database;

        GameObject systemsRoot =
            GameObject.Find("GameSystems");

        if (systemsRoot == null)
        {
            systemsRoot =
                new GameObject(
                    "GameSystems"
                );

            Undo.RegisterCreatedObjectUndo(
                systemsRoot,
                "Create GameSystems"
            );
        }

        GameObject databaseObject =
            new GameObject(
                "ItemDatabase"
            );

        Undo.RegisterCreatedObjectUndo(
            databaseObject,
            "Create ItemDatabase"
        );

        databaseObject.transform.SetParent(
            systemsRoot.transform
        );

        database =
            databaseObject.AddComponent<
                ItemDatabase
            >();

        EditorSceneManager.MarkSceneDirty(
            databaseObject.scene
        );

        return database;
    }

    private static void AssignDatabaseItems(
        ItemDatabase database,
        List<InventoryItemData> items)
    {
        if (database == null)
            return;

        SerializedObject serializedDatabase =
            new SerializedObject(database);

        SerializedProperty list =
            serializedDatabase.FindProperty(
                "items"
            );

        if (list == null ||
            !list.isArray)
        {
            Debug.LogError(
                "ItemDatabase không có field items.",
                database
            );

            return;
        }

        list.arraySize = items.Count;

        for (int i = 0;
             i < items.Count;
             i++)
        {
            list.GetArrayElementAtIndex(i)
                .objectReferenceValue =
                    items[i];
        }

        serializedDatabase
            .ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(database);
        database.RebuildLookup();

        if (database.gameObject.scene.IsValid())
        {
            EditorSceneManager.MarkSceneDirty(
                database.gameObject.scene
            );
        }
    }

    private static int RebuildFishingManagers(
        Dictionary<
            string,
            InventoryItemData
        > fishItems)
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
            SerializedObject serializedManager =
                new SerializedObject(manager);

            SerializedProperty fishLoots =
                serializedManager.FindProperty(
                    "fishLoots"
                );

            if (fishLoots == null ||
                !fishLoots.isArray)
            {
                Debug.LogError(
                    "FishingManager không có " +
                    "field fishLoots.",
                    manager
                );

                continue;
            }

            Dictionary<string, Sprite>
                oldIcons =
                    ReadExistingFishIcons(
                        fishLoots
                    );

            fishLoots.arraySize =
                Fishes.Length;

            for (int i = 0;
                 i < Fishes.Length;
                 i++)
            {
                FishDefinition definition =
                    Fishes[i];

                SerializedProperty loot =
                    fishLoots
                        .GetArrayElementAtIndex(i);

                fishItems.TryGetValue(
                    definition.itemId,
                    out InventoryItemData item
                );

                SetRelativeObject(
                    loot,
                    item,
                    "inventoryItem"
                );

                SetRelativeString(
                    loot,
                    definition.displayName,
                    "itemName"
                );

                Sprite icon =
                    item != null
                        ? item.Icon
                        : null;

                if (icon == null)
                {
                    oldIcons.TryGetValue(
                        definition.itemId,
                        out icon
                    );
                }

                SetRelativeObject(
                    loot,
                    icon,
                    "icon"
                );

                SetRelativeBool(
                    loot,
                    true,
                    "isFish"
                );

                SetRelativeFloat(
                    loot,
                    definition.minDepth,
                    "minDepth"
                );

                SetRelativeFloat(
                    loot,
                    definition.maxDepth,
                    "maxDepth"
                );

                SetRelativeFloat(
                    loot,
                    definition.lootWeight,
                    "weight"
                );

                SetRelativeFloat(
                    loot,
                    definition.fishPower,
                    "fishPower"
                );

                SetRelativeString(
                    loot,
                    definition.preferredBait,
                    "preferredBait"
                );

                SetRelativeFloat(
                    loot,
                    definition.preferredBaitBonus,
                    "preferredBaitBonus"
                );

                SetRelativeFloat(
                    loot,
                    definition.minWeightKg,
                    "minWeightKg"
                );

                SetRelativeFloat(
                    loot,
                    definition.maxWeightKg,
                    "maxWeightKg"
                );

                SetRelativeFloat(
                    loot,
                    definition.expReward,
                    "expReward"
                );

                SetRelativeInt(
                    loot,
                    definition.baseSellPrice,
                    "baseSellPrice"
                );
            }

            serializedManager
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

    private static Dictionary<string, Sprite>
        ReadExistingFishIcons(
            SerializedProperty fishLoots)
    {
        Dictionary<string, Sprite> result =
            new Dictionary<string, Sprite>();

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

            SerializedProperty nameProperty =
                loot.FindPropertyRelative(
                    "itemName"
                );

            SerializedProperty iconProperty =
                loot.FindPropertyRelative(
                    "icon"
                );

            InventoryItemData item =
                itemProperty != null
                    ? itemProperty
                        .objectReferenceValue
                        as InventoryItemData
                    : null;

            Sprite icon =
                iconProperty != null
                    ? iconProperty
                        .objectReferenceValue
                        as Sprite
                    : null;

            if (icon == null)
                continue;

            if (item != null &&
                !string.IsNullOrWhiteSpace(
                    item.ItemId))
            {
                result[
                    NormalizeId(item.ItemId)
                ] = icon;

                continue;
            }

            if (nameProperty == null)
                continue;

            string normalizedName =
                NormalizeName(
                    nameProperty.stringValue
                );

            foreach (FishDefinition definition
                     in Fishes)
            {
                if (NormalizeName(
                        definition.displayName) ==
                    normalizedName)
                {
                    result[
                        NormalizeId(
                            definition.itemId)
                    ] = icon;

                    break;
                }
            }
        }

        return result;
    }

    private static int AssignBaitShopArrays(
        List<ScriptableObject> baitAssets)
    {
        Type shopType =
            FindTypeByName(
                "FishingBaitShopUI"
            );

        if (shopType == null)
            return 0;

        return AssignArrayToSceneComponents(
            shopType,
            "availableBaits",
            baitAssets.Cast<
                UnityEngine.Object
            >().ToList()
        );
    }

    private static int
        AssignEquipmentShopArrays(
            List<ScriptableObject>
                equipmentShopAssets)
    {
        Type shopType =
            FindTypeByName(
                "FishingEquipmentShopUI"
            );

        if (shopType == null)
            return 0;

        return AssignArrayToSceneComponents(
            shopType,
            "availableEquipment",
            equipmentShopAssets.Cast<
                UnityEngine.Object
            >().ToList()
        );
    }

    private static int
        AssignArrayToSceneComponents(
            Type componentType,
            string arrayFieldName,
            List<UnityEngine.Object> assets)
    {
        MonoBehaviour[] behaviours =
            Resources.FindObjectsOfTypeAll<
                MonoBehaviour
            >();

        int updated = 0;

        foreach (MonoBehaviour behaviour
                 in behaviours)
        {
            if (behaviour == null ||
                behaviour.GetType() !=
                componentType)
            {
                continue;
            }

            if (!behaviour.gameObject.scene.IsValid())
                continue;

            SerializedObject serialized =
                new SerializedObject(behaviour);

            SerializedProperty array =
                serialized.FindProperty(
                    arrayFieldName
                );

            if (array == null ||
                !array.isArray)
            {
                Debug.LogError(
                    componentType.Name +
                    " không có field " +
                    arrayFieldName +
                    ".",
                    behaviour
                );

                continue;
            }

            array.arraySize = assets.Count;

            for (int i = 0;
                 i < assets.Count;
                 i++)
            {
                array.GetArrayElementAtIndex(i)
                    .objectReferenceValue =
                        assets[i];
            }

            serialized
                .ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(behaviour);
            EditorSceneManager.MarkSceneDirty(
                behaviour.gameObject.scene
            );

            updated++;
        }

        return updated;
    }

    private static int ValidateInventoryItems()
    {
        List<InventoryItemData> items =
            FindAllInventoryItems();

        Dictionary<string, InventoryItemData>
            ids =
                new Dictionary<
                    string,
                    InventoryItemData
                >();

        Dictionary<string, InventoryItemData>
            names =
                new Dictionary<
                    string,
                    InventoryItemData
                >();

        int errors = 0;

        foreach (InventoryItemData item in items)
        {
            if (item == null)
                continue;

            if (!item.IsValid)
            {
                Debug.LogError(
                    "InventoryItemData không hợp lệ: " +
                    AssetDatabase.GetAssetPath(item),
                    item
                );

                errors++;
                continue;
            }

            string id =
                NormalizeId(item.ItemId);

            if (ids.TryGetValue(
                    id,
                    out InventoryItemData duplicateId))
            {
                Debug.LogError(
                    "Trùng Item ID \"" +
                    id +
                    "\": " +
                    duplicateId.name +
                    " và " +
                    item.name,
                    item
                );

                errors++;
            }
            else
            {
                ids.Add(id, item);
            }

            string name =
                NormalizeName(
                    item.DisplayName
                );

            if (names.TryGetValue(
                    name,
                    out InventoryItemData duplicateName))
            {
                Debug.LogError(
                    "Trùng tên \"" +
                    item.DisplayName +
                    "\": " +
                    duplicateName.name +
                    " và " +
                    item.name,
                    item
                );

                errors++;
            }
            else
            {
                names.Add(name, item);
            }
        }

        return errors;
    }

    private static int
        ValidateSpecializedAssets(
            string typeName,
            params string[] requiredReferences)
    {
        Type type = FindTypeByName(typeName);

        if (type == null)
        {
            Debug.LogError(
                "Không tìm thấy class " +
                typeName +
                "."
            );

            return 1;
        }

        string[] guids =
            AssetDatabase.FindAssets(
                "t:" + typeName
            );

        int errors = 0;

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

            foreach (string field
                     in requiredReferences)
            {
                SerializedProperty property =
                    serialized.FindProperty(field);

                if (property == null)
                    continue;

                if (property.propertyType ==
                        SerializedPropertyType
                            .ObjectReference &&
                    property.objectReferenceValue ==
                        null)
                {
                    Debug.LogError(
                        typeName +
                        " thiếu " +
                        field +
                        ": " +
                        path,
                        asset
                    );

                    errors++;
                }
            }
        }

        return errors;
    }

    private static int
        ValidateFishingManagers()
    {
        FishingManager[] managers =
            UnityEngine.Object
                .FindObjectsByType<
                    FishingManager
                >(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None
                );

        int errors = 0;

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
                Debug.LogError(
                    "FishingManager thiếu fishLoots.",
                    manager
                );

                errors++;
                continue;
            }

            for (int i = 0;
                 i < fishLoots.arraySize;
                 i++)
            {
                SerializedProperty loot =
                    fishLoots
                        .GetArrayElementAtIndex(i);

                SerializedProperty item =
                    loot.FindPropertyRelative(
                        "inventoryItem"
                    );

                if (item == null ||
                    item.objectReferenceValue ==
                        null)
                {
                    Debug.LogError(
                        "Fish Loot " +
                        i +
                        " chưa gắn InventoryItemData.",
                        manager
                    );

                    errors++;
                }
            }
        }

        return errors;
    }

    private static int ValidateShopArrays(
        string componentTypeName,
        string arrayFieldName)
    {
        Type componentType =
            FindTypeByName(
                componentTypeName
            );

        if (componentType == null)
            return 0;

        MonoBehaviour[] behaviours =
            Resources.FindObjectsOfTypeAll<
                MonoBehaviour
            >();

        int errors = 0;

        foreach (MonoBehaviour behaviour
                 in behaviours)
        {
            if (behaviour == null ||
                behaviour.GetType() !=
                componentType ||
                !behaviour.gameObject.scene.IsValid())
            {
                continue;
            }

            SerializedObject serialized =
                new SerializedObject(behaviour);

            SerializedProperty array =
                serialized.FindProperty(
                    arrayFieldName
                );

            if (array == null ||
                !array.isArray ||
                array.arraySize == 0)
            {
                Debug.LogError(
                    componentTypeName +
                    "." +
                    arrayFieldName +
                    " đang trống.",
                    behaviour
                );

                errors++;
            }
        }

        return errors;
    }

    private static void SynchronizeIcons(
        InventoryItemData inventoryItem,
        ScriptableObject specializedAsset)
    {
        if (inventoryItem == null ||
            specializedAsset == null)
        {
            return;
        }

        SerializedObject serializedItem =
            new SerializedObject(inventoryItem);

        SerializedObject serializedSpecialized =
            new SerializedObject(
                specializedAsset
            );

        SerializedProperty itemIcon =
            serializedItem.FindProperty(
                "icon"
            );

        SerializedProperty specializedIcon =
            FindFirstProperty(
                serializedSpecialized,
                "icon",
                "itemIcon",
                "sprite"
            );

        if (itemIcon == null ||
            specializedIcon == null ||
            specializedIcon.propertyType !=
                SerializedPropertyType
                    .ObjectReference)
        {
            return;
        }

        UnityEngine.Object itemValue =
            itemIcon.objectReferenceValue;

        UnityEngine.Object specializedValue =
            specializedIcon
                .objectReferenceValue;

        if (itemValue != null &&
            specializedValue == null)
        {
            specializedIcon
                .objectReferenceValue =
                    itemValue;

            serializedSpecialized
                .ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(
                specializedAsset
            );
        }
        else if (specializedValue != null &&
                 itemValue == null)
        {
            itemIcon.objectReferenceValue =
                specializedValue;

            serializedItem
                .ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(
                inventoryItem
            );
        }
    }

    private static void SetCategoryIfAvailable(
        SerializedObject target,
        string category)
    {
        string[] candidates =
        {
            "equipmentType",
            "type",
            "category",
            "equipmentCategory"
        };

        foreach (string candidate in candidates)
        {
            SerializedProperty property =
                target.FindProperty(candidate);

            if (property == null)
                continue;

            if (property.propertyType ==
                SerializedPropertyType.String)
            {
                property.stringValue = category;
                return;
            }

            if (property.propertyType ==
                SerializedPropertyType.Enum)
            {
                int index =
                    FindCategoryEnumIndex(
                        property,
                        category
                    );

                if (index >= 0)
                {
                    property.enumValueIndex =
                        index;

                    return;
                }
            }
        }
    }

    private static int FindCategoryEnumIndex(
        SerializedProperty property,
        string category)
    {
        string[] aliases;

        switch (category)
        {
            case "Rod":
                aliases = new[]
                {
                    "Rod",
                    "FishingRod",
                    "CanCau"
                };
                break;

            case "Reel":
                aliases = new[]
                {
                    "Reel",
                    "FishingReel",
                    "MayCau"
                };
                break;

            case "Line":
                aliases = new[]
                {
                    "Line",
                    "FishingLine",
                    "DayCau"
                };
                break;

            case "Hook":
                aliases = new[]
                {
                    "Hook",
                    "FishingHook",
                    "MocCau"
                };
                break;

            default:
                aliases = new[]
                {
                    category
                };
                break;
        }

        for (int i = 0;
             i < property.enumNames.Length;
             i++)
        {
            string enumName =
                NormalizeName(
                    property.enumNames[i]
                );

            foreach (string alias in aliases)
            {
                if (enumName ==
                    NormalizeName(alias))
                {
                    return i;
                }
            }
        }

        return -1;
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
                    assembly
                        .GetTypes()
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
                // Bỏ qua assembly không đọc được.
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

    private static string NormalizeName(
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

    private static void EnsureFolder(
        string fullPath)
    {
        string[] parts =
            fullPath.Split('/');

        string current = parts[0];

        for (int i = 1;
             i < parts.Length;
             i++)
        {
            string next =
                current +
                "/" +
                parts[i];

            if (!AssetDatabase.IsValidFolder(
                    next))
            {
                AssetDatabase.CreateFolder(
                    current,
                    parts[i]
                );
            }

            current = next;
        }
    }

    private static SerializedProperty
        FindFirstProperty(
            SerializedObject target,
            params string[] names)
    {
        foreach (string name in names)
        {
            SerializedProperty property =
                target.FindProperty(name);

            if (property != null)
                return property;
        }

        return null;
    }

    private static void SetString(
        SerializedObject target,
        string name,
        string value)
    {
        SerializedProperty property =
            target.FindProperty(name);

        if (property != null &&
            property.propertyType ==
                SerializedPropertyType.String)
        {
            property.stringValue = value;
        }
    }

    private static void SetInt(
        SerializedObject target,
        string name,
        int value)
    {
        SerializedProperty property =
            target.FindProperty(name);

        if (property != null &&
            property.propertyType ==
                SerializedPropertyType.Integer)
        {
            property.intValue = value;
        }
    }

    private static void SetBool(
        SerializedObject target,
        string name,
        bool value)
    {
        SerializedProperty property =
            target.FindProperty(name);

        if (property != null &&
            property.propertyType ==
                SerializedPropertyType.Boolean)
        {
            property.boolValue = value;
        }
    }

    private static void SetEnumIndex(
        SerializedObject target,
        string name,
        int value)
    {
        SerializedProperty property =
            target.FindProperty(name);

        if (property != null &&
            property.propertyType ==
                SerializedPropertyType.Enum)
        {
            property.enumValueIndex =
                Mathf.Clamp(
                    value,
                    0,
                    property.enumNames.Length - 1
                );
        }
    }

    private static void SetFirstString(
        SerializedObject target,
        string value,
        params string[] names)
    {
        SerializedProperty property =
            FindFirstProperty(
                target,
                names
            );

        if (property != null &&
            property.propertyType ==
                SerializedPropertyType.String)
        {
            property.stringValue = value;
        }
    }

    private static void SetFirstInt(
        SerializedObject target,
        int value,
        params string[] names)
    {
        SerializedProperty property =
            FindFirstProperty(
                target,
                names
            );

        if (property != null &&
            property.propertyType ==
                SerializedPropertyType.Integer)
        {
            property.intValue = value;
        }
    }

    private static void SetFirstFloat(
        SerializedObject target,
        float value,
        params string[] names)
    {
        SerializedProperty property =
            FindFirstProperty(
                target,
                names
            );

        if (property != null &&
            property.propertyType ==
                SerializedPropertyType.Float)
        {
            property.floatValue = value;
        }
    }

    private static void SetFirstBool(
        SerializedObject target,
        bool value,
        params string[] names)
    {
        SerializedProperty property =
            FindFirstProperty(
                target,
                names
            );

        if (property != null &&
            property.propertyType ==
                SerializedPropertyType.Boolean)
        {
            property.boolValue = value;
        }
    }

    private static void
        SetFirstObjectReference(
            SerializedObject target,
            UnityEngine.Object value,
            params string[] names)
    {
        SerializedProperty property =
            FindFirstProperty(
                target,
                names
            );

        if (property != null &&
            property.propertyType ==
                SerializedPropertyType
                    .ObjectReference)
        {
            property.objectReferenceValue =
                value;
        }
    }

    private static void SetRelativeString(
        SerializedProperty target,
        string value,
        string name)
    {
        SerializedProperty property =
            target.FindPropertyRelative(name);

        if (property != null)
            property.stringValue = value;
    }

    private static void SetRelativeInt(
        SerializedProperty target,
        int value,
        string name)
    {
        SerializedProperty property =
            target.FindPropertyRelative(name);

        if (property != null)
            property.intValue = value;
    }

    private static void SetRelativeFloat(
        SerializedProperty target,
        float value,
        string name)
    {
        SerializedProperty property =
            target.FindPropertyRelative(name);

        if (property != null)
            property.floatValue = value;
    }

    private static void SetRelativeBool(
        SerializedProperty target,
        bool value,
        string name)
    {
        SerializedProperty property =
            target.FindPropertyRelative(name);

        if (property != null)
            property.boolValue = value;
    }

    private static void SetRelativeObject(
        SerializedProperty target,
        UnityEngine.Object value,
        string name)
    {
        SerializedProperty property =
            target.FindPropertyRelative(name);

        if (property != null)
            property.objectReferenceValue = value;
    }
}
#endif
