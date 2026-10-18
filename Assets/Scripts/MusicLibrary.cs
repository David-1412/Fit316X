using UnityEngine;

/// <summary>
/// The music tracks used by MusicManager. The asset lives at Resources/MusicLibrary
/// so MusicManager can load it without any scene setup. Swap clips in the Inspector.
/// </summary>
[CreateAssetMenu(fileName = "MusicLibrary", menuName = "Audio/Music Library")]
public class MusicLibrary : ScriptableObject
{
    [Header("Background")]
    [Tooltip("Plays on the menu and on every floor when nothing else is playing.")]
    public AudioClip background;

    [Header("Cutscenes")]
    [Tooltip("Longer story cutscenes, e.g. the rooftop walk and Old Man dialogue.")]
    public AudioClip cutscene;
    [Tooltip("Short cutscenes, e.g. an NPC walking away after talking.")]
    public AudioClip shortCutscene;

    [Header("Moments")]
    [Tooltip("Plays for a few seconds when a puzzle is solved, then the background returns.")]
    public AudioClip puzzleSolved;
    public float puzzleSolvedSeconds = 8f;
    [Tooltip("The final choice on the rooftop.")]
    public AudioClip finalChoice;

    [Header("Endings")]
    public AudioClip truthEnding;
    public AudioClip controlEnding;
}
