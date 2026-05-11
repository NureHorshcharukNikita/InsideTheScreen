using System;
using UnityEngine;

public partial class BattleSystem : MonoBehaviour
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

    private int? selectedCardIndex = null;

    private void Start()
    {
        if (player == null)
        {
            DevLog.Log("Player not assigned");
            return;
        }

        enemy = ResolveEnemyFromPendingEncounter(enemy);
        if (enemy == null)
        {
            DevLog.Log("Enemy not assigned");
            return;
        }

        var deck = new Deck(player.DeckData);

        BattleDebugPrinter.PrintCards("Starting deck", deck.Cards);

        deckManager = new DeckManager();
        deckManager.Initialize(deck.Cards);

        deckUI.Bind(deckManager.Deck);

        turnManager = new TurnManager(player, enemy, deckManager);
        cardPlayer = new CardPlayer(player, deckManager, turnManager);

        turnManager.StartBattle();
        WireEnemyIntentForBattleStart();
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

    private void NotifyHandChanged()
    {
        HandChanged?.Invoke(deckManager, selectedCardIndex);
    }
}
