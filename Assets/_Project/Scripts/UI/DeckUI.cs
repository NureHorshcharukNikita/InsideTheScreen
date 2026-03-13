using TMPro;
using UnityEngine;

public class DeckUI : MonoBehaviour
{
    [SerializeField] private TMP_Text valueText;

    private Deck deck;

    public void Bind(Deck deck)
    {
        if (this.deck != null)
            this.deck.DeckCountChanged -= RefreshDeck;

        this.deck = deck;

        if (this.deck != null)
        {
            this.deck.DeckCountChanged += RefreshDeck;
            RefreshDeck(this.deck.Count, this.deck.MaxCount);
        }
    }

    private void OnDisable()
    {
        if (deck == null)
            return;

        deck.DeckCountChanged -= RefreshDeck;
    }

    private void RefreshDeck(int current, int max)
    {
        valueText.text = current + " / " + max;
    }
}
