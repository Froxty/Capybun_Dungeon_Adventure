using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 6f;
    public float jumpForce = 7f;

    [Header("Ground Detection (SphereCast)")]
    public Transform groundPoint;
    public float groundRadius = 0.15f;
    public LayerMask whatIsGround;

    [Header("Optional Animations")]
    public Animator animator;

    [Header("Input Routing")]
    public bool acceptInput = true;   // toggled via SetAcceptInput()

    Rigidbody rb;
    Vector2 moveInput;
    bool isGrounded;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (whatIsGround.value == 0) whatIsGround = LayerMask.GetMask("Ground");
    }

    // Call this instead of setting acceptInput directly
    public void SetAcceptInput(bool value)
    {
        if (acceptInput == value) return;
        acceptInput = value;

        // Always clear cached input when toggling control
        moveInput = Vector2.zero;

        // Stop horizontal drift #buggo fixed
        var v = rb.linearVelocity;
        rb.linearVelocity = new Vector3(0f, v.y, 0f);
        
        if (animator)
            animator.SetBool("IsMoving", false);
    }

    // --- Input System ---
    public void OnMove(InputValue v)
    {
        if (!acceptInput) return;
        moveInput = v.Get<Vector2>();
    }

    public void OnJump(InputValue v)
    {
        if (!acceptInput) return;
        if (v.isPressed && isGrounded)
        {
            var vel = rb.linearVelocity;
            vel.y = 0f;
            rb.linearVelocity = vel;
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
    }

    void FixedUpdate()
    {
        Vector2 input = acceptInput ? moveInput : Vector2.zero;
        Vector3 move = new Vector3(input.x, 0f, input.y) * moveSpeed;
        rb.linearVelocity = new Vector3(move.x, rb.linearVelocity.y, move.z);
    }

    void Update()
    {
        isGrounded = Physics.CheckSphere(
            groundPoint ? groundPoint.position + Vector3.down * 0.02f : transform.position,
            groundRadius,
            whatIsGround,
            QueryTriggerInteraction.Ignore
        );

        if (animator)
        {
            animator.SetBool("IsGrounded", isGrounded);
            animator.SetBool("IsMoving", acceptInput && moveInput.sqrMagnitude > 0.1f);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (!groundPoint) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(groundPoint.position, groundRadius);
    }
}
