using System;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

// Two characters face off at a distance without touching, e.g. Hideshi scaring the wolf away: the
// attacker snaps forward and holds there snarling, then the defender lunges at it and it flinches
// back. Over and over; they face each other and end where they started.
[Serializable, SubclassMenu("Characters/Standoff")]
public sealed class StandoffStep : StoryStep
{
    // Parts of each round: the snap, the snarl, the defender's lunge; the rest is settling back.
    private const float SnapEnd = 0.2f;
    private const float SnarlEnd = 0.4f;
    private const float ScareEnd = 0.55f;
    private const float Flinch = 0.4f;
    private const float Shake = 0.03f;

    [SerializeField] private StoryTarget attacker = new(StoryTargetType.SceneObject);
    [SerializeField] private StoryTarget defender = new(StoryTargetType.Npc);
    [SerializeField, Min(0.1f)] private float duration = 2.4f;
    [Tooltip("How far the attacker snaps forward, in units.")]
    [SerializeField, Min(0f)] private float snap = 0.5f;
    [Tooltip("How far the defender lunges at it to scare it, in units.")]
    [SerializeField, Min(0f)] private float scare = 0.35f;
    [Tooltip("Rounds per second.")]
    [SerializeField, Min(0.1f)] private float pace = 1.25f;

    public override IEnumerator Run(StoryEventContext context)
    {
        Transform attackerBody = attacker.Resolve(context);
        Transform defenderBody = defender.Resolve(context);
        if (attackerBody == null || defenderBody == null)
            yield break;

        context.TakeControl(attackerBody);
        context.TakeControl(defenderBody);

        Vector3 attackerStart = attackerBody.position;
        Vector3 defenderStart = defenderBody.position;
        Vector3 toDefender = defenderStart - attackerStart;
        toDefender.z = 0f;
        Vector3 direction = toDefender.sqrMagnitude > 0.0001f ? toDefender.normalized : Vector3.right;

        FlipSpriteToFace.Apply(attackerBody, direction);
        CharacterWalkAnimation.Apply(StoryEventContext.GetAnimator(attackerBody), false, direction);
        FlipSpriteToFace.Apply(defenderBody, -direction);
        CharacterWalkAnimation.Apply(StoryEventContext.GetAnimator(defenderBody), false, -direction);

        for (float elapsed = 0f; elapsed < duration; elapsed += GameClock.DeltaTime)
        {
            float rounds = elapsed * pace;
            float t = rounds - Mathf.Floor(rounds);
            float forward;
            float lunge = 0f;
            Vector3 snarl = Vector3.zero;

            if (t < SnapEnd)
            {
                forward = Ease(t / SnapEnd) * snap;
            }
            else if (t < SnarlEnd)
            {
                forward = snap;
                snarl = new Vector3(Random.Range(-Shake, Shake), Random.Range(-Shake, Shake) * 0.5f, 0f);
            }
            else if (t < ScareEnd)
            {
                float k = Ease((t - SnarlEnd) / (ScareEnd - SnarlEnd));
                forward = Mathf.Lerp(snap, -snap * Flinch, k);
                lunge = k * scare;
            }
            else
            {
                float k = Ease((t - ScareEnd) / (1f - ScareEnd));
                forward = Mathf.Lerp(-snap * Flinch, 0f, k);
                lunge = Mathf.Lerp(scare, 0f, k);
            }

            attackerBody.position = attackerStart + direction * forward + snarl;
            defenderBody.position = defenderStart - direction * lunge;
            yield return null;
        }

        attackerBody.position = attackerStart;
        defenderBody.position = defenderStart;
    }

    private static float Ease(float t)
    {
        t = Mathf.Clamp01(t);
        return t * (2f - t);
    }

    public override string ToString()
    {
        return $"{attacker} and {defender} face off";
    }
}
