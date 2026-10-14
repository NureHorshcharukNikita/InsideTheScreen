using System;
using System.Collections;
using UnityEngine;

// TV noise over the whole screen, a glow on the TV and a hiss (the intro). The first Screen
// Noise step of an event creates the effect, later ones change how strong it is; Pull Into
// Screen raises it too. It is removed when the event ends.
[Serializable, SubclassMenu("Effects/Screen Noise")]
public sealed class ScreenNoiseStep : StoryStep
{
    [Tooltip("Where the glow appears, e.g. the TV. Only used by the step that creates the effect.")]
    [SerializeField] private Transform screen;
    [Tooltip("Colour of the noise and the glow. Only used by the step that creates the effect.")]
    [SerializeField] private Color tint = new(0.62f, 0.78f, 0.8f, 1f);
    [Tooltip("Looping hiss, e.g. white noise. It grows and fades with the noise and the glow. Only used by the step that creates the effect.")]
    [SerializeField] private AudioClip sound;
    [Tooltip("How loud the sound is at full noise.")]
    [SerializeField, Range(0f, 1f)] private float soundVolume = 0.3f;
    [SerializeField, Range(0f, 1f)] private float noise;
    [SerializeField, Range(0f, 1f)] private float glow;
    [Tooltip("How long it takes to reach the new noise and glow.")]
    [SerializeField, Min(0f)] private float duration = 1f;
    [SerializeField] private bool waitUntilDone = true;

    public override IEnumerator Run(StoryEventContext context)
    {
        ScreenNoiseOverlay overlay = context.GetEffect<ScreenNoiseOverlay>();
        if (overlay == null)
        {
            Transform player = context.Player;
            Vector3 position = screen != null ? screen.position : player != null ? player.position : Vector3.zero;
            overlay = ScreenNoiseOverlay.Create(position, tint, StoryEventContext.GetSprite(player));
            overlay.PlaySound(sound, soundVolume);
            context.AddEffect(overlay);
        }

        overlay.SetNoise(noise, duration);
        overlay.SetGlow(glow, duration);

        if (waitUntilDone)
            yield return new WaitForGameSeconds(duration);
    }

    public override string ToString()
    {
        return $"Screen noise {noise:0.##}, glow {glow:0.##} ({duration:0.##} s)";
    }
}
