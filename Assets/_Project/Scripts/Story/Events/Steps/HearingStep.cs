using System;
using System.Collections;
using UnityEngine;

// How well the player hears everything, e.g. waking up in the forest: silence, then muffled
// sounds, then clear. Works on the whole mix (music, ambience, effects). Everything sounds
// normal again when the event ends.
[Serializable, SubclassMenu("Effects/Hearing")]
public sealed class HearingStep : StoryStep
{
    [SerializeField, Range(0f, 1f)] private float volume = 1f;
    [Tooltip("0 = clear, 1 = as if through a wall.")]
    [SerializeField, Range(0f, 1f)] private float muffle;
    [Tooltip("How long it takes to get there.")]
    [SerializeField, Min(0f)] private float duration = 1f;
    [SerializeField] private bool waitUntilDone = true;

    public override IEnumerator Run(StoryEventContext context)
    {
        StoryHearing hearing = StoryHearing.Get(context);
        if (hearing == null)
            yield break;

        hearing.Set(volume, muffle, duration);
        if (waitUntilDone)
            yield return new WaitForGameSeconds(duration);
    }

    public override string ToString()
    {
        return $"Hearing: volume {volume:0.##}, muffle {muffle:0.##} ({duration:0.##} s)";
    }
}
