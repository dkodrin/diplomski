using UnityEngine;

[RequireComponent(typeof(Camera))]
public class SplitCamera : MonoBehaviour
{
    [Header("Assign this camera’s player")]
    public Transform player;

    [Header("Room Size (world units)")]
    public float roomWidth  = 32f;
    public float roomHeight = 18f;

    [Header("Smooth Follow")]
    [Tooltip("Time (s) to smooth toward target")]
    public float smoothTime = 0.1f;

    private Camera cam;
    private Vector3 velocity = Vector3.zero;
    private float halfCamWidth;
    private float halfCamHeight;

    void Awake()
    {
        cam = GetComponent<Camera>();
        cam.orthographic     = true;
        cam.orthographicSize = roomHeight / 2f;  
        halfCamHeight = cam.orthographicSize;
        halfCamWidth  = halfCamHeight * cam.aspect;
    }

    void LateUpdate()
    {
        if (player == null) return;

        // 1) Compute room indices by rounding (so center-aligned)
        int rx = Mathf.RoundToInt(player.position.x / roomWidth);
        int ry = Mathf.RoundToInt(player.position.y / roomHeight);

        // 2) Compute world‐space center of that room
        float roomCenterX = rx * roomWidth;
        float roomCenterY = ry * roomHeight;

        // 3) Horizontal follow: clamp player‐X so camera stays inside room bounds
        float minX = roomCenterX - roomWidth  / 2f + halfCamWidth;
        float maxX = roomCenterX + roomWidth  / 2f - halfCamWidth;
        float targetX = Mathf.Clamp(player.position.x, minX, maxX);

        // 4) Vertical locked to room center
        float targetY = roomCenterY;

        Vector3 targetPos = new Vector3(targetX, targetY, transform.position.z);

        // 5) Smoothly move there
        transform.position = Vector3.SmoothDamp(
            transform.position,
            targetPos,
            ref velocity,
            smoothTime
        );
    }
}
