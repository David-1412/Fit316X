using System;
using UnityEngine;

/// <summary>Persists an F2 wing's reveal and the gem installed in its bulb.</summary>
[RequireComponent(typeof(BulbPuzzle))]
public class F2RoomReveal : MonoBehaviour, ISaveableState
{
    public GameObject darkness;
    private bool revealed;

    public void Reveal()
    {
        if (!revealed) PuzzleSolved.Announce();
        revealed = true;
        if (darkness != null) darkness.SetActive(false);
    }

    [Serializable]
    private class State
    {
        public bool revealed;
        public string socket;
    }

    public string CaptureState() => JsonUtility.ToJson(new State {
        revealed = revealed, socket = GetComponent<BulbPuzzle>().CaptureGemState()
    });

    public void RestoreState(string state)
    {
        if (string.IsNullOrEmpty(state)) return;
        var saved = JsonUtility.FromJson<State>(state);
        if (saved == null) return;
        GetComponent<BulbPuzzle>().RestoreGemState(saved.socket);
        revealed = saved.revealed;
        if (darkness != null) darkness.SetActive(!revealed);
    }
}
