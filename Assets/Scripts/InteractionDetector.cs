using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class InteractionDetector : MonoBehaviour
{
    private IInteractable interactableInRange = null; //Closest Interactable
    public GameObject interactionIcon;

    void Start()
    {
        interactionIcon.SetActive(false);
    }

    public void OnInteract(InputAction.CallbackContext context)
    {
        if (!context.performed)
            return;

        Debug.Log("<color=yellow>InteractionDetector: 'E' key pressed!</color>");

        if (interactableInRange == null)
        {
            Debug.Log("<color=red>InteractionDetector: No interactable object in range!</color>");
            interactionIcon?.SetActive(false);
            return;
        }

        Debug.Log("<color=yellow>InteractionDetector: Interacting with object!</color>");
        interactableInRange.Interact();

        if (!interactableInRange.CanInteract())
        {
            interactionIcon?.SetActive(false);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Debug.Log($"<color=cyan>InteractionDetector: Trigger entered with {collision.gameObject.name}</color>");
        if(collision.TryGetComponent(out IInteractable interactable))
        {
            if (interactable.CanInteract())
            {
                Debug.Log($"<color=green>InteractionDetector: Found interactable: {collision.gameObject.name}</color>");
                interactableInRange = interactable;
                interactionIcon.SetActive(true);
            }
            else
            {
                Debug.Log($"<color=orange>InteractionDetector: Found interactable {collision.gameObject.name}, but CanInteract() is false.</color>");
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.TryGetComponent(out IInteractable interactable) && interactable == interactableInRange)
        {
            interactableInRange = null;
            interactionIcon.SetActive(false);
        }
    }
}
