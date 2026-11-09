using System;
using System.Collections;
using UnityEngine;

// Plays a sound effect, or switches the music or the ambience. Does not wait for it. A looping
// sound effect (Sfx Loop) goes on over the next steps until a step without a clip stops it, e.g.
// the wolf growling from when it comes out of the bushes until Hideshi has chased it off; at the
// end of the event it stops anyway.
[Serializable, SubclassMenu("Scene/Sound")]
public sealed class SoundStep : StoryStep
{
    // Played after the event when a looping sound was left on.
    private const float LoopLeftOnFade = 1f;

    public enum SoundChannel
    {
        Sfx,
        Music,
        Ambience,
        SfxLoop
    }

    [SerializeField] private SoundChannel channel = SoundChannel.Sfx;
    [Tooltip("Music / Ambience / Sfx Loop: empty = stop what is playing.")]
    [SerializeField] private AudioClip clip;
    [Tooltip("Sfx and Sfx Loop.")]
    [SerializeField, Range(0f, 1f)] private float volume = 1f;
    [Tooltip("Music / Ambience / Sfx Loop: how long it takes to come in or to die away.")]
    [SerializeField, Min(0f)] private float fadeDuration = 1f;

    public override bool IsInstant => true;

    public override IEnumerator Run(StoryEventContext context)
    {
        AudioManager audio = AudioManager.Instance;
        switch (channel)
        {
            case SoundChannel.Music when audio != null:
                if (clip != null)
                    audio.PlayMusic(clip, false, fadeDuration);
                else
                    audio.StopMusic(fadeDuration);
                break;
            case SoundChannel.Ambience when audio != null:
                if (clip != null)
                    audio.PlayAmbience(clip, false, fadeDuration);
                else
                    audio.StopAmbience(fadeDuration);
                break;
            case SoundChannel.SfxLoop when audio != null:
                if (clip == null)
                {
                    audio.StopSfxLoop(fadeDuration);
                    break;
                }

                audio.PlaySfxLoop(clip, volume, fadeDuration);
                context.OnRelease(() =>
                {
                    if (AudioManager.Instance != null)
                        AudioManager.Instance.StopSfxLoop(LoopLeftOnFade);
                });
                break;
            case SoundChannel.Sfx:
                AudioManager.TryPlaySfx(clip, volume);
                break;
        }

        yield break;
    }

    public override string ToString()
    {
        string name = channel == SoundChannel.SfxLoop ? "Sfx loop" : channel.ToString();
        return $"{name}: {(clip != null ? clip.name : channel == SoundChannel.Sfx ? "(none)" : "stop")}";
    }
}
