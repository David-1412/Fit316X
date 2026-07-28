using System.Collections.Generic;
using UnityEngine;

public class EchoRecorder : MonoBehaviour
{
    public List<EchoSnapshot> history = new List<EchoSnapshot>();
    public float maxHistoryDuration = 10f; // Store up to 10 seconds of history

    void OnEnable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        history.Clear();
    }

    void FixedUpdate()
    {
        bool interacting = false;
        if (UnityEngine.InputSystem.Keyboard.current != null)
        {
            interacting = UnityEngine.InputSystem.Keyboard.current.eKey.isPressed;
        }
        
        EchoSnapshot snap = new EchoSnapshot
        {
            timestamp = Time.time,
            position = transform.position,
            isInteracting = interacting
        };
        
        history.Add(snap);

        // Keep the buffer clean to prevent memory bloat
        if (history.Count > 0 && Time.time - history[0].timestamp > maxHistoryDuration)
        {
            history.RemoveAt(0);
        }
    }
}
