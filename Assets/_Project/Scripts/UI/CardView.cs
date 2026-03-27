using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CardView : MonoBehaviour, IPointerDownHandler
{
    [SerializeField] private TMP_Text cardNameText;
    [SerializeField] private TMP_Text cardCostText;
    [SerializeField] private TMP_Text cardDescriptionText;
    [SerializeField] private TMP_Text countText;
    [SerializeField] private Image cardIcon;
    [SerializeField] private GameObject selectionBorder;

    private int cardIndex;
    private Action<int> onPointerDown;

    public void Setup(CardData data, int index, Action<int> onClick, bool selected = false)
    {
        Setup(data, null, index, onClick, selected);
    }

    public void Setup(CardData data, int? count, int index, Action<int> onClick, bool selected = false)
    {
        cardIndex = index;
        onPointerDown = onClick;

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

        SetSelected(selected);
    }

    public void SetSelected(bool selected)
    {
        if (selectionBorder != null)
            selectionBorder.SetActive(selected);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        onPointerDown?.Invoke(cardIndex);
    }
}