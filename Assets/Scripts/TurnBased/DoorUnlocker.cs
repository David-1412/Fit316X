using UnityEngine;

/// <summary>
/// Attached to a door GameObject. At runtime it subscribes to PelletSystem.OnAllCollected
/// and deactivates the door when all pellets are collected.
/// This avoids using UnityEvent.AddListener in editor code (which doesn't persist).
/// </summary>
public class DoorUnlocker : MonoBehaviour
{
    [Tooltip("The door GameObject to deactivate when all pellets are collected. Defaults to this GameObject.")]
    public GameObject doorToOpen;

    private void Start()
    {
        if (doorToOpen == null) doorToOpen = gameObject;

        if (PelletSystem.Instance != null)
            PelletSystem.Instance.OnAllCollected.AddListener(OpenDoor);
        else
            Debug.LogWarning("[DoorUnlocker] PelletSystem not found in scene!");
    }

    private void OnDestroy()
    {
        if (PelletSystem.Instance != null)
            PelletSystem.Instance.OnAllCollected.RemoveListener(OpenDoor);
    }

    private void OpenDoor()
    {
        if (doorToOpen != null)
            doorToOpen.SetActive(false);
    }
}
