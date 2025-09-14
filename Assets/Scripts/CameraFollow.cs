using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public RunnerController player;
    public Vector3 offset = new Vector3(0f, 3f, -5f);
    public float followLerp = 10f;

    [Header("Flip Pulse")]
    [Tooltip("How close the camera gets during a flip (1 = no change, 0.6 = 40% closer).")]
    [Range(0.2f, 1f)] public float flipZoomFactor = 0.6f;
    [Tooltip("How long it takes for the camera to return to normal after a flip.")]
    public float flipRecoverTime = 0.35f;

    // internal
    private bool prevOnCeiling = false;
    private float flipBlend = 0f; // 0..1 (1 = fully zoomed for the flip pulse)

    void LateUpdate()
    {
        if (!target || !player) return;
        if (player.deathCooldown > 0) return;

        // Detect flip event
        if (player.onCeiling != prevOnCeiling)
        {
            flipBlend = 1f;
            prevOnCeiling = player.onCeiling;
        }

        // Ease flip pulse back to 0
        if (flipBlend > 0f)
            flipBlend = Mathf.MoveTowards(flipBlend, 0f, Time.deltaTime / Mathf.Max(0.0001f, flipRecoverTime));

        // Offset: invert vertically when ceiling-running
        Vector3 baseOffset = player.onCeiling
            ? new Vector3(offset.x, -Mathf.Abs(offset.y), offset.z)
            : new Vector3(offset.x, Mathf.Abs(offset.y), offset.z);

        // Zoom pulse
        Vector3 closerOffset = baseOffset * flipZoomFactor;
        Vector3 useOffset = Vector3.Lerp(baseOffset, closerOffset, flipBlend);

        // Position follow
        Vector3 desired = target.position + useOffset;
        transform.position = Vector3.Lerp(transform.position, desired, Time.deltaTime * followLerp);

        // Always point straight forward (+Z) with correct up
        Vector3 forward = Vector3.forward;
        Vector3 up = player.onCeiling ? Vector3.down : Vector3.up;

        Quaternion lookRot = Quaternion.LookRotation(forward, up);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, Time.deltaTime * followLerp);
    }
}
