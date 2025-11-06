using UnityEngine;
using UnityEngine.InputSystem;
using System;
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    public float maxHealth = 10f;

    [Header("Health Bar")]
    public HealthBar healthBar;

    [Header("Regen")]
    public float regenDelaySeconds = 5f;   // wait time after last damage
    public float regenPerSecond    = 0.5f; // heal speed once regen starts

    public static event Action OnPlayerRespawned;

    // optional: hook to your existing PlayerController so only the active one gets debug input
    [Header("Optional: gate debug input by active controller")]
    public PlayerController controller;

    float currentHealth;
    float lastDamageTime = -999f;

    void Awake()
    {
        currentHealth = maxHealth;

        if (!controller) controller = GetComponent<PlayerController>();

        if (!healthBar)
        {
            Debug.LogWarning($"[PlayerHealth] No HealthBar assigned for {name}! Please assign manually.");
        }
        else
        {
            healthBar.UpdateHealthBar(maxHealth, currentHealth);
        }
    }

    void Update()
    {
        // Passive regen if enough time has passed and we're not full
        if (currentHealth < maxHealth && (Time.time - lastDamageTime) >= regenDelaySeconds)
        {
            float old = currentHealth;
            currentHealth = Mathf.Min(maxHealth, currentHealth + regenPerSecond * Time.deltaTime);
            if (healthBar && !Mathf.Approximately(old, currentHealth))
                healthBar.UpdateHealthBar(maxHealth, currentHealth);
        }
    }

    // ===== Debug Controls (optional) =====
    public void OnDebugDamage(InputValue v)
    {
        // Only respond if this player is the active one (acceptInput true)
        if (v.isPressed && (controller == null || controller.acceptInput))
            TakeDamage(1f);
    }

    public void OnDebugHeal(InputValue v)
    {
        if (v.isPressed && (controller == null || controller.acceptInput))
            Heal(1f);
    }
    // ====================================

    public void TakeDamage(float amount)
    {
        float old = currentHealth;
        currentHealth = Mathf.Clamp(currentHealth - amount, 0f, maxHealth);
        lastDamageTime = Time.time; // reset regen timer

        if (healthBar && !Mathf.Approximately(old, currentHealth))
            healthBar.UpdateHealthBar(maxHealth, currentHealth);

        // if (currentHealth <= 0f) Die();
    }

    public void Heal(float amount)
    {
        float old = currentHealth;
        currentHealth = Mathf.Clamp(currentHealth + amount, 0f, maxHealth);

        if (healthBar && !Mathf.Approximately(old, currentHealth))
            healthBar.UpdateHealthBar(maxHealth, currentHealth);
    }

    // --- Respawn---
    /*
    void Die()
    {
        Debug.Log("[PlayerHealth] Player has died.");
        if (respawnPoint && playerRoot) StartCoroutine(HandleRespawn());
        else Debug.LogWarning("[PlayerHealth] Missing respawnPoint or playerRoot.");
    }

    IEnumerator HandleRespawn()
    {
        var cc = playerRoot.GetComponentInChildren<CharacterController>();
        if (cc) cc.enabled = false;
        yield return null;
        playerRoot.transform.position = respawnPoint.position;
        yield return null;
        if (cc) cc.enabled = true;

        currentHealth = maxHealth;
        if (healthBar) healthBar.UpdateHealthBar(maxHealth, currentHealth);

        Debug.Log($"[PlayerHealth] Respawned at {respawnPoint.position}");
        OnPlayerRespawned?.Invoke();
    }
    */
}
