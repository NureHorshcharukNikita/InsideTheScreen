using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Runs several steps at the same time and waits until all of them are done, e.g. two
// characters walking side by side.
[Serializable, SubclassMenu("Flow/Parallel")]
public sealed class ParallelStep : StoryStep
{
    [SerializeReference, SubclassPicker] private List<StoryStep> steps = new();

    public override bool IsInstant => steps.TrueForAll(step => step == null || step.IsInstant);

    public override IEnumerator Run(StoryEventContext context)
    {
        List<Coroutine> running = new();
        foreach (StoryStep step in steps)
        {
            if (step != null)
                running.Add(context.StartCoroutine(context.Run(step)));
        }

        foreach (Coroutine routine in running)
            yield return routine;
    }

    public override string ToString()
    {
        return $"Parallel ({steps.Count} steps)";
    }
}
