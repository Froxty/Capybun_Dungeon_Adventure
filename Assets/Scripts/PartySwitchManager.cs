using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class PartySwitchManager : MonoBehaviour
{
    [Header("Characters")]
    public PlayerController capybarda;
    public PlayerController spellbun;

    [Header("Optional cameras")]
    public GameObject capybardaCamera;
    public GameObject spellbunCamera;

    [Header("Events")]
    public UnityEvent<bool> onActiveChanged; // true = Capy active, false = Bun

    int activeIndex = 0;

    void Start() => ApplyActive();

    public void OnCameraSwitch(InputValue v)
    {
        if (!v.isPressed) return;
        activeIndex = (activeIndex + 1) % 2;
        ApplyActive();
    }

 void ApplyActive()
{
    bool capyActive = activeIndex == 0;

    capybarda.SetAcceptInput(capyActive);
    spellbun.SetAcceptInput(!capyActive);

    if (capybardaCamera) capybardaCamera.SetActive(capyActive);
    if (spellbunCamera)  spellbunCamera.SetActive(!capyActive);

    // Notify UI or other listeners
    onActiveChanged?.Invoke(capyActive);
}

}
