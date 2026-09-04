using UnityEngine;

public sealed class SceneAudio : MonoBehaviour
{
    [Header("Looping Audio")]
    [SerializeField] private AudioClip musicClip;
    [SerializeField] private AudioClip ambienceClip;
    [SerializeField, Min(0f)] private float fadeDuration = 1f;

    [Header("Empty Clips")]
    [SerializeField] private bool stopMusicWhenEmpty;
    [SerializeField] private bool stopAmbienceWhenEmpty = true;

    private void Start()
    {
        if (AudioManager.Instance == null)
            return;

        if (musicClip != null)
            AudioManager.Instance.PlayMusic(musicClip, fadeDuration: fadeDuration);
        else if (stopMusicWhenEmpty)
            AudioManager.Instance.StopMusic(fadeDuration);

        if (ambienceClip != null)
            AudioManager.Instance.PlayAmbience(ambienceClip, fadeDuration: fadeDuration);
        else if (stopAmbienceWhenEmpty)
            AudioManager.Instance.StopAmbience(fadeDuration);
    }
}
