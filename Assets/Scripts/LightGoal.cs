using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// LightGoal — when a beam of the correct colour hits this object it activates.
/// Place on any GameObject with a Collider2D (isTrigger = false).
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class LightGoal : MonoBehaviour
{
    [Header("Puzzle Settings")]
    [Tooltip("The beam colour required to activate this goal.")]
    public Color requiredColor = Color.red;

    [Tooltip("How close the beam colour must be to requiredColor (0–1). Lower = stricter.")]
    [Range(0.05f, 1f)]
    public float colorTolerance = 0.35f;

    [Header("Visuals")]
    public SpriteRenderer goalRenderer;
    public Color inactiveColor = new Color(0.25f, 0.25f, 0.25f, 1f);
    public Color activeColor   = Color.white; // set to requiredColor in Awake

    // ── Static registry — RuntimeInitializeOnLoadMethod keeps it clean ────
    private static List<LightGoal> allGoals = new List<LightGoal>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void DomainReload()
    {
        allGoals = new List<LightGoal>();
    }

    /// <summary>Called by BeamMachine at the start of every Update to clear last-frame state.</summary>
    public static void ResetAll()
    {
        for (int i = allGoals.Count - 1; i >= 0; i--)
        {
            if (allGoals[i] == null) { allGoals.RemoveAt(i); continue; }
            allGoals[i].SetActivated(false);
        }
    }

    // ── State ─────────────────────────────────────────────────────────────
    public bool IsActivated { get; private set; }

    // ── Unity ─────────────────────────────────────────────────────────────
    private void Awake()
    {
        if (!allGoals.Contains(this)) allGoals.Add(this);

        if (goalRenderer == null)
            goalRenderer = GetComponent<SpriteRenderer>();

        activeColor = requiredColor;
        Refresh();
    }

    private void OnDestroy()
    {
        allGoals.Remove(this);
    }

    // ── Public API ─────────────────────────────────────────────────────────
    /// <summary>Called by BeamMachine each frame when its beam hits this collider.</summary>
    public void ReceiveBeam(Color beamColor)
    {
        bool matches = ColorsMatch(beamColor, requiredColor, colorTolerance);
        SetActivated(matches);
    }

    // ── Internal ──────────────────────────────────────────────────────────
    private void SetActivated(bool value)
    {
        bool changed = value != IsActivated;
        IsActivated = value;
        Refresh();

        if (changed)
            LightPuzzleController.Instance?.OnGoalStateChanged();
    }

    private void Refresh()
    {
        if (goalRenderer == null) return;
        if (IsActivated)
        {
            goalRenderer.color = activeColor;
            // Pulse scale to make activation obvious
            goalRenderer.transform.localScale = Vector3.one * 1.3f;
        }
        else
        {
            goalRenderer.color = inactiveColor;
            goalRenderer.transform.localScale = Vector3.one;
        }
    }

    /// <summary>Loose colour comparison with tolerance per channel.</summary>
    private static bool ColorsMatch(Color a, Color b, float tol)
    {
        return Mathf.Abs(a.r - b.r) < tol &&
               Mathf.Abs(a.g - b.g) < tol &&
               Mathf.Abs(a.b - b.b) < tol;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = requiredColor;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }
}
