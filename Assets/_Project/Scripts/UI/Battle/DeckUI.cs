using TMPro;
using UnityEngine;

public class DeckUI : MonoBehaviour
{
    [SerializeField] private TMP_Text valueText;

    private Deck deck;

    private void OnEnable()
    {
        if (deck == null)
            SetEmptyValue();
    }

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
        else
        {
            SetEmptyValue();
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
        if (valueText == null)
            return;

        valueText.text = current + " / " + max;
    }

    private void SetEmptyValue()
    {
        if (valueText == null)
            return;

        valueText.text = "0 / 0";
    }
}