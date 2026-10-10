using System;
using System.Collections;
using UnityEngine;

// Moves a character instantly; the new position is saved. To move the player into another
// area, use a PlayerArrivalPoint as the destination: it also switches the camera bounds and
// the minimap (e.g. from Haru's home into the forest). Cover the jump with a fade or an effect.
[Serializable, SubclassMenu("Characters/Teleport")]
public sealed class TeleportStep : StoryStep
{
    [SerializeField] private StoryTarget actor = new();
    [SerializeField] private StoryTarget destination = new(StoryTargetType.SceneObject);
    [Tooltip("Added to the destination, e.g. to appear next to a character instead of on top of them.")]
    [SerializeField] private Vector2 offset;

    public override bool IsInstant => true;

    public override IEnumerator Run(StoryEventContext context)
    {
        Transform mover = actor.Resolve(context);
        Transform target = destination.Resolve(context);
        if (mover == null || target == null)
            yield break;

        Vector3 position = target.position + (Vector3)offset;

        if (mover == context.Player)
        {
            if (target.TryGetComponent(out PlayerArrivalPoint arrival))
                arrival.MovePlayer(mover, offset);
            else
                PlayerTransfer.MovePlayer(mover, position, null);
        }
        else if (mover.TryGetComponent(out NpcCharacter npc))
        {
            npc.TeleportTo(position);
        }
        else
        {
            position.z = mover.position.z;
            mover.position = position;
        }
    }

    public override string ToString()
    {
        return $"{actor} teleports to {destination}";
    }
}
