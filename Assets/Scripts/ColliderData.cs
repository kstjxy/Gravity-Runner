using UnityEngine;

public enum ColliderKind
{
    Obstacle,
    Pickup
}

[RequireComponent(typeof(Collider))]
public class ColliderData : MonoBehaviour
{
    public ColliderKind kind = ColliderKind.Obstacle;

    private void Reset()
    {
        // Default: obstacle collider, non-trigger so we can toggle in Inspector
        var col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true; // Usually we want triggers for pickups/obstacles in this game
        }
    }
}