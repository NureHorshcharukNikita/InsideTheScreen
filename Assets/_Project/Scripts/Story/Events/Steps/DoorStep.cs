using System;
using System.Collections;
using UnityEngine;

// Opens or shuts a door, e.g. Hideshi closing the front door behind him. A shut door stays shut
// while someone stands next to it, and opens for people again once everyone has stepped away.
[Serializable, SubclassMenu("Scene/Door")]
public sealed class DoorStep : StoryStep
{
    public enum DoorAction
    {
        Shut,
        Open
    }

    [SerializeField] private Door door;
    [SerializeField] private DoorAction action = DoorAction.Shut;
    [Tooltip("Wait until the door has finished swinging.")]
    [SerializeField] private bool waitUntilDone = true;

    public override bool IsInstant => !waitUntilDone;

    public override IEnumerator Run(StoryEventContext context)
    {
        if (door == null)
            yield break;

        if (action == DoorAction.Shut)
            door.Shut();
        else
            door.Open();

        while (waitUntilDone && door != null && door.IsSwinging)
            yield return null;
    }

    public override string ToString()
    {
        return $"{action} {(door != null ? door.name : "(door)")}";
    }
}
