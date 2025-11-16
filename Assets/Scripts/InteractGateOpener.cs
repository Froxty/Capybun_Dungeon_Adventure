using System.Collections;
using UnityEngine;

public class InteractGateOpener : MonoBehaviour
{
    [Header("Gate To Open")]
    [Tooltip("Gate Transform that will rotate open.")]
    public Transform gate;

    [Tooltip("World rotation the gate should end at when fully open.")]
    public Vector3 openRotation = new Vector3(0, 90, 0);

    [Tooltip("How fast the gate rotates from closed to open.")]
    public float openSpeed = 2f;

    [Header("Timing")]
    [Tooltip("Delay after the player presses Interact before the camera starts to pan. Use this to let the interact animation play.")]
    public float delayBeforeCameraPan = 0.25f;

    [Tooltip("Delay after the camera pan STARTS before the gate begins opening")]
    public float delayBeforeGateOpens = 0.5f;

    [Header("Camera Focus")]
    [Tooltip("PartySwitchManager that controls Capy/Bun cameras and the interact camera.")]
    public PartySwitchManager partySwitchManager;

    [Tooltip("If true, the camera will pan to the gate using the interact camera, then return.")]
    public bool focusCameraOnGate = true;

    [Header("Sound")]
    [Tooltip("AudioSource used to play the gate open SFX.")]
    public AudioSource audioSource;

    [Tooltip("Sound that plays once when the gate starts to open.")]
    public AudioClip openSFX;

    bool playerInRange = false;
    bool opened = false;

    void Start()
    {
        if (!gate)
            Debug.LogError($"[GateOpener] No gate assigned on {name}!");
    }

    void OnTriggerEnter(Collider other)
    {
        if (!opened && other.CompareTag("Player"))
            playerInRange = true;
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
            playerInRange = false;
    }

    public void TryInteract()
    {
        if (!playerInRange || opened || !gate) return;

        opened = true;
        StartCoroutine(GateSequence());
    }

    IEnumerator GateSequence()
    {
        // 1) Let the player interact animation play before ANY camera movement
        if (delayBeforeCameraPan > 0f)
            yield return new WaitForSeconds(delayBeforeCameraPan);

        // 2) Start camera pan & focus
        if (focusCameraOnGate && partySwitchManager != null)
        {
            partySwitchManager.FocusOnTarget(gate);
        }

        // 3) Wait for the camera to get over there
        if (delayBeforeGateOpens > 0f)
            yield return new WaitForSeconds(delayBeforeGateOpens);

        // 4) SFX right as the gate begins to move
        if (audioSource && openSFX)
            audioSource.PlayOneShot(openSFX);

        // 5) Smooth gate open
        Quaternion startRot = gate.rotation;
        Quaternion targetRot = Quaternion.Euler(openRotation);
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime * openSpeed;
            gate.rotation = Quaternion.Slerp(startRot, targetRot, t);
            yield return null;
        }
    }
}
