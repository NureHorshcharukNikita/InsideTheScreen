using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class EnemyIntentView : MonoBehaviour
{
    private const string NoPlanPlaceholder = "\u2014";

    [SerializeField] private EnemyBrain brain;
    [SerializeField] private TMP_Text intentLabel;
    [SerializeField] private Image intentIcon;
    [SerializeField] private GameObject intentContainer;

    [Header("Layout above sprite")]
    [SerializeField] private float aboveSpritePadding = 10f;

    private EnemyBrain subscribedBrain;
    private Character targetCharacter;
    private RectTransform rectTransformCache;
    private RectTransform followRectTransform;
    private Canvas canvasCache;

    private bool _pendingRevealAfterEnemyAct;
    private bool _awaitingHandDealFlyReveal;
    private Coroutine _revealRoutine;
    private Coroutine _battleStartRevealRoutine;
    private Color _labelBaseColor = Color.white;
    private Color _iconBaseColor = Color.white;
    private bool _capturedBaseColors;

    public void NotifyEnemyActed()
    {
        StopAllRevealCoroutines();
        _awaitingHandDealFlyReveal = false;
        _pendingRevealAfterEnemyAct = true;
        SetIntentVisualAlpha(0f);
    }

    public void NotifyHandDealFlyFinished()
    {
        StopBattleStartRevealRoutine();
        if (!_awaitingHandDealFlyReveal)
            return;

        _awaitingHandDealFlyReveal = false;
        StartRevealIntentAnimation();
    }

    public void ScheduleHandFlyRevealFallback()
    {
        if (!_awaitingHandDealFlyReveal)
            return;

        StopBattleStartRevealRoutine();
        _battleStartRevealRoutine = StartCoroutine(HandFlyRevealFallbackRoutine());
    }

    public void BindEnemy(EnemyCharacter enemyCharacter, bool deferInitialRevealUntilHandFlyFinishes = false)
    {
        StopAllRevealCoroutines();
        _pendingRevealAfterEnemyAct = false;
        _awaitingHandDealFlyReveal = false;

        UnsubscribeFromBrain();

        if (enemyCharacter == null)
        {
            brain = null;
            targetCharacter = null;
            Refresh();
            return;
        }

        targetCharacter = enemyCharacter;
        brain = enemyCharacter.Brain;
        SubscribeToBrain();

        if (deferInitialRevealUntilHandFlyFinishes)
        {
            _awaitingHandDealFlyReveal = true;
            Refresh(keepVisualHidden: true);
        }
        else
            Refresh();
    }

    private void Awake()
    {
        rectTransformCache = transform as RectTransform;
        canvasCache = GetComponentInParent<Canvas>();
        ResolveFollowRect();
    }

    private void OnEnable()
    {
        ResolveBrainIfMissing();
        ResolveTargetCharacterIfMissing();
        Refresh(keepVisualHidden: true);
    }

    private void OnDisable()
    {
        StopAllRevealCoroutines();
        UnsubscribeFromBrain();
    }

    private void OnPlannedChanged()
    {
        bool reveal = _pendingRevealAfterEnemyAct;
        _pendingRevealAfterEnemyAct = false;
        Refresh(keepVisualHidden: reveal);
        if (reveal)
            StartRevealIntentAnimation();
    }

    private void Refresh(bool keepVisualHidden = false)
    {
        ResolveBrainIfMissing();
        ResolveTargetCharacterIfMissing();
        SubscribeToBrain();

        if (intentLabel == null)
            return;

        CaptureBaseColorsIfNeeded();

        if (brain == null)
        {
            intentLabel.text = "";
            SetIntentContainerActive(false);
            intentLabel.enabled = false;
            ClearIcon();
            SetIntentVisualAlpha(1f);
            return;
        }

        string text = brain.CurrentPlan.HasAbility
            ? brain.CurrentPlan.GetIntentLabel()
            : NoPlanPlaceholder;

        intentLabel.text = text;

        bool show = !string.IsNullOrEmpty(text);
        SetIntentContainerActive(show);
        intentLabel.enabled = show;

        if (intentIcon != null)
        {
            Sprite s = brain.CurrentPlan.Ability != null ? brain.CurrentPlan.Ability.icon : null;
            intentIcon.sprite = s;
            intentIcon.enabled = show && s != null;
        }

        SetIntentVisualAlpha(keepVisualHidden ? 0f : 1f);
    }

    private void ResolveBrainIfMissing()
    {
        if (brain != null)
            return;

        brain = GetComponentInParent<EnemyBrain>();
        if (brain == null)
            brain = FindAnyObjectByType<EnemyBrain>();
    }

    private void ResolveTargetCharacterIfMissing()
    {
        if (targetCharacter != null)
            return;

        ResolveBrainIfMissing();
        if (brain != null)
            targetCharacter = brain.GetComponent<Character>();
    }

    private void SubscribeToBrain()
    {
        if (subscribedBrain == brain)
            return;

        UnsubscribeFromBrain();
        if (brain == null)
            return;

        brain.PlannedActionChanged += OnPlannedChanged;
        subscribedBrain = brain;
    }

    private void UnsubscribeFromBrain()
    {
        if (subscribedBrain == null)
            return;

        subscribedBrain.PlannedActionChanged -= OnPlannedChanged;
        subscribedBrain = null;
    }
}
