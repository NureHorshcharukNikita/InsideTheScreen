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
        }
    }

    private void OnDisable()
    {
        if (view != null)
            view.ContinueRequested -= Continue;
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
        AddDialogueDataLines(npc.Dialogue);

        if (activeLines.Count == 0)
            return false;

        activeNpc = npc;
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

        activeLineIndex++;
        if (activeLineIndex >= activeLines.Count)
        {
            EndDialogue();
            return;
        }

        ShowCurrentLine();
    }

    public void EndDialogue()
    {
        if (!dialogueActive)
            return;

        dialogueActive = false;
        activeLines.Clear();

        if (view != null)
            view.Hide();

        if (activeNpc != null)
            activeNpc.EndInteraction();

        activeNpc = null;
        GameStateManager.SetState(previousState);
    }

    private void ShowCurrentLine()
    {
        ResolvePlayer();

        DialogueLine line = activeLines[activeLineIndex];
        bool playerSpeaking = line.speakerSide == DialogueSpeakerSide.Left;
        string fallbackName = playerSpeaking ? PlayerName : activeNpc.DisplayName;
        Sprite fallbackPortrait = playerSpeaking ? PlayerPortrait : activeNpc.DialogueSprite;

        view.ShowLine(
            line,
            fallbackName,
            fallbackPortrait,
            PlayerPortrait,
            activeNpc.DialogueSprite);
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
