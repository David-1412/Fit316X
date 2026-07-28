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
}
