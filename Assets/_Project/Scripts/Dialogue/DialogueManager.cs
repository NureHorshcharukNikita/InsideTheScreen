using System.Collections.Generic;
using UnityEngine;

public sealed class DialogueManager : MonoBehaviour
{
    [SerializeField] private DialogueView view;

    private static DialogueManager instance;

    public static DialogueManager Instance
    {
        get
        {
            if (instance == null)
                instance = FindFirstObjectByType<DialogueManager>(FindObjectsInactive.Include);

            return instance;
        }
    }

    private readonly List<DialogueLine> activeLines = new();
    private NpcCharacter activeNpc;
    private NpcDialogueEntry activeDialogueEntry;
    private string activeNpcKey;
    private PlayerCharacter activePlayer;
    private int activeLineIndex;
    private GameState previousState;
    private bool dialogueActive;

    private Sprite PlayerPortrait => activePlayer != null ? activePlayer.DialogueSprite : null;

    private string PlayerName => activePlayer != null ? activePlayer.DisplayName : "Player";

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        ResolveView();
        ResolvePlayer();
    }

    private void OnEnable()
    {
        ResolveView();
        ResolvePlayer();

        if (view != null)
        {
            view.ContinueRequested -= Continue;
            view.ContinueRequested += Continue;
            view.ChoiceRequested -= Choose;
            view.ChoiceRequested += Choose;
        }
    }

    private void OnDisable()
    {
        if (view != null)
        {
            view.ContinueRequested -= Continue;
            view.ChoiceRequested -= Choose;
        }
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    private void Update()
    {
        if (dialogueActive && Input.GetKeyDown(KeyCode.Space))
            Continue();
    }

    public bool StartDialogue(NpcCharacter npc)
    {
        if (npc == null || !npc.CanTalk)
            return false;

        ResolveView();
        ResolvePlayer();
        if (view == null)
        {
            Debug.LogError($"{nameof(DialogueView)} is not assigned.", this);
            return false;
        }

        activeLines.Clear();
        DialogueData dialogue = npc.SelectDialogue(out NpcDialogueEntry selectedEntry);
        AddDialogueDataLines(dialogue);

        if (activeLines.Count == 0)
            return false;

        activeNpc = npc;
        activeDialogueEntry = selectedEntry;
        activeNpcKey = npc.PersistenceKey;
        activeLineIndex = 0;
        dialogueActive = true;
        previousState = GameStateManager.State;
        GameStateManager.SetState(GameState.Dialogue);
        npc.TryInteract(NpcInteractionType.Talk);
        ShowCurrentLine();
        return true;
    }

    public void Continue()
    {
        if (!dialogueActive)
            return;

        if (CurrentLineHasChoices())
            return;

        DialogueLine line = activeLines[activeLineIndex];
        if (line.endDialogueAfterLine)
        {
            EndDialogue();
            return;
        }

        GoToLine(GetNextLineIndex(line));
    }

    public void Choose(int choiceIndex)
    {
        if (!dialogueActive)
            return;

        DialogueLine line = activeLines[activeLineIndex];
        List<DialogueChoice> choices = GetValidChoices(line);
        if (choiceIndex < 0 || choiceIndex >= choices.Count)
            return;

        DialogueChoice choice = choices[choiceIndex];
        if (choice == null)
            return;

        ExecuteActions(choice.actions);

        if (choice.endDialogue)
        {
            EndDialogue();
            return;
        }

        int nextLineIndex = choice.nextLineIndex < 0
            ? GetNextLineIndex(line)
            : choice.nextLineIndex;

        GoToLine(nextLineIndex);
    }

    private void GoToLine(int lineIndex)
    {
        if (lineIndex < 0 || lineIndex >= activeLines.Count)
        {
            EndDialogue();
            return;
        }

        activeLineIndex = lineIndex;
        ShowCurrentLine();
    }

    private int GetNextLineIndex(DialogueLine line)
    {
        return line != null && line.nextLineIndex >= 0
            ? line.nextLineIndex
            : activeLineIndex + 1;
    }

    public void EndDialogue()
    {
        if (!dialogueActive)
            return;

        dialogueActive = false;
        activeLines.Clear();

        if (view != null)
            view.Hide();

        NpcDialogueProgress.RecordDialogueFinished(activeNpcKey, activeDialogueEntry?.Key);

        if (activeNpc != null)
            activeNpc.EndInteraction();

        activeNpc = null;
        activeDialogueEntry = null;
        activeNpcKey = null;
        GameStateManager.SetState(previousState);
    }

    private void ShowCurrentLine()
    {
        ResolvePlayer();

        DialogueLine line = activeLines[activeLineIndex];
        bool playerSpeaking = line.speakerSide == DialogueSpeakerSide.Left;
        string fallbackName = playerSpeaking ? PlayerName : activeNpc.DisplayName;
        Sprite fallbackPortrait = playerSpeaking ? PlayerPortrait : activeNpc.DialogueSprite;

        ExecuteActions(line.actions);

        view.ShowLine(
            line,
            fallbackName,
            fallbackPortrait,
            PlayerPortrait,
            activeNpc.DialogueSprite,
            GetValidChoices(line));
    }

    private static void ExecuteActions(IReadOnlyList<DialogueAction> actions)
    {
        if (actions == null)
            return;

        for (int i = 0; i < actions.Count; i++)
            actions[i]?.Execute();
    }

    private bool CurrentLineHasChoices()
    {
        return activeLineIndex >= 0
            && activeLineIndex < activeLines.Count
            && GetValidChoices(activeLines[activeLineIndex]).Count > 0;
    }

    private static List<DialogueChoice> GetValidChoices(DialogueLine line)
    {
        List<DialogueChoice> choices = new();
        if (line?.choices == null)
            return choices;

        for (int i = 0; i < line.choices.Count; i++)
        {
            DialogueChoice choice = line.choices[i];
            if (choice != null && !string.IsNullOrWhiteSpace(choice.text))
                choices.Add(choice);
        }

        return choices;
    }

    private void AddDialogueDataLines(DialogueData dialogue)
    {
        if (dialogue == null || dialogue.lines == null)
            return;

        for (int i = 0; i < dialogue.lines.Count; i++)
        {
            DialogueLine line = dialogue.lines[i];
            if (line != null && !string.IsNullOrWhiteSpace(line.text))
                activeLines.Add(line);
        }
    }

    private void ResolveView()
    {
        if (view == null)
            view = FindFirstObjectByType<DialogueView>(FindObjectsInactive.Include);
    }

    private void ResolvePlayer()
    {
        if (activePlayer == null)
            activePlayer = FindFirstObjectByType<PlayerCharacter>(FindObjectsInactive.Include);
    }
}
