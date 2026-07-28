using UnityEngine;

public class PersistentObject : MonoBehaviour
{
    [Tooltip("A unique ID to ensure only one instance of this object exists across scene loads.")]
    public string uniqueId;

    void Awake()
    {
        // Find all objects of this type
        PersistentObject[] objects = FindObjectsByType<PersistentObject>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (PersistentObject obj in objects)
        {
            if (obj != this && obj.uniqueId == this.uniqueId)
            {
                Destroy(gameObject);
                return;
            }
        }

        DontDestroyOnLoad(gameObject);
    }
}