using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryPreviewPanel : MonoBehaviour
{
    [Header("Preview")]
    [SerializeField] private GameObject selectedCardContent;
    [SerializeField] private TMP_Text emptyPreviewText;
    [SerializeField] private CardView previewCard;
    [SerializeField] private TMP_Text descriptionText;

    [Header("Buttons")]
    [SerializeField] private Button addButton;
    [SerializeField] private Button removeButton;

    private CardData selectedCard;
    private bool selectedFromDeck;

    public CardData SelectedCard => selectedCard;
    public bool SelectedFromDeck => selectedFromDeck;

    private void Awake()
    {
        DisableButtonSelection(addButton);
        DisableButtonSelection(removeButton);
    }

    private void DisableButtonSelection(Button button)
    {
        var navigation = button.navigation;
        navigation.mode = Navigation.Mode.None;
        button.navigation = navigation;
    }

    public void Show(
        CardData card,
        bool fromDeck,
        bool canAdd,
        bool canRemove)
    {
        if (card == null)
            return;

        selectedCard = card;
        selectedFromDeck = fromDeck;

        selectedCardContent.SetActive(true);
        emptyPreviewText.gameObject.SetActive(false);

        previewCard.gameObject.SetActive(true);
        previewCard.Setup(card, null, 0, null);

        descriptionText.gameObject.SetActive(true);
        descriptionText.text = card.Description;
        RebuildDescriptionLayout();

        var scrollRect = descriptionText.GetComponentInParent<ScrollRect>();
        if (scrollRect != null)
            scrollRect.verticalNormalizedPosition = 1f;

        addButton.interactable = canAdd;
        removeButton.interactable = canRemove;
    }

    public void Clear()
    {
        selectedCard = null;
        selectedFromDeck = false;

        selectedCardContent.SetActive(false);

        emptyPreviewText.gameObject.SetActive(true);
        emptyPreviewText.text = "Select a card to view details";

        previewCard.gameObject.SetActive(false);

        descriptionText.text = "";

        addButton.interactable = false;
        removeButton.interactable = false;
    }

    private void RebuildDescriptionLayout()
    {
        descriptionText.ForceMeshUpdate();
        Canvas.ForceUpdateCanvases();

        RectTransform textRect = descriptionText.rectTransform;
        float availableWidth = Mathf.Max(1f, textRect.rect.width);
        float preferredHeight = descriptionText
            .GetPreferredValues(descriptionText.text, availableWidth, Mathf.Infinity)
            .y;

        textRect.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Vertical,
            Mathf.Ceil(preferredHeight));
        LayoutRebuilder.ForceRebuildLayoutImmediate(textRect);

        if (textRect.parent is RectTransform descriptionContent)
            LayoutRebuilder.ForceRebuildLayoutImmediate(descriptionContent);

        if (selectedCardContent.transform is RectTransform selectedContent)
            LayoutRebuilder.ForceRebuildLayoutImmediate(selectedContent);

        Canvas.ForceUpdateCanvases();
    }
}
