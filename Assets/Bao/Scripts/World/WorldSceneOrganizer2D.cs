using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-100)]
[DisallowMultipleComponent]
public sealed class WorldSceneOrganizer2D : MonoBehaviour
{
    [Header("Automatic Y sorting")]
    [SerializeField]
    private string unifiedSortingLayer = "Objects";

    [SerializeField, Min(1)]
    private int sortingOrdersPerUnit = 100;

    [SerializeField, Min(0.02f)]
    private float movingObjectSortInterval = 0.05f;

    [SerializeField]
    private string[] sortableLayerNames =
    {
        "Player",
        "Body",
        "Arm",
        "hair",
        "Mountain",
        "Bushes",
        "Rocks",
        "Tree",
        "Building",
        "house",
        "tree",
        "NPC",
        "Objects"
    };

    [Header("World collision")]
    [SerializeField]
    private string[] solidLayerNames =
    {
        "Mountain",
        "Bushes",
        "Rocks",
        "Tree",
        "Building",
        "house",
        "tree",
        "NPC",
        "Objects"
    };

    [SerializeField, Range(0.1f, 1f)]
    private float solidWidthRatio = 0.42f;

    [SerializeField, Range(0.05f, 0.5f)]
    private float solidHeightRatio = 0.1f;

    [SerializeField, Min(0.05f)]
    private float minimumSolidHeight = 0.14f;

    [SerializeField, Min(0.05f)]
    private float maximumSolidHeight = 0.55f;

    private readonly HashSet<string> sortableLayers =
        new HashSet<string>();

    private readonly HashSet<string> solidLayers =
        new HashSet<string>();

    private readonly List<SortEntry> movingEntries =
        new List<SortEntry>();

    private float nextMovingSortTime;

    private sealed class SortEntry
    {
        public SpriteRenderer Renderer;
        public SpriteRenderer DepthRenderer;
        public Transform Anchor;
        public int LocalOrderOffset;
        public bool UseAnchorPosition;
    }

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.BeforeSceneLoad
    )]
    private static void RegisterSceneLoadHandler()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private static void HandleSceneLoaded(
        Scene scene,
        LoadSceneMode mode)
    {
        PlayerController player =
            FindFirstObjectByType<PlayerController>(
                FindObjectsInactive.Include
            );

        if (player == null || player.gameObject.scene != scene)
            return;

        WorldSceneOrganizer2D existing =
            FindFirstObjectByType<WorldSceneOrganizer2D>(
                FindObjectsInactive.Include
            );

        if (existing != null && existing.gameObject.scene == scene)
            return;

        GameObject organizer =
            new GameObject("World Scene Organizer 2D");

        SceneManager.MoveGameObjectToScene(organizer, scene);
        organizer.AddComponent<WorldSceneOrganizer2D>();
    }

    private void Awake()
    {
        BuildLayerSets();
        OrganizeScene();
    }

    private void LateUpdate()
    {
        if (Time.unscaledTime < nextMovingSortTime)
            return;

        nextMovingSortTime =
            Time.unscaledTime + movingObjectSortInterval;

        for (int i = movingEntries.Count - 1; i >= 0; i--)
        {
            SortEntry entry = movingEntries[i];

            if (entry.Renderer == null || entry.Anchor == null)
            {
                movingEntries.RemoveAt(i);
                continue;
            }

            ApplySorting(entry);
        }
    }

    [ContextMenu("Organize World Scene")]
    public void OrganizeScene()
    {
        BuildLayerSets();
        movingEntries.Clear();

        SpriteRenderer[] renderers =
            FindObjectsByType<SpriteRenderer>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        int sortedCount = 0;
        int colliderCount = 0;

        foreach (SpriteRenderer renderer in renderers)
        {
            if (renderer == null ||
                !renderer.gameObject.scene.IsValid())
            {
                continue;
            }

            string originalLayer = renderer.sortingLayerName;
            bool isChoppableTree =
                renderer.GetComponent<ChoppableTree>() != null;

            if (!sortableLayers.Contains(originalLayer) &&
                !isChoppableTree)
            {
                continue;
            }

            SortEntry entry = CreateSortEntry(
                renderer,
                originalLayer
            );

            renderer.sortingLayerName = unifiedSortingLayer;
            ApplySorting(entry);
            sortedCount++;

            if (ShouldUpdateWhileMoving(renderer, originalLayer))
                movingEntries.Add(entry);

            if ((solidLayers.Contains(originalLayer) ||
                 isChoppableTree) &&
                EnsureSolidFootprint(
                    renderer,
                    entry.DepthRenderer,
                    originalLayer,
                    isChoppableTree
                ))
            {
                colliderCount++;
            }
        }

        Debug.Log(
            $"WorldSceneOrganizer2D: sorted {sortedCount} sprites, " +
            $"added {colliderCount} solid colliders.",
            this
        );
    }

    private SortEntry CreateSortEntry(
        SpriteRenderer renderer,
        string originalLayer)
    {
        bool actorLayer = IsActorLayer(originalLayer);
        Transform anchor;

        if (originalLayer == "NPC")
        {
            Rigidbody2D npcBody =
                renderer.GetComponentInParent<Rigidbody2D>();

            anchor = npcBody != null
                ? npcBody.transform
                : renderer.transform;
        }
        else
        {
            anchor = actorLayer
                ? FindActorAnchor(renderer.transform)
                : renderer.transform;
        }

        return new SortEntry
        {
            Renderer = renderer,
            DepthRenderer = actorLayer
                ? renderer
                : FindVisualRootRenderer(renderer),
            Anchor = anchor,
            LocalOrderOffset = GetLocalOrderOffset(
                originalLayer,
                renderer.sortingOrder
            ),
            UseAnchorPosition = actorLayer
        };
    }

    private void ApplySorting(SortEntry entry)
    {
        float groundY = entry.UseAnchorPosition
            ? entry.Anchor.position.y
            : entry.DepthRenderer.bounds.min.y;

        int order =
            Mathf.RoundToInt(-groundY * sortingOrdersPerUnit) +
            entry.LocalOrderOffset;

        entry.Renderer.sortingOrder =
            Mathf.Clamp(order, short.MinValue, short.MaxValue);
    }

    private bool EnsureSolidFootprint(
        SpriteRenderer renderer,
        SpriteRenderer depthRenderer,
        string originalLayer,
        bool isChoppableTree)
    {
        // Một cây/nhà có thể gồm nhiều sprite con. Chỉ sprite gốc được giữ
        // hitbox vật lý để tránh collider chồng lên nhau và chặn quá xa.
        if (renderer != depthRenderer)
            return false;

        Collider2D[] localColliders =
            renderer.GetComponents<Collider2D>();

        Collider2D[] parentColliders =
            renderer.GetComponentsInParent<Collider2D>(true);

        foreach (Collider2D collider in parentColliders)
        {
            if (collider != null &&
                collider.gameObject != renderer.gameObject &&
                !collider.isTrigger)
            {
                return false;
            }
        }

        if (renderer.sprite == null)
            return false;

        Bounds spriteBounds = renderer.sprite.bounds;
        Vector2 footprint = CalculateFootprint(
            spriteBounds,
            originalLayer,
            isChoppableTree
        );
        Vector2 offset = new Vector2(
            spriteBounds.center.x,
            spriteBounds.min.y + footprint.y * 0.5f
        );

        foreach (Collider2D collider in localColliders)
        {
            if (collider == null || collider.isTrigger)
                continue;

            ResizeCollider(collider, footprint, offset);
            return false;
        }

        BoxCollider2D solidCollider =
            renderer.gameObject.AddComponent<BoxCollider2D>();

        solidCollider.isTrigger = false;
        solidCollider.size = footprint;
        solidCollider.offset = offset;

        return true;
    }

    private Vector2 CalculateFootprint(
        Bounds spriteBounds,
        string originalLayer,
        bool isChoppableTree)
    {
        float width;
        float height;

        if (originalLayer == "NPC")
        {
            width = Mathf.Clamp(spriteBounds.size.x * 0.3f, 0.38f, 0.56f);
            height = Mathf.Clamp(spriteBounds.size.y * 0.11f, 0.24f, 0.38f);
        }
        else if (isChoppableTree || IsTreeLayer(originalLayer))
        {
            width = Mathf.Clamp(spriteBounds.size.x * 0.28f, 0.42f, 1.25f);
            height = Mathf.Clamp(spriteBounds.size.y * 0.07f, 0.2f, 0.46f);
        }
        else if (IsBuildingLayer(originalLayer))
        {
            width = Mathf.Clamp(spriteBounds.size.x * 0.72f, 0.6f, 5f);
            height = Mathf.Clamp(spriteBounds.size.y * 0.1f, 0.3f, 0.75f);
        }
        else
        {
            width = Mathf.Max(0.15f, spriteBounds.size.x * solidWidthRatio);
            height = Mathf.Clamp(
                spriteBounds.size.y * solidHeightRatio,
                minimumSolidHeight,
                maximumSolidHeight
            );
        }

        return new Vector2(width, height);
    }

    private static void ResizeCollider(
        Collider2D collider,
        Vector2 size,
        Vector2 offset)
    {
        if (collider is BoxCollider2D box)
        {
            box.size = size;
            box.offset = offset;
        }
        else if (collider is CapsuleCollider2D capsule)
        {
            capsule.direction = size.x >= size.y
                ? CapsuleDirection2D.Horizontal
                : CapsuleDirection2D.Vertical;
            capsule.size = size;
            capsule.offset = offset;
        }
        else if (collider is CircleCollider2D circle)
        {
            circle.radius = Mathf.Min(size.x, size.y) * 0.5f;
            circle.offset = offset;
        }
    }

    private static SpriteRenderer FindVisualRootRenderer(
        SpriteRenderer renderer)
    {
        SpriteRenderer root = renderer;
        Transform current = renderer.transform.parent;

        while (current != null)
        {
            SpriteRenderer parentRenderer =
                current.GetComponent<SpriteRenderer>();

            if (parentRenderer == null)
                break;

            root = parentRenderer;
            current = current.parent;
        }

        return root;
    }

    private bool ShouldUpdateWhileMoving(
        SpriteRenderer renderer,
        string originalLayer)
    {
        if (IsActorLayer(originalLayer))
            return true;

        Rigidbody2D body =
            renderer.GetComponentInParent<Rigidbody2D>();

        return body != null &&
               body.bodyType != RigidbodyType2D.Static;
    }

    private Transform FindActorAnchor(Transform start)
    {
        Rigidbody2D body =
            start.GetComponentInParent<Rigidbody2D>();

        if (body != null)
            return body.transform;

        Transform current = start;

        while (current.parent != null)
        {
            if (current.CompareTag("Player") ||
                current.name.Equals(
                    "Player",
                    System.StringComparison.OrdinalIgnoreCase
                ))
            {
                return current;
            }

            current = current.parent;
        }

        return current;
    }

    private static bool IsActorLayer(string layerName)
    {
        return layerName == "Player" ||
               layerName == "Body" ||
               layerName == "Arm" ||
               layerName == "hair" ||
               layerName == "NPC";
    }

    private static bool IsTreeLayer(string layerName)
    {
        return layerName == "Tree" || layerName == "tree";
    }

    private static bool IsBuildingLayer(string layerName)
    {
        return layerName == "Building" || layerName == "house";
    }

    private static int GetLocalOrderOffset(
        string layerName,
        int originalOrder)
    {
        int preservedOrder = Mathf.Clamp(originalOrder, -20, 20);

        switch (layerName)
        {
            case "Body":
                return 10 + preservedOrder;
            case "Arm":
                return 20 + preservedOrder;
            case "hair":
                return 30 + preservedOrder;
            default:
                return preservedOrder;
        }
    }

    private void BuildLayerSets()
    {
        sortableLayers.Clear();
        solidLayers.Clear();

        AddLayerNames(sortableLayerNames, sortableLayers);
        AddLayerNames(solidLayerNames, solidLayers);
    }

    private static void AddLayerNames(
        string[] names,
        HashSet<string> destination)
    {
        if (names == null)
            return;

        foreach (string layerName in names)
        {
            if (!string.IsNullOrWhiteSpace(layerName))
                destination.Add(layerName.Trim());
        }
    }
}
