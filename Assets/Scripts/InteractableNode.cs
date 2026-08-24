using UnityEngine;

public class InteractableNode : MonoBehaviour
{
    // Keeps track of whether the player is standing inside the trigger zone
    private bool playerIsNear = false;

    void Update()
    {
        // If the player is in range AND presses the 'E' key
        if (playerIsNear && Input.GetKeyDown(KeyCode.E))
        {
            // Rotate the block 90 degrees clockwise
            transform.Rotate(0, 0, -90f);

            Debug.Log("Block rotated 90 degrees!");
        }
    }

    // This runs automatically when something enters the Circle Collider Trigger
    void OnTriggerEnter2D(Collider2D other)
    {
        // Check if the object that entered is the Player
        if (other.CompareTag("Player"))
        {
            playerIsNear = true;
            // Floating "Press E" UI sprite here!
        }
    }

    // This runs automatically when something leaves the Trigger
    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerIsNear = false;
            // Tip: Disable the floating "Press E" sprite here.
        }
    }
}
