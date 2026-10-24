using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Runs one of two lists of steps, depending on whether the player is below or above a point when
// it gets there, e.g. the wolf comes at them from the side of the clearing with more room.
[Serializable, SubclassMenu("Flow/Player Above Or Below")]
public sealed class PlayerAboveOrBelowStep : StoryStep
{
    [SerializeField] private Transform point;
    [SerializeReference, SubclassPicker] private List<StoryStep> whenBelow = new();
    [SerializeReference, SubclassPicker] private List<StoryStep> whenAbove = new();

    public override bool IsInstant =>
        whenBelow.TrueForAll(step => step == null || step.IsInstant)
        && whenAbove.TrueForAll(step => step == null || step.IsInstant);

    public override IEnumerator Run(StoryEventContext context)
    {
        foreach (StoryStep step in Branch(context))
            yield return context.Run(step);
    }

    public override void Skip(StoryEventContext context)
    {
        foreach (StoryStep step in Branch(context))
            step?.Skip(context);
    }

    public override IEnumerable<DialogueAction> GetActions()
    {
        foreach (StoryStep step in whenBelow.Concat(whenAbove))
        {
            if (step == null)
                continue;

            foreach (DialogueAction action in step.GetActions())
                yield return action;
        }
    }

    private List<StoryStep> Branch(StoryEventContext context)
    {
        Transform player = context.Player;
        bool above = player != null && point != null && player.position.y > point.position.y;
        return above ? whenAbove : whenBelow;
    }

    public override string ToString()
    {
        return $"Player below / above {(point != null ? point.name : "(point)")}: {whenBelow.Count} / {whenAbove.Count} steps";
    }
}
