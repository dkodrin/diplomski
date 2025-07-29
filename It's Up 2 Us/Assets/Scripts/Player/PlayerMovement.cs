using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Move & Crouch")]
    public float moveSpeed = 5f;
    [Tooltip("Speed multiplier when crouching on the ground")]
    public float crouchSpeedMultiplier = 0.5f;
    public KeyCode leftKey   = KeyCode.A;
    public KeyCode rightKey  = KeyCode.D;
    public KeyCode crouchKey = KeyCode.S;

    [Header("Jump")]
    public KeyCode jumpKey   = KeyCode.W;
    public float jumpHeight  = 2f;

    [Header("Ground / Roof Check")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.1f;
    [Tooltip("Only real floor tiles here")]
    public LayerMask groundLayer;
    [Tooltip("Other players’ foot‑colliders only")]
    public LayerMask playerLayer;

    [Header("Colliders (assign in Inspector)")]
    public BoxCollider2D bottomCollider;  // 1×1 feet
    public BoxCollider2D topCollider;     // upper body

    [Header("Crouch Transition")]
    public float transitionTime  = 0.1f;
    [Tooltip("Buffer to inset only the top edge when uncrouching")]
    public float uncrouchBuffer  = 0.03f;

    [Header("Ladder Climb")]
    [Tooltip("LayerMask for ladder climb zones")]
    public LayerMask ladderLayer;
    [Tooltip("Vertical speed when climbing")]
    public float climbSpeed = 3f;

    Rigidbody2D rb;
    Vector3     topOrigLocalPos;
    Vector3     topCrouchLocalPos;
    float       crouchSpeed;
    bool        isCrouching   = false;
    bool        jumpReady     = true;  // start able to jump
    bool        isClimbing    = false;
    float       origGravity;

    void Awake()
    {
        rb          = GetComponent<Rigidbody2D>();
        origGravity = rb.gravityScale;

        topOrigLocalPos   = topCollider.transform.localPosition;
        topCrouchLocalPos = bottomCollider.transform.localPosition;

        float dist = Vector3.Distance(topOrigLocalPos, topCrouchLocalPos);
        crouchSpeed = dist / Mathf.Max(0.001f, transitionTime);
    }

    void Update()
    {
        // 1) Crouch
        if (Input.GetKey(crouchKey))
            isCrouching = true;
        else if (isCrouching && CanUncrouch())
            isCrouching = false;

        // slide collider
        Vector3 targetPos = isCrouching ? topCrouchLocalPos : topOrigLocalPos;
        topCollider.transform.localPosition = Vector3.MoveTowards(
            topCollider.transform.localPosition,
            targetPos,
            crouchSpeed * Time.deltaTime
        );

        // 2) Horizontal input
        float h = Input.GetKey(leftKey)  ? -1f
                : Input.GetKey(rightKey) ?  1f
                : 0f;

        // 3) Movement / Climb
        if (isClimbing)
        {
            rb.gravityScale = 0f;
            float v = Input.GetKey(jumpKey)   ?  climbSpeed
                    : Input.GetKey(crouchKey) ? -climbSpeed
                    : 0f;
            float speed = moveSpeed * (isCrouching && IsGrounded() ? crouchSpeedMultiplier : 1f);
            rb.velocity = new Vector2(h * speed, v);
        }
        else
        {
            rb.gravityScale = origGravity;
            float speed = moveSpeed * (isCrouching && IsGrounded() ? crouchSpeedMultiplier : 1f);
            rb.velocity = new Vector2(h * speed, rb.velocity.y);

            // 4) Jump
            if (jumpReady && Input.GetKeyDown(jumpKey))
            {
                float g   = Mathf.Abs(Physics2D.gravity.y * rb.gravityScale);
                float v0  = Mathf.Sqrt(2f * g * jumpHeight);
                rb.velocity = new Vector2(rb.velocity.x, v0);
                jumpReady  = false;
            }
        }
    }

    bool IsGrounded()
    {
        return Physics2D.OverlapCircle(
            groundCheck.position,
            groundCheckRadius,
            groundLayer
        );
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        // only reset jump when our feet land on ground or player
        if (col.otherCollider != bottomCollider) return;

        int mask = 1 << col.collider.gameObject.layer;
        if ((mask & (groundLayer | playerLayer)) == 0) return;

        foreach (var ct in col.contacts)
        {
            if (ct.normal.y > 0.5f)
            {
                jumpReady = true;
                break;
            }
        }
    }

    bool CanUncrouch()
    {
        Vector3 localCenter = topOrigLocalPos + (Vector3)topCollider.offset;
        Vector2 worldCenter = (Vector2)topCollider.transform.parent.TransformPoint(localCenter);
        Vector2 size = Vector2.Scale(topCollider.size, topCollider.transform.lossyScale);
        size.y = Mathf.Max(0f, size.y - uncrouchBuffer);
        Vector2 testCenter = worldCenter + Vector2.down * (uncrouchBuffer * 0.5f);
        return Physics2D.OverlapBox(testCenter, size, 0f, groundLayer) == null;
    }

    void OnTriggerEnter2D(Collider2D col)
    {
        if (((1 << col.gameObject.layer) & ladderLayer) != 0)
            isClimbing = true;
    }

    void OnTriggerExit2D(Collider2D col)
    {
        if (((1 << col.gameObject.layer) & ladderLayer) != 0)
            isClimbing = false;
    }
}
