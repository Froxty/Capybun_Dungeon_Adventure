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

    [Header("Interact Sound")]
    [Tooltip("AudioSource used to play the interact sound.")]
    public AudioSource interactAudioSource;

    [Tooltip("Sound played when the player uses Interact.")]
    public AudioClip interactSFX;

    // Animator Trigger Names
    string interactTrigger = "Interact";
    string dieTrigger = "Die";

    [Header("Input Routing")]
    public bool acceptInput = true; // toggled via SetAcceptInput()

    // --- Private ---
    Rigidbody rb;
    Vector2 moveInput;
    bool isGrounded;
    bool faceRight = true;  // last horizontal facing (true = right)
    Transform spriteTransform;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (whatIsGround.value == 0)
            whatIsGround = LayerMask.GetMask("Ground");

        // Auto-find sprite in children if not assigned
        if (!sprite)
            sprite = GetComponentInChildren<SpriteRenderer>();

        if (sprite)
            spriteTransform = sprite.transform;
    }

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
        if (!acceptInput || !v.isPressed) return;
        Debug.Log("[Player] Interact pressed.");

        // Play interact animation
        if (animator)
            animator.SetTrigger(interactTrigger);

        // Play interact SFX
        if (interactAudioSource && interactSFX)
            interactAudioSource.PlayOneShot(interactSFX);

        // Check for interactables in range
        Collider[] hits = Physics.OverlapSphere(transform.position, 1.5f);
        foreach (var hit in hits)
        {
            // 1) Gate opener
            var opener = hit.GetComponent<InteractGateOpener>();
            if (opener != null)
            {
                Debug.Log("[Player] Found InteractGateOpener — calling TryInteract()");
                opener.TryInteract();
                return; // stop after first successful interact
            }

            // 2) Item spawner
            var spawner = hit.GetComponent<InteractSpawnItem>();
            if (spawner != null)
            {
                Debug.Log("[Player] Found InteractSpawnItem — calling TryInteract()");
                spawner.TryInteract();
                return;
            }

            // 3) Key door
            var keyDoor = hit.GetComponent<InteractKeyDoorOpener>();
            if (keyDoor != null)
            {
                Debug.Log("[Player] Found InteractKeyDoorOpener — calling TryInteract()");
                keyDoor.TryInteract();
                return;
            }
        }
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

        // Sprites Face Camera (only rotate child visual)
        if (Camera.main && spriteTransform)
        {
            Vector3 fwd = Camera.main.transform.forward;
            
            spriteTransform.forward = fwd;
        }
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
