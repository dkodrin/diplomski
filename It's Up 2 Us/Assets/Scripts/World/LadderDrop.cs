using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(SpriteRenderer), typeof(Rigidbody2D))]
public class LadderDrop : MonoBehaviour
{
    [Header("Plate & Drop")]
    [Tooltip("The PressurePlate that triggers the ladder to drop")]
    public PressurePlate plate;
    [Tooltip("How many world units (tiles) the ladder should drop downward")]
    public float dropDistance = 8f;
    [Tooltip("Speed at which the ladder slides down")]
    public float slideSpeed = 2f;

    [Header("Ladder Dimensions")]
    [Tooltip("Visual height of the ladder in world units")]
    public float ladderHeight = 8f;

    [Header("Climbing")]
    [Tooltip("Child BoxCollider2D (Is Trigger) in front of the ladder – pivoted & offset at center")]
    public BoxCollider2D climbZone;
    [Tooltip("Vertical speed when climbing")]
    public float climbSpeed = 3f;

    private Vector3 _hiddenPos;
    private Vector3 _floorPos;
    private Vector3 _targetPos;
    private bool _dropped = false;
    private List<Rigidbody2D> _climbers = new List<Rigidbody2D>();

    void Awake()
    {
        // 1) Make kinematic so its trigger works reliably
        var body = GetComponent<Rigidbody2D>();
        body.bodyType     = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;

        // 2) Tile the sprite over the configured ladderHeight
        var sr = GetComponent<SpriteRenderer>();
        sr.drawMode = SpriteDrawMode.Tiled;
        float width = sr.sprite.bounds.size.x;
        sr.size = new Vector2(width, ladderHeight);

        // 3) Resize the climbZone to match that height
        //    Pivot is at center, so offset stays at (0,0)
        climbZone.size   = new Vector2(width, ladderHeight);
        climbZone.offset = Vector2.zero;

        // 4) Record start (hidden) & end (floor) positions
        _hiddenPos = transform.position;
        _floorPos  = _hiddenPos - Vector3.up * dropDistance;
        _targetPos = _hiddenPos;

        // 5) Subscribe to the plate’s state-change event
        plate.onStateChanged.AddListener(OnPlateStateChanged);
    }

    void OnPlateStateChanged(int state)
    {
        // Drop once when the plate becomes fully "On" (state == 2)
        if (!_dropped && state == 2)
        {
            _dropped   = true;
            _targetPos = _floorPos;
        }
    }

    void Update()
    {
        // Slide ladder toward its target position each frame
        transform.position = Vector3.MoveTowards(
            transform.position,
            _targetPos,
            slideSpeed * Time.deltaTime
        );
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!_dropped) return;
        var rb = other.attachedRigidbody;
        if (rb == null) return;
        if (rb.GetComponent<PlayerMovement>() == null) return;

        if (!_climbers.Contains(rb))
        {
            _climbers.Add(rb);
            rb.gravityScale = 0f;  // disable gravity while climbing
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        var rb = other.attachedRigidbody;
        if (rb == null) return;
        if (rb.GetComponent<PlayerMovement>() == null) return;

        rb.gravityScale = 1f;   // restore gravity
        _climbers.Remove(rb);
    }

    void FixedUpdate()
    {
        if (!_dropped) return;

        foreach (var rb in _climbers)
        {
            var pm = rb.GetComponent<PlayerMovement>();
            float vy = 0f;
            if (Input.GetKey(pm.jumpKey))        vy = climbSpeed;
            else if (Input.GetKey(pm.crouchKey)) vy = -climbSpeed;

            rb.velocity = new Vector2(rb.velocity.x, vy);
        }
    }
}
