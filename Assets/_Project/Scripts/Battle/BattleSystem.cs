using System;
using System.Collections.Generic;
using UnityEngine;

public class BattleSystem : MonoBehaviour
{
    public BattleState CurrentBattleState { get; private set; } = BattleState.Running;

    public event Action<DeckManager, int?> HandChanged;

    [Header("Characters")]
    [SerializeField] private PlayerCharacter player;
    [SerializeField] private EnemyCharacter enemy;

    [Header("UI")]
    [SerializeField] private DeckUI deckUI;
    [SerializeField] private BattleEndUI battleEndUI;

    private DeckManager deckManager;
    private CardPlayer cardPlayer;
    private TurnManager turnManager;

    private int? selectedCardIndex = null;

    private void Start()
    {
        if (DeckProvider.Instance == null)
        {
            DevLog.Log("DeckProvider instance not found.");
            return;
        }

        var deck = DeckProvider.Instance.Deck;

        BattleDebugPrinter.PrintCards("Starting deck", deck.Cards);

        deckManager = new DeckManager();
        deckManager.Initialize(deck.Cards);

        deckUI.Bind(deckManager.Deck);

        turnManager = new TurnManager(player, enemy, deckManager);
        cardPlayer = new CardPlayer(player, deckManager, turnManager);

        BattleDebugPrinter.PrintCards("Deck order", deckManager.Deck.Cards);

        turnManager.StartBattle();
        BattleDebugPrinter.PrintCards("Hand", deckManager.Hand.Cards);

        NotifyHandChanged();
    }

    public void OnTargetClicked(IEffectTarget target)
    {
        if (!CanPlay()) return;

        if (selectedCardIndex == null)
            return;

        PlayCardFromHand(selectedCardIndex.Value, target);
        selectedCardIndex = null;

        NotifyHandChanged();
    }

    public void PlayCardFromHand(int index, IEffectTarget target)
    {
        if (!CanPlay()) return;

        if (index < 0 || index >= deckManager.Hand.Count)
            return;

        var card = deckManager.Hand.Cards[index];

        if (cardPlayer.TryPlayCard(index, card, target))
        {
            DevLog.Log("Played: " + card.CardName);

            BattleDebugPrinter.PrintCards("Hand", deckManager.Hand.Cards);
            BattleDebugPrinter.PrintCards("Discard", deckManager.DiscardPile.Cards);

            AfterAction();
            NotifyHandChanged();
        }
    }

    public void EndTurn()
    {
        if (!CanPlay()) return;

        turnManager.EndPlayerTurn();

        BattleDebugPrinter.PrintCards("Hand", deckManager.Hand.Cards);
        AfterAction();

        selectedCardIndex = null;

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
            battleEndUI.ShowVictory();
            return;
        }

        if (player.CurrentHealth <= 0)
        {
            SetDefeat();
            battleEndUI.ShowDefeat();
        }
    }

    public void SelectCard(int index)
    {
        if (!CanPlay()) return;

        if (index < 0 || index >= deckManager.Hand.Count)
            return;

        selectedCardIndex = index;

        DevLog.Log("Selected card: " + deckManager.Hand.Cards[index].CardName);
    }

    public void DeselectCard()
    {
        if (selectedCardIndex == null)
            return;

        selectedCardIndex = null;
        DevLog.Log("Card deselected");

        NotifyHandChanged();
    }

    private void NotifyHandChanged()
    {
        HandChanged?.Invoke(deckManager, selectedCardIndex);
    }
}