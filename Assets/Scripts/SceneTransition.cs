using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransition : MonoBehaviour
{
    public string targetSceneName;
    public Vector2 targetPosition;

    /// <summary>
    /// If true, look for a GameObject named "PlayerSpawn" in the target scene
    /// and teleport the player there instead of using targetPosition.
    /// </summary>
    public bool useSpawnMarker = false;

    // Persists across scene load so we can apply the position after the scene is ready
    private static Vector2  s_PendingPosition;
    private static bool     s_HasPendingPosition;
    private static bool     s_UseSpawnMarker;

    private void Awake()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        // Store where to spawn in the new scene
        s_PendingPosition    = targetPosition;
        s_HasPendingPosition = true;
        s_UseSpawnMarker     = useSpawnMarker;

        // Ensure pause state is cleared before switching scenes
        PauseController.SetPause(false);

        SceneManager.LoadScene(targetSceneName);
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!s_HasPendingPosition) return;
        s_HasPendingPosition = false;

        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        if (s_UseSpawnMarker)
        {
            // Try to find a PlayerSpawn marker placed by the dungeon generator
            var marker = GameObject.Find("PlayerSpawn");
            if (marker != null)
            {
                player.transform.position = marker.transform.position;
                return;
            }
        }

        player.transform.position = s_PendingPosition;
    }
}