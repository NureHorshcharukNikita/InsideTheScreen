using UnityEngine;
using UnityEngine.UI;

// Asks before the first fast skip of a dialogue (holding the skip key, the Next key or the skip arrow),
// so the player does not rush through a talk by accident. Skip only lets it go on for the rest of
// these dialogues and cutscenes: back to walking about, the next fast skip asks again. With Don't Ask
// Again ticked it never asks again in this game (saved with it, like the quest tracker's place). Either
// way the skip itself waits for the key to be held again (DialogueView).
// The panel is the SkipDialogueWarning prefab under each scene's DialogueRoot; this only fills it in.
public sealed class DialogueSkipWarning : MonoBehaviour
{
    // Story flag: "Don't ask again" ticked, saved with the game.
    public const string DontAskFlag = "ui_dialogue_skip_unasked";

    [SerializeField] private Button skipButton;
    [SerializeField] private Button cancelButton;
    [Tooltip("The Don't Ask Again row: clicking it ticks the box.")]
    [SerializeField] private Button dontAskButton;
    [SerializeField] private Image dontAskBox;
    [SerializeField] private Sprite unticked;
    [SerializeField] private Sprite ticked;

    // Skip picked: fast skipping goes on without asking until the player is back to walking about.
    private static bool allowedUntilPlay;

    private bool dontAsk;
    private bool wired;

    public bool IsOpen => gameObject.activeSelf;

    public static bool MayFastSkip => allowedUntilPlay || StoryFlagManager.HasFlag(DontAskFlag);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetForPlayMode()
    {
        allowedUntilPlay = false;
        GameStateManager.StateChanged -= ForgetOnceWalking;
        GameStateManager.StateChanged += ForgetOnceWalking;
    }

    private static void ForgetOnceWalking()
    {
        if (GameStateManager.IsGameplay)
            allowedUntilPlay = false;
    }

    public void Ask()
    {
        Wire();
        dontAsk = false;
        ShowBox();
        gameObject.SetActive(true);
    }

    private void Update()
    {
        if (GameInputSettings.GetKeyDown(GameInputAction.Close))
            Close();
    }

    private void Wire()
    {
        if (wired)
            return;

        wired = true;
        if (skipButton != null)
            skipButton.onClick.AddListener(Skip);
        if (cancelButton != null)
            cancelButton.onClick.AddListener(Close);
        if (dontAskButton != null)
            dontAskButton.onClick.AddListener(ToggleDontAsk);
    }

    private void Skip()
    {
        if (dontAsk)
            StoryFlagManager.SetFlag(DontAskFlag);

        allowedUntilPlay = true;
        Close();
    }

    private void ToggleDontAsk()
    {
        dontAsk = !dontAsk;
        ShowBox();
    }

    private void ShowBox()
    {
        if (dontAskBox != null)
            dontAskBox.sprite = dontAsk ? ticked : unticked;
    }

    public void Close()
    {
        if (gameObject.activeSelf)
            gameObject.SetActive(false);
    }
}
