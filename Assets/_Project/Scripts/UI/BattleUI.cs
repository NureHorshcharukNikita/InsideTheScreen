using UnityEngine;

public class BattleUI : MonoBehaviour
{
    [SerializeField] private BattleSystem battleSystem;
    [SerializeField] private CardView cardPrefab;
    [SerializeField] private Transform handPanel;
    [SerializeField] private ActionPointsUI actionPointsUI;
    [SerializeField] private PlayerCharacter player;

    private void OnEnable()
    {
        player.ActionPointsChanged += RefreshActionPoints;
        battleSystem.HandChanged += RefreshHand;

        RefreshActionPoints(player.CurrentActionPoints, player.MaxActionPoints);
    }

    private void OnDisable()
    {
        player.ActionPointsChanged -= RefreshActionPoints;
        battleSystem.HandChanged -= RefreshHand;
    }

    private void RefreshActionPoints(int current, int max)
    {
        actionPointsUI.UpdatePoints(current, max);
    }

    private void RefreshHand(DeckManager deckManager, int? selectedCardIndex)
    {
        foreach (Transform child in handPanel)
            Destroy(child.gameObject);

        for (int i = 0; i < deckManager.Hand.Count; i++)
        {
            var cardData = deckManager.Hand.Cards[i];
            int index = i;
            bool isSelected = selectedCardIndex == index;

            CardView cardView = Instantiate(cardPrefab, handPanel);
            cardView.Setup(cardData, index, battleSystem.SelectCard);
        }
    }
}