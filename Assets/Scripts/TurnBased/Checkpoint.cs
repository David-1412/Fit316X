using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class Checkpoint : MonoBehaviour
{
    private bool _activated = false;

    private void Awake()
    {
        // Ensure the collider is set to trigger
        var col = GetComponent<BoxCollider2D>();
        if (col != null) col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Debug.Log("Checkpoint triggered by: " + collision.gameObject.name + " with tag: " + collision.tag);

        if (!_activated && collision.CompareTag("Player"))
        {
            _activated = true;

            // Trigger the save
            if (SaveController.Instance != null)
            {
                SaveController.Instance.SaveGame();
                if (DungeonHUD.Instance != null)
                {
                    DungeonHUD.Instance.ShowCombatLog("<color=#00FFFF>Checkpoint Reached! Progress Saved.</color>");
                }
                Debug.Log("Checkpoint activated and game saved!");
            }
            else
            {
                Debug.LogWarning("SaveController instance not found. Cannot save checkpoint.");
            }
        }
    }
}
