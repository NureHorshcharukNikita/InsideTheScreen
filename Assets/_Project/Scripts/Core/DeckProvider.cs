using System.Collections.Generic;
using UnityEngine;

public class DeckProvider : MonoBehaviour
{
    public static DeckProvider Instance { get; private set; }

    [Header("Debug")]
    [SerializeField] private int maxDeckSize = 16;
    [SerializeField] private List<CardData> testCards = new();

    public Deck Deck { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        Deck = new Deck(maxDeckSize);

        foreach (var card in testCards)
            Deck.Add(card);
    }
}