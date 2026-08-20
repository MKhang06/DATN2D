using UnityEngine;

[DefaultExecutionOrder(-200)]
[DisallowMultipleComponent]
public class ChoppableTreeGroupSetup : MonoBehaviour
{
    [Header("Source")]
    [SerializeField]
    private ChoppableTree sourceTree;

    private void Awake()
    {
        SetupTrees();
    }

    [ContextMenu("Setup Choppable Trees")]
    public void SetupTrees()
    {
        if (sourceTree == null)
        {
            Debug.LogError(
                $"{name}: chưa gán cây mẫu để thiết lập chặt cây.",
                this
            );
            return;
        }

        SpriteRenderer[] treeRenderers =
            GetComponentsInChildren<SpriteRenderer>(true);

        int configuredCount = 0;

        foreach (SpriteRenderer treeRenderer in treeRenderers)
        {
            if (treeRenderer == null)
                continue;

            GameObject treeObject = treeRenderer.gameObject;

            if (treeObject == sourceTree.gameObject)
                continue;

            EnsureInteractionTrigger(treeObject);

            ChoppableTree choppableTree =
                treeObject.GetComponent<ChoppableTree>();

            if (choppableTree == null)
                choppableTree = treeObject.AddComponent<ChoppableTree>();

            choppableTree.CopySettingsFrom(sourceTree);
            configuredCount++;
        }

        Debug.Log(
            $"{name}: đã thiết lập {configuredCount} cây có thể chặt.",
            this
        );
    }

    private void EnsureInteractionTrigger(GameObject treeObject)
    {
        Collider2D[] colliders =
            treeObject.GetComponents<Collider2D>();

        foreach (Collider2D collider in colliders)
        {
            if (collider != null && collider.isTrigger)
                return;
        }

        CircleCollider2D trigger =
            treeObject.AddComponent<CircleCollider2D>();

        trigger.isTrigger = true;

        CircleCollider2D sourceTrigger =
            FindSourceCircleTrigger();

        if (sourceTrigger == null)
            return;

        trigger.offset = sourceTrigger.offset;
        trigger.radius = sourceTrigger.radius;
        trigger.sharedMaterial = sourceTrigger.sharedMaterial;
    }

    private CircleCollider2D FindSourceCircleTrigger()
    {
        CircleCollider2D[] sourceColliders =
            sourceTree.GetComponents<CircleCollider2D>();

        foreach (CircleCollider2D collider in sourceColliders)
        {
            if (collider != null && collider.isTrigger)
                return collider;
        }

        return null;
    }
}
