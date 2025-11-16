using System.Collections;
using UnityEngine;

public class InteractSpawnItem : MonoBehaviour
{
    [Header("Item To Spawn")]
    public GameObject itemPrefab;      // what to spawn
    public Transform spawnPoint;       // where to spawn it
    public float spawnDelay = 0.3f;    // delay before spawning (optional)

    [Header("Optional Camera Focus")]
    public PartySwitchManager partySwitchManager;
    public bool focusCameraOnSpawn = false;
    public float cameraDelay = 0.0f;   // delay before camera moves

    [Header("Sound")]
    public AudioSource audioSource;
    public AudioClip spawnSFX;

    bool playerInRange = false;
    bool used = false;                 // only spawn once

    void Start()
    {
        if (!itemPrefab)
            Debug.LogWarning($"[SpawnItem] No itemPrefab assigned on {name}.");
        if (!spawnPoint)
            Debug.LogWarning($"[SpawnItem] No spawnPoint assigned on {name}.");
    }

    void OnTriggerEnter(Collider other)
    {
        if (!used && other.CompareTag("Player"))
            playerInRange = true;
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
            playerInRange = false;
    }

    public void TryInteract()
    {
        if (!playerInRange || used || !itemPrefab || !spawnPoint) return;

        used = true;

        // Start spawn flow
        StartCoroutine(SpawnRoutine());

        // Optional camera focus like the gate
        if (focusCameraOnSpawn && partySwitchManager != null)
            StartCoroutine(DelayedCameraFocus());
    }

    IEnumerator SpawnRoutine()
    {
        if (spawnDelay > 0f)
            yield return new WaitForSeconds(spawnDelay);

        // SFX
        if (audioSource && spawnSFX)
            audioSource.PlayOneShot(spawnSFX);

        // Spawn item
        Instantiate(itemPrefab, spawnPoint.position, spawnPoint.rotation);

        // Optional: remove the trigger after using it
        // Destroy(gameObject);
    }

    IEnumerator DelayedCameraFocus()
    {
        if (cameraDelay > 0f)
            yield return new WaitForSeconds(cameraDelay);

        partySwitchManager.FocusOnTarget(spawnPoint);
    }
}
