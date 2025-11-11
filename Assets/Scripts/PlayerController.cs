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

    [Header("Visuals / Animation")]
    public Animator animator;
    public SpriteRenderer sprite;

    //[Header("Animator Trigger Names")]
    string interactTrigger = "Interact"; 
    string dieTrigger = "Die";           

    [Header("Input Routing")]
    public bool acceptInput = true; // toggled via SetAcceptInput()

    // --- Private ---
    Rigidbody rb;
    Vector2 moveInput;
    bool isGrounded;
    bool faceRight = true;  // last horizontal facing (true = right)

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (whatIsGround.value == 0) whatIsGround = LayerMask.GetMask("Ground");
        if (!sprite) sprite = GetComponentInChildren<SpriteRenderer>();
    }

    /// Enable/disable player control. Clears cached input and halts horizontal drift.
    bool BodyFree() => rb && !rb.isKinematic;

    public void SetAcceptInput(bool value)
    {
        if (acceptInput == value) return;
        acceptInput = value;
        moveInput = Vector2.zero;

        if (BodyFree())
        {
            var v = rb.linearVelocity;
            rb.linearVelocity = new Vector3(0f, v.y, 0f);
        }

        if (animator) animator.SetBool("IsMoving", false);
    }


    // --- Input System callbacks ---
    public void OnMove(InputValue v)
    {
        if (!acceptInput) return;
        moveInput = v.Get<Vector2>();
    }

    public void OnCast(InputValue v)
    {
        if (!acceptInput) return;
        Debug.Log("Interact pressed");
        if (animator) animator.SetTrigger(interactTrigger);
    }

    public void OnDebugDie(InputValue v)
    {
        if (!v.isPressed) return;
        PlayDeath();
    }

    public void OnJump(InputValue v)
    {
        if (!acceptInput || !BodyFree()) return;
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
        if (!BodyFree()) return; // <-- skip while kinematic during respawn

        Vector2 input = acceptInput ? moveInput : Vector2.zero;
        Vector3 move = new Vector3(input.x, 0f, input.y) * moveSpeed;
        rb.linearVelocity = new Vector3(move.x, rb.linearVelocity.y, move.z);
    }


    void Update()
    {
        // Ground check (sphere)
        isGrounded = Physics.CheckSphere(
            groundPoint ? groundPoint.position + Vector3.down * 0.02f : transform.position,
            groundRadius,
            whatIsGround,
            QueryTriggerInteraction.Ignore
        );

        // --- Facing: only update when there's X input; Z-only keeps last facing ---
        if (Mathf.Abs(moveInput.x) > 0.05f)
        {
            faceRight = moveInput.x > 0f;

            // Flip sprite renderer if provided (flipX=false → facing right)
            if (sprite) sprite.flipX = !faceRight;

            if (animator) animator.SetBool("FaceRight", faceRight);
        }

        // Animator booleans
        if (animator)
        {
            bool moving = acceptInput && moveInput.sqrMagnitude > 0.01f;
            animator.SetBool("IsMoving", moving);
            animator.SetBool("IsGrounded", isGrounded);
        }

        // Sprites Face Camera [Test]
        if (Camera.main) transform.forward = Camera.main.transform.forward;
    }

    void OnDrawGizmosSelected()
    {
        if (!groundPoint) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(groundPoint.position, groundRadius);
    }

    // ---------------------------
    // Animation helpers
    // ---------------------------

    public void PlayDeath()
    {
        if (animator && !string.IsNullOrEmpty(dieTrigger))
            animator.SetTrigger(dieTrigger);
        SetAcceptInput(false);
    }
}
