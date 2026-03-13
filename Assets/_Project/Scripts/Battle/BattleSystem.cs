using System;
using System.Collections.Generic;
using UnityEngine;

public class BattleSystem : MonoBehaviour
{
    [Header("Characters")]
    [SerializeField] private PlayerCharacter player;
    [SerializeField] private EnemyCharacter enemy;

    [Header("Deck")]
    [SerializeField] private List<CardData> startingDeck;

    [Header("UI")]
    [SerializeField] private DeckUI deckUI;

    private DeckManager deckManager;
    private CardPlayer cardPlayer;
    private TurnManager turnManager;

    private int? selectedCardIndex = null;

    public event Action<DeckManager, int?> HandChanged;

    private void Start()
    {
        BattleDebugPrinter.PrintCards("Starting deck", startingDeck);

        deckManager = new DeckManager();
        deckManager.Initialize(startingDeck);

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
        if (selectedCardIndex == null)
            return;

        PlayCardFromHand(selectedCardIndex.Value, target);
        selectedCardIndex = null;

        NotifyHandChanged();
    }

    public void PlayCardFromHand(int index, IEffectTarget target)
    {
        if (index < 0 || index >= deckManager.Hand.Count)
            return;

        var card = deckManager.Hand.Cards[index];

        if (cardPlayer.TryPlayCard(card, target))
        {
            Debug.Log("Played: " + card.CardName);

            BattleDebugPrinter.PrintCards("Hand", deckManager.Hand.Cards);
            BattleDebugPrinter.PrintCards("Discard", deckManager.DiscardPile.Cards);

            BattleStateChecker.Check(player, enemy);

            NotifyHandChanged();
        }
    }

    public void EndTurn()
    {
        turnManager.EndPlayerTurn();

        BattleDebugPrinter.PrintCards("Hand", deckManager.Hand.Cards);
        BattleStateChecker.Check(player, enemy);

        selectedCardIndex = null;

        NotifyHandChanged();
    }

    public void SelectCard(int index)
    {
        if (index < 0 || index >= deckManager.Hand.Count)
            return;

        selectedCardIndex = index;

        Debug.Log("Selected card: " + deckManager.Hand.Cards[index].CardName);
    }

    public void DeselectCard()
    {
        if (selectedCardIndex == null)
            return;

        selectedCardIndex = null;
        Debug.Log("Card deselected");

        NotifyHandChanged();
    }

    private void NotifyHandChanged()
    {
        HandChanged?.Invoke(deckManager, selectedCardIndex);
    }
}