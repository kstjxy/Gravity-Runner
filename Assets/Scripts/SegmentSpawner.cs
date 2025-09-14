using System.Collections.Generic;
using UnityEngine;

public class SegmentSpawner : MonoBehaviour
{
    [Header("Refs")]
    public Transform player;                  // Player transform (Z drives spawning)
    public GameObject startFloorPrefab;       // First segment (Floor) at z = 0

    [Header("Segment Prefabs")]
    public GameObject floorPrefab;
    public GameObject ceilPrefab;
    public GameObject gapFloorPrefab;
    public GameObject gapCeilPrefab;
    public GameObject connF2CPrefab;          // Floor -> Ceil
    public GameObject connC2FPrefab;          // Ceil  -> Floor

    [Header("Obstacle Prefabs")]
    public GameObject dispenserPrefab;
    public GameObject longRockPrefab;
    public GameObject rockPrefab;
    public GameObject sawPrefab;
    public GameObject spikesPrefab;

    [Header("Placement")]
    public float ceilHeight = 10f;            // Y of ceiling lane
    public float laneX = 0f;                  // Centerline X for all segments

    [Header("Streaming Tuning")]
    public float buildAhead = 80f;            // Keep this much track ahead of player
    public float recycleBehind = 40f;         // Recycle segments once fully this far behind player

    // ---- Runtime state ----
    public bool hasFloor = true;              // starts on floor
    public bool hasCeil = false;

    private float trackEndZ = 0f;             // cumulative: where the NEXT segment starts
    private readonly LinkedList<TerrainSegment> active = new(); // ordered list
    private SegmentKind? lastKind = null;     // for adjacency rules

    void Start()
    {
        // Seed with the starting Floor segment at z = 0
        var startGO = Instantiate(startFloorPrefab, Vector3.zero, Quaternion.identity, transform);
        var seg = startGO.GetComponent<TerrainSegment>();
        if (!seg)
        {
            Debug.LogError("[SegmentSpawner] Start prefab missing TerrainSegment.");
            return;
        }

        // Place (Y/rotation) by type, then snap to the exact cumulative start
        PlaceByKind(startGO, seg);
        startGO.transform.position = new Vector3(laneX, startGO.transform.position.y, trackEndZ);

        // Record start/end, advance cumulative end
        seg.startZ = trackEndZ;
        seg.endZ = trackEndZ + seg.length;
        trackEndZ = seg.endZ;

        active.AddLast(seg);

        // Initial lane state: on floor
        hasFloor = true; hasCeil = false;
        lastKind = seg.kind;

        // IMPORTANT: Do NOT place an obstacle on the start segment per spec
    }

    void Update()
    {
        if (GameManager.Instance && !GameManager.Instance.isRunning) return;
        if (!player) return;

        float playerZ = player.position.z;

        // Build ahead until we have enough world length in front of the player
        while (trackEndZ - playerZ < buildAhead)
            SpawnNext();

        // Recycle segments that are fully behind the player
        while (active.First != null)
        {
            var first = active.First.Value;
            if (playerZ - first.endZ > recycleBehind)
            {
                Destroy(first.gameObject);
                active.RemoveFirst();
            }
            else break;
        }
    }

    // ---------------- Core spawning ----------------

    void SpawnNext()
    {
        // Get allowed kinds by current lane + adjacency rules
        var allowed = GetAllowedKinds();
        if (allowed.Count == 0)
        {
            // Safety: force a solid in the current lane
            allowed.Add(hasFloor ? SegmentKind.Floor : SegmentKind.Ceil);
        }

        // Pick one at random
        var pick = allowed[Random.Range(0, allowed.Count)];
        var prefab = PrefabFor(pick);
        if (!prefab)
        {
            Debug.LogError($"[SegmentSpawner] Missing prefab for {pick}");
            return;
        }

        // Instantiate, place (Y/rot) by type, then snap to cumulative end Z
        var go = Instantiate(prefab, Vector3.zero, Quaternion.identity, transform);
        var seg = go.GetComponent<TerrainSegment>();
        if (!seg)
        {
            Debug.LogError("[SegmentSpawner] Spawned prefab missing TerrainSegment.");
            Destroy(go);
            return;
        }

        PlaceByKind(go, seg); // adjusts Y/rot for Ceil only; Floor/Conn left as-authored

        go.transform.position = new Vector3(laneX, go.transform.position.y, trackEndZ);

        seg.startZ = trackEndZ;
        seg.endZ = trackEndZ + seg.length;
        trackEndZ = seg.endZ;

        active.AddLast(seg);

        // Place ONE random obstacle if this is a Floor or Ceil segment (no gaps/connections)
        if (seg.kind == SegmentKind.Floor || seg.kind == SegmentKind.Ceil)
        {
            TryPlaceRandomObstacle(seg);
        }

        // Update lane state if we placed a connection
        ApplyStateTransition(pick);

        lastKind = pick;
    }

    // Allowed kinds per current lane, with gap/connection adjacency constraint
    List<SegmentKind> GetAllowedKinds()
    {
        var list = new List<SegmentKind>();

        bool lastWasGap =
            lastKind == SegmentKind.Gap_Floor || lastKind == SegmentKind.Gap_Ceil;

        bool lastWasConn =
            lastKind == SegmentKind.Conn_FloorToCeil || lastKind == SegmentKind.Conn_CeilToFloor;

        // After Gap or Connection, next must be a solid (non-gap, non-conn)
        bool mustPickSolid = lastWasGap || lastWasConn;

        if (hasFloor && !hasCeil)
        {
            if (mustPickSolid)
            {
                list.Add(SegmentKind.Floor);
            }
            else
            {
                list.Add(SegmentKind.Floor);
                list.Add(SegmentKind.Gap_Floor);
                list.Add(SegmentKind.Conn_FloorToCeil);
            }
        }
        else if (hasCeil && !hasFloor)
        {
            if (mustPickSolid)
            {
                list.Add(SegmentKind.Ceil);
            }
            else
            {
                list.Add(SegmentKind.Ceil);
                list.Add(SegmentKind.Gap_Ceil);
                list.Add(SegmentKind.Conn_CeilToFloor);
            }
        }
        else
        {
            // Fallback if state ever gets invalid
            list.Add(SegmentKind.Floor);
        }

        return list;
    }

    // Lane state transitions (connections only)
    void ApplyStateTransition(SegmentKind k)
    {
        if (k == SegmentKind.Conn_FloorToCeil)
        {
            hasFloor = false;
            hasCeil = true;
        }
        else if (k == SegmentKind.Conn_CeilToFloor)
        {
            hasCeil = false;
            hasFloor = true;
        }
        // Floor/Ceil/Gap don't change lane state
    }

    // Map kind -> prefab
    GameObject PrefabFor(SegmentKind k)
    {
        return k switch
        {
            SegmentKind.Floor => floorPrefab,
            SegmentKind.Ceil => ceilPrefab,
            SegmentKind.Gap_Floor => gapFloorPrefab,
            SegmentKind.Gap_Ceil => gapCeilPrefab,
            SegmentKind.Conn_FloorToCeil => connF2CPrefab,
            SegmentKind.Conn_CeilToFloor => connC2FPrefab,
            _ => null
        };
    }

    // Only apply changes for segments of type Ceil; others left as-authored
    void PlaceByKind(GameObject go, TerrainSegment seg)
    {
        if (seg.type == SegmentType.Ceil)
        {
            var pos = go.transform.position;
            pos.y = ceilHeight;
            go.transform.position = pos;

            // Flip mesh downward for visual consistency
            go.transform.rotation = Quaternion.Euler(0f, 0f, 180f);
        }
        else
        {
            // Floor or Conn: keep authored rotation; ensure base at y=0
            var pos = go.transform.position;
            pos.y = 0f;
            go.transform.position = pos;
        }
    }

    // ---------------- Obstacle placement ----------------

    void TryPlaceRandomObstacle(TerrainSegment seg)
    {
        // Choose a random obstacle prefab
        GameObject prefab = GetRandomObstaclePrefab();
        if (!prefab) return;

        // Instantiate as a CHILD of the segment so it recycles with it
        var obstacleGO = Instantiate(prefab, seg.transform);

        var data = obstacleGO.GetComponent<ObstacleData>();
        if (!data)
        {
            Debug.LogWarning("[SegmentSpawner] Obstacle prefab missing ObstacleData.");
            Destroy(obstacleGO);
            return;
        }

        float halfSegW = seg.width * 0.5f;
        Vector3 localPos = Vector3.zero;
        Quaternion localRot = Quaternion.identity;

        float minX = -halfSegW + data.width * 0.5f + 2f;
        float maxX = halfSegW - data.width * 0.5f - 2f;


        if (data.type == ObstacleType.Dispenser)
        {
            // Dispenser only on extreme left or right; rotate Y by ±90
            bool placeLeft = (Random.value < 0.5f);
            float sideX = placeLeft ? (-halfSegW + data.width * 0.5f) : (halfSegW - data.width * 0.5f);
            float z = Random.Range(-5f, 5f);

            localPos = new Vector3(sideX, 0f, z);
            // ±90 around Y (use local since parent/ceil may be flipped on Z)
            float yaw = placeLeft ? -90f : 90f;
            localRot = Quaternion.Euler(0f, yaw, 0f);
        }
        else
        {
            // Random X in inner bounds; random Z in inner bounds
            float x = Random.Range(minX, maxX);
            float z = Random.Range(-5f, 5f);
            localPos = new Vector3(x, 0f, z);
            localRot = Quaternion.identity;
        }

        // Apply local transform
        obstacleGO.transform.localPosition = localPos;
        obstacleGO.transform.localRotation = localRot;

        return;
    }

    GameObject GetRandomObstaclePrefab()
    {
        // Uniform random among the five; adjust weights if needed later
        int pick = Random.Range(0, 5);
        return pick switch
        {
            0 => dispenserPrefab,
            1 => longRockPrefab,
            2 => rockPrefab,
            3 => sawPrefab,
            4 => spikesPrefab,
            _ => rockPrefab
        };
    }
}
