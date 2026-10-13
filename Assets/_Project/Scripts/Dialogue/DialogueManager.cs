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
    private readonly List<DialogueAction> deferredActions = new();
    private NpcCharacter activeNpc;
    private NpcDialogueEntry activeDialogueEntry;
    private System.Action activeOnEnded;
    private string activeNpcKey;
    private PlayerCharacter activePlayer;
    private int activeLineIndex;
    private GameState previousState;
    private bool dialogueActive;

    private Sprite PlayerPortrait => activePlayer != null ? activePlayer.DialogueSprite : null;

    private CharacterProfile PlayerProfile =>
        activePlayer != null && activePlayer.Data != null ? activePlayer.Data.profile : null;

    private CharacterProfile NpcProfile =>
        activeNpc != null && activeNpc.Data != null ? activeNpc.Data.profile : null;

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

    public bool IsDialogueActive => dialogueActive;

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
        deferredActions.Clear();
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

    // Story scenes without an NPC (see DialogueStep). Speakers come from the lines themselves;
    // endActions run after the last line, together with the quest actions of the dialogue,
    // and onEnded is called after them.
    public bool StartDialogue(
        DialogueData dialogue,
        IReadOnlyList<DialogueAction> endActions = null,
        System.Action onEnded = null)
    {
        if (dialogueActive)
            return false;

        ResolveView();
        ResolvePlayer();
        if (view == null)
        {
            Debug.LogError($"{nameof(DialogueView)} is not assigned.", this);
            return false;
        }

        activeLines.Clear();
        deferredActions.Clear();
        AddDialogueDataLines(dialogue);

        if (activeLines.Count == 0)
            return false;

        if (endActions != null)
            deferredActions.AddRange(endActions);

        activeOnEnded = onEnded;
        activeNpc = null;
        activeDialogueEntry = null;
        activeNpcKey = null;
        activeLineIndex = 0;
        dialogueActive = true;
        previousState = GameStateManager.State;
        GameStateManager.SetState(GameState.Dialogue);
        ShowCurrentLine();
        return true;
    }

    public void Continue()
    {
        if (!dialogueActive)
            return;

        if (view != null && view.TryCompleteTextReveal())
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

        ExecuteActions(choice.actions, activeNpc, activePlayer);

        if (!dialogueActive)
            return;

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

        GameStateManager.SetState(previousState);
        ExecuteDeferredQuestActions();

        activeNpc = null;
        activeDialogueEntry = null;
        activeNpcKey = null;

        System.Action onEnded = activeOnEnded;
        activeOnEnded = null;
        onEnded?.Invoke();
    }

    private void ShowCurrentLine()
    {
        ResolvePlayer();

        DialogueLine line = activeLines[activeLineIndex];
        bool playerSpeaking = line.speakerSide == DialogueSpeakerSide.Left;
        Sprite npcPortrait = activeNpc != null ? activeNpc.DialogueSprite : null;
        string fallbackName = playerSpeaking ? PlayerName : activeNpc != null ? activeNpc.DisplayName : string.Empty;
        Sprite fallbackPortrait = playerSpeaking ? PlayerPortrait : npcPortrait;

        ExecuteActions(line.actions, activeNpc, activePlayer);

        if (!dialogueActive)
            return;

        view.ShowLine(
            line,
            fallbackName,
            fallbackPortrait,
            GetListenerPortrait(DialogueSpeakerSide.Left, PlayerPortrait),
            GetListenerPortrait(DialogueSpeakerSide.Right, npcPortrait),
            GetVoice(line),
            GetValidChoices(line));
    }

    private DialogueVoice GetVoice(DialogueLine line)
    {
        CharacterProfile speaker = GetSpeaker(line);
        return speaker != null ? speaker.Voice : null;
    }

    // Whose line it is: the one named on it (story dialogues name everyone, e.g. Hideshi and Haru
    // at home), else the player on the left and the NPC on the right.
    private CharacterProfile GetSpeaker(DialogueLine line)
    {
        if (string.IsNullOrWhiteSpace(line.speakerName))
            return line.speakerSide == DialogueSpeakerSide.Left ? PlayerProfile : NpcProfile;

        string speakerName = line.speakerName.Trim();
        if (IsNamed(PlayerProfile, speakerName))
            return PlayerProfile;

        if (IsNamed(NpcProfile, speakerName))
            return NpcProfile;

        // Anyone else: the profiles of the characters in the scene are loaded.
        foreach (CharacterProfile profile in Resources.FindObjectsOfTypeAll<CharacterProfile>())
        {
            if (IsNamed(profile, speakerName))
                return profile;
        }

        return null;
    }

    private static bool IsNamed(CharacterProfile profile, string speakerName)
    {
        return profile != null
            && string.Equals(profile.DisplayName, speakerName, System.StringComparison.OrdinalIgnoreCase);
    }

    // Who sits on a side while the other one talks, before anyone has spoken there: the one who
    // speaks on that side in this dialogue (e.g. Hideshi on the left in the home intro), else the
    // player on the left and the NPC on the right. A story event's dialogue has no NPC: a side
    // nobody speaks on then stays empty.
    private Sprite GetListenerPortrait(DialogueSpeakerSide side, Sprite defaultPortrait)
    {
        foreach (DialogueLine line in activeLines)
        {
            if (line != null && line.speakerSide == side && !string.IsNullOrWhiteSpace(line.text))
                return line.portraitOverride != null ? line.portraitOverride : defaultPortrait;
        }

        return defaultPortrait;
    }

    private void ExecuteActions(IReadOnlyList<DialogueAction> actions, NpcCharacter npc, PlayerCharacter player)
    {
        if (actions == null)
            return;

        for (int i = 0; i < actions.Count; i++)
        {
            DialogueAction action = actions[i];
            if (action == null)
                continue;

            if (IsDeferredAction(action.type))
            {
                deferredActions.Add(action);
                continue;
            }

            action.Execute(npc, player);
        }
    }

    private void ExecuteDeferredQuestActions()
    {
        if (deferredActions.Count == 0)
            return;

        DialogueAction[] actions = deferredActions.ToArray();
        deferredActions.Clear();

        for (int i = 0; i < actions.Length; i++)
            actions[i]?.Execute(activeNpc, activePlayer);
    }

    // Run once the dialogue has closed: quests, and a battle, which must not cut a line short.
    private static bool IsDeferredAction(DialogueActionType type)
    {
        return type == DialogueActionType.StartQuest
            || type == DialogueActionType.CompleteQuest
            || type == DialogueActionType.FailQuest
            || type == DialogueActionType.StartBattle;
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
