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

}
