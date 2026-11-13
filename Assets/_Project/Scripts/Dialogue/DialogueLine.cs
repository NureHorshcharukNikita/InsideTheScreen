using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

[Serializable]
public sealed class DialogueLine
{
    public DialogueSpeakerSide speakerSide = DialogueSpeakerSide.Right;
    public string speakerName;
    public Sprite portraitOverride;
    [Tooltip("No portrait while this line is said, e.g. a voice from the TV.")]
    public bool hidePortrait;
    [Tooltip("Sounds like this instead of the speaker's voice, e.g. the TV.")]
    public DialogueVoice voiceOverride;

    [TextArea(2, 5)]
    public string text;
    [Tooltip("Cards whose numbers the text reads out, as they are now: {card0.value} is the first one's main number (its damage or healing), {card0.name} its name. Empty = the text as written.")]
    public List<CardData> cards = new();
    [Tooltip("Fighters whose health the text reads out, from the data they fight with: {fighter0.maxHealth} is the first one's full health, e.g. Hideshi Battle Data. {haru.health} and {haru.maxHealth} read Haru's own, as he is now. Empty = none.")]
    public List<EnemyData> fighters = new();

    // The text with the cards' numbers and the health in it (Cards, Fighters), e.g. "The rock deals 4 damage".
    public string ShownText
    {
        get
        {
            if (string.IsNullOrEmpty(text) || text.IndexOf('{') < 0)
                return text;

            string shown = text;
            for (int i = 0; cards != null && i < cards.Count; i++)
            {
                CardData card = cards[i];
                if (card == null)
                    continue;

                string prefix = "{card" + i + ".";
                shown = shown.Replace(prefix + "value}", CardDescriptionFormatter.Format(card, "{value}"))
                    .Replace(prefix + "name}", card.CardName);
            }

            for (int i = 0; fighters != null && i < fighters.Count; i++)
            {
                if (fighters[i] != null)
                    shown = shown.Replace("{fighter" + i + ".maxHealth}", Number(Mathf.Max(1, fighters[i].maxHealth)));
            }

            if (shown.Contains("{haru."))
            {
                PlayerCharacter haru = UnityEngine.Object.FindFirstObjectByType<PlayerCharacter>(FindObjectsInactive.Include);
                if (haru != null)
                {
                    shown = shown.Replace("{haru.health}", Number(haru.CurrentHealth))
                        .Replace("{haru.maxHealth}", Number(haru.MaxHealth));
                }
            }

            return shown;
        }
    }

    private static string Number(int value)
    {
        return value.ToString(CultureInfo.InvariantCulture);
    }

    [Header("Flow")]
    [Tooltip("Said only when all of these are met; otherwise the dialogue goes on as if the line were not there. Empty = always said.")]
    public List<StoryCondition> conditions = new();
    [Tooltip("Line to show after this line. -1 continues to the next line.")]
    public int nextLineIndex = -1;
    public bool endDialogueAfterLine;
    public List<DialogueAction> actions = new();

    [Header("Choices")]
    public List<DialogueChoice> choices = new();
}
