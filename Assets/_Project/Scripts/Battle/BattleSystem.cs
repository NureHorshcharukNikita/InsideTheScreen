using System.Collections.Generic;
using UnityEngine;

public class BattleSystem : MonoBehaviour
{
    [Header("Characters")]
    [SerializeField] private PlayerCharacter player;
    [SerializeField] private EnemyCharacter enemy;

    [Header("Deck")]
    [SerializeField] private List<CardData> startingDeck;

    private DeckManager deckManager;
    private CardPlayer cardPlayer;
    private TurnManager turnManager;

    private void Start()
    {
        BattleDebugPrinter.PrintCards("Starting deck", startingDeck);

        deckManager = new DeckManager();
        deckManager.Initialize(startingDeck);

        turnManager = new TurnManager(player, enemy, deckManager);
        cardPlayer = new CardPlayer(player, deckManager, turnManager);
        BattleDebugPrinter.PrintCards("Deck order", deckManager.Deck.Cards);

        turnManager.StartBattle();
        BattleDebugPrinter.PrintCards("Hand", deckManager.Hand.Cards);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
            PlayCardFromHand(0, enemy);

        if (Input.GetKeyDown(KeyCode.Alpha2))
            PlayCardFromHand(0, player);

        if (Input.GetKeyDown(KeyCode.Alpha3))
            PlayCardFromHand(1, enemy);

        if (Input.GetKeyDown(KeyCode.Alpha4))
            PlayCardFromHand(1, player);

        if (Input.GetKeyDown(KeyCode.Space))
            EndTurn();
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
        }
    }

    public void EndTurn()
    {
        turnManager.EndPlayerTurn();

        BattleDebugPrinter.PrintCards("Hand", deckManager.Hand.Cards);
        BattleStateChecker.Check(player, enemy);
    }
}