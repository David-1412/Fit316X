using UnityEngine;

/// <summary>
/// Placed in Dungeon_Floor2 scene. On Start it finds the persistent player
/// and adds GridTurnPlayer to them, activating turn-based mode.
/// When the scene unloads, GridTurnPlayer's OnDestroy restores normal movement.
/// </summary>
public class DungeonSetup : MonoBehaviour
{
    private void Start()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogError("[DungeonSetup] No GameObject tagged 'Player' found in scene!");
            return;
        }

        // Only add GridTurnPlayer if not already present
        if (player.GetComponent<GridTurnPlayer>() == null)
            player.AddComponent<GridTurnPlayer>();

        // Snap player to spawn point if available
        var spawn = GameObject.Find("PlayerSpawn");
        if (spawn != null)
        {
            var p = spawn.transform.position;
            player.transform.position = new Vector3(Mathf.Round(p.x), Mathf.Round(p.y), 0f);
        }
    }
}
