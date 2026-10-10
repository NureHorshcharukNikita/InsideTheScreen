using System;
using System.Collections;
using UnityEngine;

// Plays a sound effect, or switches the music or the ambience. Does not wait for it.
[Serializable, SubclassMenu("Scene/Sound")]
public sealed class SoundStep : StoryStep
{
    public enum SoundChannel
    {
        Sfx,
        Music,
        Ambience
    }

    [SerializeField] private SoundChannel channel = SoundChannel.Sfx;
    [Tooltip("Music / Ambience: empty = stop what is playing.")]
    [SerializeField] private AudioClip clip;
    [Tooltip("Sfx only.")]
    [SerializeField, Range(0f, 1f)] private float volume = 1f;
    [Tooltip("Music / Ambience only.")]
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
            case SoundChannel.Sfx:
                AudioManager.TryPlaySfx(clip, volume);
                break;
        }

        yield break;
    }

    public override string ToString()
    {
        return $"{channel}: {(clip != null ? clip.name : channel == SoundChannel.Sfx ? "(none)" : "stop")}";
    }
}
