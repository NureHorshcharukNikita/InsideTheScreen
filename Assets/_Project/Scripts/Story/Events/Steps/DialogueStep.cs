using System;
using System.Collections;
using UnityEngine;

// Plays a dialogue and waits until it is closed. There is no NPC behind it: names and
// portraits come from the lines themselves. Quest actions of the lines run at the end as usual.
// One dialogue can be played in parts with other steps between them: a line with End Dialogue
// After Line closes it, and a later Dialogue step with Continue goes on after that line.
[Serializable, SubclassMenu("Story/Dialogue")]
public sealed class DialogueStep : StoryStep
{
    [SerializeField] private DialogueData dialogue;
    [Tooltip("Go on from where an earlier step of this event closed the same dialogue, instead of from the start.")]
    [SerializeField] private bool continueDialogue;

    public override IEnumerator Run(StoryEventContext context)
    {
        DialogueManager dialogueManager = DialogueManager.Instance;
        if (dialogue == null || dialogueManager == null)
            yield break;

        int startLine = continueDialogue ? context.GetDialogueStop(dialogue) : 0;
        bool finished = false;
        bool started = dialogueManager.StartDialogue(dialogue, startLine, null, nextLine =>
        {
            context.SetDialogueStop(dialogue, nextLine);
            finished = true;
        });
        if (!started)
            yield break;

        yield return new WaitUntil(() => finished);

        // The dialogue gives back the state it started in; keep the event in control.
        GameStateManager.SetState(GameState.Dialogue);
    }

    public override string ToString()
    {
        string title = $"Dialogue: {(dialogue != null ? dialogue.name : "(none)")}";
        return continueDialogue ? $"{title} (continued)" : title;
    }
}
