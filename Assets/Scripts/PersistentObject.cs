using UnityEngine;

public class PersistentObject : MonoBehaviour
{
    [Tooltip("A unique ID to ensure only one instance of this object exists across scene loads.")]
    public string uniqueId;

    void Awake()
    {
        // Find all objects of this type
        PersistentObject[] objects = FindObjectsByType<PersistentObject>(FindObjectsInactive.Exclude);
        foreach (PersistentObject obj in objects)
        {
            if (obj != this && obj.uniqueId == this.uniqueId)
            {
                // Switch the duplicate off straight away: Destroy only happens at the end of
                // the frame, and until then its components (e.g. a second EventSystem) would
                // still run and can break the original.
                gameObject.SetActive(false);
                Destroy(gameObject);
                return;
            }
        }

        DontDestroyOnLoad(gameObject);
    }
}