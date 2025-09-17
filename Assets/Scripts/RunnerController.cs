using UnityEngine;

public class RunnerController : MonoBehaviour
{
    [Header("Pickup Prefabs")]
    public GameObject shieldPrefab;
    public GameObject boostPrefab;

    [Header("Movement Speeds")]
    public float playerSpeed = 10f;       // forward speed (m/s)
    public float horizontalSpeed = 8f;    // strafe speed (m/s)
    public float jumpForce = 10f;         // jump impulse
    public float jumpCooldown = 0.2f;
    private float jumpTimer = 0f;

    [Header("Left/Right Bounds")]
    public float leftLimit = -5.5f;
    public float rightLimit = 5.5f;

    [Header("Gravity Switch")]
    public bool onCeiling = false;        // current state
    public float gravityAccel = 25f;      // "fall" acceleration
    public float flipCooldown = 0.5f;
    private float flipTimer = 0f;

    [Header("Ground Check")]
    public float groundCheckDist = 1.1f;

    [Header("Death Bounds")]
    public float deathBelowY = -1f;       // slightly below floor
    public float deathAboveY = 11f;       // slightly above ceil

    [Header("Animation")]
    public Animator animator;             // assign in inspector (runner model)

    // --- Shield state ---
    private bool hasShield = false;
    private GameObject shieldVisual; // instance of shieldPrefab attached to player

    // --- Boost state ---
    [Header("Boost")]
    public float boostDuration = 5f;
    public float boostSpeedMultiplier = 1.5f;
    [Tooltip("Height to hold during boost (middle between floor and ceil).")]
    public float midFlyHeight = 5f;
    [Tooltip("How quickly we move to/from mid height.")]
    public float boostAscendLerp = 10f;

    private bool isBoosting = false;
    private float boostTimer = 0f;
    private float basePlayerSpeed = 0f;
    private GameObject boostVisual;

    private bool fallingFromFlip = false;
    private bool isJumping = false;

    private Rigidbody rb;

    public float deathCooldown = 0;
    public float idleCooldown = 3f;
    private float invulnTimer = 0f;

    void AlignToForward()
    {
        // Always face world +Z. Flip 'up' based on ceiling state.
        Vector3 up = onCeiling ? Vector3.down : Vector3.up;
        transform.rotation = Quaternion.LookRotation(Vector3.forward, up);
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;  // custom gravity

        // Prevent physics from rotating the runner
        rb.constraints |= RigidbodyConstraints.FreezeRotationX
                       | RigidbodyConstraints.FreezeRotationY
                       | RigidbodyConstraints.FreezeRotationZ;

        if (animator)
        {
            animator.SetBool("isJumping", false);
            animator.SetBool("isFalling", false);
        }

        AlignToForward();
        basePlayerSpeed = playerSpeed; // initial baseline
    }

    void Update()
    {
        // If we're in idle countdown, ignore control
        if (idleCooldown > 0f) return;

        // If we're dying, stop all jump/flip input and force velocity down to gravity-only
        if (deathCooldown > 0f)
        {
            rb.velocity = new Vector3(0f, -rb.velocity.y, 0f);
            return;
        }

        if (invulnTimer > 0f)
            invulnTimer -= Time.deltaTime;


        // Stop all control if game ended
        if (GameManager.Instance != null && !GameManager.Instance.isRunning) return;

        // --- Constant forward motion ---
        transform.Translate(Vector3.forward * Time.deltaTime * playerSpeed, Space.World);

        // --- Horizontal input ---
        float xInput = 0f;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) xInput = -1f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) xInput = 1f;

        // Invert when on ceiling only if camera flip is enabled
        bool flipEnabled = Settings.Instance ? Settings.Instance.cameraFlipEnabled : true;
        if (onCeiling && flipEnabled) xInput *= -1f;

        // Apply horizontal movement with clamp
        Vector3 pos = transform.position;
        pos += Vector3.right * xInput * horizontalSpeed * Time.deltaTime;
        pos.x = Mathf.Clamp(pos.x, leftLimit, rightLimit);

        // While boosting, keep us at mid height (smoothly)
        if (isBoosting)
        {
            pos.y = Mathf.Lerp(pos.y, midFlyHeight, Time.deltaTime * boostAscendLerp);
        }
        transform.position = pos;

        // --- Jump (W / UpArrow / Space) ---
        // During boost we disable jumping (we're flying). Gravity switch is still allowed.
        if (!isBoosting &&
            (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.Space)) &&
            IsGrounded() && !fallingFromFlip && !isJumping)
        {
            isJumping = true;
            jumpTimer = jumpCooldown;

            Vector3 impulse = (onCeiling ? Vector3.down : Vector3.up) * jumpForce;
            Vector3 v = rb.velocity; v.y = 0f; rb.velocity = v;
            rb.AddForce(impulse, ForceMode.VelocityChange);

            if (animator) animator.SetBool("isJumping", true);
        }
        else if (isJumping && jumpTimer <= 0f && IsGrounded())
        {
            isJumping = false;
            if (animator) animator.SetBool("isJumping", false);
        }

        // --- Gravity switch (Left Mouse Button) ---
        // Allowed during boost
        if (Input.GetMouseButtonDown(0))
            TryFlip();

        if (flipTimer > 0f) flipTimer -= Time.deltaTime;
        if (jumpTimer > 0f) jumpTimer -= Time.deltaTime;

        // --- Animation state maintenance ---
        if (fallingFromFlip && IsGrounded())
        {
            fallingFromFlip = false;
            if (animator) animator.SetBool("isFalling", false);
        }

        AlignToForward();

        // --- Boost timer ---
        if (isBoosting)
        {
            boostTimer -= Time.deltaTime;
            if (boostTimer <= 0f)
            {
                EndBoost();
            }
        }
    }

    void FixedUpdate()
    {
        if (idleCooldown > 0f)
        {
            idleCooldown -= Time.deltaTime;

            int secs = Mathf.CeilToInt(Mathf.Max(0f, idleCooldown));
            var ui = GameManager.Instance ? GameManager.Instance.gameUI : null;
            if (ui) ui.SetIdleCountdown(secs);

            if (idleCooldown <= 0f)
            {
                if (ui) ui.HideIdleCountdown();
                if (animator) animator.Play("Run");
            }
            return;
        }

        if (deathCooldown > 0)
        {
            deathCooldown -= Time.deltaTime;
            if (deathCooldown <= 0)
            {
                GameManager.Instance?.GameOver();
            }
            return;
        }

        if (GameManager.Instance != null && !GameManager.Instance.isRunning) return;

        // Custom gravity:
        // When boosting, we "fly" at mid height, so we suppress vertical acceleration.
        if (!isBoosting)
        {
            Vector3 gdir = onCeiling ? Vector3.up : Vector3.down;
            rb.AddForce(gdir * gravityAccel, ForceMode.Acceleration);
        }
        else
        {
            // keep vertical velocity neutral while boosting
            var v = rb.velocity; v.y = 0f; rb.velocity = v;
        }
    }

    void LateUpdate()
    {
        if (idleCooldown > 0 || deathCooldown > 0) return;
        if (GameManager.Instance != null && !GameManager.Instance.isRunning) return;

        // Death check by Y bounds
        if (transform.position.y < deathBelowY || transform.position.y > deathAboveY)
        {
            GameManager.Instance?.GameOver();
        }

        AlignToForward();
    }

    void TryFlip()
    {
        if (flipTimer > 0f) return;
        if (fallingFromFlip) return;

        // During boost we allow flips, but we don't add vertical impulse (we’re flying).
        if (!isBoosting)
        {
            Vector3 impulse = (onCeiling ? Vector3.down : Vector3.up) * (jumpForce * 0.25f);
            Vector3 v = rb.velocity; v.y = 0f; rb.velocity = v;
            rb.AddForce(impulse, ForceMode.VelocityChange);
        }

        flipTimer = flipCooldown;
        onCeiling = !onCeiling;

        AlignToForward();

        // Only set falling anim when not boosting (since we hold mid height)
        if (!isBoosting)
        {
            fallingFromFlip = true;
            if (animator) animator.SetBool("isFalling", true);
        }
    }

    bool IsGrounded()
    {
        Vector3 dir = onCeiling ? Vector3.up : Vector3.down;
        return Physics.Raycast(transform.position, dir, groundCheckDist, ~0, QueryTriggerInteraction.Ignore);
    }

    void OnTriggerEnter(Collider other)
    {
        if (deathCooldown > 0) return;
        if (GameManager.Instance != null && !GameManager.Instance.isRunning) return;

        var cdata = other.GetComponent<ColliderData>();
        if (!cdata) return;

        if (cdata.kind == ColliderKind.Obstacle)
        {
            if (invulnTimer > 0f) return;
            // If we have a shield, consume it and disable this obstacle's collider
            if (hasShield)
            {
                ConsumeShield();
                invulnTimer = 0.5f;
                return;
            }

            // No shield -> die
            if (animator) animator.Play("death");
            deathCooldown = 1.5f;
            return;
        }

        if (cdata.kind == ColliderKind.Pickup)
        {
            var pData = other.GetComponent<PickupData>();
            if (!pData) { Destroy(other.gameObject); return; }

            switch (pData.type)
            {
                case PickupType.Shield:
                    TryGainShield();
                    break;

                case PickupType.Boost:
                    TryStartBoost();
                    break;
            }

            Destroy(other.gameObject); // consume pickup
        }
    }

    // -------- Shield helpers --------

    void TryGainShield()
    {
        if (hasShield) return;               // only one shield at a time
        if (!shieldPrefab) return;

        hasShield = true;

        shieldVisual = Instantiate(shieldPrefab, transform);
        shieldVisual.transform.localPosition = Vector3.zero;
        shieldVisual.transform.localRotation = Quaternion.identity;

        var col = shieldVisual.GetComponent<Collider>();
        if (col) col.enabled = false;
    }

    void ConsumeShield()
    {
        hasShield = false;
        if (shieldVisual)
        {
            Destroy(shieldVisual);
            shieldVisual = null;
        }
    }

    // -------- Boost helpers --------

    void TryStartBoost()
    {
        if (!boostPrefab) { StartBoost(); return; } // no visual assigned, still boost

        // Spawn visual and disable its collider if any
        boostVisual = Instantiate(boostPrefab, transform);
        boostVisual.transform.localPosition = Vector3.zero;
        boostVisual.transform.localRotation = Quaternion.identity;

        var col = boostVisual.GetComponent<Collider>();
        if (col) col.enabled = false;

        StartBoost();
    }

    void StartBoost()
    {
        if (!isBoosting)
        {
            // record baseline and apply speed multiplier
            basePlayerSpeed = Mathf.Approximately(basePlayerSpeed, 0f) ? playerSpeed : basePlayerSpeed;
            playerSpeed = basePlayerSpeed * boostSpeedMultiplier;
        }

        isBoosting = true;
        boostTimer = boostDuration;

        // zero vertical velocity so we can smoothly hold mid height
        var v = rb.velocity; v.y = 0f; rb.velocity = v;
    }

    void EndBoost()
    {
        isBoosting = false;
        playerSpeed = basePlayerSpeed; // restore baseline speed

        if (boostVisual)
        {
            Destroy(boostVisual);
            boostVisual = null;
        }
        // Gravity resumes in FixedUpdate; we will naturally land on current plane
    }
}
