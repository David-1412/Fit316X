using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Events;

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

    [Header("Colour Mixing")]
    [Tooltip("If ticked, all beam colours hitting this goal are mixed (averaged) and the MIX must match requiredColor.\n" +
             "e.g. White (1,1,1) + Blue (0,0,1) = (0.5, 0.5, 1).")]
    public bool mixBeams = false;

    [Tooltip("Mix mode only: how many DIFFERENT beam colours must be hitting at once.")]
    [Min(1)]
    public int requiredBeamCount = 2;

    [Tooltip("Mix mode only: tint the goal with the current mix so players can see what they're making.")]
    public bool showMixPreview = true;

    [Header("Unlocking")]
    [Tooltip("Once lit, stay lit forever — the player can then move the pedestals to the next gate.")]
    public bool stayLitOnceUnlocked = false;

    [Tooltip("Fires once, the first time this goal is lit. Hook up sounds, hide a coloured seal, etc.")]
    public UnityEvent OnUnlocked;

    public bool IsUnlocked { get; private set; }

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
    /// Beams are now collected during Update and evaluated once in LateUpdate,
    /// so several BeamMachines can hit the same goal in one frame without
    /// resetting each other. Kept for compatibility — only cleans the list.
    public static void ResetAll()
    {
        for (int i = allGoals.Count - 1; i >= 0; i--)
            if (allGoals[i] == null) allGoals.RemoveAt(i);
    }

    // Beam colours received this frame (from any number of BeamMachines)
    private readonly List<Color> incomingBeams = new List<Color>();

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
        incomingBeams.Add(beamColor);
    }

    /// <summary>Runs after every BeamMachine.Update — decides if this goal is lit.</summary>
    private void LateUpdate()
    {
        if (stayLitOnceUnlocked && IsUnlocked)
        {
            incomingBeams.Clear();
            return;
        }

        bool lit;

        if (mixBeams)
        {
            List<Color> unique = UniqueColors(incomingBeams);
            Color mix = Average(unique);
            lit = unique.Count >= requiredBeamCount && ColorsMatch(mix, requiredColor, colorTolerance);

            if (!lit && showMixPreview && goalRenderer != null && unique.Count > 0)
            {
                SetActivated(false);
                goalRenderer.color = Color.Lerp(inactiveColor, mix, 0.6f); // hint of the current mix
                incomingBeams.Clear();
                return;
            }
        }
        else
        {
            lit = false;
            foreach (var c in incomingBeams)
                if (ColorsMatch(c, requiredColor, colorTolerance)) { lit = true; break; }
        }

        if (lit && !IsUnlocked)
        {
            IsUnlocked = true;
            OnUnlocked?.Invoke();
        }

        SetActivated(lit);
        incomingBeams.Clear();
    }

    private List<Color> UniqueColors(List<Color> colors)
    {
        var unique = new List<Color>();
        foreach (var c in colors)
        {
            bool seen = false;
            foreach (var u in unique)
                if (ColorsMatch(c, u, 0.01f)) { seen = true; break; }
            if (!seen) unique.Add(c);
        }
        return unique;
    }

    private static Color Average(List<Color> colors)
    {
        if (colors.Count == 0) return Color.black;
        float r = 0, g = 0, b = 0;
        foreach (var c in colors) { r += c.r; g += c.g; b += c.b; }
        return new Color(r / colors.Count, g / colors.Count, b / colors.Count, 1f);
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
