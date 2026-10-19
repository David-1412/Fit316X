using UnityEngine;

/// <summary>
/// One place for the "puzzle solved" feedback: the on-screen popup and the music sting.
/// Silent while a save is being loaded, so already-solved rooms don't announce themselves.
/// </summary>
public static class PuzzleSolved
{
    public const string Message = "Puzzle Solved!";
    private static readonly Color Gold = new Color(1f, 0.85f, 0.2f, 1f);

    public static void Announce()
    {
        if (SaveController.IsRestoring) return;
        PopupText.Show(Message, Gold);
        MusicManager.PlayPuzzleSolved();
    }
}
