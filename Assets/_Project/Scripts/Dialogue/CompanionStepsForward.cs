using System.Collections;
using UnityEngine;

// Before a talk Hideshi leads (NpcData.companionLeadsTalks, e.g. with Eldon): the father steps up
// to where Haru stood, and Haru, the warier of the two, backs off a step to the side, still looking
// at the one they came to talk to. Played like a story event (StoryEventContext) while the game is
// in the Dialogue state; the first line shows once both stand (DialogueManager).
public static class CompanionStepsForward
{
    private const float Speed = 3f;
    // Haru's step: back from the NPC, and aside to the other side than his father comes from, so
    // their ways do not cross and he is not hidden behind him.
    private const float StepBack = 0.8f;
    private const float StepAside = 1f;
    // Further away than this (round a corner, left behind), the father does not come over.
    private const float MaxCompanionDistance = 6f;

    private static readonly Collider2D[] OverlapResults = new Collider2D[8];

    public static bool CanPlay(Transform npc, Transform player, Transform companion)
    {
        return npc != null && player != null && companion != null
            && Vector2.Distance(player.position, companion.position) <= MaxCompanionDistance;
    }

    public static IEnumerator Play(StoryEventContext context, Transform npc, Transform companion)
    {
        Transform player = context.Player;
        if (player == null || npc == null || companion == null)
            yield break;

        Vector2 spot = player.position;
        Vector2 away = spot - (Vector2)npc.position;
        away = away.sqrMagnitude > 0.0001f ? away.normalized : Vector2.down;
        Vector2 side = new(-away.y, away.x);
        float fatherSide = Vector2.Dot((Vector2)companion.position - spot, side) >= 0f ? 1f : -1f;
        Vector2 back = spot + away * StepBack;
        Vector2 haruSpot = FirstFreeSpot(player, companion, npc, spot,
            back - side * (fatherSide * StepAside),
            back + side * (fatherSide * StepAside),
            back);

        Mover haru = new(context, player, haruSpot, npc.position, true);
        Mover father = new(context, companion, spot, npc.position, false);
        while (!haru.Done || !father.Done)
        {
            float step = Speed * GameClock.DeltaTime;
            haru.Advance(step);
            father.Advance(step);
            yield return null;
        }

        haru.Stop(context);
        father.Stop(context);
    }

    // The first spot Haru fits on: walls, props and people other than the three of them rule a
    // spot out. None free: he stays where he is.
    private static Vector2 FirstFreeSpot(Transform player, Transform companion, Transform npc, Vector2 stay,
        params Vector2[] spots)
    {
        if (!player.TryGetComponent(out Collider2D feet))
            return spots[0];

        Vector2 size = (Vector2)feet.bounds.size * 0.95f;
        Vector2 feetOffset = (Vector2)feet.bounds.center - (Vector2)player.position;
        ContactFilter2D filter = new() { useTriggers = false };
        filter.SetLayerMask(Physics2D.GetLayerCollisionMask(player.gameObject.layer));

        foreach (Vector2 spot in spots)
        {
            int count = Physics2D.OverlapBox(spot + feetOffset, size, 0f, filter, OverlapResults);
            bool free = true;
            for (int i = 0; i < count && free; i++)
            {
                Transform hit = OverlapResults[i].transform;
                free = hit.IsChildOf(player) || hit.IsChildOf(companion) || hit.IsChildOf(npc);
            }

            if (free)
                return spot;
        }

        return stay;
    }

    private sealed class Mover
    {
        private readonly Transform body;
        private readonly Animator animator;
        private readonly Vector2 target;
        private readonly Vector2 lookAt;
        private readonly bool backingOff;

        public Mover(StoryEventContext context, Transform body, Vector2 target, Vector2 lookAt, bool backingOff)
        {
            this.body = body;
            this.target = target;
            this.lookAt = lookAt;
            this.backingOff = backingOff;
            context.TakeControl(body);
            animator = StoryEventContext.GetAnimator(body);
        }

        public bool Done => ((Vector2)body.position - target).sqrMagnitude <= 0.0001f;

        // Every frame, also standing: PlayerMovement resets IsMoving while the game is not in Gameplay.
        public void Advance(float distance)
        {
            Vector2 position = body.position;
            if (Done)
            {
                Face(lookAt - position, false);
                return;
            }

            Vector2 next = Vector2.MoveTowards(position, target, distance);
            body.position = new Vector3(next.x, next.y, body.position.z);
            // Haru backs off looking at the NPC; his father looks where he goes.
            Face(backingOff ? lookAt - next : target - position, true);
        }

        public void Stop(StoryEventContext context)
        {
            Face(lookAt - (Vector2)body.position, false);
            context.SavePosition(body);
        }

        private void Face(Vector2 direction, bool walking)
        {
            if (direction.sqrMagnitude > 0.0001f)
                FlipSpriteToFace.Apply(body, direction);
            CharacterWalkAnimation.Apply(animator, walking, direction);
        }
    }
}
