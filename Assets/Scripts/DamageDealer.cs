using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Collider))]
public class DamageDealer : MonoBehaviour
{
    [Header("Damage Settings")]
    [Tooltip("How much damage this hit inflicts.")]
    [SerializeField] float damageAmount = 1f;

    [Tooltip("Destroy After Dealing Damage?")]
    [SerializeField] bool destroyOnHit = false;

    [Tooltip("Optional delay before self-destruction.")]
    [SerializeField] float destroyDelay = 0f;

    [Tooltip("If true, ignores multiple rapid collisions with the same object.")]
    [SerializeField] bool preventMultiHit = true;

    // Tracks what we've already hit (only used if preventMultiHit is true)
    readonly HashSet<PlayerHealth> _alreadyHit = new();

    void OnTriggerEnter(Collider other)
    {
        TryDealDamage(other);
    }

    void OnCollisionEnter(Collision collision)
    {
        TryDealDamage(collision.collider);
    }

    void TryDealDamage(Collider target)
    {
        if (!target) return;

        var health = target.GetComponent<PlayerHealth>();
        if (!health) return; // Not damageable

        if (preventMultiHit && _alreadyHit.Contains(health))
            return;

        health.TakeDamage(damageAmount);

        if (preventMultiHit)
            _alreadyHit.Add(health);

        if (destroyOnHit)
            Destroy(gameObject, destroyDelay);
    }
}
