
using Cysharp.Threading.Tasks;

using DG.Tweening;

using System;
using System.Collections.Generic;

using TMPro;

using UnityEngine;
using UnityEngine.UI;


[RequireComponent(typeof(MenuBook))]
[DefaultExecutionOrder(1)]
public class MenuBookInteractionLogic : MonoBehaviour
{
    [SerializeField] private HubManager hubManager = null;

    [Header("Game Canvas:")]
    [SerializeField] private GameObject gameCanvasObject = null;
    [SerializeField] private CanvasGroup gameCanvasGroup = null;

    [Header("Pause Canvas:")]
    [SerializeField] private GameObject pauseCanvasObject = null;
    [SerializeField] private CanvasGroup pauseCanvasGroup = null;
    [SerializeField] private List<MenuSection> menuSections = new();
    [SerializeField] private Image pauseBackgroundImage = null;

    [Header("Page Canvas:")]
    [SerializeField] private TextMeshProUGUI playResumeTMP = null;

    [Header("Fade Screen:")]
    [SerializeField] private GameObject fadeCanvasObject = null;

    private MenuBook menuBook = null;

    private bool inMainMenu = true;
    private float pausingAlpha = 0f;
    private bool isOpened = false;
    private int openedSectionIndex = 0;

    private Tween pausingTween = null;


    #region Lifecycle ============================================================================= Lifecycle

    protected virtual void Awake()
    {
        menuBook = GetComponent<MenuBook>();
        menuBook.SetInteractable(true);

        SetOpenedSection(0, forceSetInstantly: true);
        SetOpenedState(true, forceSetInstantly: true);
    }

    protected virtual void Update()
    {
        if (!inMainMenu && Input.GetKeyDown(KeyCode.Escape))
        { SetOpenedState(!isOpened); }
    }

    #endregion Lifecycle


    #region Button Methods ======================================================================== Button Methods

    public void B_CloseMenu() => SetOpenedState(false);

    public void B_OpenControls() => SetOpenedSection(0);

    public void B_OpenSettings() => SetOpenedSection(1);

    public void B_OpenCredits() => SetOpenedSection(2);

    public void B_Quit() => Application.Quit();

    public void B_ResetProgress() => hubManager.ResetProgress().Forget();

    #endregion Button Methods


    #region Private Methods ======================================================================= Private Methods

    private void SetOpenedState(bool newOpenedState, bool forceSetInstantly = false)
    {
        if (!forceSetInstantly && isOpened == newOpenedState) { return; }

        isOpened = newOpenedState;
        menuBook.SetInteractable(isOpened);
        
        if (isOpened)
        { Services.Game.Pause(); }
        else
        { Services.Game.Unpause(lockCursor: true); }

        if (pausingTween?.IsActive() ?? false)
        { pausingTween.Kill(); }

        if (forceSetInstantly)
        {
            SetPausingAlpha(isOpened ? 1f : 0f);
        }
        else
        {
            float startAlpha = pausingAlpha;
            float endAlpha = isOpened ? 1f : 0f;
            float duration = 0.5f * Mathf.Abs(endAlpha - startAlpha);

            pausingTween = DOVirtual.Float(startAlpha, endAlpha, duration, alpha =>
            {
                SetPausingAlpha(alpha);
            })
                .SetEase(Ease.InOutSine)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    if (!isOpened)
                    { SetOpenedSection(0, forceSetInstantly: true); }

                    if (inMainMenu)
                    {
                        inMainMenu = false;
                        playResumeTMP.text = "Resume";
                        pauseBackgroundImage.color = Color.black * 0.6f;
                    }
                });
        }
    }

    private void SetOpenedSection(int index, bool forceSetInstantly = false)
    {
        if (!forceSetInstantly && openedSectionIndex == index || index < 0 || index >= menuSections.Count) { return; }

        openedSectionIndex = index;

        for (int i = 0; i < menuSections.Count; i++)
        {
            bool isActive = i == index;

            MenuSection section = menuSections[i];
            section.FrameObject.SetActive(isActive);
            
            if (section.FadeTween != null && section.FadeTween.IsActive())
            { section.FadeTween.Kill(); }

            float startAlpha = section.FadeAlpha;
            float endAlpha = isActive ? 1.0f : 0.0f;
            float duration = forceSetInstantly ? 0f :
                ((isActive ? 0.1f : 0.5f) * Mathf.Abs(endAlpha - startAlpha));

            section.FadeTween = DOVirtual.Float(startAlpha, endAlpha, duration, alpha =>
            {
                Color color = Color.Lerp(Color.black, Color.white, alpha);
                section.ButtonText.color = color;
                section.ButtonImage.color = color;
                section.FadeAlpha = alpha;
            })
                .SetEase(Ease.InOutSine)
                .SetUpdate(true);
        }
    }

    private void SetPausingAlpha(float alpha)
    {
        pausingAlpha = Mathf.Clamp01(alpha);

        bool isGameVisible = pausingAlpha < 1f;
        bool isPausingVisible = pausingAlpha > 0f;

        menuBook.BookVisualObject.SetActive(isPausingVisible);
        menuBook.SetOpeningAlpha(pausingAlpha);

        gameCanvasObject.SetActive(isGameVisible);
        gameCanvasGroup.alpha = 1f - pausingAlpha;

        pauseCanvasObject.SetActive(isPausingVisible);
        pauseCanvasGroup.alpha = pausingAlpha;
    }

    #endregion Private Methods
}


[Serializable]
public class MenuSection
{
    public GameObject FrameObject = null;
    public TextMeshProUGUI ButtonText = null;
    public Image ButtonImage = null;

    [NonSerialized] public float FadeAlpha = 0f;
    [NonSerialized] public Tween FadeTween = null;
}