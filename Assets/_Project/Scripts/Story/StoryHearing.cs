using UnityEngine;

// Sits on the Audio Listener while Hearing steps are in effect (e.g. waking up in the forest):
// eases the volume of everything and a low-pass filter over the whole mix on the GameClock.
// Removed when the event ends, which makes everything sound normal again.
public sealed class StoryHearing : MonoBehaviour
{
    private const float ClearCutoff = 22000f;
    private const float MuffledCutoff = 400f;

    private AudioLowPassFilter filter;
    private float volume = 1f;
    private float volumeTarget = 1f;
    private float volumeSpeed;
    private float muffle;
    private float muffleTarget;
    private float muffleSpeed;

    public static StoryHearing Get(StoryEventContext context)
    {
        AudioListener listener = FindFirstObjectByType<AudioListener>();
        if (listener == null)
            return null;

        if (!listener.TryGetComponent(out StoryHearing hearing))
        {
            hearing = listener.gameObject.AddComponent<StoryHearing>();
            context.OnRelease(() =>
            {
                if (hearing != null)
                    Destroy(hearing);
            });
        }

        return hearing;
    }

    // muffle: 0 = clear, 1 = as if through a wall.
    public void Set(float targetVolume, float targetMuffle, float duration)
    {
        volumeTarget = Mathf.Clamp01(targetVolume);
        muffleTarget = Mathf.Clamp01(targetMuffle);
        if (duration <= 0f)
        {
            volume = volumeTarget;
            muffle = muffleTarget;
            Apply();
            return;
        }

        volumeSpeed = Mathf.Abs(volumeTarget - volume) / duration;
        muffleSpeed = Mathf.Abs(muffleTarget - muffle) / duration;
    }

    private void Awake()
    {
        filter = gameObject.AddComponent<AudioLowPassFilter>();
        Apply();
    }

    private void Update()
    {
        float deltaTime = GameClock.DeltaTime;
        volume = Mathf.MoveTowards(volume, volumeTarget, volumeSpeed * deltaTime);
        muffle = Mathf.MoveTowards(muffle, muffleTarget, muffleSpeed * deltaTime);
        Apply();
    }

    private void Apply()
    {
        AudioListener.volume = volume;
        // Exponential, so the sound opens up evenly to the ear.
        if (filter != null)
            filter.cutoffFrequency = ClearCutoff * Mathf.Pow(MuffledCutoff / ClearCutoff, muffle);
    }

    private void OnDestroy()
    {
        AudioListener.volume = 1f;
        if (filter != null)
            Destroy(filter);
    }
}
