using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// A light that burns at night: the lanterns of Leymar's lamp posts, the trader's campfire, the
// windows of the houses. Its Light2Ds (on it or its children) fade in as the world gets dark
// (WorldTint) and out at dawn, with a soft flicker like a flame. A campfire keeps a little of its
// glow by day. Windows can go out late at night (Out When): one house after another, each after its
// own Out Delay, so a village goes to sleep before the player's eyes; loaded later, they are out.
public sealed class NightLamp : MonoBehaviour
{
    [Tooltip("How bright each light is at night.")]
    [SerializeField, Min(0f)] private float nightIntensity = 1.2f;
    [Tooltip("Share of the night glow it keeps by day, e.g. a campfire. 0 = out by day.")]
    [SerializeField, Range(0f, 1f)] private float dayShare;
    [Tooltip("How much the flame wavers: 0 = a steady light.")]
    [SerializeField, Range(0f, 0.5f)] private float flicker = 0.08f;
    [SerializeField, Min(0f)] private float flickerSpeed = 5f;

    [Header("Going out")]
    [Tooltip("Goes out once all of these are met, e.g. Leymar going to sleep after the talk with Eldon. Empty = burns all night.")]
    [SerializeField] private List<StoryCondition> outWhen = new();
    [Tooltip("Seconds after Out When is met before it goes out: different for each house, so they go dark one by one.")]
    [SerializeField, Min(0f)] private float outDelay;
    [SerializeField, Min(0.01f)] private float outFadeSeconds = 0.5f;

    private Light2D[] lights;
    private float seed;
    // 1 = burning, 0 = out (Out When).
    private float lit = 1f;
    private bool goingOut;
    private float outAt;

    private void Awake()
    {
        lights = GetComponentsInChildren<Light2D>(true);
        // Each lamp wavers on its own.
        seed = Random.value * 100f;

        // Already out when the scene loads late at night.
        goingOut = IsOutTime();
        lit = goingOut ? 0f : 1f;
        outAt = GameClock.Now;
    }

    private void Update()
    {
        bool outTime = IsOutTime();
        if (outTime && !goingOut)
        {
            goingOut = true;
            outAt = GameClock.Now + outDelay;
        }
        else if (!outTime && goingOut)
        {
            goingOut = false;
            lit = 1f;
        }

        if (goingOut && GameClock.Now >= outAt)
            lit = Mathf.MoveTowards(lit, 0f, GameClock.DeltaTime / outFadeSeconds);

        float glow = Mathf.Lerp(dayShare, 1f, WorldTint.Darkness) * lit;
        for (int i = 0; i < lights.Length; i++)
        {
            float waver = 1f + flicker * (Mathf.PerlinNoise(seed + i * 7.3f, GameClock.Now * flickerSpeed) * 2f - 1f);
            float intensity = nightIntensity * glow * waver;
            lights[i].intensity = intensity;
            lights[i].enabled = intensity > 0.01f;
        }
    }

    private bool IsOutTime()
    {
        return outWhen.Count > 0 && StoryCondition.AreMet(outWhen);
    }
}
