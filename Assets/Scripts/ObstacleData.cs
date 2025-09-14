using UnityEngine;

public enum ObstacleType
{
    Dispenser,
    Long_Rock,
    Rock,
    Saw,
    Spikes
}

public class ObstacleData : MonoBehaviour
{
    public ObstacleType type;

    [Tooltip("World-space size used for placement bounds.")]
    public float length = 3f;  // along +Z (local)
    public float width = 2f;  // along +X (local)
}
