using UnityEngine;

public class PuzzleRoomManager : MonoBehaviour
{
    public TemporalSwitch[] switchesRequired;
    public UnityEngine.Events.UnityEvent OnPuzzleSolved = new UnityEngine.Events.UnityEvent();

    private bool solved = false;
    private int activatedCount = 0;

    void Start()
    {
        if (switchesRequired == null || switchesRequired.Length == 0)
        {
            Debug.LogError("[PuzzleRoomManager] No switches assigned! Finding them automatically...");
            switchesRequired = FindObjectsOfType<TemporalSwitch>();
        }

        foreach (var sw in switchesRequired)
        {
            if (sw == null) continue;
            sw.OnSwitchActivated.AddListener(OnSwitchActivated);
            sw.OnSwitchDeactivated.AddListener(OnSwitchDeactivated);
            Debug.Log($"[PuzzleRoomManager] Subscribed to switch: {sw.name}");
        }

        Debug.Log($"[PuzzleRoomManager] Ready. Requires {switchesRequired.Length} switches.");
    }

    void OnSwitchActivated()
    {
        if (solved) return;
        activatedCount++;
        Debug.Log($"[PuzzleRoomManager] Switch activated! {activatedCount}/{switchesRequired.Length}");

        if (activatedCount >= switchesRequired.Length)
        {
            SolvePuzzle();
        }
    }

    void OnSwitchDeactivated()
    {
        if (solved) return;
        activatedCount = Mathf.Max(0, activatedCount - 1);
        Debug.Log($"[PuzzleRoomManager] Switch deactivated. {activatedCount}/{switchesRequired.Length}");
    }

    void SolvePuzzle()
    {
        solved = true;
        Debug.Log("[PuzzleRoomManager] PUZZLE SOLVED!");

        OnPuzzleSolved?.Invoke();

        // Find UICanvas and show feedback text using TextMeshPro if available, fallback to legacy UI.Text
        var canvas = FindAnyObjectByType<Canvas>();
        if (canvas != null)
        {
            var textObj = new GameObject("SolvedText");
            textObj.transform.SetParent(canvas.transform, false);

            // Try TextMeshPro first
            var tmp = textObj.AddComponent<TMPro.TextMeshProUGUI>();
            if (tmp != null)
            {
                tmp.text = "✦  ROOM SOLVED  ✦";
                tmp.fontSize = 60;
                tmp.color = new Color(1f, 0.9f, 0.2f, 1f);
                tmp.alignment = TMPro.TextAlignmentOptions.Center;
                tmp.fontStyle = TMPro.FontStyles.Bold;
                var rect = textObj.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.1f, 0.4f);
                rect.anchorMax = new Vector2(0.9f, 0.6f);
                rect.anchoredPosition = Vector2.zero;
                rect.sizeDelta = Vector2.zero;
            }

            Destroy(textObj, 3f);
        }
    }
}
