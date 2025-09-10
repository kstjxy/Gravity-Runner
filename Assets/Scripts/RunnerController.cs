using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Speeds")]
    public float playerSpeed = 10f;       // forward speed (m/s)
    public float horizontalSpeed = 8f;    // strafe speed (m/s)
    public float jumpForce = 10f;          // jump impulse

    [Header("Left/Right Bounds")]
    public float leftLimit = -5.5f;
    public float rightLimit = 5.5f;

    [Header("Gravity Switch")]
    public bool onCeiling = false;        // current state
    public float gravityAccel = 25f;      // "fall" acceleration
    public float flipCooldown = 0.5f;
    private float flipTimer = 0f;
    private bool hasGrounded = false;

    [Header("Ground Check")]
    public float groundCheckDist = 1.1f;

    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;  // custom gravity
    }

    void Update()
    {
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

        // --- Jump ---
        if ((Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.Space))
            && IsGrounded())
        {
            Vector3 impulse = (onCeiling ? Vector3.down : Vector3.up) * jumpForce;
            Vector3 v = rb.velocity; v.y = 0f; rb.velocity = v;
            rb.AddForce(impulse, ForceMode.VelocityChange);
        }

        // --- Gravity switch ---
        if (Input.GetKeyDown(KeyCode.Space))
            TryFlip();

        if (flipTimer > 0f) flipTimer -= Time.deltaTime;
    }

    void FixedUpdate()
    {
        // Custom gravity
        Vector3 gdir = onCeiling ? Vector3.up : Vector3.down;
        rb.AddForce(gdir * gravityAccel, ForceMode.Acceleration);
    }

    void TryFlip()
    {
        if (flipTimer > 0f) return;
        if (!hasGrounded) return;
        flipTimer = flipCooldown;
        onCeiling = !onCeiling;
        hasGrounded = false;

        // Visual flip
        transform.rotation = transform.rotation * Quaternion.Euler(0f, 0f, 180f);
    }

    bool IsGrounded()
    {
        Vector3 dir = onCeiling ? Vector3.up : Vector3.down;
        bool res = Physics.Raycast(transform.position, dir, groundCheckDist, ~0, QueryTriggerInteraction.Ignore);
        if (res)
            hasGrounded = true;
        return res;
    }
}
