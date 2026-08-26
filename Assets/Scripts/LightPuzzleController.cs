using UnityEngine;
using UnityEngine.Events;
using System.Linq;
using TMPro;

/// <summary>
/// LightPuzzleController — tracks all LightGoals and fires OnPuzzleSolved
/// when every goal is simultaneously activated.
/// 
/// Setup:
///   • Place once in the scene.
///   • Assign all LightGoal objects in the goals array.
///   • Wire OnPuzzleSolved to open a door, play a cutscene, etc.
/// </summary>
public class LightPuzzleController : MonoBehaviour
{
    public static LightPuzzleController Instance { get; private set; }

    [Header("Goals")]
    [Tooltip("Drag all LightGoal objects in this room here.")]
    public LightGoal[] goals;

    [Header("On Solve")]
    public UnityEvent OnPuzzleSolved;

    private bool solved = false;

    // ── Unity ─────────────────────────────────────────────────────────────
    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ── Called by LightGoal whenever any goal state changes ───────────────
    public void OnGoalStateChanged()
    {
        if (solved) return;

        // Auto-populate if designer forgot to assign goals
        if (goals == null || goals.Length == 0)
            goals = FindObjectsByType<LightGoal>(FindObjectsSortMode.None);

        bool allActive = goals.Length > 0 && goals.All(g => g != null && g.IsActivated);

        if (allActive)
        {
            solved = true;
            Debug.Log("[LightPuzzleController] All goals satisfied — PUZZLE SOLVED!");
            OnPuzzleSolved?.Invoke();
            ShowSolvedFeedback();
        }
    }

    // ── Win Feedback ──────────────────────────────────────────────────────
    private void ShowSolvedFeedback()
    {
        var canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null) return;

        var textObj = new GameObject("LightPuzzleSolvedText");
        textObj.transform.SetParent(canvas.transform, false);

        var tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text      = "✦  PUZZLE SOLVED  ✦";
        tmp.fontSize  = 60;
        tmp.color     = new Color(1f, 0.85f, 0.2f, 1f);
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = FontStyles.Bold;

        var rect = textObj.GetComponent<RectTransform>();
        rect.anchorMin       = new Vector2(0.1f, 0.4f);
        rect.anchorMax       = new Vector2(0.9f, 0.6f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta        = Vector2.zero;

        Destroy(textObj, 3f);
    }
}
