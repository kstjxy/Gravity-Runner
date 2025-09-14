using UnityEngine;

public class RunnerController : MonoBehaviour
{
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

    private bool fallingFromFlip = false; // true after flip until grounded again
    private bool isJumping = false;

    private Rigidbody rb;

    // --- Helpers ---
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
    }

    void Update()
    {
        // Stop all control if game ended
        if (GameManager.Instance != null && !GameManager.Instance.isRunning) return;

        // --- Constant forward motion ---
        transform.Translate(Vector3.forward * Time.deltaTime * playerSpeed, Space.World);

        // --- Horizontal input ---
        float xInput = 0f;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) xInput = -1f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) xInput = 1f;

        // Invert when on ceiling
        if (onCeiling) xInput *= -1f;

        // Apply horizontal movement with clamp
        Vector3 pos = transform.position;
        pos += Vector3.right * xInput * horizontalSpeed * Time.deltaTime;
        pos.x = Mathf.Clamp(pos.x, leftLimit, rightLimit);
        transform.position = pos;

        // --- Jump (W / UpArrow / Space) ---
        if ((Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.Space))
            && IsGrounded() && !fallingFromFlip && !isJumping)
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
        if (Input.GetMouseButtonDown(0))
            TryFlip();

        if (flipTimer > 0f) flipTimer -= Time.deltaTime;
        if (jumpTimer > 0f) jumpTimer -= Time.deltaTime;

        // --- Animation state maintenance ---
        // If we finished a flip and got grounded again, return to Run
        if (fallingFromFlip && IsGrounded())
        {
            fallingFromFlip = false;
            if (animator) animator.SetBool("isFalling", false);
        }

        // Keep character facing forward every frame
        AlignToForward();
    }

    void FixedUpdate()
    {
        if (GameManager.Instance != null && !GameManager.Instance.isRunning) return;

        // Custom gravity
        Vector3 gdir = onCeiling ? Vector3.up : Vector3.down;
        rb.AddForce(gdir * gravityAccel, ForceMode.Acceleration);
    }

    void LateUpdate()
    {
        if (GameManager.Instance != null && !GameManager.Instance.isRunning) return;

        // Death check by Y bounds
        if (transform.position.y < deathBelowY || transform.position.y > deathAboveY)
        {
            if (GameManager.Instance != null) GameManager.Instance.GameOver();
        }

        // Final safeguard for rotation after all updates
        AlignToForward();
    }

    void TryFlip()
    {
        if (flipTimer > 0f) return;
        if (fallingFromFlip) return;

        // Small impulse away from the current plane to feel responsive
        Vector3 impulse = (onCeiling ? Vector3.down : Vector3.up) * (jumpForce * 0.25f);
        Vector3 v = rb.velocity; v.y = 0f; rb.velocity = v;
        rb.AddForce(impulse, ForceMode.VelocityChange);

        flipTimer = flipCooldown;
        onCeiling = !onCeiling;

        // DO NOT multiply rotation; just realign to forward with flipped up
        AlignToForward();

        // Enter falling animation until grounded again
        fallingFromFlip = true;
        if (animator) animator.SetBool("isFalling", true);
    }

    bool IsGrounded()
    {
        Vector3 dir = onCeiling ? Vector3.up : Vector3.down;
        return Physics.Raycast(transform.position, dir, groundCheckDist, ~0, QueryTriggerInteraction.Ignore);
    }

    void OnTriggerEnter(Collider other)
    {
        if (GameManager.Instance != null && !GameManager.Instance.isRunning) return;

        var cdata = other.GetComponent<ColliderData>();
        if (cdata == null) return;

        switch (cdata.kind)
        {
            case ColliderKind.Obstacle:
                GameManager.Instance?.GameOver();
                break;

            case ColliderKind.Pickup:
                // TODO: handle pickup (increase score, shield, etc.)
                Destroy(other.gameObject); // simple consume
                break;
        }
    }

}
