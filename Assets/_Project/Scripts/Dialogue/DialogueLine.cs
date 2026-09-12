using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class DialogueLine
{
    public DialogueSpeakerSide speakerSide = DialogueSpeakerSide.Right;
    public string speakerName;
    public Sprite portraitOverride;

    [TextArea(2, 5)]
    public string text;

    [Header("Flow")]
    [Tooltip("Line to show after this line. -1 continues to the next line.")]
    public int nextLineIndex = -1;
    public bool endDialogueAfterLine;

    [Header("Choices")]
    public List<DialogueChoice> choices = new();
}
