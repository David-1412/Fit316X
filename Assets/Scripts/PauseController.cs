using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseController : MonoBehaviour
{
    public static bool IsGamePaused { get; private set; } = false;

    private void Awake()
    {
        // Reset pause state whenever a new scene loads so stale flags
        // from mid-transition scenes don't lock out the menu.
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        IsGamePaused = false;
    }

    public static void SetPause(bool pause)
    {
        IsGamePaused = pause;
    }
}
