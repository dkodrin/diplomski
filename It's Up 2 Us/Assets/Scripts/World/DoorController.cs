using UnityEngine;

public class DoorController : MonoBehaviour
{
    [Tooltip("All PressurePlate components that must be fully ON to open the door")]
    public PressurePlate[] plates;

    [Tooltip("How far to slide up when opening")]
    public float openHeight = 2f;
    [Tooltip("Speed at which the door slides")]
    public float slideSpeed = 2f;

    [Tooltip("If true: door stays open and plates lock permanently once triggered.\n" +
             "If false: door closes again as soon as any plate is released.")]
    public bool stayOpen = false;

    private Vector3 _closedPos;
    private Vector3 _openPos;
    private bool    _permanentlyOpen = false;

    void Awake()
    {
        _closedPos = transform.position;
        _openPos   = _closedPos + Vector3.up * openHeight;

        foreach (var plate in plates)
            plate.onStateChanged.AddListener(OnPlateStateChanged);
    }

    void OnPlateStateChanged(int state)
    {
        if (_permanentlyOpen) return;

        // Check if all plates are fully ON right now
        bool allOn = true;
        foreach (var plate in plates)
        {
            if (plate.LastState != 2)
            {
                allOn = false;
                break;
            }
        }

        if (allOn && stayOpen)
        {
            // Lock all plates permanently and never close again
            _permanentlyOpen = true;
            foreach (var plate in plates)
                plate.Lock();
        }
    }

    void Update()
    {
        if (_permanentlyOpen)
        {
            // Slide to open and stay there
            transform.position = Vector3.MoveTowards(
                transform.position,
                _openPos,
                slideSpeed * Time.deltaTime
            );
            return;
        }

        // Temporary mode: open while all plates are ON, close when any is released
        bool allOn = true;
        foreach (var plate in plates)
        {
            if (plate.LastState != 2)
            {
                allOn = false;
                break;
            }
        }

        Vector3 target = allOn ? _openPos : _closedPos;
        transform.position = Vector3.MoveTowards(
            transform.position,
            target,
            slideSpeed * Time.deltaTime
        );
    }
}