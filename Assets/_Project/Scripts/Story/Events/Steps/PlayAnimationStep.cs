using System;
using System.Collections;
using UnityEngine;

// Switches an object's Animator to one of its states, e.g. the TV screen from the news to noise.
// The object must be active: show it first with a Set Active step if it is hidden. Does not wait.
[Serializable, SubclassMenu("Scene/Play Animation")]
public sealed class PlayAnimationStep : StoryStep
{
    [SerializeField] private Animator animator;
    [Tooltip("Name of the state in the Animator Controller, e.g. Noise.")]
    [SerializeField] private string state;

    public override bool IsInstant => true;

    public override IEnumerator Run(StoryEventContext context)
    {
        if (animator != null && !string.IsNullOrWhiteSpace(state))
            animator.Play(state, 0, 0f);

        yield break;
    }

    public override string ToString()
    {
        return $"Play {(string.IsNullOrWhiteSpace(state) ? "(state)" : state)} on {(animator != null ? animator.name : "(animator)")}";
    }
}
