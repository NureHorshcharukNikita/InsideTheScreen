using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

// Starts, completes or fails quests and sets or clears story flags: the same actions dialogues
// and story triggers use.
[Serializable, SubclassMenu("Story/Actions")]
public sealed class ActionsStep : StoryStep
{
    [SerializeField] private List<DialogueAction> actions = new();

    public override bool IsInstant => true;

    public override IEnumerator Run(StoryEventContext context)
    {
        PlayerCharacter player = Object.FindFirstObjectByType<PlayerCharacter>();
        foreach (DialogueAction action in actions)
            action?.Execute(null, player);

        yield break;
    }

    public override string ToString()
    {
        List<string> parts = new();
        foreach (DialogueAction action in actions)
        {
            if (action == null || action.type == DialogueActionType.None)
                continue;

            string subject = action.quest != null ? action.quest.name : action.storyFlag;
            parts.Add($"{action.type} {subject}");
        }

        return parts.Count > 0 ? $"Actions: {string.Join(", ", parts)}" : "Actions";
    }
}
