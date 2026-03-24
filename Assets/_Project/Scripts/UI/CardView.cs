using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CardView : MonoBehaviour
{
    [SerializeField] private TMP_Text cardNameText;
    [SerializeField] private TMP_Text cardCostText;
    [SerializeField] private TMP_Text cardDescriptionText;
    [SerializeField] private TMP_Text countText;
    [SerializeField] private Image cardIcon;
    [SerializeField] private Button button;

    private int cardIndex;

    public void Setup(CardData data, int index, System.Action<int> onClick)
    {
        Setup(data, null, index, onClick);
    }

    public void Setup(CardData data, int? count, int index, System.Action<int> onClick)
    {
        cardIndex = index;

        if (cardNameText != null)
            cardNameText.text = data.CardName;

        if (cardCostText != null)
            cardCostText.text = data.Cost.ToString();

        if (cardDescriptionText != null)
            cardDescriptionText.text = data.Description;

        if (countText != null)
        {
            if (count.HasValue)
            {
                countText.text = $"x{count.Value}";
                countText.gameObject.SetActive(true);
            }
            else
            {
                countText.gameObject.SetActive(false);
            }
        }

        if (cardIcon != null)
        {
            cardIcon.sprite = data.Icon;
            cardIcon.enabled = data.Icon != null;
            cardIcon.preserveAspect = true;
        }

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClick?.Invoke(cardIndex));
        }
    }
}