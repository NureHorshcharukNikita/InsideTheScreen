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
    private DiscardPile discardPile = new();

    private void Start()
    {
        PrintStartingDeck();

        foreach (var card in startingDeck)
            deck.Add(card);

        deck.Shuffle();
        PrintDeck();

        DrawCards(5);
        PrintHand();
    }

    void PrintStartingDeck()
    {
        string result = "Starting deck:\n";

        for (int i = 0; i < startingDeck.Count; i++)
        {
            result += (i + 1) + ": " + startingDeck[i].CardName + '\n';
        }

        Debug.Log(result);
    }

    void PrintDeck()
    {
        string result = "Deck order:\n";

        for (int i = 0; i < deck.Cards.Count; i++)
        {
            result += (i + 1) + ": " + deck.Cards[i].CardName + '\n';
        }

        Debug.Log(result);
    }

    void DrawCards(int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            if (deck.Count == 0 && hand.Cards.Count == 0)
            {
                ReshuffleDiscard();
            }

            var card = deck.Draw();

            if (card != null)
            {
                hand.Add(card);
                Debug.Log("Draw card: " + card.CardName);
            }
        }
    }

    public void PlayCard(CardData card, IEffectTarget target)
    {
        if (card == null)
            return;

        if (!CanUseCardOnTarget(card, target))
        {
            Debug.Log($"{card.CardName} cannot be used on this target");
            return;
        }

        if (!player.SpendActionPoints(card.Cost))
        {
            Debug.Log("Not enough action points");
            return;
        }

        CardResolver.Resolve(card, player, target);

        hand.Remove(card);
        discardPile.Add(card);

        Debug.Log("Played: " + card.CardName);
        PrintHand();

        CheckBattleState();
    }

    public void EndTurn()
    {
        EnemyTurn();

        player.RestoreActionPoints();

        DrawCards(1);
        PrintHand();
    }

    void PrintHand()
    {
        string result = "Hand:\n";

        for (int i = 0; i < hand.Cards.Count; i++)
        {
            result += (i + 1) + ": " + hand.Cards[i].CardName + '\n';
        }

        Debug.Log(result);
    }

    void ReshuffleDiscard()
    {
        if (discardPile.Count == 0)
            return;

        Debug.Log("Reshuffling discard pile");

        foreach (var card in discardPile.Cards)
            deck.Add(card);

        discardPile.Clear();
        deck.Shuffle();
        PrintDeck();
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

    bool CanUseCardOnTarget(CardData card, IEffectTarget target)
    {
        foreach (var entry in card.Effects)
        {
            if (entry.effect == null)
                continue;

            if (entry.targetType == EffectTargetType.Enemy && ReferenceEquals(target, enemy))
                return true;

            if (entry.targetType == EffectTargetType.Self && ReferenceEquals(target, player))
                return true;
        }

        return false;
    }

    public void PlayCardFromHand(int index, IEffectTarget target)
    {
        if (index < 0 || index >= hand.Cards.Count)
            return;

        PlayCard(hand.Cards[index], target);
    }
}