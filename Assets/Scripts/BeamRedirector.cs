using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Beam Redirector — accepts an incoming light beam and re-emits it in a
/// configurable direction. Auto-activates when a beam hits it (no gem needed).
/// Player presses E to rotate the output direction by 90 degrees.
/// 
/// Setup:
///   • Add a Collider2D (set to trigger=false) so BeamMachine raycasts can hit it.
///     Make sure it is on the same LayerMask as your obstacleLayer on BeamMachine.
///   • Optionally assign an arrowRenderer (SpriteRenderer child) to show current direction.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class BeamRedirector : MonoBehaviour, IInteractable
{
    [Header("Visuals")]
    [Tooltip("Optional child SpriteRenderer that rotates to show output direction (e.g. an arrow sprite).")]
    public Transform arrowTransform;

    [Header("Starting Direction")]
    [Tooltip("0=Up  1=Right  2=Down  3=Left")]
    public int startRotationState = 0;

    private int rotationState;

    // ── Unity ─────────────────────────────────────────────────────────────
    private void Awake()
    {
        rotationState = startRotationState;
        ApplyRotationVisual();
    }

    // ── Public API (called by BeamMachine each frame) ─────────────────────
    /// <summary>Returns the world-space direction this redirector emits the beam.</summary>
    public Vector2 GetOutputDirection()
    {
        return rotationState switch
        {
            1 => Vector2.right,
            2 => Vector2.down,
            3 => Vector2.left,
            _ => Vector2.up
        };
    }

    // ── IInteractable ─────────────────────────────────────────────────────
    public bool CanInteract() => true;

    public void Interact()
    {
        rotationState = (rotationState + 1) % 4;
        ApplyRotationVisual();
        SoundEffectManager.Play("PickUp");
    }

    // ── Helpers ───────────────────────────────────────────────────────────
    private void ApplyRotationVisual()
    {
        if (arrowTransform != null)
        {
            arrowTransform.rotation = Quaternion.Euler(0, 0, -rotationState * 90f + 180f);

            // Ensure the arrow always renders on top of the base, no matter what sorting layer the base is on
            SpriteRenderer baseSr  = GetComponent<SpriteRenderer>();
            SpriteRenderer arrowSr = arrowTransform.GetComponent<SpriteRenderer>();
            
            if (baseSr != null && arrowSr != null)
            {
                arrowSr.sortingLayerID = baseSr.sortingLayerID;
                arrowSr.sortingOrder   = baseSr.sortingOrder + 1;
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Draw a yellow arrow showing output direction in Scene view
        Gizmos.color = Color.yellow;
        Vector2 dir = GetOutputDirection();
        Vector3 from = transform.position;
        Vector3 to   = from + (Vector3)(dir * 1.2f);
        Gizmos.DrawLine(from, to);
        Gizmos.DrawSphere(to, 0.1f);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Instantly fix sorting layers if you tweak them in the inspector
        ApplyRotationVisual();
    }
#endif
}
