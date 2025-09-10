using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Speeds")]
    public float playerSpeed = 10f;       // forward speed
    public float horizontalSpeed = 5f;    // left/right strafe
    public float jumpForce = 7f;          // jump impulse

    [Header("Left/Right Bounds")]
    public float leftLimit = -5.5f;
    public float rightLimit = 5.5f;

    [Header("Ground Check")]
    public float groundCheckDist = 1f;

    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        // --- Constant forward motion ---
        transform.Translate(Vector3.forward * Time.deltaTime * playerSpeed, Space.World);

        // --- Left movement ---
        if ((Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) &&
            transform.position.x > leftLimit)
        {
            transform.Translate(Vector3.left * Time.deltaTime * horizontalSpeed, Space.World);
        }

        // --- Right movement ---
        if ((Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) &&
            transform.position.x < rightLimit)
        {
            transform.Translate(Vector3.right * Time.deltaTime * horizontalSpeed, Space.World);
        }

        // --- Jump ---
        if ((Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.Space))
            && IsGrounded())
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.VelocityChange);
        }
    }

    bool IsGrounded()
    {
        Debug.DrawRay(transform.position, Vector3.down * groundCheckDist, Color.green);
        // No mask: just raycast down against any collider
        return Physics.Raycast(transform.position, Vector3.down, groundCheckDist, ~0, QueryTriggerInteraction.Ignore);
    }
}
