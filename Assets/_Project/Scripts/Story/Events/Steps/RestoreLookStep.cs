using System;
using System.Collections;
using UnityEngine;

// Gives a character back the size and colour it had when the event took control of it,
// e.g. after Pull Into Screen. The end of the event does this for everyone anyway.
[Serializable, SubclassMenu("Characters/Restore Look")]
public sealed class RestoreLookStep : StoryStep
{
    [SerializeField] private StoryTarget actor = new();

    public override bool IsInstant => true;

    public override IEnumerator Run(StoryEventContext context)
    {
        context.RestoreLook(actor.Resolve(context));
        yield break;
    }

    public override string ToString()
    {
        return $"{actor}: restore look";
    }
}
