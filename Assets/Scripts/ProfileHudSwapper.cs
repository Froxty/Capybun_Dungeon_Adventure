using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class ProfileHudSwapper : MonoBehaviour
{
    [Header("Capy/Bun Canvas")]
    public Canvas capyCanvas; // Capy_Profile_Canvas
    public Canvas bunCanvas;  // Bun_Profile_Canvas
    public int activeOrder = 10;
    public int inactiveOrder = 0;

    [Header("Capy/Bun Frames")]
    public RectTransform capyFrame;
    public RectTransform bunFrame;

    [Header("Health Bars")]
    public RectTransform capyHealth;
    public RectTransform bunHealth; 

    [Header("Frame Color Settings")]
    public Image capyFrameImage; // Assign the Image on Capy_Frame
    public Image bunFrameImage;  // Assign the Image on Bun_Frame
    public Color activeColor = Color.white;
    public Color inactiveColor = Color.black;
    public float colorFadeDuration = 0.25f;

    [Header("Tween")]
    public float duration = 0.25f;
    public Ease ease = Ease.OutBack;

    [Header("Scene starts with…")]
    public bool initialCapyActive = true;

    // Cache Layout
    RectLayout capyPortraitBig, capyPortraitSmall;
    RectLayout bunPortraitBig,  bunPortraitSmall;
    RectLayout capyHealthBig,   capyHealthSmall;
    RectLayout bunHealthBig,    bunHealthSmall;

    void Awake()
    {
        // Cache portrait layouts from current scene state
        if (initialCapyActive)
        {
            capyPortraitBig = Read(capyFrame);
            bunPortraitSmall = Read(bunFrame);
            bunPortraitBig = capyPortraitBig;
            capyPortraitSmall = bunPortraitSmall;
        }
        else
        {
            bunPortraitBig = Read(bunFrame);
            capyPortraitSmall = Read(capyFrame);
            capyPortraitBig = bunPortraitBig;
            bunPortraitSmall = capyPortraitSmall;
        }

        // Cache health layouts (if assigned)
        if (capyHealth && bunHealth)
        {
            if (initialCapyActive)
            {
                capyHealthBig = Read(capyHealth);
                bunHealthSmall = Read(bunHealth);
                bunHealthBig = capyHealthBig;
                capyHealthSmall = bunHealthSmall;
            }
            else
            {
                bunHealthBig = Read(bunHealth);
                capyHealthSmall = Read(capyHealth);
                capyHealthBig = bunHealthBig;
                bunHealthSmall = capyHealthSmall;
            }
        }

        // Initialize starting colors
        if (capyFrameImage) capyFrameImage.color = initialCapyActive ? activeColor : inactiveColor;
        if (bunFrameImage)  bunFrameImage.color  = initialCapyActive ? inactiveColor : activeColor;
    }

    public void Apply(bool capyActive)
    {
        // 1) Sorting order
        if (capyCanvas) capyCanvas.sortingOrder = capyActive ? activeOrder : inactiveOrder;
        if (bunCanvas)  bunCanvas.sortingOrder  = capyActive ? inactiveOrder : activeOrder;

        // 2) Portrait tweens
        TweenTo(capyFrame, capyActive ? capyPortraitBig  : capyPortraitSmall);
        TweenTo(bunFrame,  capyActive ? bunPortraitSmall : bunPortraitBig);

        // 3) Health tweens
        if (capyHealth && bunHealth)
        {
            TweenTo(capyHealth, capyActive ? capyHealthBig  : capyHealthSmall);
            TweenTo(bunHealth,  capyActive ? bunHealthSmall : bunHealthBig);
        }

        // 4) Frame color fade
        if (capyFrameImage) capyFrameImage.DOColor(capyActive ? activeColor : inactiveColor, colorFadeDuration);
        if (bunFrameImage)  bunFrameImage.DOColor(capyActive ? inactiveColor : activeColor, colorFadeDuration);
    }

    // Helpers
    struct RectLayout { public Vector2 anchorMin, anchorMax, pivot, anchoredPos, sizeDelta; }

    static RectLayout Read(RectTransform t) => new RectLayout
    {
        anchorMin   = t.anchorMin,
        anchorMax   = t.anchorMax,
        pivot       = t.pivot,
        anchoredPos = t.anchoredPosition,
        sizeDelta   = t.sizeDelta
    };

    void TweenTo(RectTransform t, RectLayout L)
    {
        if (!t) return;
        DOTween.Kill(t);
        var seq = DOTween.Sequence();
        seq.Join(t.DOAnchorMin(L.anchorMin, duration));
        seq.Join(t.DOAnchorMax(L.anchorMax, duration));
        seq.Join(t.DOPivot    (L.pivot,     duration));
        seq.Join(t.DOSizeDelta(L.sizeDelta, duration));
        seq.Join(t.DOAnchorPos(L.anchoredPos, duration));
        seq.SetEase(ease);
    }
}
