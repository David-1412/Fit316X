using UnityEngine;

/// <summary>
/// A pressure plate that activates when the player OR an echo ghost stands on it.
/// Notifies all linked PuzzleDoors when its state changes.
/// 
/// Setup in Inspector:
///   - Attach to a GameObject with BoxCollider2D (isTrigger = true)
///   - Link PuzzleDoors in the linkedDoors array
///   - Optionally assign pressedSprite / releasedSprite for visual feedback
/// </summary>
public class PressurePlate : MonoBehaviour
{
    [Header("Linked Doors")]
    [Tooltip("All PuzzleDoors that need to know when this plate changes state")]
    public PuzzleDoor[] linkedDoors;

    [Header("Visuals")]
    public SpriteRenderer plateRenderer;
    public Color colorReleased = new Color(0.3f, 0.5f, 1f, 1f); // blue - unpressed
    public Color colorPressed  = new Color(0f,   1f,   0.4f, 1f); // green - pressed
    public float pressedScaleY = 0.7f; // squish effect when pressed

    // How many objects are currently standing on this plate
    private int _overlapCount = 0;
    private Vector3 _originalScale;

    public bool IsPressed => _overlapCount > 0;

    // ── Lifecycle ────────────────────────────────────────────────────

    private void Awake()
    {
        if (plateRenderer == null)
            plateRenderer = GetComponent<SpriteRenderer>();

        _originalScale = transform.localScale;
        UpdateVisuals();
    }

    // ── Trigger detection ────────────────────────────────────────────

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsRelevant(other)) return;

        _overlapCount++;
        if (_overlapCount == 1)          // just became active
        {
            UpdateVisuals();
            NotifyDoors();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!IsRelevant(other)) return;

        _overlapCount = Mathf.Max(0, _overlapCount - 1);
        if (_overlapCount == 0)          // just became inactive
        {
            UpdateVisuals();
            NotifyDoors();
        }
    }

    // Accepts: player, echo ghost (tagged "EchoGhost"), and pushable blocks
    private static bool IsRelevant(Collider2D col)
        => col.CompareTag("Player")
        || col.CompareTag("EchoGhost")
        || col.CompareTag("PushableBlock");

    // ── Helpers ──────────────────────────────────────────────────────

    private void NotifyDoors()
    {
        if (linkedDoors == null) return;
        foreach (var door in linkedDoors)
            if (door != null) door.OnPlateChanged();
    }

    private void UpdateVisuals()
    {
        if (plateRenderer != null)
            plateRenderer.color = IsPressed ? colorPressed : colorReleased;

        // Squish when pressed
        var s = _originalScale;
        transform.localScale = IsPressed
            ? new Vector3(s.x, s.y * pressedScaleY, s.z)
            : s;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (linkedDoors == null) return;
        Gizmos.color = Color.cyan;
        foreach (var door in linkedDoors)
            if (door != null)
                Gizmos.DrawLine(transform.position, door.transform.position);
    }
#endif
}
