using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Saves the game, then goes back to the start menu. Used by the Settings page's
/// "Main Menu" button. The start menu clears the carried-over Player, HUD and camera itself.
/// </summary>
public class ReturnToMainMenu : MonoBehaviour
{
    public const string MenuSceneName = "StartMenu";

    private static bool s_Leaving;

    public static void Go()
    {
        if (s_Leaving) return;
        s_Leaving = true;

        GameObject runner = new GameObject("ReturnToMainMenu (auto)");
        DontDestroyOnLoad(runner);
        runner.AddComponent<ReturnToMainMenu>().StartCoroutine(runner.GetComponent<ReturnToMainMenu>().Run());
    }

    private IEnumerator Run()
    {
        // Save first so "Continue" picks up from here; the save preview is captured a frame later
        if (SaveController.Instance != null) SaveController.Instance.SaveGame();
        yield return null;
        yield return null;
        yield return new WaitForSecondsRealtime(0.1f);

        PauseController.SetPause(false);
        Time.timeScale = 1f;

        if (Application.CanStreamedLevelBeLoaded(MenuSceneName)) SceneManager.LoadScene(MenuSceneName);
        else SceneManager.LoadScene(0); // the start menu is build scene 0

        s_Leaving = false;
        Destroy(gameObject);
    }
}
