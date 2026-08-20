using System.Collections.Generic;
using UnityEngine;

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
    private float solidWidthRatio = 0.62f;

    [SerializeField, Range(0.05f, 0.5f)]
    private float solidHeightRatio = 0.16f;

    [SerializeField, Min(0.05f)]
    private float minimumSolidHeight = 0.2f;

    [SerializeField, Min(0.05f)]
    private float maximumSolidHeight = 1.1f;

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
        public Transform Anchor;
        public int LocalOrderOffset;
        public bool UseAnchorPosition;
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
                EnsureSolidFootprint(renderer))
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
        Transform anchor = actorLayer
            ? FindActorAnchor(renderer.transform)
            : renderer.transform;

        return new SortEntry
        {
            Renderer = renderer,
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
            : entry.Renderer.bounds.min.y;

        int order =
            Mathf.RoundToInt(-groundY * sortingOrdersPerUnit) +
            entry.LocalOrderOffset;

        entry.Renderer.sortingOrder =
            Mathf.Clamp(order, short.MinValue, short.MaxValue);
    }

    private bool EnsureSolidFootprint(SpriteRenderer renderer)
    {
        Collider2D[] localColliders =
            renderer.GetComponents<Collider2D>();

        foreach (Collider2D collider in localColliders)
        {
            if (collider != null && !collider.isTrigger)
                return false;
        }

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
        float width = Mathf.Max(
            0.15f,
            spriteBounds.size.x * solidWidthRatio
        );
        float height = Mathf.Clamp(
            spriteBounds.size.y * solidHeightRatio,
            minimumSolidHeight,
            maximumSolidHeight
        );

        BoxCollider2D solidCollider =
            renderer.gameObject.AddComponent<BoxCollider2D>();

        solidCollider.isTrigger = false;
        solidCollider.size = new Vector2(width, height);
        solidCollider.offset = new Vector2(
            spriteBounds.center.x,
            spriteBounds.min.y + height * 0.5f
        );

        return true;
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
