using TMPro;
using UnityEngine.UI;

public partial class CardView
{
    private void ApplyCardData(CardData data, int? count)
    {
        if (cardNameText != null)
            cardNameText.text = data != null ? data.CardName : string.Empty;

        if (cardCostText != null)
            cardCostText.text = data != null ? data.Cost.ToString() : string.Empty;

        if (cardDescriptionText != null)
            cardDescriptionText.text = data != null ? data.Description : string.Empty;

        ApplyCount(count);
        ApplyIcon(data);
    }

    private void ApplyCount(int? count)
    {
        if (countText == null)
            return;

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

    private void ApplyIcon(CardData data)
    {
        if (cardIcon == null)
            return;

        cardIcon.sprite = data != null ? data.Icon : null;
        cardIcon.enabled = data != null && data.Icon != null;
        cardIcon.preserveAspect = true;
    }
}
