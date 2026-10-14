using System;
using System.Collections;
using UnityEngine;

// Switches an object's Animator to one of its states, e.g. the TV screen from the news to noise.
// A hidden object is shown first. The first frame of the state is applied at once, so the frame
// the object was left on (e.g. the news on a TV switched off) never flashes up. Does not wait.
[Serializable, SubclassMenu("Scene/Play Animation")]
public sealed class PlayAnimationStep : StoryStep
{
    [SerializeField] private Animator animator;
    [Tooltip("Name of the state in the Animator Controller, e.g. Noise.")]
    [SerializeField] private string state;

    public override bool IsInstant => true;

    public override IEnumerator Run(StoryEventContext context)
    {
        if (animator == null || string.IsNullOrWhiteSpace(state))
            yield break;

        if (!animator.gameObject.activeSelf)
            animator.gameObject.SetActive(true);

        animator.Play(state, 0, 0f);
        animator.Update(0f);
    }

    public override string ToString()
    {
        return $"Play {(string.IsNullOrWhiteSpace(state) ? "(state)" : state)} on {(animator != null ? animator.name : "(animator)")}";
    }
}
