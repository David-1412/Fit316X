using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class DialogueTriggerArea : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("The NPC whose dialogue will be triggered.")]
    public NPC targetNPC;
    
    [Tooltip("Should this trigger only work once?")]
    public bool triggerOnlyOnce = true;

    private bool hasTriggered = false;

    private void Awake()
    {
        // Ensure the collider is set to trigger so the player can walk through it
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (hasTriggered && triggerOnlyOnce) return;

        // Check if the colliding object is the Player
        if (collision.CompareTag("Player"))
        {
            if (targetNPC != null)
            {
                // Call the NPC's interact method to force dialogue
                targetNPC.Interact();
                hasTriggered = true;
            }
            else
            {
                Debug.LogWarning("DialogueTriggerArea on " + gameObject.name + " is missing a target NPC!");
            }
        }
    }
}
