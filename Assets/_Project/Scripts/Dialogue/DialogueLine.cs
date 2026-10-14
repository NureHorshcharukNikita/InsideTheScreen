using System;
using System.Collections.Generic;
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
