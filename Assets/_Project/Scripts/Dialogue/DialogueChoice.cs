using System;

[Serializable]
public sealed class DialogueChoice
{
    public string text;

    [UnityEngine.Tooltip("Line to show after choosing this answer. -1 uses the current line flow.")]
    public int nextLineIndex = -1;

    public bool endDialogue;
}
