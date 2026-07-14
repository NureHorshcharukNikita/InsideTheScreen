using System;
using System.Collections;
using UnityEngine;

public class BattleSystem : MonoBehaviour
{
    public BattleState CurrentBattleState { get; private set; } = BattleState.Running;

    public event Action<DeckManager, int?> HandChanged;

    [Header("Characters")]
    [SerializeField] private EnemyCharacter enemy;
    [SerializeField] private PlayerCharacter player;

    [Header("UI")]
    [SerializeField] private DeckUI deckUI;
    [SerializeField] private BattleEndUI battleEndUI;
    [SerializeField] private BattleTurnUI battleTurnUI;
    [SerializeField] private HandUI handUI;
    [SerializeField] private EnemyIntentView enemyIntentView;

    [Header("Turn Pacing")]
    [SerializeField] private float enemyIntentTelegraphDuration = 0.5f;
    [SerializeField] private float enemyIntentHideDuration = 0.15f;
    [SerializeField] private float enemyActionRecoveryDelay = 0.75f;

    private DeckManager deckManager;
    private CardPlayer cardPlayer;
    private TurnManager turnManager;
    private BattleEncounterResolver encounterResolver;
    private BattleSystemPlayerActions playerActions;
    private BattleSystemIntentBinder intentBinder;

    private int? selectedCardIndex = null;
    private bool turnTransitionInProgress;
    private bool skipTurnTransitionRequested;

    public bool IsTurnTransitionInProgress => turnTransitionInProgress;

    private void Start()
    {
        if (player == null)
        {
            DevLog.Log("Player not assigned");
            return;
        }

        encounterResolver = new BattleEncounterResolver();
        enemy = encounterResolver.ResolveEnemy(enemy);
        if (enemy == null)
        {
            DevLog.Log("Enemy not assigned");
            return;
        }

        deckManager = new DeckManager();
        deckManager.Initialize(player.DeckData);

        BattleDebugPrinter.PrintCards("Starting deck", deckManager.Deck.Cards);

        deckUI.Bind(deckManager.Deck);

        turnManager = new TurnManager(player, enemy, deckManager);
        cardPlayer = new CardPlayer(player, deckManager, turnManager);
        playerActions = new BattleSystemPlayerActions(
            () => deckManager,
            () => cardPlayer,
            () => turnManager,
            CanPlay,
            AfterAction,
            NotifyHandChanged,
            RequestEndPlayerTurn,
            () => selectedCardIndex,
            value => selectedCardIndex = value);
        intentBinder = new BattleSystemIntentBinder(enemyIntentView);

        turnManager.StartBattle();
        intentBinder.WireForBattleStart(turnManager, enemy);
        StartCoroutine(StartBattleRoutine());
    }

    private IEnumerator StartBattleRoutine()
    {
        BeginTurnTransition();

        yield return null;
        while (FadeManager.Instance != null && FadeManager.Instance.IsFading)
            yield return null;

        if (battleTurnUI != null)
            yield return battleTurnUI.PlayTurnAnnouncement(TurnOwner.Player, ShouldSkipTurnTransition);

        enemyIntentView?.RevealCurrentPlan();

        turnManager.DrawStartingHand();
        NotifyHandChanged();

        EndTurnTransition();
    }

    public void RequestSkipTurnTransition()
    {
        if (!turnTransitionInProgress || CurrentBattleState != BattleState.Running)
            return;

        skipTurnTransitionRequested = true;
        battleTurnUI?.RequestSkip();
        enemyIntentView?.SkipToHidden();
    }

    private void BeginTurnTransition()
    {
        skipTurnTransitionRequested = false;
        turnTransitionInProgress = true;
    }

    private void EndTurnTransition()
    {
        skipTurnTransitionRequested = false;
        turnTransitionInProgress = false;
    }

    private bool ShouldSkipTurnTransition()
    {
        return skipTurnTransitionRequested;
    }

    private IEnumerator WaitSkippable(float seconds)
    {
        if (seconds <= 0f || ShouldSkipTurnTransition())
            yield break;

        float elapsed = 0f;
        while (elapsed < seconds && !ShouldSkipTurnTransition())
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    private void RequestEndPlayerTurn()
    {
        if (turnTransitionInProgress)
            return;

        StartCoroutine(EndPlayerTurnRoutine());
    }

    private IEnumerator EndPlayerTurnRoutine()
    {
        if (turnManager == null || !turnManager.TryBeginEnemyTurn())
            yield break;

        BeginTurnTransition();
        selectedCardIndex = null;

        if (battleTurnUI != null)
            yield return battleTurnUI.PlayTurnAnnouncement(TurnOwner.Enemy, ShouldSkipTurnTransition);

        if (enemyIntentView != null && !ShouldSkipTurnTransition())
        {
            yield return enemyIntentView.ShowCurrentIntentDuringEnemyTurn(
                enemyIntentTelegraphDuration,
                ShouldSkipTurnTransition);
            yield return enemyIntentView.HideIntent(enemyIntentHideDuration, ShouldSkipTurnTransition);
        }
        else
        {
            enemyIntentView?.SkipToHidden();
        }

        yield return PlayEnemyActionAnimation();

        yield return WaitSkippable(enemyActionRecoveryDelay);

        BattleDebugPrinter.PrintCards("Hand", deckManager.Hand.Cards);
        AfterAction();

        if (CurrentBattleState != BattleState.Running)
        {
            EndTurnTransition();
            yield break;
        }

        if (battleTurnUI != null && !ShouldSkipTurnTransition())
            yield return battleTurnUI.PlayTurnAnnouncement(TurnOwner.Player, ShouldSkipTurnTransition);
        else
            battleTurnUI?.ForceHide();

        turnManager.StartNextPlayerTurn();
        turnManager.PlanNextEnemyAction();
        enemyIntentView?.RevealCurrentPlan();
        selectedCardIndex = null;
        NotifyHandChanged();

        EndTurnTransition();
    }

    public void SetVictory()
    {
        CurrentBattleState = BattleState.Victory;
    }

    public void SetDefeat()
    {
        CurrentBattleState = BattleState.Defeat;
    }

    public bool CanPlay()
    {
        return CurrentBattleState == BattleState.Running
               && !turnTransitionInProgress
               && (handUI == null || !handUI.IsAnimatingCards);
    }

    public void AfterAction()
    {
        if (enemy.CurrentHealth <= 0)
        {
            SetVictory();
            ExplorationPlayerSession.SaveHealth(player.CurrentHealth);
            handUI?.ReleaseAllBattleCardDrags();
            battleEndUI?.ShowVictory();
            return;
        }

        if (player.CurrentHealth <= 0)
        {
            SetDefeat();
            handUI?.ReleaseAllBattleCardDrags();
            battleEndUI?.ShowDefeat();
        }
    }

    public void OnTargetClicked(ICombatant target)
    {
        playerActions?.OnTargetClicked(target);
    }

    public bool TryPlayCardFromHand(int index, ICombatant target)
    {
        return playerActions != null && playerActions.TryPlayCardFromHand(index, target);
    }

    public void EndTurn()
    {
        if (turnTransitionInProgress)
        {
            RequestSkipTurnTransition();
            return;
        }

        playerActions?.EndTurn();
    }

    private IEnumerator PlayEnemyActionAnimation()
    {
        if (turnManager == null || enemy?.Brain == null || !enemy.Brain.CurrentPlan.HasAbility)
        {
            turnManager?.ExecuteEnemyTurn();
            yield break;
        }

        enemy.TryGetComponent(out EnemyAttackLungeAnimation animation);
        Character target = enemy.Brain.CurrentPlan.PrimaryTargetForUi;
        bool executed = false;

        void ExecuteOnce()
        {
            if (executed)
                return;

            executed = true;
            turnManager.ExecuteEnemyTurn();
        }

        if (animation != null)
            yield return animation.Play(enemy, target, ExecuteOnce, ShouldSkipTurnTransition);
        else
            ExecuteOnce();

        if (!executed)
            ExecuteOnce();
    }

    public void SelectCard(int index)
    {
        playerActions?.SelectCard(index);
    }

    public void DeselectCard()
    {
        playerActions?.DeselectCard();
    }

    private void OnDestroy()
    {
        intentBinder?.Unwire();
    }

    private void NotifyHandChanged()
    {
        HandChanged?.Invoke(deckManager, selectedCardIndex);
    }
}
