/// <summary>
/// Implement on a scene component whose state should survive saving, floor changes
/// and Continue (puzzle pieces, doors, chests...). SaveController finds every
/// component implementing this in the active floor and stores the returned string.
/// </summary>
public interface ISaveableState
{
    /// <summary>Returns this component's current state as a short string.</summary>
    string CaptureState();

    /// <summary>
    /// Applies a state returned by CaptureState. Called right after the scene loads
    /// (after Awake, before Start).
    /// </summary>
    void RestoreState(string state);
}
