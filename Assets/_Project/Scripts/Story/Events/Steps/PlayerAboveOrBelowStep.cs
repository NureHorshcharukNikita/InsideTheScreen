using System;
using System.Collections;
using System.Collections.Generic;
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
        Transform player = context.Player;
        bool above = player != null && point != null && player.position.y > point.position.y;
        foreach (StoryStep step in above ? whenAbove : whenBelow)
            yield return context.Run(step);
    }

    public override string ToString()
    {
        return $"Player below / above {(point != null ? point.name : "(point)")}: {whenBelow.Count} / {whenAbove.Count} steps";
    }
}
