using System;
using System.Collections;
using UnityEngine;

public enum AudioChannel
{
    Master,
    Music,
    Ambience,
    Sfx,
    Ui
}

public sealed class AudioManager : MonoBehaviour
{
    private const string MasterVolumeKey = "audio.masterVolume";
    private const string MusicVolumeKey = "audio.musicVolume";
    private const string AmbienceVolumeKey = "audio.ambienceVolume";
    private const string SfxVolumeKey = "audio.sfxVolume";
    private const string UiVolumeKey = "audio.uiVolume";

    public static AudioManager Instance { get; private set; }
    public static event Action VolumesChanged;

    [Header("Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource ambienceSource;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource uiSource;

    [Header("Volumes")]
    [SerializeField, Range(0f, 1f)] private float masterVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float musicVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float ambienceVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float sfxVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float uiVolume = 1f;

    [Header("Persistence")]
    [SerializeField] private bool saveVolumes = true;

    private Coroutine musicFadeRoutine;
    private Coroutine ambienceFadeRoutine;

    public float MasterVolume => masterVolume;
    public float MusicVolume => musicVolume;
    public float AmbienceVolume => ambienceVolume;
    public float SfxVolume => sfxVolume;
    public float UiVolume => uiVolume;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        InitializeSources();
        LoadVolumes();
        ApplyVolumes();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public static void TryPlaySfx(AudioClip clip, float volumeScale = 1f)
    {
        if (Instance != null)
            Instance.PlaySfx(clip, volumeScale);
    }

    public static void TryPlayUi(AudioClip clip, float volumeScale = 1f)
    {
        if (Instance != null)
            Instance.PlayUi(clip, volumeScale);
    }

    public static void TryPlaySfxAtPosition(AudioClip clip, Vector3 position, float volumeScale = 1f)
    {
        if (clip == null || Instance == null)
            return;

        AudioSource.PlayClipAtPoint(
            clip,
            position,
            Mathf.Clamp01(volumeScale) * Instance.masterVolume * Instance.sfxVolume);
    }

    public void PlayMusic(AudioClip clip, bool restart = false, float fadeDuration = 0f)
    {
        PlayLoopingClip(
            musicSource,
            clip,
            restart,
            fadeDuration,
            AudioChannel.Music,
            ref musicFadeRoutine);
    }

    public void PlayAmbience(AudioClip clip, bool restart = false, float fadeDuration = 0f)
    {
        PlayLoopingClip(
            ambienceSource,
            clip,
            restart,
            fadeDuration,
            AudioChannel.Ambience,
            ref ambienceFadeRoutine);
    }

    public void StopMusic(float fadeDuration = 0f)
    {
        StopLoopingClip(musicSource, fadeDuration, ref musicFadeRoutine);
    }

    public void StopAmbience(float fadeDuration = 0f)
    {
        StopLoopingClip(ambienceSource, fadeDuration, ref ambienceFadeRoutine);
    }

    public void PlaySfx(AudioClip clip, float volumeScale = 1f)
    {
        PlayOneShot(sfxSource, clip, volumeScale);
    }

    public void PlayUi(AudioClip clip, float volumeScale = 1f)
    {
        PlayOneShot(uiSource, clip, volumeScale);
    }

    public void SetVolume(AudioChannel channel, float volume)
    {
        volume = Mathf.Clamp01(volume);

        switch (channel)
        {
            case AudioChannel.Master:
                masterVolume = volume;
                break;
            case AudioChannel.Music:
                musicVolume = volume;
                break;
            case AudioChannel.Ambience:
                ambienceVolume = volume;
                break;
            case AudioChannel.Sfx:
                sfxVolume = volume;
                break;
            case AudioChannel.Ui:
                uiVolume = volume;
                break;
        }

        ApplyVolumes();
        SaveVolumes();
        VolumesChanged?.Invoke();
    }

    public float GetVolume(AudioChannel channel)
    {
        return channel switch
        {
            AudioChannel.Master => masterVolume,
            AudioChannel.Music => musicVolume,
            AudioChannel.Ambience => ambienceVolume,
            AudioChannel.Sfx => sfxVolume,
            AudioChannel.Ui => uiVolume,
            _ => 1f
        };
    }

    public float GetOutputVolume(AudioChannel channel)
    {
        return masterVolume * GetVolume(channel);
    }

    private void InitializeSources()
    {
        musicSource = EnsureSource(musicSource, "Music Source", true);
        ambienceSource = EnsureSource(ambienceSource, "Ambience Source", true);
        sfxSource = EnsureSource(sfxSource, "SFX Source", false);
        uiSource = EnsureSource(uiSource, "UI Source", false);
    }

    private AudioSource EnsureSource(AudioSource source, string sourceName, bool loop)
    {
        if (source == null)
            source = gameObject.AddComponent<AudioSource>();

        source.name = sourceName;
        source.playOnAwake = false;
        source.loop = loop;
        source.spatialBlend = 0f;
        source.ignoreListenerPause = false;
        source.ignoreListenerVolume = false;
        return source;
    }

    private void PlayLoopingClip(
        AudioSource source,
        AudioClip clip,
        bool restart,
        float fadeDuration,
        AudioChannel channel,
        ref Coroutine fadeRoutine)
    {
        if (source == null || clip == null)
            return;

        if (!restart && source.clip == clip && source.isPlaying)
            return;

        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        if (fadeDuration > 0f && source.isPlaying)
            fadeRoutine = StartCoroutine(SwitchLoopingClip(source, clip, fadeDuration, channel));
        else
        {
            source.clip = clip;
            source.volume = GetOutputVolume(channel);
            source.Play();
            fadeRoutine = null;
        }
    }

    private void StopLoopingClip(AudioSource source, float fadeDuration, ref Coroutine fadeRoutine)
    {
        if (source == null)
            return;

        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        if (fadeDuration > 0f && source.isPlaying)
            fadeRoutine = StartCoroutine(FadeOutThenStop(source, fadeDuration));
        else
        {
            source.Stop();
            source.clip = null;
            fadeRoutine = null;
        }
    }

    private void PlayOneShot(AudioSource source, AudioClip clip, float volumeScale)
    {
        if (source == null || clip == null)
            return;

        source.PlayOneShot(clip, Mathf.Clamp01(volumeScale));
    }

    private IEnumerator SwitchLoopingClip(AudioSource source, AudioClip clip, float duration, AudioChannel channel)
    {
        yield return FadeSourceVolume(source, duration * 0.5f, source.volume, 0f);

        source.clip = clip;
        source.Play();

        yield return FadeSourceVolume(source, duration * 0.5f, 0f, GetOutputVolume(channel));
    }

    private IEnumerator FadeOutThenStop(AudioSource source, float duration)
    {
        yield return FadeSourceVolume(source, duration, source.volume, 0f);

        source.Stop();
        source.clip = null;
    }

    private IEnumerator FadeSourceVolume(AudioSource source, float duration, float from, float to)
    {
        if (duration <= 0f)
        {
            source.volume = to;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            source.volume = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        source.volume = to;
    }

    private void ApplyVolumes()
    {
        if (musicSource != null)
            musicSource.volume = GetOutputVolume(AudioChannel.Music);

        if (ambienceSource != null)
            ambienceSource.volume = GetOutputVolume(AudioChannel.Ambience);

        if (sfxSource != null)
            sfxSource.volume = GetOutputVolume(AudioChannel.Sfx);

        if (uiSource != null)
            uiSource.volume = GetOutputVolume(AudioChannel.Ui);
    }

    private void LoadVolumes()
    {
        if (!saveVolumes)
            return;

        masterVolume = PlayerPrefs.GetFloat(MasterVolumeKey, masterVolume);
        musicVolume = PlayerPrefs.GetFloat(MusicVolumeKey, musicVolume);
        ambienceVolume = PlayerPrefs.GetFloat(AmbienceVolumeKey, ambienceVolume);
        sfxVolume = PlayerPrefs.GetFloat(SfxVolumeKey, sfxVolume);
        uiVolume = PlayerPrefs.GetFloat(UiVolumeKey, uiVolume);
    }

    private void SaveVolumes()
    {
        if (!saveVolumes)
            return;

        PlayerPrefs.SetFloat(MasterVolumeKey, masterVolume);
        PlayerPrefs.SetFloat(MusicVolumeKey, musicVolume);
        PlayerPrefs.SetFloat(AmbienceVolumeKey, ambienceVolume);
        PlayerPrefs.SetFloat(SfxVolumeKey, sfxVolume);
        PlayerPrefs.SetFloat(UiVolumeKey, uiVolume);
        PlayerPrefs.Save();
    }
}
