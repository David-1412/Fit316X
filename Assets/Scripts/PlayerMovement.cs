using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    private Rigidbody2D rb;
    private Vector2 moveInput;
    private Animator animator;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (PauseController.IsGamePaused)
        {
            if(rb.linearVelocity != Vector2.zero)
            {
                rb.linearVelocity = Vector2.zero;
                StopMovementAnimations();
            }
            return;
        }

        rb.linearVelocity = moveInput * moveSpeed;
        animator.SetBool("isWalking", rb.linearVelocity.magnitude > 0);
        UpdateDirectionalSprite();
    }

    public void Move(InputAction.CallbackContext context)
    {
        if (context.canceled)
        {
            StopMovementAnimations();
        }

        moveInput = context.ReadValue<Vector2>();

        animator.SetFloat("InputX", moveInput.x);
        animator.SetFloat("InputY", moveInput.y);
    }

    void StopMovementAnimations()
    {
        animator.SetBool("isWalking", false);
        animator.SetFloat("LastInputX", moveInput.x);
        animator.SetFloat("LastInputY", moveInput.y);
    }

    void UpdateDirectionalSprite()
    {
        UnityEngine.U2D.Animation.SpriteResolver resolver = GetComponent<UnityEngine.U2D.Animation.SpriteResolver>();
        if (resolver == null) return;

        // Determine direction based on moveInput or LastInput
        Vector2 dir = rb.linearVelocity.magnitude > 0 ? moveInput : new Vector2(animator.GetFloat("LastInputX"), animator.GetFloat("LastInputY"));

        string label = "Idle (3)_0"; // Down default

        if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
        {
            if (dir.x > 0) label = "Idle (3)_3"; // Right (was 2)
            else label = "Idle (3)_2"; // Left (was 3)
        }
        else if (Mathf.Abs(dir.y) > 0)
        {
            if (dir.y > 0) label = "Idle (3)_1"; // Up
            else label = "Idle (3)_0"; // Down
        }

        resolver.SetCategoryAndLabel("Idle", label);
    }
}
