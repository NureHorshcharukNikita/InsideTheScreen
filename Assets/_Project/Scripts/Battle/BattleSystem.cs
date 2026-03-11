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
    [SerializeField] private CardView cardPrefab;
    [SerializeField] private Transform handPanel;

    private DeckManager deckManager;
    private CardPlayer cardPlayer;
    private TurnManager turnManager;

    private int? selectedCardIndex = null;

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

        RefreshHandUI();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
            EndTurn();

        if (Input.GetKeyDown(KeyCode.E))
            OnTargetClicked(enemy);

        if (Input.GetKeyDown(KeyCode.P))
            OnTargetClicked(player);
    }

    public void OnTargetClicked(IEffectTarget target)
    {
        if (selectedCardIndex == null)
            return;

        PlayCardFromHand(selectedCardIndex.Value, target);
        selectedCardIndex = null;
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

            RefreshHandUI();
        }
    }

    public void EndTurn()
    {
        turnManager.EndPlayerTurn();

        BattleDebugPrinter.PrintCards("Hand", deckManager.Hand.Cards);
        BattleStateChecker.Check(player, enemy);

        selectedCardIndex = null;
        RefreshHandUI();
    }

    private void RefreshHandUI()
    {
        foreach (Transform child in handPanel)
            Destroy(child.gameObject);

        for (int i = 0; i < deckManager.Hand.Count; i++)
        {
            var cardData = deckManager.Hand.Cards[i];
            int index = i;

            CardView cardView = Instantiate(cardPrefab, handPanel);
            cardView.Setup(cardData, index, OnCardClicked);
        }
    }

    private void OnCardClicked(int index)
    {
        if (index < 0 || index >= deckManager.Hand.Count)
            return;

        selectedCardIndex = index;

        Debug.Log("Selected card: " + deckManager.Hand.Cards[index].CardName);
    }
}