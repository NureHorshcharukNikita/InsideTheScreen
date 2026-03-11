using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CardView : MonoBehaviour
{
    [SerializeField] private TMP_Text cardNameText;
    [SerializeField] private TMP_Text cardCostText;
    [SerializeField] private TMP_Text cardDescriptionText;
    [SerializeField] private Button button;

    private int cardIndex;

    public void Setup(CardData data, int index, System.Action<int> onClick)
    {
        cardIndex = index;

        cardNameText.text = data.CardName;
        cardCostText.text = data.Cost.ToString();
        cardDescriptionText.text = data.Description;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => onClick(cardIndex));
    }
}