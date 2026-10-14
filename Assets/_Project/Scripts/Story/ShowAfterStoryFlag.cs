using UnityEngine;

// Shows an object for good once a story flag is set, also when the scene loads later, e.g. the
// console Hideshi puts on the floor in the home intro. A story event can show it earlier with a
// Set Active step. Sits on an active object and shows `target` (a hidden child): an inactive
// object could not check the flag itself.
public sealed class ShowAfterStoryFlag : MonoBehaviour
{
    [SerializeField] private GameObject target;
    [SerializeField, StoryFlag] private string storyFlag;

    private void Awake()
    {
        StoryFlagManager.Changed += Apply;
        Apply();
    }

    private void OnDestroy()
    {
        StoryFlagManager.Changed -= Apply;
    }

    private void Apply()
    {
        if (target != null && !string.IsNullOrWhiteSpace(storyFlag) && StoryFlagManager.HasFlag(storyFlag))
            target.SetActive(true);
    }
}
