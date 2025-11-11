using UnityEngine;
using UnityEngine.InputSystem;
using System;
using System.Collections;

[RequireComponent(typeof(PlayerController))]
[RequireComponent(typeof(Rigidbody))]
public class PlayerHealth : MonoBehaviour
{
    // ====== Minimal Inspector ======
    [Header("Health")]
    [SerializeField] float maxHealth = 10f;

    [Header("UI")]
    [SerializeField] HealthBar healthBar;

    [Header("Regen")]
    [SerializeField] float regenDelaySeconds = 5f;
    [SerializeField] float regenPerSecond    = 0.5f;

    [Header("Respawn")]
    [Tooltip("This character's spawn Transform (Capy uses Capy spawn, Bun uses Bun spawn).")]
    [SerializeField] Transform myRespawn;

    // ====== Internal constants (hidden) ======
    const string kDeathTag         = "Death"; // Animator state tag to wait for
    const string kIdleStateName    = "Idle";  // Animator idle state name
    const int    kIdleLayer        = 0;       // base layer
    const float  kFallbackRespawnS = 1.1f;    // used if we can't detect the tagged state

    // Party-wide signal: 1 player dies, BOTH respawn.
    public static event Action OnPartyRespawnRequested;
    public static event Action OnPlayerRespawned;

    static bool         s_partyRespawning = false; // guard double-requests
    static PlayerHealth s_lastDeceased    = null;  // who died
    static bool         s_dyingWasActive  = false; // did they have control right before death?

    PlayerController controller;
    Rigidbody        rb;

    float currentHealth;
    float lastDamageTime = -999f;
    bool  isDead = false;

    // Reset statics on domain reload / scene load so states don't leak between plays
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        s_partyRespawning = false;
        s_lastDeceased    = null;
        s_dyingWasActive  = false;
    }

    void Awake()
    {
        currentHealth = maxHealth;

        controller = GetComponent<PlayerController>();
        rb         = GetComponent<Rigidbody>();

        if (healthBar) healthBar.UpdateHealthBar(maxHealth, currentHealth);
        else Debug.LogWarning($"[PlayerHealth] No HealthBar assigned for {name}.");

        OnPartyRespawnRequested += HandlePartyRespawnRequested;
    }

    void OnDestroy()
    {
        OnPartyRespawnRequested -= HandlePartyRespawnRequested;
        if (s_lastDeceased == this) s_lastDeceased = null;
    }

    void Update()
    {
        if (isDead) return;

        if (currentHealth < maxHealth && (Time.time - lastDamageTime) >= regenDelaySeconds)
        {
            float old = currentHealth;
            currentHealth = Mathf.Min(maxHealth, currentHealth + regenPerSecond * Time.deltaTime);
            if (healthBar && !Mathf.Approximately(old, currentHealth))
                healthBar.UpdateHealthBar(maxHealth, currentHealth);
        }
    }

    // ===== Debug (optional) =====
    public void OnDebugDamage(InputValue v)
    {
        if (v.isPressed && (controller == null || controller.acceptInput))
            TakeDamage(1f);
    }
    public void OnDebugHeal(InputValue v)
    {
        if (v.isPressed && (controller == null || controller.acceptInput))
            Heal(1f);
    }
    // ============================

    public void TakeDamage(float amount)
    {
        if (isDead) return;

        float old = currentHealth;
        currentHealth = Mathf.Clamp(currentHealth - amount, 0f, maxHealth);
        lastDamageTime = Time.time;

        if (healthBar && !Mathf.Approximately(old, currentHealth))
            healthBar.UpdateHealthBar(maxHealth, currentHealth);

        if (currentHealth <= 0f) Die();
    }

    public void Heal(float amount)
    {
        if (isDead) return;

        float old = currentHealth;
        currentHealth = Mathf.Clamp(currentHealth + amount, 0f, maxHealth);

        if (healthBar && !Mathf.Approximately(old, currentHealth))
            healthBar.UpdateHealthBar(maxHealth, currentHealth);
    }

    // --- Death / Party Respawn ---
    void Die()
    {
        if (isDead) return;
        isDead = true;

        // Record who died and whether they currently had control.
        s_lastDeceased   = this;
        s_dyingWasActive = controller && controller.acceptInput;

        // Lock input + stop motion
        if (controller) controller.SetAcceptInput(false);
        ZeroVelocity(); // body is dynamic here, safe to zero

        // Play death animation
        if (controller && controller.animator) controller.PlayDeath();

        StartCoroutine(WaitDeathThenRequestPartyRespawn());
    }

    IEnumerator WaitDeathThenRequestPartyRespawn()
    {
        bool usedTagWait = false;
        float start = Time.time;

        if (controller && controller.animator)
        {
            var anim = controller.animator;

            // Wait to ENTER the Death-tagged state (timeout to be safe)
            float timeoutEnter = 1f;
            while (!anim.GetCurrentAnimatorStateInfo(0).IsTag(kDeathTag) &&
                   (Time.time - start) < timeoutEnter)
                yield return null;

            // If we entered it, wait to EXIT it
            if (anim.GetCurrentAnimatorStateInfo(0).IsTag(kDeathTag))
            {
                usedTagWait = true;
                while (anim.GetCurrentAnimatorStateInfo(0).IsTag(kDeathTag))
                    yield return null;
            }
        }

        if (!usedTagWait)
            yield return new WaitForSeconds(kFallbackRespawnS);

        if (!s_partyRespawning)
        {
            s_partyRespawning = true;
            OnPartyRespawnRequested?.Invoke();
            s_partyRespawning = false;
        }
    }

    void HandlePartyRespawnRequested()
    {
        StartCoroutine(RespawnRoutine());
    }

    IEnumerator RespawnRoutine()
    {
        if (myRespawn)
            transform.SetPositionAndRotation(myRespawn.position, myRespawn.rotation);
        else
            Debug.LogWarning($"[PlayerHealth] {name} missing myRespawn; so can't teleport");

        ZeroVelocity();
        yield return null;

        // Restore health/UI
        currentHealth = maxHealth;
        if (healthBar) healthBar.UpdateHealthBar(maxHealth, currentHealth);

        // Ensure Animator is out of Death
        if (controller && controller.animator)
        {
            controller.animator.ResetTrigger("Die");
            controller.animator.CrossFade(kIdleStateName, 0f, kIdleLayer, 0f);
        }

        // Decide who gets control after the party respawn:
        // - If you are the last deceased → you get control iff s_dyingWasActive
        // - If you are the survivor      → you get control iff !s_dyingWasActive
        bool shouldBeActive =
            (this == s_lastDeceased) ? s_dyingWasActive : !s_dyingWasActive;

        isDead = false;
        lastDamageTime = Time.time; // avoid instant regen tick
        if (controller) controller.SetAcceptInput(shouldBeActive);

        OnPlayerRespawned?.Invoke();
    }

    void ZeroVelocity()
    {
        if (!rb) return;
        if (rb.isKinematic) return; // safety net
        rb.linearVelocity = Vector3.zero;  
        rb.angularVelocity = Vector3.zero;
    }
}
