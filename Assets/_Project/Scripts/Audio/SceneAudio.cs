using System.Collections.Generic;
using UnityEngine;

// The music and the ambience of a scene, started when it loads. With a Night Ambience the ambience
// follows how dark the world is (WorldTint): the night sounds once the sun has set and the world is
// about half dark (Night Ambience At), not at the first dusk - the rosy sunset after the wolf keeps
// the birds - and the day ones again as it brightens. They change over slowly, as the world darkens
// or brightens. The night sounds can wait for the story too (Night Ambience Conditions): the first
// night they come in as Hideshi, away from the trader's campfire, says it is getting dark. While
// nothing can be heard (a Hearing step down to silence, e.g. asleep in the barn with the screen
// black) they change at once, so the morning wakes up to the birds instead of the crickets
// crossfading into them. A scene loaded at night starts with the night sounds.
public sealed class SceneAudio : MonoBehaviour
{
    // The day and the night sounds crossfading as the world passes the darkness, or as the story
    // lets the night sounds in, under a line of dialogue.
    private const float ChangeOverSeconds = 5f;
    // The whole mix this quiet (AudioListener.volume, StoryHearing) counts as silence.
    private const float SilentVolume = 0.01f;
    // Back to the day sounds only a little brighter than the night ones came in, so a tint easing
    // around the mark does not swap them back and forth.
    private const float BackToDayMargin = 0.1f;

    [Header("Looping Audio")]
    [SerializeField] private AudioClip musicClip;
    [Tooltip("By day, or always without a Night Ambience.")]
    [SerializeField] private AudioClip ambienceClip;
    [Tooltip("Instead of the ambience while the world is dark. Empty = the same ambience day and night.")]
    [SerializeField] private AudioClip nightAmbienceClip;
    [Tooltip("How much darker than by day the world must be for the night sounds: 0 = day, 1 = black. The sunset after the wolf is about 0.25, a night 0.55-0.6.")]
    [SerializeField, Range(0.05f, 1f)] private float nightAmbienceAt = 0.45f;
    [Tooltip("The night sounds only once all of them are met, dark as the world may be, e.g. once Hideshi has said it is getting dark by the trader's campfire (a1_met_trader). Empty = by the darkness alone.")]
    [SerializeField] private List<StoryCondition> nightAmbienceConditions = new();
    [SerializeField, Min(0f)] private float fadeDuration = 1f;

    [Header("Empty Clips")]
    [SerializeField] private bool stopMusicWhenEmpty;
    [SerializeField] private bool stopAmbienceWhenEmpty = true;

    // Which ambience plays: none chosen yet until the first frame, when the time of day is set.
    private bool? nightPlaying;

    private void Start()
    {
        if (AudioManager.Instance == null)
            return;

        if (musicClip != null)
            AudioManager.Instance.PlayMusic(musicClip, fadeDuration: fadeDuration);
        else if (stopMusicWhenEmpty)
            AudioManager.Instance.StopMusic(fadeDuration);

        // With a night ambience, the first Update picks: WorldNight tints the world in its Start.
        if (nightAmbienceClip == null)
            PlayAmbience(ambienceClip, fadeDuration);
    }

    private void Update()
    {
        if (nightAmbienceClip == null || AudioManager.Instance == null)
            return;

        float dimming = WorldTint.Dimming;
        bool dark = nightPlaying == true ? dimming >= nightAmbienceAt - BackToDayMargin : dimming >= nightAmbienceAt;
        bool night = dark && StoryCondition.AreMet(nightAmbienceConditions);
        if (night == nightPlaying)
            return;

        float fade = nightPlaying == null ? fadeDuration
            : AudioListener.volume <= SilentVolume ? 0f
            : Mathf.Max(fadeDuration, ChangeOverSeconds);
        nightPlaying = night;
        PlayAmbience(night ? nightAmbienceClip : ambienceClip, fade);
    }

    private void PlayAmbience(AudioClip clip, float fade)
    {
        if (clip != null)
            AudioManager.Instance.PlayAmbience(clip, fadeDuration: fade);
        else if (stopAmbienceWhenEmpty)
            AudioManager.Instance.StopAmbience(fade);
    }
}
