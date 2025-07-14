using UnityEngine;

[RequireComponent(typeof(Camera))]
public class DualCamera : MonoBehaviour
{
    [Header("Player References")]
    public Transform player1;
    public Transform player2;

    [Header("Room Settings")]
    public float roomWidth  = 32f;
    public float roomHeight = 18f;

    [Header("Smoothing")]
    public float smoothTime = 0.1f;

    private Camera   cam;
    private Vector3  velocity = Vector3.zero;

    void Awake()
    {
        cam = GetComponent<Camera>();
        cam.orthographic     = true;
        cam.orthographicSize = roomHeight / 2f;
    }

    void LateUpdate()
    {
        // 1) midpoint between the two players
        Vector3 mid = (player1.position + player2.position) * 0.5f;

        // 2) determine which room index we're closest to
        int roomX = Mathf.RoundToInt(mid.x / roomWidth);
        int roomY = Mathf.RoundToInt(mid.y / roomHeight);

        // 3) compute the exact center of that room
        float tx = roomX * roomWidth;
        float ty = roomY * roomHeight;
        Vector3 targetPos = new Vector3(tx, ty, transform.position.z);

        // 4) smoothly move camera there
        transform.position = Vector3.SmoothDamp(
            transform.position,
            targetPos,
            ref velocity,
            smoothTime
        );
    }
}
