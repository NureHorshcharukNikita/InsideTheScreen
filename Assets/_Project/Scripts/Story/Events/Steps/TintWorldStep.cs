using System;
using System.Collections;
using UnityEngine;

// Tints the world, under the dialogue and the HUD: a deeper night inside the barn, the light of the
// morning after. Eases from the tint there is to this one; alpha is how strong it is (0 = none).
// While the event plays, the time of day (WorldNight) leaves the tint alone; when it ends, the
// world goes back to the time of day the story is at.
[Serializable, SubclassMenu("Effects/Tint World")]
public sealed class TintWorldStep : StoryStep
{
    private const float HandBackSeconds = 2.5f;

    [Tooltip("Alpha is how strong: 0 = no tint.")]
    [SerializeField] private Color color = new(0.04f, 0.06f, 0.16f, 0.5f);
    [Tooltip("How long it takes to get there.")]
    [SerializeField, Min(0f)] private float duration = 1.5f;
    [SerializeField] private bool waitUntilDone;

    public override bool IsInstant => !waitUntilDone;

    public override IEnumerator Run(StoryEventContext context)
    {
        WorldTint tint = WorldTint.Get();
        tint.Hold();
        context.OnRelease(() =>
        {
            if (tint == null)
                return;

            tint.Release();
            if (!WorldTint.IsHeldByStory)
                WorldNight.Restore(tint, HandBackSeconds);
        });

        tint.SetTint(color, duration);
        while (waitUntilDone && tint != null && tint.IsChanging)
            yield return null;
    }

    public override string ToString()
    {
        return $"Tint world: {color.a:0.##} ({duration:0.##} s)";
    }
}
