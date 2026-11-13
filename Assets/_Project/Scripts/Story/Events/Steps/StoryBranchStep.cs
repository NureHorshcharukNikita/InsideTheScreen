using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Runs one of two lists of steps, depending on the story when it gets there, e.g. at a closed exit
// Dad walks Haru back when he is with him, and Haru turns back alone while Dad waits in the village
// (hideshi_stays_behind): one zone for both.
[Serializable, SubclassMenu("Flow/If Story")]
public sealed class StoryBranchStep : StoryStep
{
    [Tooltip("All of them must be met for Then; otherwise Otherwise runs.")]
    [SerializeField] private List<StoryCondition> conditions = new();
    [SerializeReference, SubclassPicker] private List<StoryStep> then = new();
    [SerializeReference, SubclassPicker] private List<StoryStep> otherwise = new();

    public override bool IsInstant =>
        then.TrueForAll(step => step == null || step.IsInstant)
        && otherwise.TrueForAll(step => step == null || step.IsInstant);

    public override IEnumerator Run(StoryEventContext context)
    {
        foreach (StoryStep step in Branch())
            yield return context.Run(step);
    }

    public override void Skip(StoryEventContext context)
    {
        foreach (StoryStep step in Branch())
            step?.Skip(context);
    }

    public override IEnumerable<DialogueAction> GetActions()
    {
        foreach (StoryStep step in then.Concat(otherwise))
        {
            if (step == null)
                continue;

            foreach (DialogueAction action in step.GetActions())
                yield return action;
        }
    }

    private List<StoryStep> Branch()
    {
        return StoryCondition.AreMet(conditions) ? then : otherwise;
    }

    public override string ToString()
    {
        return $"If story ({conditions.Count} conditions): {then.Count} / {otherwise.Count} steps";
    }
}
