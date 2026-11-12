using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum QuestTrackerStepState
{
    // The step to do now.
    Current,
    // A step after it.
    Later,
    Done
}

// One step of a quest in the quest tracker (QuestTrackerEntry): the box of the quest log, ticked once
// the step is done, what to do, and the quest's count on the step it belongs to ("2/4"). The step to
// do now is bright, the ones after it dimmer, done ones ticked, struck out and greyed. A step done
// while it is on screen is struck out from left to right, greying as it goes, and its box gives a
// little hop; so does the count when it moves.
public sealed class QuestTrackerStep : MonoBehaviour
{
    private const float HopSeconds = 0.35f;
    private const float HopScale = 0.4f;
    private const float StrikeSeconds = 0.7f;

    [SerializeField] private Image box;
    [SerializeField] private TMP_Text label;
    [Tooltip("The quest's count, on the right. Hidden on a step without one.")]
    [SerializeField] private TMP_Text count;
    [SerializeField] private Sprite uncheckedSprite;
    [SerializeField] private Sprite checkedSprite;

    [Header("Colours")]
    [SerializeField] private Color currentColor = new(0.96f, 0.91f, 0.84f, 1f);
    [SerializeField] private Color laterColor = new(0.8f, 0.76f, 0.7f, 1f);
    [SerializeField] private Color doneColor = new(0.62f, 0.58f, 0.53f, 1f);
    [SerializeField] private Color countColor = new(1f, 0.84f, 0.45f, 1f);
    [Tooltip("The box of a step after the one to do now.")]
    [SerializeField] private Color laterBoxColor = new(1f, 1f, 1f, 0.55f);

    // What it showed last, to move when it changes; nothing yet = no movement (just shown).
    private bool shownBefore;
    private string shownCount;
    private bool shownDone;
    private string words = string.Empty;
    // The colour it greys from while being struck out.
    private Color colorBefore;
    // How far the hops have got, 0 to 1; 1 = still.
    private float countHop = 1f;
    private float boxHop = 1f;
    // How much of it is struck out, 0 to 1 (only while done).
    private float strike;

    // Shown anew for another quest (or the same one coming back): the first look does not move.
    public void Forget()
    {
        shownBefore = false;
        countHop = 1f;
        boxHop = 1f;
        SetScale(count != null ? count.rectTransform : null, 1f);
        SetScale(box != null ? box.rectTransform : null, 1f);
    }

    public void Show(string text, string countText, QuestTrackerStepState state)
    {
        bool done = state == QuestTrackerStepState.Done;
        bool hasCount = !string.IsNullOrEmpty(countText);
        Color color = state switch
        {
            QuestTrackerStepState.Done => doneColor,
            QuestTrackerStepState.Later => laterColor,
            _ => currentColor
        };

        if (shownBefore && hasCount && countText != shownCount)
            countHop = 0f;

        // Done when first shown: struck out already. Just done, before the player's eyes: struck out
        // from here on. Done before: as far as the line has got.
        if (!done)
            strike = 0f;
        else if (!shownBefore)
            strike = 1f;
        else if (!shownDone)
        {
            strike = 0f;
            boxHop = 0f;
            colorBefore = label != null ? label.color : currentColor;
        }

        shownBefore = true;
        shownCount = countText;
        shownDone = done;
        words = text ?? string.Empty;

        if (box != null)
        {
            box.sprite = done ? checkedSprite : uncheckedSprite;
            box.color = state == QuestTrackerStepState.Later ? laterBoxColor : Color.white;
        }

        if (done)
            ShowStrike();
        else if (label != null)
        {
            SetLabel(words);
            label.color = color;
        }

        if (count == null)
            return;

        if (count.gameObject.activeSelf != hasCount)
            count.gameObject.SetActive(hasCount);

        if (!hasCount)
            return;

        if (count.text != countText)
            count.text = countText;

        count.color = done ? doneColor : countColor;
    }

    private void Update()
    {
        if (countHop < 1f)
        {
            countHop = Mathf.Min(1f, countHop + Time.unscaledDeltaTime / HopSeconds);
            SetScale(count != null ? count.rectTransform : null, Hop(countHop));
        }

        if (boxHop < 1f)
        {
            boxHop = Mathf.Min(1f, boxHop + Time.unscaledDeltaTime / HopSeconds);
            SetScale(box != null ? box.rectTransform : null, Hop(boxHop));
        }

        if (shownDone && strike < 1f)
        {
            strike = Mathf.Min(1f, strike + Time.unscaledDeltaTime / StrikeSeconds);
            ShowStrike();
        }
    }

    // The line through the words as far as it has got, the words greying as it goes.
    private void ShowStrike()
    {
        if (label == null)
            return;

        float eased = Mathf.SmoothStep(0f, 1f, strike);
        int through = Mathf.RoundToInt(words.Length * eased);
        SetLabel(through >= words.Length
            ? $"<s>{words}</s>"
            : through <= 0 ? words : $"<s>{words.Substring(0, through)}</s>{words.Substring(through)}");
        label.color = strike >= 1f ? doneColor : Color.Lerp(colorBefore, doneColor, eased);
    }

    private void SetLabel(string value)
    {
        if (label.text != value)
            label.text = value;
    }

    // Up and back down again.
    private static float Hop(float t)
    {
        return 1f + HopScale * Mathf.Sin(t * Mathf.PI);
    }

    private static void SetScale(RectTransform target, float scale)
    {
        if (target != null)
            target.localScale = new Vector3(scale, scale, 1f);
    }
}
