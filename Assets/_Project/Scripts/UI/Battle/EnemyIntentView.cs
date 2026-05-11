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

    private bool pendingRevealAfterEnemyAct;
    private bool awaitingHandDealFlyReveal;

    public void NotifyEnemyActed()
    {
        EnsureInitialized();
        revealAnimator.StopAll();
        awaitingHandDealFlyReveal = false;
        pendingRevealAfterEnemyAct = true;
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
        pendingRevealAfterEnemyAct = false;
        awaitingHandDealFlyReveal = false;

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
        bool reveal = pendingRevealAfterEnemyAct;
        pendingRevealAfterEnemyAct = false;
        Refresh(keepVisualHidden: reveal);
        if (reveal)
            revealAnimator.StartReveal();
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
