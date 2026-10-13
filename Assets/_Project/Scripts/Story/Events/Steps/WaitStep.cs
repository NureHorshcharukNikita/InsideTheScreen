using System;
using System.Collections;
using UnityEngine;

[Serializable, SubclassMenu("Flow/Wait")]
public sealed class WaitStep : StoryStep
{
    [SerializeField, Min(0f)] private float seconds = 1f;

    public override IEnumerator Run(StoryEventContext context)
    {
        yield return new WaitForGameSeconds(seconds);
    }

    public override string ToString()
    {
        return $"Wait {seconds:0.##} s";
    }
}
