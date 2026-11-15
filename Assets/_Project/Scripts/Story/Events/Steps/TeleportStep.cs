using System;
using System.Collections;
using UnityEngine;

// Moves a character instantly; the new position is saved. To move the player into another
// area, use a PlayerArrivalPoint as the destination: it also switches the camera bounds and
// the minimap (e.g. from Haru's home into the forest). Cover the jump with a fade or an effect.
// Back To Its Place: an NPC goes back to where it is placed in the scene, e.g. Brannoc out of sight
// once he has gone in for the night, so he is out there again in the morning, not at the door.
[Serializable, SubclassMenu("Characters/Teleport")]
public sealed class TeleportStep : StoryStep
{
    [SerializeField] private StoryTarget actor = new();
    [SerializeField] private StoryTarget destination = new(StoryTargetType.SceneObject);
    [Tooltip("Added to the destination, e.g. to appear next to a character instead of on top of them.")]
    [SerializeField] private Vector2 offset;
    [Tooltip("An NPC goes back to where it is placed in the scene, wherever it is moved to there. Destination and Offset are not used.")]
    [SerializeField] private bool backToItsPlace;

    public override bool IsInstant => true;

    public override IEnumerator Run(StoryEventContext context)
    {
        Transform mover = actor.Resolve(context);
        if (mover == null)
            yield break;

        if (backToItsPlace)
        {
            if (mover.TryGetComponent(out NpcCharacter placed))
                placed.TeleportTo(placed.ScenePosition);
            yield break;
        }

        Transform target = destination.Resolve(context);
        if (target == null)
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
        return backToItsPlace ? $"{actor} goes back to its place in the scene" : $"{actor} teleports to {destination}";
    }
}
