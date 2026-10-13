using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// The HP bars of the fighters standing behind sit on a canvas drawn among the sprites, and the
// battle UI canvas takes the pointer first wherever it has a raycast target: its decorative
// images (the dark band, the hand panel, the HP bars) are no raycast targets, so the badges
// there still get pointer enter and exit.
public sealed class BattleEffectBadgeUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private Image iconImage;
    private TMP_Text valueText;
    private string tooltipTitle;
    private string tooltipBody;

    public void Build(BattleEffectDisplayEntry entry)
    {
        tooltipTitle = entry.Title;
        tooltipBody = entry.Tooltip;

        Image background = gameObject.AddComponent<Image>();
        background.color = new Color(0.03f, 0.025f, 0.025f, 0.28f);
        background.raycastTarget = true;

        RectTransform rect = (RectTransform)transform;
        rect.sizeDelta = new Vector2(55f, 49f);

        CreateIcon(entry.Icon, entry.FallbackLabel);
        CreateValue(entry.BadgeValue);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        BattleEffectTooltipUI.Show(tooltipTitle, tooltipBody, eventData.position, this);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        BattleEffectTooltipUI.HideFor(this);
    }

    // The badge going away under the cursor (rebuilt on an armor or status change, or its HP bar
    // hidden) gets no pointer exit, so it takes its tooltip along here.
    private void OnDisable()
    {
        BattleEffectTooltipUI.HideFor(this);
    }

    private void CreateIcon(Sprite icon, string fallbackLabel)
    {
        GameObject iconObject = new("Icon", typeof(RectTransform));
        iconObject.transform.SetParent(transform, false);

        RectTransform iconRect = (RectTransform)iconObject.transform;
        iconRect.anchorMin = new Vector2(0f, 0f);
        iconRect.anchorMax = new Vector2(1f, 1f);
        iconRect.offsetMin = new Vector2(4f, 4f);
        iconRect.offsetMax = new Vector2(-4f, -4f);

        if (icon != null)
        {
            iconImage = iconObject.AddComponent<Image>();
            iconImage.sprite = icon;
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;
            return;
        }

        TMP_Text label = iconObject.AddComponent<TextMeshProUGUI>();
        label.text = fallbackLabel;
        label.fontSize = 20f;
        label.alignment = TextAlignmentOptions.Center;
        label.color = new Color(0.96f, 0.91f, 0.86f, 1f);
        label.raycastTarget = false;
    }

    private void CreateValue(string value)
    {
        GameObject valueObject = new("Value", typeof(RectTransform));
        valueObject.transform.SetParent(transform, false);

        RectTransform valueRect = (RectTransform)valueObject.transform;
        valueRect.anchorMin = new Vector2(0f, 0f);
        valueRect.anchorMax = new Vector2(1f, 0.42f);
        valueRect.offsetMin = new Vector2(2f, 0f);
        valueRect.offsetMax = new Vector2(-2f, 0f);

        valueText = valueObject.AddComponent<TextMeshProUGUI>();
        valueText.text = value;
        valueText.fontSize = 18f;
        valueText.enableAutoSizing = true;
        valueText.fontSizeMin = 12f;
        valueText.fontSizeMax = 18f;
        valueText.fontStyle = FontStyles.Bold;
        valueText.alignment = TextAlignmentOptions.Center;
        valueText.color = Color.white;
        valueText.textWrappingMode = TextWrappingModes.NoWrap;
        valueText.overflowMode = TextOverflowModes.Ellipsis;
        valueText.raycastTarget = false;
    }
}
