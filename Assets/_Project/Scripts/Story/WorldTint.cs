using UnityEngine;
using UnityEngine.Rendering.Universal;

// The colour of the light over the world of the scene: the night (WorldNight), or a story event's
// own light (Tint World), e.g. the morning after the night in the barn. It tints the scene's Global
// Light 2D, so everything lit gets darker - the minimap too, which films the same world - while the
// lamps (NightLamp) still shine. A tint's alpha is how strong it is: 0 = daylight, 1 = all its colour.
// Eases on the GameClock (the Dialogue game state pauses Time.timeScale).
public sealed class WorldTint : MonoBehaviour
{
    // Tint strength from which the lamps start to glow, and at which they are fully lit.
    private const float LampsStartAt = 0.15f;
    private const float LampsFullAt = 0.45f;

    private static WorldTint instance;

    private Light2D globalLight;
    private Color dayColor = Color.white;
    private Color current = new(1f, 1f, 1f, 0f);
    private Color from;
    private Color to;
    private float duration;
    private float elapsed;
    private int storyHolds;

    public bool IsChanging => elapsed < duration;

    // Story events tinting the world hold it, so the time of day does not change it under them.
    public static bool IsHeldByStory => instance != null && instance.storyHolds > 0;

    // 0 by day, 1 deep in the night: how brightly the lamps burn.
    public static float Darkness => instance != null
        ? Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(LampsStartAt, LampsFullAt, instance.current.a))
        : 0f;

    // How much darker than by day the world is, by how bright its light is (its colour counted, so a
    // rosy sunset is less dark than a blue night of the same strength): 0 by day, about 0.25 at the
    // sunset after the wolf, 0.55-0.6 at night. The night sounds go by it (SceneAudio).
    public static float Dimming
    {
        get
        {
            if (instance == null)
                return 0f;

            Color tint = instance.current;
            Color light = Color.Lerp(Color.white, new Color(tint.r, tint.g, tint.b, 1f), tint.a);
            return Mathf.Clamp01(1f - (0.2126f * light.r + 0.7152f * light.g + 0.0722f * light.b));
        }
    }

    // On the scene's Global Light 2D, made the first time it is needed.
    public static WorldTint Get()
    {
        if (instance != null)
            return instance;

        Light2D light = FindGlobalLight();
        GameObject host = light != null ? light.gameObject : new GameObject("WorldTint");
        instance = host.AddComponent<WorldTint>();
        instance.globalLight = light;
        if (light != null)
            instance.dayColor = light.color;

        return instance;
    }

    // Eases from the tint there is now to this one.
    public void SetTint(Color color, float seconds)
    {
        from = current;
        to = color;
        duration = Mathf.Max(0f, seconds);
        elapsed = 0f;
        if (duration <= 0f)
            Apply(to);
    }

    public void Hold()
    {
        storyHolds++;
    }

    public void Release()
    {
        storyHolds = Mathf.Max(0, storyHolds - 1);
    }

    private void Update()
    {
        if (!IsChanging)
            return;

        elapsed += GameClock.DeltaTime;
        Apply(Color.Lerp(from, to, Mathf.Clamp01(elapsed / duration)));
    }

    private void Apply(Color tint)
    {
        current = tint;
        if (globalLight != null)
            globalLight.color = dayColor * Color.Lerp(Color.white, new Color(tint.r, tint.g, tint.b, 1f), tint.a);
    }

    private static Light2D FindGlobalLight()
    {
        foreach (Light2D light in FindObjectsByType<Light2D>(FindObjectsSortMode.None))
        {
            if (light.lightType == Light2D.LightType.Global)
                return light;
        }

        return null;
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }
}
