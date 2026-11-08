using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// A story event walking Haru up to an NPC for a talk Hideshi leads (TalkToStep with
// NpcData.companionLeadsTalks: Eldon's first meeting): the father walks up to the spot the talk is
// held from, and Haru, the warier of the two, stops a step behind him to the side, looking at the one
// they came to talk to. With no companion, Haru walks up to the spot himself. Talks started with the
// Interact key begin where everyone stands: nobody steps forward then.
public static class CompanionStepsForward
{
    // Haru's step: back from the NPC, and aside to the other side than his father comes from, so
    // their ways do not cross and he is not hidden behind him.
    private const float StepBack = 0.8f;
    private const float StepAside = 1f;
    // Further away than this (round a corner, left behind), the father does not come over.
    private const float MaxCompanionDistance = 6f;

    private static readonly Collider2D[] OverlapResults = new Collider2D[8];
    private static readonly Vector2[] Sides = { Vector2.down, Vector2.up, Vector2.left, Vector2.right };

    public static bool CanPlay(Transform npc, Transform player, Transform companion)
    {
        return npc != null && player != null && companion != null
            && Vector2.Distance(player.position, companion.position) <= MaxCompanionDistance;
    }

    // spot: where the one leading the talk stands, the father, or Haru when companion is null.
    public static IEnumerator Play(StoryEventContext context, Transform npc, Transform companion, Vector2 spot,
        float speed, bool avoidObstacles)
    {
        Transform player = context.Player;
        if (player == null || npc == null)
            yield break;

        Vector2 haruSpot = companion != null ? FindHaruSpot(player, companion, npc, spot) : spot;
        Mover haru = new(context, player, haruSpot, npc.position, true);
        Mover father = companion != null ? new Mover(context, companion, spot, npc.position, false) : null;
        // Once both are under event control: their physics is off, so they are not in each other's way.
        if (avoidObstacles)
        {
            haru.GoAround();
            father?.GoAround();
        }

        while (!haru.Done || father is { Done: false })
        {
            float step = speed * GameClock.DeltaTime;
            haru.Advance(step);
            father?.Advance(step);
            yield return null;
        }

        haru.Stop(context);
        father?.Stop(context);
    }

    // Where the talk is held from when Haru walks up from further away (TalkToStep): distance from
    // the NPC straight below, above or beside it, on the side Haru comes from, or on the next side
    // round when a wall or a prop stands there. Nothing free: on Haru's side all the same.
    public static Vector2 FindStandSpot(Transform npc, Transform player, Transform companion, float distance)
    {
        Vector2 center = npc.position;
        Vector2 fromNpc = (Vector2)player.position - center;
        Vector2[] spots = new Vector2[Sides.Length];
        Vector2[] sides = (Vector2[])Sides.Clone();
        Array.Sort(sides, (a, b) => Vector2.Dot(b, fromNpc).CompareTo(Vector2.Dot(a, fromNpc)));
        for (int i = 0; i < sides.Length; i++)
            spots[i] = center + sides[i] * distance;

        return FirstFreeSpot(player, companion, npc, spots[0], spots);
    }

    private static Vector2 FindHaruSpot(Transform player, Transform companion, Transform npc, Vector2 spot)
    {
        Vector2 away = spot - (Vector2)npc.position;
        away = away.sqrMagnitude > 0.0001f ? away.normalized : Vector2.down;
        Vector2 side = new(-away.y, away.x);
        float fatherSide = Vector2.Dot((Vector2)companion.position - spot, side) >= 0f ? 1f : -1f;
        Vector2 back = spot + away * StepBack;
        return FirstFreeSpot(player, companion, npc, spot,
            back - side * (fatherSide * StepAside),
            back + side * (fatherSide * StepAside),
            back);
    }

    // The first spot Haru fits on: walls, props and people other than the three of them rule a
    // spot out. None free: stay.
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
                free = hit.IsChildOf(player) || companion != null && hit.IsChildOf(companion) || hit.IsChildOf(npc);
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
        private readonly bool backsOffFacingNpc;
        private readonly List<Vector2> way = new();
        private int corner;

        public Mover(StoryEventContext context, Transform body, Vector2 target, Vector2 lookAt, bool backsOffFacingNpc)
        {
            this.body = body;
            this.target = target;
            this.lookAt = lookAt;
            this.backsOffFacingNpc = backsOffFacingNpc;
            way.Add(target);
            context.TakeControl(body);
            animator = StoryEventContext.GetAnimator(body);
        }

        public bool Done => corner >= way.Count;

        // Around walls, props and people in the way (StoryPath); straight where nothing is.
        public void GoAround()
        {
            List<Vector2> around = StoryPath.Find(body.position, target);
            if (around == null)
                return;

            way.Clear();
            way.AddRange(around);
        }

        // Every frame, also standing: PlayerMovement resets IsMoving while the game is not in Gameplay.
        public void Advance(float distance)
        {
            Vector2 position = body.position;
            while (!Done && (position - way[corner]).sqrMagnitude <= 0.0001f)
                corner++;

            if (Done)
            {
                Face(lookAt - position, false);
                return;
            }

            Vector2 heading = way[corner] - position;
            Vector2 next = Vector2.MoveTowards(position, way[corner], distance);
            body.position = new Vector3(next.x, next.y, body.position.z);
            // Haru backs off looking at the NPC; walking up, and his father always, look where they go.
            bool backingOff = backsOffFacingNpc && Vector2.Dot(heading, lookAt - position) < 0f;
            Face(backingOff ? lookAt - next : heading, true);
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
