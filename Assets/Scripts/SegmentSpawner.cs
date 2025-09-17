using System.Collections.Generic;
using UnityEngine;

public class SegmentSpawner : MonoBehaviour
{
    [Header("Refs")]
    public Transform player;
    public GameObject startFloorPrefab;

    [Header("Segment Prefabs")]
    public GameObject floorPrefab;
    public GameObject ceilPrefab;
    public GameObject gapFloorPrefab;
    public GameObject gapCeilPrefab;
    public GameObject connF2CPrefab;
    public GameObject connC2FPrefab;

    [Header("Obstacle Prefabs")]
    public GameObject dispenserPrefab;
    public GameObject longRockPrefab;
    public GameObject rockPrefab;
    public GameObject sawPrefab;
    public GameObject spikesPrefab;

    [Header("Pick-up Prefabs")]
    public GameObject shieldPrefab;
    public GameObject boostPrefab;

    [Header("Placement")]
    public float ceilHeight = 10f;
    public float laneX = 0f;

    [Header("Streaming Tuning")]
    public float buildAhead = 80f;
    public float recycleBehind = 40f;

    // ---- NEW: Pickup spawn tuning ----
    [Header("Pickup Spawn Rules")]
    [Tooltip("Base chance per placeable segment.")]
    [Range(0f, 1f)] public float pickupChance = 0.10f;      // 10%
    [Tooltip("Min placeable segments to wait after placing a pickup.")]
    public int pickupCooldownSegments = 5;
    [Tooltip("Force place if this many placeable segments passed with no pickup.")]
    public int pickupForcePlaceSegments = 10;

    [Header("Pickup Footprint Defaults (local X/Z)")]
    public float shieldWidth = 1.5f;
    public float shieldLength = 1.5f;
    public float boostWidth = 1.5f;
    public float boostLength = 2.0f;

    // ---- Runtime state ----
    public bool hasFloor = true;
    public bool hasCeil = false;

    private float trackEndZ = 0f;
    private readonly LinkedList<TerrainSegment> active = new();
    private SegmentKind? lastKind = null;

    // NEW: pickup placement accounting
    private int placeableSinceLastPickup = 9999; // start large so we're not blocked initially

    void Start()
    {
        var startGO = Instantiate(startFloorPrefab, Vector3.zero, Quaternion.identity, transform);
        var seg = startGO.GetComponent<TerrainSegment>();
        if (!seg)
        {
            Debug.LogError("[SegmentSpawner] Start prefab missing TerrainSegment.");
            return;
        }

        PlaceByKind(startGO, seg);
        startGO.transform.position = new Vector3(laneX, startGO.transform.position.y, trackEndZ);

        seg.startZ = trackEndZ;
        seg.endZ = trackEndZ + seg.length;
        trackEndZ = seg.endZ;

        active.AddLast(seg);

        hasFloor = true; hasCeil = false;
        lastKind = seg.kind;

        // Do NOT place obstacle or pickup on the start segment per spec
        placeableSinceLastPickup = pickupCooldownSegments; // allow pickup soon after start
    }

    void Update()
    {
        if (GameManager.Instance && !GameManager.Instance.isRunning) return;
        if (!player) return;

        float playerZ = player.position.z;

        while (trackEndZ - playerZ < buildAhead)
            SpawnNext();

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

    void SpawnNext()
    {
        var allowed = GetAllowedKinds();
        if (allowed.Count == 0)
            allowed.Add(hasFloor ? SegmentKind.Floor : SegmentKind.Ceil);

        var pick = allowed[Random.Range(0, allowed.Count)];
        var prefab = PrefabFor(pick);
        if (!prefab)
        {
            Debug.LogError($"[SegmentSpawner] Missing prefab for {pick}");
            return;
        }

        var go = Instantiate(prefab, Vector3.zero, Quaternion.identity, transform);
        var seg = go.GetComponent<TerrainSegment>();
        if (!seg)
        {
            Debug.LogError("[SegmentSpawner] Spawned prefab missing TerrainSegment.");
            Destroy(go);
            return;
        }

        PlaceByKind(go, seg);
        go.transform.position = new Vector3(laneX, go.transform.position.y, trackEndZ);

        seg.startZ = trackEndZ;
        seg.endZ = trackEndZ + seg.length;
        trackEndZ = seg.endZ;

        active.AddLast(seg);

        // ---- Place Obstacle + maybe Pickup on placeable segments ----
        if (seg.kind == SegmentKind.Floor || seg.kind == SegmentKind.Ceil)
        {
            // Obstacle first
            TryPlaceRandomObstacle(seg);

            // Update counter for placeable segment encountered
            placeableSinceLastPickup++;

            // Pickup rules
            bool placedPickup = false;
            if (CanPlacePickup())
            {
                placedPickup = PlaceRandomPickup(seg);
            }

            if (placedPickup)
                placeableSinceLastPickup = 0;       // reset cooldown counter
            else
                placeableSinceLastPickup = Mathf.Min(placeableSinceLastPickup, pickupForcePlaceSegments + 1); // cap
        }

        ApplyStateTransition(pick);
        lastKind = pick;
    }

    // ----- Allowed kinds, transitions, prefab map are unchanged -----

    List<SegmentKind> GetAllowedKinds()
    {
        var list = new List<SegmentKind>();

        bool lastWasGap =
            lastKind == SegmentKind.Gap_Floor || lastKind == SegmentKind.Gap_Ceil;

        bool lastWasConn =
            lastKind == SegmentKind.Conn_FloorToCeil || lastKind == SegmentKind.Conn_CeilToFloor;

        bool mustPickSolid = lastWasGap || lastWasConn;

        if (hasFloor && !hasCeil)
        {
            if (mustPickSolid) list.Add(SegmentKind.Floor);
            else { list.Add(SegmentKind.Floor); list.Add(SegmentKind.Gap_Floor); list.Add(SegmentKind.Conn_FloorToCeil); }
        }
        else if (hasCeil && !hasFloor)
        {
            if (mustPickSolid) list.Add(SegmentKind.Ceil);
            else { list.Add(SegmentKind.Ceil); list.Add(SegmentKind.Gap_Ceil); list.Add(SegmentKind.Conn_CeilToFloor); }
        }
        else
        {
            list.Add(SegmentKind.Floor);
        }

        return list;
    }

    void ApplyStateTransition(SegmentKind k)
    {
        if (k == SegmentKind.Conn_FloorToCeil) { hasFloor = false; hasCeil = true; }
        else if (k == SegmentKind.Conn_CeilToFloor) { hasCeil = false; hasFloor = true; }
    }

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

    void PlaceByKind(GameObject go, TerrainSegment seg)
    {
        if (seg.type == SegmentType.Ceil)
        {
            var pos = go.transform.position;
            pos.y = ceilHeight;
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(0f, 0f, 180f);
        }
        else
        {
            var pos = go.transform.position;
            pos.y = 0f;
            go.transform.position = pos;
        }
    }

    // ---------------- Obstacle placement (unchanged except bounds calc comments) ----------------

    void TryPlaceRandomObstacle(TerrainSegment seg)
    {
        GameObject prefab = GetRandomObstaclePrefab();
        if (!prefab) return;

        var obstacleGO = Instantiate(prefab, seg.transform);
        var data = obstacleGO.GetComponent<ObstacleData>();
        if (!data)
        {
            Debug.LogWarning("[SegmentSpawner] Obstacle prefab missing ObstacleData.");
            Destroy(obstacleGO);
            return;
        }

        float halfSegW = seg.width * 0.5f;

        // X bounds (inner) to keep on road; small margin from edges
        float minX = -halfSegW + data.width * 0.5f + 2f;
        float maxX = halfSegW - data.width * 0.5f - 2f;

        // Z bounds: assume segment local Z is approx centered; keep within [-seg.length/2, +seg.length/2]
        float halfSegL = seg.length * 0.5f;
        float minZ = -halfSegL + data.length * 0.5f;
        float maxZ = halfSegL - data.length * 0.5f;

        Vector3 localPos = Vector3.zero;
        Quaternion localRot = Quaternion.identity;

        if (data.type == ObstacleType.Dispenser)
        {
            bool placeLeft = (Random.value < 0.5f);
            float sideX = placeLeft ? (-halfSegW + data.width * 0.5f) : (halfSegW - data.width * 0.5f);
            float z = Random.Range(minZ, maxZ);

            localPos = new Vector3(sideX, 0f, z);
            float yaw = placeLeft ? -90f : 90f;
            localRot = Quaternion.Euler(0f, yaw, 0f);
        }
        else
        {
            float x = Random.Range(minX, maxX);
            float z = Random.Range(minZ, maxZ);
            localPos = new Vector3(x, 0f, z);
            localRot = Quaternion.identity;
        }

        obstacleGO.transform.localPosition = localPos;
        obstacleGO.transform.localRotation = localRot;
    }

    GameObject GetRandomObstaclePrefab()
    {
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

    // -------------------- PICKUP LOGIC --------------------

    bool CanPlacePickup()
    {
        // Only consider when we've met the cooldown
        if (placeableSinceLastPickup < pickupCooldownSegments)
            return false;

        // Force place after N without pickup
        if (placeableSinceLastPickup >= pickupForcePlaceSegments)
            return true;

        // Otherwise random 10% chance
        return Random.value < pickupChance;
    }

    bool PlaceRandomPickup(TerrainSegment seg)
    {
        // Decide type: 1/3 Boost, 2/3 Shield
        bool pickBoost = (Random.value < (1f / 3f));
        GameObject prefab = pickBoost ? boostPrefab : shieldPrefab;
        if (!prefab) return false;

        // Footprint for overlap tests (local X/Z half sizes)
        float pw = pickBoost ? boostWidth : shieldWidth;
        float pl = pickBoost ? boostLength : shieldLength;
        float hx = pw * 0.5f;
        float hz = pl * 0.5f;

        // Segment bounds
        float halfSegW = seg.width * 0.5f;
        float halfSegL = seg.length * 0.5f;

        float minX = -halfSegW + hx + 0.5f;  // small margin
        float maxX = halfSegW - hx - 0.5f;
        float minZ = -halfSegL + hz + 0.5f;
        float maxZ = halfSegL - hz - 0.5f;

        if (minX > maxX || minZ > maxZ) return false; // segment too small

        // Gather obstacle rectangles (local space)
        var obsRects = CollectObstacleRects(seg);

        // Try a few random locations to avoid obstacle overlap
        const int kTries = 12;
        for (int attempt = 0; attempt < kTries; attempt++)
        {
            float x = Random.Range(minX, maxX);
            float z = Random.Range(minZ, maxZ);

            if (!OverlapsAny(new RectXZ(new Vector2(x, z), new Vector2(hx, hz)), obsRects))
            {
                // Place it
                var go = Instantiate(prefab, seg.transform);
                go.transform.localPosition = new Vector3(x, 0f, z);
                go.transform.localRotation = Quaternion.identity;

                // Mark as pickup in gameplay (so player can trigger)
                var cd = go.GetComponent<ColliderData>();
                if (!cd) cd = go.AddComponent<ColliderData>();
                cd.kind = ColliderKind.Pickup;

                return true;
            }
        }

        // Failed to find non-overlapping spot
        return false;
    }

    // ---- Simple AABB helper types in local X/Z ----
    struct RectXZ
    {
        public Vector2 c; // center (x,z)
        public Vector2 h; // half-sizes (hx,hz)
        public RectXZ(Vector2 center, Vector2 half) { c = center; h = half; }
    }

    List<RectXZ> CollectObstacleRects(TerrainSegment seg)
    {
        var list = new List<RectXZ>();
        var obstacles = seg.GetComponentsInChildren<ObstacleData>();
        foreach (var o in obstacles)
        {
            // local center and sizes
            Vector3 lp = o.transform.localPosition;
            float hx = o.width * 0.5f;
            float hz = o.length * 0.5f;
            list.Add(new RectXZ(new Vector2(lp.x, lp.z), new Vector2(hx, hz)));
        }
        return list;
    }

    bool OverlapsAny(RectXZ a, List<RectXZ> others)
    {
        for (int i = 0; i < others.Count; i++)
        {
            var b = others[i];
            // AABB overlap test in XZ
            bool overlapX = Mathf.Abs(a.c.x - b.c.x) <= (a.h.x + b.h.x);
            bool overlapZ = Mathf.Abs(a.c.y - b.c.y) <= (a.h.y + b.h.y);
            if (overlapX && overlapZ) return true;
        }
        return false;
    }
}
