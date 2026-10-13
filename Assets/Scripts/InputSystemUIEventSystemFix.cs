using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

public static class InputSystemUIEventSystemFix
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InitializeOnSceneLoad()
    {
        SceneManager.sceneLoaded += (scene, mode) => ReplaceStandaloneInputModules();
        ReplaceStandaloneInputModules();
    }

    private static void ReplaceStandaloneInputModules()
    {
        foreach (var eventSystem in Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include))
        {
            // Skip duplicates being removed (PersistentObject deactivates them first).
            // Giving them an Input System module would disable the shared UI actions
            // when they are destroyed, leaving every button unclickable.
            if (eventSystem == null || !eventSystem.gameObject.activeInHierarchy)
                continue;

            var standaloneModule = eventSystem.GetComponent<StandaloneInputModule>();
            if (standaloneModule == null)
                continue;

            var uiModule = eventSystem.GetComponent<InputSystemUIInputModule>();
            if (uiModule == null)
            {
                uiModule = eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
            }

            uiModule.deselectOnBackgroundClick = true;

            Object.DestroyImmediate(standaloneModule);
        }
    }
}
