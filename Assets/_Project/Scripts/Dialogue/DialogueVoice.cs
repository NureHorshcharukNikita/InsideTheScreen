using UnityEngine;

// How a character sounds while their lines type out, Undertale style: a short blip every few
// letters at a pitch of their own. Set on the character's profile; one voice can serve many.
[CreateAssetMenu(menuName = "Game/Dialogue/Dialogue Voice")]
public sealed class DialogueVoice : ScriptableObject
{
    [Tooltip("Short sounds, about 0.05-0.1 s. Each blip picks one at random.")]
    [SerializeField] private AudioClip[] clips;
    [Tooltip("1 = the sound as it is. Lower for a deeper voice, higher for a squeakier one.")]
    [SerializeField, Range(0.5f, 2f)] private float pitch = 1f;
    [Tooltip("Each blip's pitch varies by up to this much, so the voice does not sound mechanical.")]
    [SerializeField, Range(0f, 0.3f)] private float pitchVariation = 0.05f;
    [Tooltip("A blip every this many letters. Spaces and punctuation do not count.")]
    [SerializeField, Min(1)] private int lettersPerBlip = 2;
    [SerializeField, Range(0f, 1f)] private float volume = 0.6f;

    public int LettersPerBlip => lettersPerBlip;

    public void PlayBlip()
    {
        if (clips == null || clips.Length == 0)
            return;

        AudioClip clip = clips[Random.Range(0, clips.Length)];
        AudioManager.TryPlayVoice(clip, pitch + Random.Range(-pitchVariation, pitchVariation), volume);
    }
}
