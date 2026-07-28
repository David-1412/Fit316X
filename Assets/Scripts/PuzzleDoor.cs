using System.Collections;
using UnityEngine;

/// <summary>
/// A gate/door that opens only when ALL linked PressurePlates are active.
/// 
/// Setup in Inspector:
///   - Attach to a GameObject with a SpriteRenderer and BoxCollider2D
///   - Add all required PressurePlates to requiredPlates[]
///   - The door will enable/disable its collider and animate open/close
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class PuzzleDoor : MonoBehaviour
{
    [Header("Required Plates")]
    [Tooltip("ALL of these plates must be pressed to open this door")]
    public PressurePlate[] requiredPlates;

    [Header("Visuals")]
    public SpriteRenderer doorRenderer;
    public Color colorClosed = new Color(0.5f, 0.5f, 0.5f, 1f);   // grey
    public Color colorOpen   = new Color(0.2f, 0.9f, 0.2f, 0.4f); // transparent green

    [Header("Animation")]
    public float openDuration  = 0.4f;
    public float closeDuration = 0.3f;

    private BoxCollider2D _col;
    private bool _isOpen = false;
    private Coroutine _animCoroutine;

    // ── Lifecycle ────────────────────────────────────────────────────

    private void Awake()
    {
        _col = GetComponent<BoxCollider2D>();

        if (doorRenderer == null)
            doorRenderer = GetComponent<SpriteRenderer>();

        // Start closed
        SetState(false, instant: true);
    }

    // ── Called by PressurePlate whenever any plate changes ───────────

    public void OnPlateChanged()
    {
        bool allPressed = AllPlatesActive();

        if (allPressed && !_isOpen)  Open();
        else if (!allPressed && _isOpen) Close();
    }

    // ── Open / Close ─────────────────────────────────────────────────

    private void Open()
    {
        _isOpen = true;
        _col.enabled = false;             // remove physics barrier

        if (_animCoroutine != null) StopCoroutine(_animCoroutine);
        _animCoroutine = StartCoroutine(AnimateColor(colorClosed, colorOpen, openDuration));
    }

    private void Close()
    {
        _isOpen = false;
        _col.enabled = true;

        if (_animCoroutine != null) StopCoroutine(_animCoroutine);
        _animCoroutine = StartCoroutine(AnimateColor(colorOpen, colorClosed, closeDuration));
    }

    private void SetState(bool open, bool instant = false)
    {
        _isOpen      = open;
        _col.enabled = !open;

        var target = open ? colorOpen : colorClosed;
        if (doorRenderer != null)
        {
            if (instant)
                doorRenderer.color = target;
            else if (_animCoroutine != null)
                StopCoroutine(_animCoroutine);
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────

    private bool AllPlatesActive()
    {
        if (requiredPlates == null || requiredPlates.Length == 0) return false;
        foreach (var plate in requiredPlates)
            if (plate == null || !plate.IsPressed) return false;
        return true;
    }

    private IEnumerator AnimateColor(Color from, Color to, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            if (doorRenderer != null)
                doorRenderer.color = Color.Lerp(from, to, t / duration);
            yield return null;
        }
        if (doorRenderer != null)
            doorRenderer.color = to;
    }

    // ── Gizmos ───────────────────────────────────────────────────────

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (requiredPlates == null) return;
        Gizmos.color = Color.yellow;
        foreach (var plate in requiredPlates)
            if (plate != null)
                Gizmos.DrawLine(transform.position, plate.transform.position);
    }
#endif
}
