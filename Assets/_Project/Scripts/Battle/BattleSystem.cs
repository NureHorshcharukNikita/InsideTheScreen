using System;
using System.Collections;
using System.Collections.Generic;
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

    [Header("Quests")]
    [SerializeField] private List<QuestData> questCatalog = new();

    [Header("Turn Pacing")]
    [SerializeField] private float enemyIntentTelegraphDuration = 0.5f;
    [SerializeField] private float enemyIntentHideDuration = 0.15f;
    [SerializeField] private float enemyActionRecoveryDelay = 0.75f;
    [Tooltip("Pause between two abilities of the same enemy turn.")]
    [SerializeField] private float enemyActionGapDelay = 0.3f;

    private DeckManager deckManager;
    private CardPlayer cardPlayer;
    private TurnManager turnManager;
    private BattleEncounterResolver encounterResolver;
    private BattleSystemPlayerActions playerActions;
    private BattleSystemIntentBinder intentBinder;

    private int? selectedCardIndex = null;
    private bool turnTransitionInProgress;
    private bool skipTurnTransitionRequested;
    private bool victoryHandled;

    public bool IsTurnTransitionInProgress => turnTransitionInProgress;

    private void Start()
    {
        QuestManager.RegisterQuests(questCatalog);

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

        BattleFacing.FaceTowards(player, player.Data != null ? player.Data.profile : null, enemy.transform);
        BattleFacing.FaceTowards(enemy, enemy.Data != null ? enemy.Data.profile : null, player.transform);

        enemy.DeathPrevented += HandleEnemyDeathPrevented;
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
            HandleVictory();
            return;
        }

        if (player.CurrentHealth <= 0)
        {
            SetDefeat();
            handUI?.ReleaseAllBattleCardDrags();
            battleEndUI?.ShowDefeat();
        }
    }

    private void HandleVictory()
    {
        if (victoryHandled)
            return;

        victoryHandled = true;
        SetVictory();

        List<CardData> droppedLoot = RollAndCollectEnemyLoot();

        string defeatFlag = enemy != null && enemy.Data != null ? enemy.Data.storyFlagOnDefeat : null;
        if (!string.IsNullOrWhiteSpace(defeatFlag))
            StoryFlagManager.SetFlag(defeatFlag);

        ExplorationPlayerSession.SaveHealth(player.CurrentHealth);
        DefeatedEncounters.MarkDefeated(PendingBattleEnemy.EncounterId);
        QuestManager.HandleEncounterDefeated(PendingBattleEnemy.EncounterId, player.InventoryData);

        handUI?.ReleaseAllBattleCardDrags();
        battleEndUI?.ShowVictory(droppedLoot);
    }

    private List<CardData> RollAndCollectEnemyLoot()
    {
        List<CardData> droppedLoot = EnemyLootRoller.Roll(enemy != null ? enemy.Data : null);
        if (player?.InventoryData == null)
            return droppedLoot;

        for (int i = 0; i < droppedLoot.Count; i++)
            player.InventoryData.AddCard(droppedLoot[i]);

        return droppedLoot;
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
        if (turnManager == null || enemy?.Brain == null)
        {
            turnManager?.ExecuteEnemyTurn();
            yield break;
        }

        turnManager.BeginEnemyTurn();

        // Read after the turn has started: a stun may have just cut the plan short.
        var abilities = new List<EnemyAbilityData>(enemy.Brain.CurrentPlan.Abilities);
        enemy.TryGetComponent(out EnemyAttackLungeAnimation animation);

        for (int i = 0; i < abilities.Count; i++)
        {
            if (!enemy.IsAlive || !player.IsAlive)
                break;

            if (i > 0)
                yield return WaitSkippable(enemyActionGapDelay);

            EnemyAbilityData ability = abilities[i];
            Character target = enemy.Brain.GetPrimaryTarget(ability);
            bool executed = false;

            void ExecuteOnce()
            {
                if (executed)
                    return;

                executed = true;
                turnManager.ExecuteEnemyAbility(ability);
            }

            if (animation != null)
                yield return animation.Play(enemy, target, ExecuteOnce, ShouldSkipTurnTransition);
            else
                ExecuteOnce();

            if (!executed)
                ExecuteOnce();
        }

        turnManager.EndEnemyTurn();
    }

    // E.g. Seal of Immortality: announce it, and since the enemy may now fight differently
    // (a second phase), show what it will really do next.
    private void HandleEnemyDeathPrevented(BattleStatus preventedBy)
    {
        if (preventedBy is DeathWardStatus ward && battleTurnUI != null && !string.IsNullOrWhiteSpace(ward.Announcement))
            StartCoroutine(battleTurnUI.PlayAnnouncement(ward.Announcement));

        if (turnManager != null && turnManager.CurrentTurn == TurnOwner.Player)
        {
            turnManager.PlanNextEnemyAction();
            enemyIntentView?.RevealCurrentPlan();
        }
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
        if (enemy != null)
            enemy.DeathPrevented -= HandleEnemyDeathPrevented;
    }

    private void NotifyHandChanged()
    {
        HandChanged?.Invoke(deckManager, selectedCardIndex);
    }
}
