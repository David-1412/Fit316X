using UnityEngine;

/// <summary>
/// A block the player can push by walking into it.
/// The block slides one tile in the push direction and stops when it hits a wall.
/// It stays where it lands permanently (can be pushed again).
/// 
/// Setup:
///   - Tag this GameObject as "PushableBlock"
///   - Add BoxCollider2D (NOT trigger) so walls stop it
///   - The player must also have a Rigidbody2D
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public class PushableBlock : MonoBehaviour
{
    [Header("Movement")]
    [Tooltip("Size of one tile in world units (should match your Grid cell size)")]
    public float tileSize = 1f;

    [Tooltip("Speed the block slides when pushed")]
    public float slideSpeed = 8f;

    [Header("Layers")]
    [Tooltip("LayerMask of walls/objects that stop the block")]
    public LayerMask blockingLayers;

    private Rigidbody2D _rb;
    private BoxCollider2D _col;
    private bool _isSliding = false;
    private Vector2 _targetPos;

    private void Awake()
    {
        _rb  = GetComponent<Rigidbody2D>();
        _col = GetComponent<BoxCollider2D>();

        _rb.bodyType     = RigidbodyType2D.Kinematic;
        _rb.gravityScale = 0f;

        // Snap to nearest tile grid on start
        SnapToGrid();
        _targetPos = _rb.position;

        // Make sure tag is set
        if (!gameObject.CompareTag("PushableBlock"))
            gameObject.tag = "PushableBlock";
    }

    private void FixedUpdate()
    {
        if (!_isSliding) return;

        // Move toward target
        Vector2 next = Vector2.MoveTowards(_rb.position, _targetPos, slideSpeed * Time.fixedDeltaTime);
        _rb.MovePosition(next);

        if (Vector2.Distance(_rb.position, _targetPos) < 0.01f)
        {
            _rb.MovePosition(_targetPos);
            _isSliding = false;
        }
    }

    // ── Called when the player walks into the block ──────────────────

    private void OnCollisionEnter2D(Collision2D col)
    {
        if (_isSliding) return;
        if (!col.gameObject.CompareTag("Player")) return;

        // Determine push direction from contact normal (reversed = push direction)
        Vector2 pushDir = -col.contacts[0].normal;
        pushDir = RoundDir(pushDir);

        TryPush(pushDir);
    }

    // ── Try to slide the block one tile in pushDir ───────────────────

    private void TryPush(Vector2 dir)
    {
        Vector2 from  = _rb.position;
        Vector2 dest  = from + dir * tileSize;

        // Raycast to check if destination is free
        Vector2 size  = _col.size * 0.9f;                   // slightly smaller to avoid edge sticking
        RaycastHit2D hit = Physics2D.BoxCast(
            from, size, 0f, dir,
            tileSize,
            blockingLayers);

        if (hit.collider != null) return; // blocked — can't push

        // Slide!
        _targetPos = dest;
        _isSliding = true;
    }

    // ── Helpers ──────────────────────────────────────────────────────

    private void SnapToGrid()
    {
        var pos = transform.position;
        transform.position = new Vector3(
            Mathf.Round(pos.x / tileSize) * tileSize,
            Mathf.Round(pos.y / tileSize) * tileSize,
            pos.z);
    }

    /// Round a direction vector to the nearest cardinal direction
    private static Vector2 RoundDir(Vector2 dir)
    {
        if (Mathf.Abs(dir.x) >= Mathf.Abs(dir.y))
            return new Vector2(Mathf.Sign(dir.x), 0f);
        else
            return new Vector2(0f, Mathf.Sign(dir.y));
    }
}
