using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
[DefaultExecutionOrder(-100)]
public class PlayerToolController : MonoBehaviour
{
    public enum ToolType
    {
        None,
        Hoe,
        WateringCan,
        Axe,
        Pickaxe,
        Sickle,
        FishingRod
    }

    [Serializable]
    private class ToolHotbarBinding
    {
        public ToolType tool;

        [Tooltip(
            "Item Name hoặc Item Id có thể xuất hiện trong Hotbar. " +
            "Chỉ cần một tên khớp là công cụ được nhận diện."
        )]
        public string[] itemNames;
    }

    [Header("Current Tool")]
    public ToolType currentTool = ToolType.None;

    [Header("Hotbar Only")]
    [Tooltip(
        "Bật: công cụ chỉ được sử dụng khi item tương ứng đang được chọn trong Hotbar."
    )]
    [SerializeField]
    private bool toolsOnlyFromHotbar = true;

    [Tooltip(
        "Tắt toàn bộ đồ vật trong ToolHolder khi bắt đầu game."
    )]
    [SerializeField]
    private bool hideAllToolsOnStart = true;

    [Tooltip(
        "Ẩn hình item mặc định trong HeldItem khi đang chọn một công cụ, tránh hiện hai vật phẩm."
    )]
    [SerializeField]
    private bool hideHeldItemWhileToolSelected = true;

    [Header("Tool Item Names")]
    [SerializeField]
    private ToolHotbarBinding[] toolBindings;

    [Header("Settings")]
    [SerializeField]
    private float interactRadius = 0.25f;

    [SerializeField]
    private LayerMask farmLayer;

    [Header("References")]
    [SerializeField]
    private InventoryManager inventoryManager;

    [SerializeField]
    private PlayerToolAnimation toolAnimation;

    [SerializeField]
    private PlayerStats playerStats;

    [SerializeField]
    private FishingManager fishingManager;

    [Tooltip("Player/HeldItem")]
    [SerializeField]
    private Transform heldItemRoot;

    private Camera cam;
    private string lastSelectedItemName = string.Empty;

    private readonly Dictionary<SpriteRenderer, bool>
        heldItemRendererStates =
            new Dictionary<SpriteRenderer, bool>();

    private void Reset()
    {
        toolBindings = CreateDefaultBindings();
    }

    private void Awake()
    {
        ResolveReferences();
        EnsureDefaultBindings();

        currentTool = ToolType.None;

        if (hideAllToolsOnStart &&
            toolAnimation != null)
        {
            toolAnimation.HideAllTools();
            toolAnimation.SetFishingLock(false);
        }
    }

    private void Start()
    {
        /*
         * Gọi lại ở Start để tắt các object bị script khác bật trong Awake.
         */
        if (hideAllToolsOnStart &&
            toolAnimation != null)
        {
            currentTool = ToolType.None;
            toolAnimation.HideAllTools();
            toolAnimation.SetFishingLock(false);
        }

        SyncToolFromSelectedHotbar(true);
    }

    private void Update()
    {
        ResolveReferences();

        /*
         * Không còn Q/E/R/T/Y/F để lấy công cụ miễn phí.
         * Chỉ đọc item đang được chọn trong Hotbar.
         */
        SyncToolFromSelectedHotbar(false);

        if (toolAnimation != null &&
            toolAnimation.IsBusy())
        {
            return;
        }

        RotateToolToMouse();

        if (Input.GetMouseButtonDown(0))
        {
            UseToolAtMouse();
        }
    }

    private void SyncToolFromSelectedHotbar(
        bool force)
    {
        if (!toolsOnlyFromHotbar)
        {
            return;
        }

        string selectedItemName =
            GetSelectedHotbarItemName();

        ToolType selectedTool =
            GetToolTypeFromItemName(
                selectedItemName
            );

        bool selectionChanged =
            !string.Equals(
                lastSelectedItemName,
                selectedItemName,
                StringComparison.Ordinal
            );

        if (!force &&
            !selectionChanged &&
            selectedTool == currentTool)
        {
            /*
             * Fishing lock có thể bị hệ thống câu bật lại.
             * Khi đang cầm cần, luôn mở khóa để xoay theo chuột.
             */
            if (currentTool ==
                    ToolType.FishingRod &&
                toolAnimation != null)
            {
                toolAnimation.SetFishingLock(false);
            }

            return;
        }

        lastSelectedItemName =
            selectedItemName;

        EquipSelectedHotbarTool(
            selectedTool
        );
    }

    private void EquipSelectedHotbarTool(
        ToolType selectedTool)
    {
        if (toolAnimation == null)
        {
            currentTool =
                ToolType.None;

            return;
        }

        /*
         * Luôn tắt hết HoeSprite, CanSprite, icon_Pickaxe,
         * icon_Chop, icon_Reap và Fishing Rod trước.
         */
        toolAnimation.HideAllTools();
        toolAnimation.SetFishingLock(false);

        RestoreHeldItemRenderers();

        currentTool =
            selectedTool;

        if (selectedTool ==
            ToolType.None)
        {
            CursorManager.Instance?.
                SetDefaultCursor();

            return;
        }

        if (hideHeldItemWhileToolSelected)
        {
            CacheAndHideHeldItemRenderers();
        }

        /*
         * Chỉ bật đúng object của item đang được chọn trong Hotbar.
         */
        toolAnimation.ShowTool(
            selectedTool
        );

        if (selectedTool ==
            ToolType.FishingRod)
        {
            /*
             * Cần câu cũ xoay ngay, không cần nhấn F lần nữa.
             */
            toolAnimation.SetFishingLock(
                false
            );
        }
    }

    private string GetSelectedHotbarItemName()
    {
        if (inventoryManager == null)
        {
            return string.Empty;
        }

        InventoryManager.InventorySlot slot =
            inventoryManager.SelectedSlot;

        if (slot == null ||
            slot.IsEmpty ||
            string.IsNullOrWhiteSpace(
                slot.itemName))
        {
            return string.Empty;
        }

        return slot.itemName;
    }

    private ToolType GetToolTypeFromItemName(
        string itemName)
    {
        if (string.IsNullOrWhiteSpace(
                itemName))
        {
            return ToolType.None;
        }

        string normalizedItem =
            Normalize(itemName);

        if (toolBindings == null)
        {
            return ToolType.None;
        }

        foreach (ToolHotbarBinding binding
                 in toolBindings)
        {
            if (binding == null ||
                binding.tool ==
                    ToolType.None ||
                binding.itemNames == null)
            {
                continue;
            }

            foreach (string candidate
                     in binding.itemNames)
            {
                string normalizedCandidate =
                    Normalize(candidate);

                if (string.IsNullOrWhiteSpace(
                        normalizedCandidate))
                {
                    continue;
                }

                if (normalizedItem ==
                        normalizedCandidate ||
                    normalizedItem.Contains(
                        normalizedCandidate) ||
                    normalizedCandidate.Contains(
                        normalizedItem))
                {
                    return binding.tool;
                }
            }
        }

        return ToolType.None;
    }

    private void RotateToolToMouse()
    {
        if (currentTool ==
            ToolType.None)
        {
            return;
        }

        if (toolAnimation == null)
        {
            return;
        }

        if (cam == null)
        {
            cam = Camera.main;
        }

        if (cam == null)
        {
            return;
        }

        Vector3 mousePos =
            cam.ScreenToWorldPoint(
                Input.mousePosition
            );

        mousePos.z = 0f;

        if (currentTool ==
            ToolType.FishingRod)
        {
            toolAnimation.SetFishingLock(
                false
            );
        }

        toolAnimation.LookAtMouse(
            mousePos
        );
    }

    private void UseToolAtMouse()
    {
        if (currentTool ==
            ToolType.None)
        {
            return;
        }

        /*
         * Kiểm tra lại Hotbar trước khi dùng.
         * Item đã bị bán/xóa/chuyển ô thì công cụ không được sử dụng.
         */
        ToolType selectedTool =
            GetToolTypeFromItemName(
                GetSelectedHotbarItemName()
            );

        if (selectedTool !=
            currentTool)
        {
            EquipSelectedHotbarTool(
                selectedTool
            );

            return;
        }

        if (EventSystem.current != null &&
            EventSystem.current
                .IsPointerOverGameObject())
        {
            return;
        }

        if (currentTool ==
            ToolType.FishingRod)
        {
            UseFishingRod();
            return;
        }

        if (cam == null)
        {
            cam = Camera.main;
        }

        if (cam == null)
        {
            return;
        }

        Vector2 mouseWorldPos =
            cam.ScreenToWorldPoint(
                Input.mousePosition
            );

        Collider2D hit =
            Physics2D.OverlapCircle(
                mouseWorldPos,
                interactRadius,
                farmLayer
            );

        FarmTile tile =
            hit != null
                ? hit.GetComponent<FarmTile>()
                : null;

        if (tile != null)
        {
            UseToolOnFarmTile(tile);
            return;
        }

        FarmPlotTilemap plots = FarmPlotTilemap.Instance;
        if (plots == null ||
            !plots.TryGetCell(mouseWorldPos, out Vector3Int cell))
        {
            return;
        }

        switch (currentTool)
        {
            case ToolType.Hoe:
                UseHoe(plots, cell);
                break;

            case ToolType.WateringCan:
                UseWateringCan(plots, cell);
                break;
        }
    }

    private void UseToolOnFarmTile(FarmTile tile)
    {
        switch (currentTool)
        {
            case ToolType.Hoe:
                UseHoe(tile);
                break;

            case ToolType.WateringCan:
                UseWateringCan(tile);
                break;

            case ToolType.Axe:
                UseAxe(tile);
                break;

            case ToolType.Pickaxe:
                UsePickaxe(tile);
                break;

            case ToolType.Sickle:
                UseSickle(tile);
                break;
        }
    }

    private void UseFishingRod()
    {
        if (GetToolTypeFromItemName(
                GetSelectedHotbarItemName()) !=
            ToolType.FishingRod)
        {
            UnequipCurrentTool();
            return;
        }

        if (fishingManager == null)
        {
            Debug.LogWarning(
                "Thiếu FishingManager trên Player."
            );

            return;
        }

        if (toolAnimation != null)
        {
            toolAnimation.UseFishingRod(
                () =>
                {
                    fishingManager
                        .TryStartFishing();
                }
            );
        }
        else
        {
            fishingManager
                .TryStartFishing();
        }
    }

    private void UseHoe(
        FarmTile tile)
    {
        if (tile == null ||
            tile.currentState != FarmTile.SoilState.Normal)
        {
            return;
        }

        UseHoeAction(tile.Hoe);
    }

    private void UseHoe(
        FarmPlotTilemap plots,
        Vector3Int cell)
    {
        if (plots == null || !plots.CanHoe(cell))
            return;

        UseHoeAction(() => plots.Hoe(cell));
    }

    private void UseHoeAction(Action applyHoe)
    {
        if (playerStats == null)
            return;

        float cost =
            GetEnergyCost(15f);

        if (!playerStats.HasEnergy(cost))
        {
            Debug.Log(
                "Không đủ Energy để cuốc đất!"
            );

            return;
        }

        playerStats.UseEnergy(cost);
        playerStats.AddXP(2);

        if (toolAnimation != null)
        {
            toolAnimation.UseHoe(
                applyHoe
            );
        }
        else
        {
            applyHoe?.Invoke();
        }
    }

    private void UseWateringCan(
        FarmTile tile)
    {
        if (tile == null ||
            tile.currentState != FarmTile.SoilState.Hoed)
        {
            return;
        }

        UseWateringAction(tile.Water);
    }

    private void UseWateringCan(
        FarmPlotTilemap plots,
        Vector3Int cell)
    {
        if (plots == null || !plots.CanWater(cell))
            return;

        UseWateringAction(() => plots.Water(cell));
    }

    private void UseWateringAction(Action applyWater)
    {
        if (playerStats == null)
            return;

        float cost =
            GetEnergyCost(10f);

        if (!playerStats.HasEnergy(cost))
        {
            Debug.Log(
                "Không đủ Energy để tưới nước!"
            );

            return;
        }

        playerStats.UseEnergy(cost);
        playerStats.AddXP(1);

        if (toolAnimation != null)
        {
            toolAnimation.UseWateringCan(
                applyWater
            );
        }
        else
        {
            applyWater?.Invoke();
        }
    }

    private void UseAxe(
        FarmTile tile)
    {
        if (playerStats == null)
            return;

        float cost =
            GetEnergyCost(20f);

        if (!playerStats.HasEnergy(cost))
        {
            Debug.Log(
                "Không đủ Energy để dùng rìu!"
            );

            return;
        }

        playerStats.UseEnergy(cost);
        playerStats.AddXP(3);

        if (toolAnimation != null)
        {
            toolAnimation.UseAxe(
                () => tile.Axe()
            );
        }
        else
        {
            tile.Axe();
        }
    }

    private void UsePickaxe(
        FarmTile tile)
    {
        if (playerStats == null)
            return;

        float cost =
            GetEnergyCost(20f);

        if (!playerStats.HasEnergy(cost))
        {
            Debug.Log(
                "Không đủ Energy để dùng cuốc chim!"
            );

            return;
        }

        playerStats.UseEnergy(cost);
        playerStats.AddXP(3);

        if (toolAnimation != null)
        {
            toolAnimation.UsePickaxe(
                () => tile.Pickaxe()
            );
        }
        else
        {
            tile.Pickaxe();
        }
    }

    private void UseSickle(
        FarmTile tile)
    {
        if (playerStats == null)
            return;

        float cost =
            GetEnergyCost(8f);

        if (!playerStats.HasEnergy(cost))
        {
            Debug.Log(
                "Không đủ Energy để dùng liềm!"
            );

            return;
        }

        playerStats.UseEnergy(cost);
        playerStats.AddXP(2);

        if (toolAnimation != null)
        {
            toolAnimation.UseSickle(
                () => tile.Sickle()
            );
        }
        else
        {
            tile.Sickle();
        }
    }

    private float GetEnergyCost(
        float baseCost)
    {
        float multiplier = 1f;

        if (CharacterPassiveManager
                .Instance != null)
        {
            multiplier =
                CharacterPassiveManager
                    .Instance
                    .EnergyCostMultiplier;
        }

        return baseCost *
               multiplier;
    }

    private void CacheAndHideHeldItemRenderers()
    {
        if (heldItemRoot == null)
        {
            return;
        }

        heldItemRendererStates.Clear();

        SpriteRenderer[] renderers =
            heldItemRoot
                .GetComponentsInChildren<
                    SpriteRenderer
                >(true);

        foreach (SpriteRenderer renderer
                 in renderers)
        {
            if (renderer == null)
            {
                continue;
            }

            heldItemRendererStates[renderer] =
                renderer.enabled;

            renderer.enabled = false;
        }
    }

    private void RestoreHeldItemRenderers()
    {
        foreach (
            KeyValuePair<SpriteRenderer, bool>
            state in heldItemRendererStates)
        {
            if (state.Key != null)
            {
                state.Key.enabled =
                    state.Value;
            }
        }

        heldItemRendererStates.Clear();
    }

    public void RefreshToolFromHotbar()
    {
        lastSelectedItemName =
            string.Empty;

        SyncToolFromSelectedHotbar(
            true
        );
    }

    public void UnequipCurrentTool()
    {
        currentTool =
            ToolType.None;

        lastSelectedItemName =
            string.Empty;

        RestoreHeldItemRenderers();

        if (toolAnimation != null)
        {
            toolAnimation.HideAllTools();
            toolAnimation.SetFishingLock(
                false
            );
        }

        CursorManager.Instance?.
            SetDefaultCursor();
    }

    private void ResolveReferences()
    {
        if (cam == null)
        {
            cam = Camera.main;
        }

        if (inventoryManager == null)
        {
            inventoryManager =
                InventoryManager.Instance;
        }

        if (inventoryManager == null)
        {
            inventoryManager =
                FindFirstObjectByType<
                    InventoryManager
                >(
                    FindObjectsInactive.Include
                );
        }

        if (toolAnimation == null)
        {
            toolAnimation =
                GetComponent<
                    PlayerToolAnimation
                >();
        }

        if (playerStats == null)
        {
            playerStats =
                GetComponent<
                    PlayerStats
                >();
        }

        if (fishingManager == null)
        {
            fishingManager =
                GetComponent<
                    FishingManager
                >();
        }

        if (heldItemRoot == null)
        {
            Transform[] children =
                transform.root
                    .GetComponentsInChildren<
                        Transform
                    >(true);

            foreach (Transform child
                     in children)
            {
                if (child != null &&
                    string.Equals(
                        child.name,
                        "HeldItem",
                        StringComparison
                            .OrdinalIgnoreCase))
                {
                    heldItemRoot = child;
                    break;
                }
            }
        }
    }

    private void EnsureDefaultBindings()
    {
        if (toolBindings != null &&
            toolBindings.Length > 0)
        {
            return;
        }

        toolBindings =
            CreateDefaultBindings();
    }

    private static ToolHotbarBinding[]
        CreateDefaultBindings()
    {
        return new[]
        {
            new ToolHotbarBinding
            {
                tool = ToolType.Hoe,
                itemNames = new[]
                {
                    "Hoe",
                    "Cuốc",
                    "Cuoc",
                    "equipment_hoe"
                }
            },

            new ToolHotbarBinding
            {
                tool = ToolType.WateringCan,
                itemNames = new[]
                {
                    "Watering Can",
                    "WateringCan",
                    "Bình Tưới",
                    "Binh Tuoi",
                    "equipment_wateringcan"
                }
            },

            new ToolHotbarBinding
            {
                tool = ToolType.Axe,
                itemNames = new[]
                {
                    "Axe",
                    "Hatchet",
                    "Rìu",
                    "Riu",
                    "Wood Axe",
                    "equipment_axe"
                }
            },

            new ToolHotbarBinding
            {
                tool = ToolType.Pickaxe,
                itemNames = new[]
                {
                    "Pickaxe",
                    "Cuốc Chim",
                    "Cuoc Chim",
                    "equipment_pickaxe"
                }
            },

            new ToolHotbarBinding
            {
                tool = ToolType.Sickle,
                itemNames = new[]
                {
                    "Sickle",
                    "Scythe",
                    "Liềm",
                    "Liem",
                    "equipment_sickle"
                }
            },

            new ToolHotbarBinding
            {
                tool = ToolType.FishingRod,
                itemNames = new[]
                {
                    "Fishing Rod",
                    "Feather Light",
                    "FeatherLight",
                    "EQ_FeatherLight",
                    "Cần Câu",
                    "Can Cau",
                    "equipment_rod"
                }
            }
        };
    }

    private static string Normalize(
        string value)
    {
        if (string.IsNullOrWhiteSpace(
                value))
        {
            return string.Empty;
        }

        string decomposed =
            value.Trim()
                .ToLowerInvariant()
                .Normalize(
                    NormalizationForm.FormD
                );

        StringBuilder builder =
            new StringBuilder();

        foreach (char character
                 in decomposed)
        {
            UnicodeCategory category =
                CharUnicodeInfo
                    .GetUnicodeCategory(
                        character
                    );

            if (category ==
                UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetterOrDigit(
                    character))
            {
                builder.Append(
                    character
                );
            }
        }

        return builder
            .ToString()
            .Normalize(
                NormalizationForm.FormC
            );
    }

    private void OnDisable()
    {
        RestoreHeldItemRenderers();

        if (toolAnimation != null)
        {
            toolAnimation.HideAllTools();
            toolAnimation.SetFishingLock(
                false
            );
        }

        currentTool =
            ToolType.None;

        CursorManager.Instance?.
            SetDefaultCursor();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(
            transform.position,
            interactRadius
        );
    }
}
