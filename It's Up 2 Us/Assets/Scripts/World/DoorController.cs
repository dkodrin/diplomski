using UnityEngine;

public class DoorController : MonoBehaviour
{
    [Tooltip("All PressurePlate (or Combined) components that must be fully ON")]
    public PressurePlate[] plates;

    [Tooltip("How far to slide up when opening")]
    public float openHeight = 2f;
    [Tooltip("Speed at which the door slides")]
    public float slideSpeed = 2f;

    private Vector3 _closedPos;
    private Vector3 _openPos;
    private bool    _opened = false;

    void Awake()
    {
        // cache positions
        _closedPos = transform.position;
        _openPos   = _closedPos + Vector3.up * openHeight;

        // subscribe to every plate's state‐change event
        foreach (var plate in plates)
        {
            plate.onStateChanged.AddListener(OnPlateStateChanged);
        }
    }

    void OnPlateStateChanged(int state)
    {
        if (_opened) return;  // already open

        // only open when *all* plates report LastState == 2 (fully ON)
        foreach (var plate in plates)
        {
            if (plate.LastState != 2)
                return;
        }

        _opened = true;
    }

    void Update()
    {
        if (_opened)
        {
            // slide toward the open position
            transform.position = Vector3.MoveTowards(
                transform.position,
                _openPos,
                slideSpeed * Time.deltaTime
            );
        }
    }
}
