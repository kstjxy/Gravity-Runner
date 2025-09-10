using UnityEngine;

public enum SegmentKind
{
    Floor,
    Ceil,
    Conn_FloorToCeil,
    Conn_CeilToFloor,
    Gap_Floor,
    Gap_Ceil
}

public enum SegmentType
{
    Floor,
    Ceil,
    Conn
}

public class TerrainSegment : MonoBehaviour
{
    [Header("Definition")]
    public SegmentKind kind;

    [Tooltip("World-space Z length of this segment.")]
    public float length = 12f;

    [Header("Placement")]
    public SegmentType type;

    [HideInInspector] public float startZ;
    [HideInInspector] public float endZ;
}