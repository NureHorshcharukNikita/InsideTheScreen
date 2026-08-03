using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EnemyIntentView : MonoBehaviour
{
    [SerializeField] private EnemyBrain brain;
    [SerializeField] private TMP_Text intentLabel;
    [SerializeField] private Image intentIcon;
    [SerializeField] private GameObject intentContainer;

    [Header("Layout above sprite")]
    [SerializeField] private float aboveSpritePadding = 10f;

    private EnemyIntentBrainBinding brainBinding;
    private EnemyIntentPresenter presenter;
    private EnemyIntentDisplay display;
    private EnemyIntentFollower follower;
    private EnemyIntentRevealAnimator revealAnimator;

    private bool awaitingHandDealFlyReveal;
    private bool hideUntilExplicitReveal;

    public IEnumerator ShowCurrentIntentDuringEnemyTurn(float visibleDuration, Func<bool> shouldSkip = null)
    {
        EnsureInitialized();
        revealAnimator.StopAll();
        hideUntilExplicitReveal = false;
        Refresh(keepVisualHidden: false);
        presenter.SetVisualAlpha(1f);

        if (visibleDuration <= 0f || IsSkipped(shouldSkip))
            yield break;

        float elapsed = 0f;
        while (elapsed < visibleDuration && !IsSkipped(shouldSkip))
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    public IEnumerator HideIntent(float fadeDuration = 0.15f, Func<bool> shouldSkip = null)
    {
        EnsureInitialized();
        revealAnimator.StopAll();
        hideUntilExplicitReveal = true;

        if (fadeDuration <= 0f || IsSkipped(shouldSkip))
        {
            presenter.SetVisualAlpha(0f);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < fadeDuration && !IsSkipped(shouldSkip))
        {
            elapsed += Time.deltaTime;
            presenter.SetVisualAlpha(1f - Mathf.Clamp01(elapsed / fadeDuration));
            yield return null;
        }

        presenter.SetVisualAlpha(0f);
    }

    public void SkipToHidden()
    {
        EnsureInitialized();
        revealAnimator.StopAll();
        hideUntilExplicitReveal = true;
        presenter.SetVisualAlpha(0f);
    }

    private static bool IsSkipped(Func<bool> shouldSkip)
    {
        return shouldSkip != null && shouldSkip();
    }

    public void RevealCurrentPlan()
    {
        EnsureInitialized();
        revealAnimator.StopAll();
        hideUntilExplicitReveal = false;
        Refresh(keepVisualHidden: false);
        revealAnimator.StartReveal();
    }

    public void NotifyEnemyActed()
    {
        EnsureInitialized();
        revealAnimator.StopAll();
        awaitingHandDealFlyReveal = false;
        presenter.SetVisualAlpha(0f);
    }

    public void NotifyHandDealFlyFinished()
    {
        EnsureInitialized();
        revealAnimator.StopFallback();
        if (!awaitingHandDealFlyReveal)
            return;

        awaitingHandDealFlyReveal = false;
        revealAnimator.StartReveal();
    }

    public void ScheduleHandFlyRevealFallback()
    {
        EnsureInitialized();
        if (!awaitingHandDealFlyReveal)
            return;

        revealAnimator.StartFallback();
    }

    public void BindEnemy(EnemyCharacter enemyCharacter, bool deferInitialRevealUntilHandFlyFinishes = false)
    {
        EnsureInitialized();
        revealAnimator.StopAll();
        awaitingHandDealFlyReveal = false;
        hideUntilExplicitReveal = false;

        brainBinding.BindEnemy(enemyCharacter);

        if (deferInitialRevealUntilHandFlyFinishes)
        {
            awaitingHandDealFlyReveal = true;
            Refresh(keepVisualHidden: true);
        }
        else
            Refresh();
    }

    private void Awake()
    {
        EnsureInitialized();
    }

    private void OnEnable()
    {
        EnsureInitialized();
        Refresh(keepVisualHidden: true);
    }

    private void OnDisable()
    {
        if (revealAnimator != null)
            revealAnimator.StopAll();
        if (brainBinding != null)
            brainBinding.Unsubscribe();
    }

    private void LateUpdate()
    {
        EnsureInitialized();
        follower.UpdatePosition(brainBinding.TargetCharacter);
    }

    private void OnPlannedChanged()
    {
        Refresh(keepVisualHidden: hideUntilExplicitReveal || awaitingHandDealFlyReveal);
    }

    private void Refresh(bool keepVisualHidden = false)
    {
        EnsureInitialized();
        brainBinding.ResolveMissingReferences();
        display.Refresh(brainBinding.Brain, keepVisualHidden);
    }

    private void OnRevealFallbackElapsed()
    {
        if (awaitingHandDealFlyReveal)
            NotifyHandDealFlyFinished();
        else
            Refresh();
    }

    private void EnsureInitialized()
    {
        if (presenter != null)
            return;

        presenter = new EnemyIntentPresenter(gameObject, intentLabel, intentIcon, intentContainer);
        brainBinding = new EnemyIntentBrainBinding(transform, brain, OnPlannedChanged);
        display = new EnemyIntentDisplay(presenter);
        follower = new EnemyIntentFollower(transform as RectTransform, intentContainer, GetComponentInParent<Canvas>(), aboveSpritePadding);
        revealAnimator = new EnemyIntentRevealAnimator(this, presenter.SetVisualAlpha, OnRevealFallbackElapsed);
    }

}
