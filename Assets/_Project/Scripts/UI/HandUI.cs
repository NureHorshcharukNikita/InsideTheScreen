using UnityEngine;

public class HandUI : MonoBehaviour
{
    [SerializeField] private BattleSystem battleSystem;
    [SerializeField] private CardView cardPrefab;

    private Transform handPanel;

    private void Awake()
    {
        handPanel = transform;
    }

    private void OnEnable()
    {
        if (battleSystem == null)
            return;

        battleSystem.HandChanged += RefreshHand;
    }

    private void OnDisable()
    {
        if (battleSystem == null)
            return;

        battleSystem.HandChanged -= RefreshHand;
    }

    private void RefreshHand(DeckManager deckManager, int? selectedCardIndex)
    {
        ClearHand();
        CreateCardViews(deckManager);
    }

    private void ClearHand()
    {
        for (int i = handPanel.childCount - 1; i >= 0; i--)
            Destroy(handPanel.GetChild(i).gameObject);
    }

    private void CreateCardViews(DeckManager deckManager)
    {
        for (int i = 0; i < deckManager.Hand.Count; i++)
        {
            var cardData = deckManager.Hand.Cards[i];
            int index = i;

            CardView cardView = Instantiate(cardPrefab, handPanel);
            cardView.Setup(cardData, index, battleSystem.SelectCard);
        }
    }
}