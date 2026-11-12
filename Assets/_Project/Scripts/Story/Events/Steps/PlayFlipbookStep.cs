using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Plays a Sprite Flipbook once on a character and waits for it, e.g. Haru putting on his belt and
// backpack. Its Animator rests meanwhile and takes over again after the last frame, facing the
// way it faced before. Set At End is set just before that, e.g. the flag that gives Haru his look
// with the bags (StoryLook), so the frames end on the look he walks on with.
[Serializable, SubclassMenu("Characters/Play Frames")]
public sealed class PlayFlipbookStep : StoryStep
{
    [SerializeField] private StoryTarget actor = new();
    [SerializeField] private SpriteFlipbook flipbook;
    [Tooltip("Set as the last frame shows, before the Animator takes over again, e.g. a1_bags_worn. Empty = none.")]
    [SerializeField, StoryFlag] private string setAtEnd;

    public override IEnumerator Run(StoryEventContext context)
    {
        Transform character = actor.Resolve(context);
        SpriteRenderer look = character != null ? character.GetComponentInChildren<SpriteRenderer>() : null;
        if (look == null || flipbook == null || flipbook.Frames.Count == 0)
        {
            StoryFlagManager.SetFlag(setAtEnd);
            yield break;
        }

        context.TakeControl(character);
        Animator animator = StoryEventContext.GetAnimator(character);
        bool animated = animator != null && animator.enabled;
        if (animated)
            animator.enabled = false;

        for (int frame = 0; frame < flipbook.Frames.Count; frame++)
        {
            look.sprite = flipbook.Frames[frame];
            foreach (SpriteFlipbook.FrameSound sound in flipbook.Sounds)
            {
                if (sound != null && sound.frame == frame)
                    AudioManager.TryPlaySfx(sound.clip, sound.volume);
            }

            yield return new WaitForGameSeconds(flipbook.FrameSeconds);
        }

        StoryFlagManager.SetFlag(setAtEnd);
        if (animated)
        {
            animator.enabled = true;
            animator.Update(0f);
        }
    }

    public override void Skip(StoryEventContext context)
    {
        StoryFlagManager.SetFlag(setAtEnd);
    }

    public override IEnumerable<DialogueAction> GetActions()
    {
        if (!string.IsNullOrWhiteSpace(setAtEnd))
            yield return new DialogueAction { type = DialogueActionType.SetStoryFlag, storyFlag = setAtEnd, silent = true };
    }

    public override string ToString()
    {
        return $"{actor} plays {(flipbook != null ? flipbook.name : "(frames)")}";
    }
}
