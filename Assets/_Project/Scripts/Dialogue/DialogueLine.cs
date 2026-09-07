using System;
using UnityEngine;

[Serializable]
public sealed class DialogueLine
{
    public DialogueSpeakerSide speakerSide = DialogueSpeakerSide.Right;
    public string speakerName;
    public Sprite portraitOverride;

    [TextArea(2, 5)]
    public string text;
}
