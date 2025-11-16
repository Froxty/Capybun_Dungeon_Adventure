using UnityEngine;
using DG.Tweening;

public class UIInteractPopup : MonoBehaviour
{
    [Header("UI Panel (root object)")]
    public RectTransform uiPanel;

    [Header("Tween Settings")]
    public float fadeDuration = 0.25f;
    public float scaleIn = 1f;
    public float scaleOut = 0.75f;
    public Ease ease = Ease.OutBack;

    CanvasGroup canvasGroup;
    bool playerInside = false;
    Tween currentTween;

    void Awake()
    {
        if (uiPanel == null)
        {
            Debug.LogError("[UIInteractPopup] No UI panel assigned!", this);
            enabled = false;
            return;
        }

        canvasGroup = uiPanel.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = uiPanel.gameObject.AddComponent<CanvasGroup>();

        // Start hidden
        uiPanel.localScale = Vector3.one * scaleOut;
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        playerInside = true;
        ShowUI();
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        playerInside = false;
        HideUI();
    }

    void ShowUI()
    {
        currentTween?.Kill();

        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        currentTween = DOTween.Sequence()
            .Join(canvasGroup.DOFade(1f, fadeDuration))
            .Join(uiPanel.DOScale(scaleIn, fadeDuration))
            .SetEase(ease);
    }

    void HideUI()
    {
        currentTween?.Kill();

        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        currentTween = DOTween.Sequence()
            .Join(canvasGroup.DOFade(0f, fadeDuration))
            .Join(uiPanel.DOScale(scaleOut, fadeDuration))
            .SetEase(Ease.InSine);
    }
}
