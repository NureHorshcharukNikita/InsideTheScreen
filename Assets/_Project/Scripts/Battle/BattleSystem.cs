using System.Collections.Generic;
using UnityEngine;

public class BattleSystem : MonoBehaviour
{
    [Header("Characters")]
    [SerializeField] private PlayerCharacter player;
    [SerializeField] private EnemyCharacter enemy;

    [Header("Deck")]
    [SerializeField] private List<CardData> startingDeck;

    private Deck deck = new();
    private Hand hand = new();

    private void Start()
    {
        foreach (var card in startingDeck)
            deck.Add(card);

        deck.Shuffle();
        PrintDeck();

        DrawCards(5);
        PrintHand();
    }

    void PrintDeck()
    {
        Debug.Log("Deck order:");

        for (int i = 0; i < startingDeck.Count; i++)
        {
            Debug.Log(startingDeck[i].CardName);
        }
    }

    void DrawCards(int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            var card = deck.Draw();

            if (card != null)
            {
                hand.Add(card);
                Debug.Log("Draw card: " + card.CardName);
            }
        }
    }

    public void PlayCard(CardData card)
    {
        if (card == null)
            return;

        if (!player.SpendActionPoints(card.Cost))
        {
            Debug.Log("Not enough action points");
            return;
        }

        CardResolver.Resolve(card, player, enemy);
        hand.Remove(card);

        Debug.Log("Played: " + card.CardName);

        CheckBattleState();
    }

    public void EndTurn()
    {
        player.RestoreActionPoints();

        DrawCards(1);
        PrintHand();

        EnemyTurn();
    }

    void PrintHand()
    {
        Debug.Log("Hand:");

        for (int i = 0; i < hand.Cards.Count; i++)
        {
            Debug.Log(i + ": " + hand.Cards[i].CardName);
        }
    }

    void EnemyTurn()
    {
        Debug.Log("Enemy attacks!");

        player.TakeDamage(5);

        CheckBattleState();
    }

    private void CheckBattleState()
    {
        if (enemy.CurrentHealth <= 0)
        {
            Debug.Log("Enemy defeated!");
        }

        if (player.CurrentHealth <= 0)
        {
            Debug.Log("Player defeated!");
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
            PlayCardFromHand(0);

        if (Input.GetKeyDown(KeyCode.Alpha2))
            PlayCardFromHand(1);

        if (Input.GetKeyDown(KeyCode.Alpha3))
            PlayCardFromHand(2);

        if (Input.GetKeyDown(KeyCode.Alpha4))
            PlayCardFromHand(3);

        if (Input.GetKeyDown(KeyCode.Alpha5))
            PlayCardFromHand(4);

        if (Input.GetKeyDown(KeyCode.Space))
            EndTurn();
    }

    public void PlayCardFromHand(int index)
    {
        if (index < 0 || index >= hand.Cards.Count)
            return;

        PlayCard(hand.Cards[index]);
    }
}