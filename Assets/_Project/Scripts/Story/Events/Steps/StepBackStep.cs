using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// A character jumps back, the way its back is turned, still looking ahead, e.g. Dad when Haru's
// card suddenly turns into a stick in his hand: Haru came up from above, Dad looks up and jumps
// down. Without a way it looks (no walk animations), straight away from Who. Not into walls or
// fences: it lands up against them.
// Back In: it walks back to where it stood before that step, e.g. Dad stepping up again to see the
// stick turn back into its card. Nothing if it has not stepped back in this event.
[Serializable, SubclassMenu("Characters/Step Back")]
public sealed class StepBackStep : StoryStep
{
    public enum Way
    {
        Away,
        BackIn
    }

    // Where each character stood before its step back, for Back In.
    private static readonly Dictionary<Transform, Vector3> SteppedFrom = new();

    [SerializeField] private StoryTarget actor = new(StoryTargetType.Npc);
    [Tooltip("Away: a jump back, the way its back is turned. Back In: a walk back to where it stood before that.")]
    [SerializeField] private Way way = Way.Away;
    [Tooltip("Who it steps back from, e.g. Haru: used only for one without walk animations (straight away from them), and for Back In to look at them again.")]
    [SerializeField] private StoryTarget from = new(StoryTargetType.Player);
    [Tooltip("Away: how far back, in world units; less where something is in the way.")]
    [SerializeField, Min(0f)] private float distance = 0.7f;
    [Tooltip("Away: how long the hop takes.")]
    [SerializeField, Min(0.05f)] private float seconds = 0.25f;
    [Tooltip("Away: how high it hops.")]
    [SerializeField, Min(0f)] private float hopHeight = 0.18f;
    [Tooltip("Back In: walking speed.")]
    [SerializeField, Min(0.1f)] private float speed = 2f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetForPlayMode()
    {
        SteppedFrom.Clear();
    }

    public override IEnumerator Run(StoryEventContext context)
    {
        Transform mover = actor.Resolve(context);
        Transform other = from.Resolve(context);
        if (mover == null)
            yield break;

        context.TakeControl(mover);
        Animator animator = StoryEventContext.GetAnimator(mover);
        if (way == Way.Away)
        {
            if (other == null)
                yield break;

            Vector3 start = mover.position;
            Vector2 away = CharacterWalkAnimation.TryGetFacing(animator, out Vector2 looking) && looking != Vector2.zero
                ? -looking
                : (Vector2)(start - other.position);
            if (away.sqrMagnitude < 0.0001f)
                away = Vector2.down;

            float back = StoryPath.ClearDistance(start, away, distance);
            Vector3 end = start + (Vector3)(away.normalized * back);
            SteppedFrom[mover] = start;
            context.OnRelease(() => SteppedFrom.Remove(mover));
            Look(mover, animator, -away);

            for (float elapsed = 0f; elapsed < seconds; elapsed += GameClock.DeltaTime)
            {
                float t = elapsed / seconds;
                float eased = 1f - (1f - t) * (1f - t);
                mover.position = Vector3.Lerp(start, end, eased) + Vector3.up * (hopHeight * 4f * t * (1f - t));
                yield return null;
            }

            mover.position = end;
            context.SavePosition(mover);
            yield break;
        }

        if (!SteppedFrom.TryGetValue(mover, out Vector3 spot))
            yield break;

        SteppedFrom.Remove(mover);
        spot.z = mover.position.z;
        Vector2 heading = spot - mover.position;
        while ((mover.position - spot).sqrMagnitude > 0.0001f)
        {
            CharacterWalkAnimation.Apply(animator, true, heading);
            FlipSpriteToFace.Apply(mover, heading);
            mover.position = Vector3.MoveTowards(mover.position, spot, speed * GameClock.DeltaTime);
            yield return null;
        }

        Look(mover, animator, other != null ? (Vector2)(other.position - mover.position) : heading);
        context.SavePosition(mover);
    }

    private static void Look(Transform mover, Animator animator, Vector2 direction)
    {
        CharacterWalkAnimation.Apply(animator, false, direction);
        FlipSpriteToFace.Apply(mover, direction);
    }

    public override string ToString()
    {
        return way == Way.Away ? $"{actor} steps back from {from}" : $"{actor} steps back in";
    }
}
