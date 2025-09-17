using UnityEngine;

public enum PickupType
{
    Shield,
    Boost
}

[RequireComponent(typeof(Collider))]
public class PickupData : MonoBehaviour
{
    public PickupType type;

    [Tooltip("World-space size used for placement bounds.")]
    public float width = 1.5f;   // along +X (local)
    public float length = 1.5f;  // along +Z (local)
}
