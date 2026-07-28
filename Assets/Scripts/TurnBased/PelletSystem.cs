using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Tracks collectible pellets in a room.
/// When the last pellet is collected, fires OnAllCollected
/// (which the room door listens to for unlocking).
/// </summary>
public class PelletSystem : MonoBehaviour
{
    public static PelletSystem Instance { get; private set; }

    public UnityEvent OnAllCollected;

    private readonly List<GameObject> _remaining = new List<GameObject>();
    private int _total;
    private int _collected;

    // Maps room identifiers (e.g. "R1") to their total and collected pellet counts
    private Dictionary<string, int> roomTotals = new Dictionary<string, int>();
    private Dictionary<string, int> roomCollected = new Dictionary<string, int>();

    public int Total     => _total;
    public int Collected => _collected;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    private string GetRoomID(string pelletName)
    {
        // Extracts "R1" from "Pellet_R1_1"
        string[] parts = pelletName.Split('_');
        if (parts.Length >= 2) return parts[1];
        return "Unknown";
    }

    // Called by Pellet.cs on Start
    public void Register(GameObject pellet)
    {
        _remaining.Add(pellet);
        _total++;

        string roomID = GetRoomID(pellet.name);
        if (!roomTotals.ContainsKey(roomID))
        {
            roomTotals[roomID] = 0;
            roomCollected[roomID] = 0;
        }
        roomTotals[roomID]++;

        DungeonHUD.Instance?.RefreshPellets(_collected, _total);
    }

    // Called by Pellet.cs on trigger
    public void Collect(GameObject pellet)
    {
        if (!_remaining.Remove(pellet)) return;
        
        string roomID = GetRoomID(pellet.name);
        pellet.SetActive(false);
        _collected++;
        DungeonHUD.Instance?.RefreshPellets(_collected, _total);

        // Update room specific progress
        if (roomTotals.ContainsKey(roomID))
        {
            roomCollected[roomID]++;
            if (roomCollected[roomID] == roomTotals[roomID])
            {
                DungeonHUD.Instance?.ShowCombatLog($"<color=yellow>Room {roomID} Cleared!</color>");
                
                // Attempt to open the door for this room
                var door = GameObject.Find($"TemporalDoor_{roomID}");
                if (door != null)
                {
                    var tempDoor = door.GetComponent<TemporalDoor>();
                    if (tempDoor != null) tempDoor.OpenDoor();
                }

                // If this was the final room and no pellets are left globally
                if (_remaining.Count == 0)
                {
                    DungeonHUD.Instance?.ShowCombatLog("<color=yellow>Dungeon Complete!</color>");
                    TurnManager.Instance?.TriggerVictory();
                }
            }
        }

        if (_remaining.Count == 0)
            OnAllCollected?.Invoke();
    }
}
