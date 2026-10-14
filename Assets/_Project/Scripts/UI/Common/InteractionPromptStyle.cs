using UnityEngine;

// The look of every interaction prompt (InteractionPrompt): the key cap, the dark panel and the
// text. Sizes are in the prompt canvas' units: 100 = 1 world unit. Kept in Resources so NPCs,
// doorways and signs find it without each holding a reference.
[CreateAssetMenu(menuName = "Game/UI/Interaction Prompt Style", fileName = ResourceName)]
public sealed class InteractionPromptStyle : ScriptableObject
{
    public const string ResourceName = "InteractionPromptStyle";

    [Header("Key")]
    public Sprite keySprite;
    public float keyHeight = 60f;
    public float keyFontSize = 34f;
    public Color keyTextColor = new(0.95f, 0.93f, 0.86f);
    [Tooltip("Room left and right of a long key name (Space, Enter), which widens the key cap.")]
    public float keyTextPadding = 18f;

    [Header("Panel")]
    public Sprite panelSprite;
    public Color panelColor = Color.white;
    public float panelHeight = 46f;
    [Tooltip("Between the key cap and the text.")]
    public float textGap = 10f;
    [Tooltip("Right of the text; around a message on its own.")]
    public float panelPadding = 20f;
    [Tooltip("Above and below a message of several lines.")]
    public float messagePadding = 10f;

    [Header("Text")]
    public float fontSize = 32f;
    public Color textColor = new(1f, 0.95f, 0.82f);
    [Tooltip("{0} is the action, e.g. Open.")]
    public string actionFormat = "- {0}";

    [Header("Animation")]
    [Tooltip("The key pops up, then the panel slides out of it.")]
    public float showDuration = 0.32f;
    [Tooltip("The panel slides back into the key, then the key goes.")]
    public float hideDuration = 0.24f;

    private static InteractionPromptStyle instance;

    public static InteractionPromptStyle Instance =>
        instance != null ? instance : instance = Resources.Load<InteractionPromptStyle>(ResourceName);
}
