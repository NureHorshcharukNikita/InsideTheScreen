using System;
using System.Collections;
using UnityEngine;
using Object = UnityEngine.Object;

// A "!" pops up over a character for a moment: they have noticed something (AlertMark, the same
// mark as an enemy spotting the player). With Hop, they also give a little startled jump.
[Serializable, SubclassMenu("Characters/Exclamation")]
public sealed class ExclaimStep : StoryStep
{
    [SerializeField] private StoryTarget actor = new();
    [Tooltip("How long the \"!\" stays.")]
    [SerializeField, Min(0f)] private float seconds = 0.8f;
    [Tooltip("A little startled jump as the \"!\" pops up.")]
    [SerializeField] private bool hop = true;
    [SerializeField, Min(0f)] private float hopHeight = 0.35f;
    [SerializeField, Min(0.05f)] private float hopDuration = 0.25f;

    public override IEnumerator Run(StoryEventContext context)
    {
        Transform character = actor.Resolve(context);
        if (character == null)
            yield break;

        context.TakeControl(character);
        GameObject mark = AlertMark.Create(character).gameObject;
        // Also gone if the event stops halfway.
        context.OnRelease(() => Remove(mark));

        float shownAt = GameClock.Now;
        if (hop)
        {
            // Up and back down along a parabola, landing exactly where it stood.
            Vector3 ground = character.position;
            for (float time = 0f; time < hopDuration; time += GameClock.DeltaTime)
            {
                float progress = time / hopDuration;
                character.position = ground + Vector3.up * (hopHeight * 4f * progress * (1f - progress));
                yield return null;
            }

            character.position = ground;
        }

        float left = seconds - (GameClock.Now - shownAt);
        if (left > 0f)
            yield return new WaitForGameSeconds(left);

        Remove(mark);
    }

    private static void Remove(GameObject mark)
    {
        if (mark != null)
            Object.Destroy(mark);
    }

    public override string ToString()
    {
        return hop ? $"\"!\" over {actor}, who hops" : $"\"!\" over {actor}";
    }
}
