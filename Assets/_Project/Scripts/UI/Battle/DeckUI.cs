using TMPro;
using UnityEngine;

public class DeckUI : MonoBehaviour
{
    [SerializeField] private TMP_Text valueText;

    private Deck deck;
    private int _displayCountBonus;

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
        _displayCountBonus = 0;

        if (this.deck != null)
        {
            this.deck.DeckCountChanged += RefreshDeck;
            ApplyDeckText(this.deck.Count, this.deck.MaxCount);
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

    public void BeginDeckCountFlyStagger(int cardsFlying)
    {
        _displayCountBonus = Mathf.Max(0, cardsFlying);
        if (deck != null)
            ApplyDeckText(deck.Count, deck.MaxCount);
    }

    public void OnDeckFlyCardArrived()
    {
        if (_displayCountBonus > 0)
            _displayCountBonus--;

        if (deck != null)
            ApplyDeckText(deck.Count, deck.MaxCount);
    }

    public void EndDeckCountFlyStagger()
    {
        _displayCountBonus = 0;
        if (deck != null)
            ApplyDeckText(deck.Count, deck.MaxCount);
        else
            SetEmptyValue();
    }

    private void RefreshDeck(int current, int max)
    {
        ApplyDeckText(current, max);
    }

    private void ApplyDeckText(int current, int max)
    {
        if (valueText == null)
            return;

        int shown = Mathf.Clamp(current + _displayCountBonus, 0, max);
        valueText.text = shown + " / " + max;
    }

    private void SetEmptyValue()
    {
        if (valueText == null)
            return;

        _displayCountBonus = 0;
        valueText.text = "0 / 0";
    }
}