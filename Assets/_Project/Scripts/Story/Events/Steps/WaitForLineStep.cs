using System;
using System.Collections;
using UnityEngine;

// In a Parallel beside a Dialogue (or Walk Up and Talk) step: waits until a line of that dialogue
// starts, e.g. Brannoc setting off down the south road as Hideshi answers Haru's first line, or for
// the crossroads as Eldon senses they are not from here. Goes on as well once the dialogue is over
// without it (skipped, or the line not said), or if it never starts.
[Serializable, SubclassMenu("Flow/Wait For Dialogue Line")]
public sealed class WaitForLineStep : StoryStep
{
    [SerializeField] private DialogueData dialogue;
    [Tooltip("The line it waits for, counted from 1 as they are listed, e.g. 2 = as the second line starts.")]
    [SerializeField, Min(1)] private int line = 2;
    [Tooltip("How long the dialogue may take to start before it is taken as not coming, in seconds, e.g. longer beside a Walk Up and Talk that walks Haru over first.")]
    [SerializeField, Min(0f)] private float startsWithin = 2f;

    public override IEnumerator Run(StoryEventContext context)
    {
        DialogueLine wanted = dialogue != null && dialogue.lines != null && line <= dialogue.lines.Count
            ? dialogue.lines[line - 1]
            : null;
        DialogueManager manager = DialogueManager.Instance;
        if (wanted == null || manager == null)
            yield break;

        bool started = false;
        float waited = 0f;
        while (true)
        {
            if (manager.IsPlaying(wanted))
            {
                started = true;
                if (manager.IsShowing(wanted))
                    yield break;
            }
            else if (started || waited > startsWithin)
            {
                yield break;
            }

            waited += GameClock.DeltaTime;
            yield return null;
        }
    }

    public override string ToString()
    {
        return dialogue != null ? $"Wait for line {line} of {dialogue.name}" : "Wait for a dialogue line";
    }
}
