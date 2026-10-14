using UnityEngine;
using UnityEngine.UI;

// TV noise for the intro: soft-tinted CRT grain with scanlines over the whole screen and a
// pulsing glow on the TV itself, and optionally a looping hiss. Built from code, so the scene
// needs no extra assets. Story event steps (Screen Noise, Pull Into Screen) set where the noise
// and glow should go; they ease there on the GameClock (the Dialogue game state pauses
// Time.timeScale). The hiss is as loud as the stronger of the two, so it grows and fades with them.
public sealed class ScreenNoiseOverlay : MonoBehaviour
{
    private const int NoiseSize = 256;
    // Part of the noise texture shown at once (vertically): smaller = bigger grains.
    private const float NoiseScale = 0.45f;
    private const float NoiseFrameTime = 0.045f;
    private const int GlowSize = 64;
    private const float GlowPixelsPerUnit = 32f;
    // A whoosh (see Whoosh): the hiss rises to this pitch and gets this much louder, then drops
    // back in WhooshRelease seconds.
    private const float WhooshPitch = 2.2f;
    private const float WhooshBoost = 1.5f;
    private const float WhooshRelease = 0.35f;
    // The hiss is softened by cutting its highest, harshest part; a whoosh opens it up a little.
    private const float HissCutoff = 3500f;
    private const float WhooshCutoff = 7000f;

    private RawImage noise;
    private SpriteRenderer glow;
    private AudioSource sound;
    private AudioLowPassFilter soundFilter;
    private float soundVolume;
    private Color tint;

    private float noiseAlpha;
    private float noiseTarget;
    private float noiseSpeed;
    private float glowStrength;
    private float glowTarget;
    private float glowSpeed;
    private float noiseTimer;
    private float whoosh;
    private float whooshFrom;
    private float whooshTime;
    private float whooshDuration;

    public Color Tint => tint;

    public static ScreenNoiseOverlay Create(Vector3 screenPosition, Color tint, SpriteRenderer sortingReference)
    {
        ScreenNoiseOverlay overlay = new GameObject("IntroScreenNoise").AddComponent<ScreenNoiseOverlay>();
        overlay.tint = tint;
        overlay.noise = CreateNoise(overlay.transform, tint);
        overlay.glow = CreateGlow(overlay.transform, screenPosition, tint, sortingReference);
        return overlay;
    }

    public void SetNoise(float alpha, float duration)
    {
        noiseTarget = Mathf.Clamp01(alpha);
        noiseSpeed = Mathf.Abs(noiseTarget - noiseAlpha) / Mathf.Max(0.01f, duration);
    }

    public void SetGlow(float strength, float duration)
    {
        glowTarget = Mathf.Clamp01(strength);
        glowSpeed = Mathf.Abs(glowTarget - glowStrength) / Mathf.Max(0.01f, duration);
    }

    // volume: how loud the hiss is at full noise.
    public void PlaySound(AudioClip clip, float volume)
    {
        if (clip == null)
            return;

        if (sound == null)
        {
            sound = gameObject.AddComponent<AudioSource>();
            sound.playOnAwake = false;
            sound.loop = true;
            sound.spatialBlend = 0f;
            soundFilter = gameObject.AddComponent<AudioLowPassFilter>();
            soundFilter.cutoffFrequency = HissCutoff;
        }

        soundVolume = Mathf.Clamp01(volume);
        sound.clip = clip;
        sound.volume = 0f;
        sound.Play();
    }

    // The hiss sweeps up while a character is pulled in (Pull Into Screen), faster towards the
    // end, and drops back right after.
    public void Whoosh(float duration)
    {
        whooshFrom = whoosh;
        whooshTime = 0f;
        whooshDuration = Mathf.Max(0.01f, duration);
    }

    private void Update()
    {
        float deltaTime = GameClock.DeltaTime;
        noiseAlpha = Mathf.MoveTowards(noiseAlpha, noiseTarget, noiseSpeed * deltaTime);
        glowStrength = Mathf.MoveTowards(glowStrength, glowTarget, glowSpeed * deltaTime);

        // A new random window into the noise texture every few frames reads as live noise.
        noiseTimer -= deltaTime;
        if (noiseTimer <= 0f)
        {
            noiseTimer = NoiseFrameTime;
            float aspect = Screen.height > 0 ? (float)Screen.width / Screen.height : 1f;
            noise.uvRect = new Rect(Random.value, Random.value, NoiseScale * aspect, NoiseScale);
        }

        // Flicker scales with the noise, so faint noise stays faint.
        float flicker = Random.Range(-0.05f, 0.05f) * Mathf.Min(1f, noiseAlpha * 3f);
        noise.color = new Color(1f, 1f, 1f, Mathf.Clamp01(noiseAlpha + flicker));

        float pulse = 1f + Mathf.Sin(GameClock.Now * 14f) * 0.07f;
        glow.color = new Color(tint.r, tint.g, tint.b, glowStrength * 0.8f);
        glow.transform.localScale = Vector3.one * (Mathf.Lerp(0.8f, 3.5f, glowStrength) * pulse);

        UpdateWhoosh(deltaTime);
        if (sound != null)
        {
            float sfxVolume = AudioManager.Instance != null ? AudioManager.Instance.GetOutputVolume(AudioChannel.Sfx) : 1f;
            float loudness = Mathf.Max(noiseAlpha, glowStrength) * Mathf.Lerp(1f, WhooshBoost, whoosh);
            sound.volume = Mathf.Clamp01(soundVolume * loudness) * sfxVolume;
            sound.pitch = Mathf.Lerp(1f, WhooshPitch, whoosh);
            soundFilter.cutoffFrequency = Mathf.Lerp(HissCutoff, WhooshCutoff, whoosh);
        }
    }

    private void UpdateWhoosh(float deltaTime)
    {
        if (whooshDuration <= 0f)
            return;

        whooshTime += deltaTime;
        if (whooshTime < whooshDuration)
        {
            float t = whooshTime / whooshDuration;
            whoosh = Mathf.Lerp(whooshFrom, 1f, t * t);
            return;
        }

        whoosh = Mathf.Clamp01(1f - (whooshTime - whooshDuration) / WhooshRelease);
        if (whoosh <= 0f)
            whooshDuration = 0f;
    }

    private void OnDestroy()
    {
        if (noise != null)
            Destroy(noise.texture);
        if (glow != null && glow.sprite != null)
        {
            Destroy(glow.sprite.texture);
            Destroy(glow.sprite);
        }
    }

    private static RawImage CreateNoise(Transform parent, Color tint)
    {
        GameObject canvasObject = new("NoiseCanvas", typeof(Canvas));
        canvasObject.transform.SetParent(parent, false);
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;

        GameObject imageObject = new("Noise", typeof(RectTransform), typeof(RawImage));
        imageObject.transform.SetParent(canvasObject.transform, false);
        RectTransform rect = (RectTransform)imageObject.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        RawImage image = imageObject.GetComponent<RawImage>();
        image.raycastTarget = false;
        image.texture = CreateNoiseTexture(tint);
        image.color = new Color(1f, 1f, 1f, 0f);
        return image;
    }

    // Grains between a dark and a light shade of the tint, every other row dimmed like scanlines.
    private static Texture2D CreateNoiseTexture(Color tint)
    {
        Texture2D texture = new(NoiseSize, NoiseSize, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Repeat
        };

        Color dark = Color.Lerp(tint, Color.black, 0.6f);
        Color light = Color.Lerp(tint, Color.white, 0.3f);
        Color32[] pixels = new Color32[NoiseSize * NoiseSize];
        for (int y = 0; y < NoiseSize; y++)
        {
            float scanline = y % 2 == 0 ? 1f : 0.8f;
            for (int x = 0; x < NoiseSize; x++)
            {
                Color color = Color.Lerp(dark, light, Random.value) * scanline;
                color.a = 1f;
                pixels[y * NoiseSize + x] = color;
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply();
        return texture;
    }

    private static SpriteRenderer CreateGlow(Transform parent, Vector3 position, Color tint, SpriteRenderer sortingReference)
    {
        GameObject glowObject = new("TvGlow", typeof(SpriteRenderer));
        glowObject.transform.SetParent(parent, false);
        glowObject.transform.position = position;

        SpriteRenderer renderer = glowObject.GetComponent<SpriteRenderer>();
        renderer.sprite = CreateGlowSprite();
        renderer.color = new Color(tint.r, tint.g, tint.b, 0f);
        if (sortingReference != null)
        {
            renderer.sortingLayerID = sortingReference.sortingLayerID;
            renderer.sortingOrder = sortingReference.sortingOrder + 10;
        }

        return renderer;
    }

    // A soft round light: opaque in the middle, fading to nothing at the edge.
    private static Sprite CreateGlowSprite()
    {
        Texture2D texture = new(GlowSize, GlowSize, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        Vector2 center = new(GlowSize * 0.5f, GlowSize * 0.5f);
        Color32[] pixels = new Color32[GlowSize * GlowSize];
        for (int y = 0; y < GlowSize; y++)
        {
            for (int x = 0; x < GlowSize; x++)
            {
                float distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center) / center.x;
                float alpha = Mathf.Clamp01(1f - distance);
                pixels[y * GlowSize + x] = new Color(1f, 1f, 1f, alpha * alpha);
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, GlowSize, GlowSize), new Vector2(0.5f, 0.5f), GlowPixelsPerUnit);
    }
}
