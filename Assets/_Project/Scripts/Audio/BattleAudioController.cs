using UnityEngine;

public sealed class BattleAudioController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private HandUI handUi;

    [Header("Card SFX")]
    [SerializeField] private AudioClip drawCardSfx;
    [SerializeField, Range(0f, 1f)] private float drawCardSfxVolume = 0.8f;

    private void Awake()
    {
        if (handUi == null)
            handUi = FindFirstObjectByType<HandUI>();
    }

    private void OnEnable()
    {
        if (handUi != null)
            handUi.DrawCardStarted += PlayDrawCardSfx;
    }

    private void OnDisable()
    {
        if (handUi != null)
            handUi.DrawCardStarted -= PlayDrawCardSfx;
    }

    private void PlayDrawCardSfx()
    {
        AudioManager.TryPlaySfx(drawCardSfx, drawCardSfxVolume);
    }
}
