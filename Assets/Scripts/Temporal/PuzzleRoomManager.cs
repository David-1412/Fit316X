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
        PuzzleSolved.Announce();
    }
}
