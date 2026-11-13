using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Runs several steps one after another, as one step: inside a Parallel step, a branch that waits
// before it starts, e.g. Garr setting off for his door while the camera is still on its way to him.
[Serializable, SubclassMenu("Flow/Sequence")]
public sealed class SequenceStep : StoryStep
{
    [SerializeReference, SubclassPicker] private List<StoryStep> steps = new();

    public override bool IsInstant => steps.TrueForAll(step => step == null || step.IsInstant);

    public override IEnumerator Run(StoryEventContext context)
    {
        foreach (StoryStep step in steps)
        {
            if (step != null)
                yield return context.Run(step);
        }
    }

    public override void Skip(StoryEventContext context)
    {
        foreach (StoryStep step in steps)
            step?.Skip(context);
    }

    public override IEnumerable<DialogueAction> GetActions()
    {
        foreach (StoryStep step in steps)
        {
            if (step == null)
                continue;

            foreach (DialogueAction action in step.GetActions())
                yield return action;
        }
    }

    public override string ToString()
    {
        return $"Sequence ({steps.Count} steps)";
    }
}
