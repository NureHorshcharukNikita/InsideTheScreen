using System;
using System.Collections;
using UnityEngine;

// Plays a dialogue and waits until it is closed. There is no NPC behind it: names and
// portraits come from the lines themselves. Quest actions of the lines run at the end as usual.
[Serializable, SubclassMenu("Story/Dialogue")]
public sealed class DialogueStep : StoryStep
{
    [SerializeField] private DialogueData dialogue;

    public override IEnumerator Run(StoryEventContext context)
    {
        DialogueManager dialogueManager = DialogueManager.Instance;
        if (dialogue == null || dialogueManager == null)
            yield break;

        bool finished = false;
        if (!dialogueManager.StartDialogue(dialogue, null, () => finished = true))
            yield break;

        yield return new WaitUntil(() => finished);

        // The dialogue gives back the state it started in; keep the event in control.
        GameStateManager.SetState(GameState.Dialogue);
    }

    public override string ToString()
    {
        return $"Dialogue: {(dialogue != null ? dialogue.name : "(none)")}";
    }
}
