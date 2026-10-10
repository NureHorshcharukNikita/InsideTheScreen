using System;
using System.Collections;
using UnityEngine;

// Shows or hides a scene object, e.g. a prop that appears during the scene.
// NPCs that leave the story are hidden with NpcData.hiddenAfterStoryFlag instead.
[Serializable, SubclassMenu("Scene/Set Active")]
public sealed class SetActiveStep : StoryStep
{
    [SerializeField] private GameObject target;
    [SerializeField] private bool active = true;

    public override bool IsInstant => true;

    public override IEnumerator Run(StoryEventContext context)
    {
        if (target != null)
            target.SetActive(active);

        yield break;
    }

    public override string ToString()
    {
        return $"{(active ? "Show" : "Hide")} {(target != null ? target.name : "(object)")}";
    }
}
