using System;
using System.Collections;
using UnityEngine;
using Object = UnityEngine.Object;

// The end of the demo: fades the screen to black and shows the end screen
// (ExplorationEndGameController), which takes the player back to the main menu. It never
// finishes, so the event keeps the game paused under the end screen; put it last.
[Serializable, SubclassMenu("Scene/End Of Demo")]
public sealed class EndOfDemoStep : StoryStep
{
    [SerializeField, Min(0f)] private float fadeToBlackDuration = 1.5f;
    [Tooltip("How long the screen stays plain black before the text fades in.")]
    [SerializeField, Min(0f)] private float blackHoldDuration = 1f;
    [SerializeField, Min(0f)] private float textFadeDuration = 1f;

    public override IEnumerator Run(StoryEventContext context)
    {
        ExplorationEndGameController endScreen = Object.FindFirstObjectByType<ExplorationEndGameController>();
        if (endScreen == null)
        {
            Debug.LogWarning("End Of Demo step: there is no ExplorationEndGameController in the scene.");
            yield break;
        }

        yield return endScreen.PlayEndOfDemo(fadeToBlackDuration, blackHoldDuration, textFadeDuration);

        while (true)
            yield return null;
    }

    public override string ToString()
    {
        return $"End of demo (fade {fadeToBlackDuration:0.##} s)";
    }
}
