using System;
using UnityEngine;

public enum StoryTargetType
{
    Player,
    Npc,
    SceneObject
}

// A character or a point a story event step works with. NPCs are picked by their NpcData and
// looked up in the scene when the step runs.
[Serializable]
public sealed class StoryTarget
{
    public StoryTargetType type;
    public NpcData npc;
    public Transform sceneObject;

    public StoryTarget()
    {
    }

    public StoryTarget(StoryTargetType type)
    {
        this.type = type;
    }

    public Transform Resolve(StoryEventContext context)
    {
        switch (type)
        {
            case StoryTargetType.Player:
                return context.Player;
            case StoryTargetType.Npc:
                NpcCharacter character = NpcCharacter.Find(npc);
                return character != null ? character.transform : null;
            default:
                return sceneObject;
        }
    }

    public override string ToString()
    {
        return type switch
        {
            StoryTargetType.Player => "Player",
            StoryTargetType.Npc => npc != null ? npc.DisplayName : "(NPC)",
            _ => sceneObject != null ? sceneObject.name : "(object)"
        };
    }
}
