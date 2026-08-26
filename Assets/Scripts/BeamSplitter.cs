using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// BeamSplitter — receives a beam and emits it in two directions simultaneously.
///
/// Three types (cycle with E):
///   LeftRight   — splits perpendicular left AND right relative to the incoming beam
///   LeftStraight  — emits left AND continues straight through
///   RightStraight — emits right AND continues straight through
///
/// "Left" and "Right" are relative to the beam's travel direction, not world space.
/// Example: a beam moving RIGHT will split into UP (left) and DOWN (right).
///
/// Setup:
///   • Add a Collider2D (isTrigger = false) so BeamMachine raycasts hit it.
///   • Optionally assign bodyRenderer — it will change colour per type for easy identification.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class BeamSplitter : MonoBehaviour, IInteractable
{
    public enum SplitterType
    {
        LeftRight,      // ← beam →  splits to both sides
        LeftStraight,   // ← beam + straight through
        RightStraight   // → beam + straight through
    }

    [Header("Settings")]
    public SplitterType splitterType = SplitterType.LeftRight;

    [Header("Visuals")]
    [Tooltip("Main body SpriteRenderer — tinted per type so players can tell them apart.")]
    public SpriteRenderer bodyRenderer;

    // Colours per type
    private static readonly Color ColorLeftRight    = new Color(0.7f, 0.3f, 1.0f); // purple
    private static readonly Color ColorLeftStraight = new Color(1.0f, 0.5f, 0.0f); // orange
    private static readonly Color ColorRightStraight= new Color(0.2f, 0.9f, 0.4f); // green

    // ── Unity ─────────────────────────────────────────────────────────────
    private void Awake()
    {
        if (bodyRenderer == null)
            bodyRenderer = GetComponent<SpriteRenderer>();

        RefreshVisual();
    }

    // ── Public API — called by BeamMachine ────────────────────────────────
    /// <summary>
    /// Returns the list of directions the beam should continue in after hitting this splitter.
    /// incomingDir is the normalised direction the beam was travelling when it hit.
    /// </summary>
    public List<Vector2> GetOutputDirections(Vector2 incomingDir)
    {
        incomingDir.Normalize();

        // Relative to travel direction:
        //   Left  = rotate 90° counter-clockwise = (-y,  x)
        //   Right = rotate 90° clockwise          = ( y, -x)
        Vector2 leftDir  = new Vector2(-incomingDir.y,  incomingDir.x);
        Vector2 rightDir = new Vector2( incomingDir.y, -incomingDir.x);

        switch (splitterType)
        {
            case SplitterType.LeftRight:
                return new List<Vector2> { leftDir, rightDir };

            case SplitterType.LeftStraight:
                return new List<Vector2> { leftDir, incomingDir };

            case SplitterType.RightStraight:
                return new List<Vector2> { rightDir, incomingDir };

            default:
                return new List<Vector2> { leftDir, rightDir };
        }
    }

    // ── IInteractable — press E to cycle type ─────────────────────────────
    public bool CanInteract() => true;

    public void Interact()
    {
        int next = ((int)splitterType + 1) % System.Enum.GetValues(typeof(SplitterType)).Length;
        splitterType = (SplitterType)next;
        RefreshVisual();
        SoundEffectManager.Play("PickUp");
        Debug.Log($"[BeamSplitter] {name} switched to {splitterType}");
    }

    // ── Visuals ───────────────────────────────────────────────────────────
    private void RefreshVisual()
    {
        if (bodyRenderer == null) return;

        bodyRenderer.color = splitterType switch
        {
            SplitterType.LeftRight     => ColorLeftRight,
            SplitterType.LeftStraight  => ColorLeftStraight,
            SplitterType.RightStraight => ColorRightStraight,
            _                          => Color.white
        };
    }

    private void OnDrawGizmosSelected()
    {
        // Draw a label in the scene view showing the current type
        Gizmos.color = Color.white;
        Vector3 pos  = transform.position;
        Gizmos.DrawWireCube(pos, new Vector3(0.9f, 0.9f, 0));
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Update colour immediately when changed in Inspector
        RefreshVisual();
    }
#endif
}
