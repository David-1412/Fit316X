using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Linq;

public class InteractionDetector : MonoBehaviour
{
    [Header("Detection")]
    [Tooltip("Radius around player to scan for interactables. Visualised as a cyan circle in Scene view.")]
    public float detectionRadius = 3.5f; // Generous default so player doesn't need pixel-perfect position

    [Tooltip("Leave as 'Everything' to detect all objects. Restrict if you have performance concerns.")]
    public LayerMask interactionLayer = ~0; // Default: all layers

    [Header("Icon")]
    public GameObject interactionIcon;

    // Internal state
    private List<IInteractable> interactablesInRange = new List<IInteractable>();
    private bool interactFiredThisFrame = false;
    private float interactCooldown = 0f;
    private const float INTERACT_COOLDOWN_DURATION = 0.4f; // seconds to block re-trigger after an interaction

    void Start()
    {
        if (interactionIcon != null)
            interactionIcon.SetActive(false);

        // Safety: if layer is 0 (nothing), reset to Everything
        if (interactionLayer.value == 0)
            interactionLayer = ~0;
    }

    private void Update()
    {
        // Always scan and update the icon regardless of cooldown
        ScanForInteractables();

        bool menuOpen = InteractionMenuUI.Instance != null && InteractionMenuUI.Instance.IsMenuOpen;
        bool canInteract = interactablesInRange.Any(i => i != null && i.CanInteract());

        if (interactionIcon != null)
            interactionIcon.SetActive(canInteract && !menuOpen);

        // Tick down cooldown — block input during this window
        if (interactCooldown > 0f)
        {
            interactCooldown -= Time.unscaledDeltaTime;
            interactFiredThisFrame = false;
            return;
        }

        // Fallback keyboard poll — only fires if Input System callback did NOT already fire this frame
        if (!menuOpen && !interactFiredThisFrame && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            TryInteract();
        }

        interactFiredThisFrame = false;
    }

    // Called by Unity's Player Input component via Send Messages / Unity Events
    public void OnInteract(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        if (interactCooldown > 0f) return; // Still cooling down

        interactFiredThisFrame = true;

        if (InteractionMenuUI.Instance != null && InteractionMenuUI.Instance.IsMenuOpen)
            return;

        TryInteract();
    }

    public void NotifyInteractionComplete()
    {
        // Called by InteractionMenuUI after a selection so we don't re-fire this frame
        interactCooldown = INTERACT_COOLDOWN_DURATION;
        interactFiredThisFrame = true;
    }

    private void TryInteract()
    {
        ScanForInteractables();

        List<InteractionOption> allOptions = new List<InteractionOption>();
        bool hasMultiInteractable = false;

        foreach (var interactable in interactablesInRange)
        {
            if (interactable == null || !interactable.CanInteract()) continue;

            if (interactable is IMultiInteractable multi)
            {
                hasMultiInteractable = true; // Flag: always show menu for these
                allOptions.AddRange(multi.GetInteractionOptions());
            }
            else
            {
                var captured = interactable;
                MonoBehaviour mb = interactable as MonoBehaviour;
                string label = mb != null ? "Interact with " + mb.gameObject.name : "Interact";
                allOptions.Add(new InteractionOption
                {
                    Name = label,
                    OnSelect = () => captured.Interact()
                });
            }
        }

        Debug.Log($"<color=cyan>InteractionDetector: {interactablesInRange.Count} interactables, {allOptions.Count} options, hasMulti={hasMultiInteractable}</color>");

        if (allOptions.Count == 0) return;

        // Always show the menu if ANY interactable is an IMultiInteractable (e.g. BeamMachine)
        // Only auto-fire for simple single legacy interactables (e.g. single-option NPC, switch)
        bool shouldShowMenu = hasMultiInteractable || allOptions.Count > 1;

        if (shouldShowMenu)
        {
            InteractionMenuUI menuUI = InteractionMenuUI.Instance
                ?? FindAnyObjectByType<InteractionMenuUI>(FindObjectsInactive.Include);

            if (menuUI != null)
            {
                Vector3 menuWorldPos = GetInteractablesCenterPosition();
                menuUI.ShowMenu(allOptions, menuWorldPos);
            }
            else
            {
                Debug.LogWarning("InteractionDetector: No InteractionMenuUI found! Firing first option.");
                allOptions[0].OnSelect?.Invoke();
            }
        }
        else
        {
            // Single simple interactable — just fire it directly (e.g. pick up gem, open chest)
            allOptions[0].OnSelect?.Invoke();
        }
    }

    private void ScanForInteractables()
    {
        interactablesInRange.Clear();

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, detectionRadius, interactionLayer);

        foreach (Collider2D hit in hits)
        {
            if (hit == null) continue;
            // Skip self
            if (hit.transform == transform || hit.transform.IsChildOf(transform)) continue;

            IInteractable interactable = hit.GetComponent<IInteractable>()
                ?? hit.GetComponentInParent<IInteractable>();

            if (interactable == null) continue;
            if (interactablesInRange.Contains(interactable)) continue;

            // Secondary check: the object's CENTER must be inside the detection radius.
            // This prevents large/stale colliders from triggering from far away.
            MonoBehaviour mb = interactable as MonoBehaviour;
            if (mb != null)
            {
                float dist = Vector2.Distance(transform.position, mb.transform.position);
                if (dist > detectionRadius) continue; // Center is outside the blue circle — skip
            }

            interactablesInRange.Add(interactable);
        }
    }

    private Vector3 GetInteractablesCenterPosition()
    {
        Vector3 center = Vector3.zero;
        int count = 0;
        foreach (var i in interactablesInRange)
        {
            if (i is MonoBehaviour mb)
            {
                center += mb.transform.position;
                count++;
            }
        }
        return count > 0 ? center / count : transform.position;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}
