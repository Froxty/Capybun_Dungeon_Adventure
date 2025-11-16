using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

public class PartySwitchManager : MonoBehaviour
{
    [Header("Characters")]
    public PlayerController capybarda;
    public PlayerController spellbun;

    [Header("Cameras")]
    public GameObject capybardaCamera;
    public GameObject spellbunCamera;
    public CinemachineCamera interactCamera;

    [Header("Interact Camera Settings")]
    public float interactFocusDuration = 1.5f;

    [Header("Events")]
    public UnityEvent<bool> onActiveChanged;

    int activeIndex = 0;
    bool inInteractFocus = false;
    Coroutine focusRoutine;

    void Start()
    {
        ApplyActive();
        if (interactCamera) interactCamera.gameObject.SetActive(false);
    }

    public void OnCameraSwitch(InputValue v)
    {
        if (!v.isPressed) return;
        if (inInteractFocus) return;

        activeIndex = (activeIndex + 1) % 2;
        ApplyActive();
    }

    void ApplyActive()
    {
        if (inInteractFocus) return;

        bool capyActive = activeIndex == 0;

        capybarda.SetAcceptInput(capyActive);
        spellbun.SetAcceptInput(!capyActive);

        if (capybardaCamera) capybardaCamera.SetActive(capyActive);
        if (spellbunCamera)  spellbunCamera.SetActive(!capyActive);
        if (interactCamera)  interactCamera.gameObject.SetActive(false);

        onActiveChanged?.Invoke(capyActive);
    }

    public void FocusOnTarget(Transform target)
    {
        if (!interactCamera || !target) return;

        if (focusRoutine != null)
            StopCoroutine(focusRoutine);

        focusRoutine = StartCoroutine(FocusRoutine(target));
    }

    IEnumerator FocusRoutine(Transform target)
    {
        inInteractFocus = true;

        // point interact camera at the target
        interactCamera.Follow = target;
        interactCamera.LookAt = target;

        // enable interact cam, disable char cams
        if (capybardaCamera) capybardaCamera.SetActive(false);
        if (spellbunCamera)  spellbunCamera.SetActive(false);
        interactCamera.gameObject.SetActive(true);

        // freeze input while focusing
        capybarda.SetAcceptInput(false);
        spellbun.SetAcceptInput(false);

        yield return new WaitForSeconds(interactFocusDuration);

        // turn interact cam off and restore normal
        interactCamera.gameObject.SetActive(false);
        capybarda.SetAcceptInput(true);
        spellbun.SetAcceptInput(true);

        inInteractFocus = false;
        ApplyActive();
        focusRoutine = null;
    }
}
