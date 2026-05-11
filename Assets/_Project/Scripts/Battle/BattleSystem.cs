using System;
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
    [SerializeField] private HandUI handUI;
    [SerializeField] private EnemyIntentView enemyIntentView;

    private DeckManager deckManager;
    private CardPlayer cardPlayer;
    private TurnManager turnManager;
    private BattleEncounterResolver encounterResolver;
    private BattleSystemPlayerActions playerActions;
    private BattleSystemIntentBinder intentBinder;

    private int? selectedCardIndex = null;

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
            () => selectedCardIndex,
            value => selectedCardIndex = value);
        intentBinder = new BattleSystemIntentBinder(handUI, enemyIntentView);

        turnManager.StartBattle();
        intentBinder.WireForBattleStart(turnManager, enemy);
        NotifyHandChanged();
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
        return CurrentBattleState == BattleState.Running;
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
        playerActions?.EndTurn();
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
