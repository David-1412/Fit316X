using UnityEngine;

/// A pane of coloured glass. Beams pass through and change colour.
[RequireComponent(typeof(Collider2D))]
public class ColorFilter : MonoBehaviour
{
    public enum FilterMode { Replace, Multiply }

    [Tooltip("Replace: the beam always becomes this colour.\n" +
             "Multiply: like real glass. White becomes this colour, but e.g. blue through red is blocked.")]
    public FilterMode mode = FilterMode.Replace;
    public Color filterColor = Color.red;

    [Header("Visuals")]
    public SpriteRenderer glassRenderer;
    [Range(0f, 1f)] public float glassAlpha = 0.6f;

    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = false; // beams ignore triggers, so it must be solid
        ApplyTint();
    }

    private void OnValidate() => ApplyTint();   // update the glass colour while editing

    private void ApplyTint()
    {
        if (glassRenderer == null) glassRenderer = GetComponent<SpriteRenderer>();
        if (glassRenderer == null) return;
        Color c = filterColor;
        c.a = glassAlpha;
        glassRenderer.color = c;
    }

    /// Returns the beam's colour after passing through this filter.
    public Color Apply(Color incoming)
    {
        Color result = mode == FilterMode.Replace ? filterColor : incoming * filterColor;
        result.a = 1f;
        return result;
    }

    /// True if the filter absorbed the beam completely (it came out black).
    public static bool IsBlocked(Color c) => c.r + c.g + c.b < 0.05f;
}