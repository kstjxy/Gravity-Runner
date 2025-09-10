using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0f, 3.5f, -8f);
    public float followLerp = 10f;

    void LateUpdate()
    {
        if (!target) return;
        Vector3 desired = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, desired, Time.deltaTime * followLerp);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            Quaternion.LookRotation((target.position + target.forward * 8f) - transform.position, Vector3.up),
            Time.deltaTime * followLerp
        );
    }
}
