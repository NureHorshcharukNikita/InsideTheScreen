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
    // The lines that can show from where the dialogue started, up to where it ends. A dialogue
    // played in parts (see DialogueStep) only has the people of the part in its portraits.
    private readonly List<DialogueLine> reachableLines = new();
    private readonly List<DialogueAction> deferredActions = new();
    // The answers for the NPC's talk topics (NpcData.talkTopics), made once per topic in a talk so
    // the one picked is known again in Choose.
    private readonly Dictionary<NpcTalkTopic, DialogueChoice> topicChoices = new();
    // Talked about in this talk: an Every Talk topic is not offered again until the next one.
    private readonly HashSet<NpcTalkTopic> topicsTalkedAbout = new();
    private DialogueChoice closeTopicChoice;
    private NpcCharacter activeNpc;
    private NpcDialogueEntry activeDialogueEntry;
    // The talk leads straight on into a story scene (DialogueData.noTalkTopics): no topics at its end.
    private bool activeWithoutTopics;
    private System.Action<int> activeOnEnded;
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

    // Talking to an NPC starts right where the player stands: nobody steps forward first. Only a story
    // event walking them up to the NPC puts Hideshi in front (TalkToStep, Eldon's first meeting).
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

        int firstLine = SkipUnsaidLines(0);
        if (firstLine < 0 || firstLine >= activeLines.Count)
            return false;

        activeNpc = npc;
        activeDialogueEntry = selectedEntry;
        activeWithoutTopics = dialogue != null && dialogue.noTalkTopics;
        activeNpcKey = npc.PersistenceKey;
        activeLineIndex = firstLine;
        CollectReachableLines();
        dialogueActive = true;
        previousState = GameStateManager.State;
        GameStateManager.SetState(GameState.Dialogue);
        npc.TryInteract(NpcInteractionType.Talk);
        ShowCurrentLine();
        return true;
    }

    // The companion walking with the player who leads this talk when a story event walks them up to
    // the NPC (NpcData.companionLeadsTalks, a talk he speaks in; TalkToStep); null when the player
    // talks alone.
    public static CompanionFollower FindCompanionLeading(NpcCharacter npc, NpcDialogueEntry entry)
    {
        if (npc == null || entry == null || !entry.needsCompanion || npc.Data == null || !npc.Data.companionLeadsTalks)
            return null;

        return CompanionFollower.FindFollowing();
    }

    // Story scenes without an NPC (see DialogueStep). Speakers come from the lines themselves;
    // endActions run after the last line, together with the quest actions of the dialogue,
    // and onEnded is called after them. A dialogue can be played in parts: onEnded gets the line
    // the next part starts from, which is passed back as startLine.
    public bool StartDialogue(
        DialogueData dialogue,
        int startLine = 0,
        IReadOnlyList<DialogueAction> endActions = null,
        System.Action<int> onEnded = null)
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

        if (startLine < 0 || startLine >= activeLines.Count)
            return false;

        int firstLine = SkipUnsaidLines(startLine);
        if (firstLine < 0 || firstLine >= activeLines.Count)
            return false;

        if (endActions != null)
            deferredActions.AddRange(endActions);

        activeOnEnded = onEnded;
        activeNpc = null;
        activeDialogueEntry = null;
        activeNpcKey = null;
        activeLineIndex = firstLine;
        CollectReachableLines();
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
            EndDialogue(GetNextLineIndex(activeLineIndex));
            return;
        }

        GoToLine(GetNextLineIndex(activeLineIndex));
    }

    public void Choose(int choiceIndex)
    {
        if (!dialogueActive)
            return;

        List<DialogueChoice> choices = GetValidChoices(activeLineIndex);
        if (choiceIndex < 0 || choiceIndex >= choices.Count)
            return;

        DialogueChoice choice = choices[choiceIndex];
        if (choice == null || TryTalkAbout(choice))
            return;

        ExecuteActions(choice.actions, activeNpc, activePlayer);

        if (!dialogueActive)
            return;

        int nextLineIndex = GetChoiceTarget(choice, activeLineIndex);
        if (choice.endDialogue)
        {
            EndDialogue(nextLineIndex);
            return;
        }

        GoToLine(nextLineIndex);
    }

    private void GoToLine(int lineIndex)
    {
        lineIndex = SkipUnsaidLines(lineIndex);
        if (lineIndex < 0 || lineIndex >= activeLines.Count)
        {
            EndDialogue(lineIndex);
            return;
        }

        activeLineIndex = lineIndex;
        ShowCurrentLine();
    }

    private int GetNextLineIndex(int lineIndex)
    {
        DialogueLine line = activeLines[lineIndex];
        return line != null && line.nextLineIndex >= 0
            ? line.nextLineIndex
            : lineIndex + 1;
    }

    private int GetChoiceTarget(DialogueChoice choice, int lineIndex)
    {
        return choice.nextLineIndex < 0 ? GetNextLineIndex(lineIndex) : choice.nextLineIndex;
    }

    // A line whose conditions are not met is passed over as if it were not there, e.g. a hint
    // that only makes sense until a quest is done.
    private static bool IsSaid(DialogueLine line)
    {
        return line != null && StoryCondition.AreMet(line.conditions);
    }

    // The line itself, or the first one after it that is said. Out of range: no more lines.
    private int SkipUnsaidLines(int lineIndex)
    {
        for (int skipped = 0; skipped <= activeLines.Count; skipped++)
        {
            if (lineIndex < 0 || lineIndex >= activeLines.Count || IsSaid(activeLines[lineIndex]))
                return lineIndex;

            lineIndex = GetNextLineIndex(lineIndex);
        }

        return -1;
    }

    // Follows the flow from the first line (next lines and choices) until the lines that end the
    // dialogue.
    private void CollectReachableLines()
    {
        reachableLines.Clear();
        bool[] reached = new bool[activeLines.Count];
        bool[] said = new bool[activeLines.Count];
        Stack<int> pending = new();
        pending.Push(activeLineIndex);

        while (pending.Count > 0)
        {
            int index = pending.Pop();
            if (index < 0 || index >= reached.Length || reached[index])
                continue;

            reached[index] = true;
            if (!IsSaid(activeLines[index]))
            {
                pending.Push(GetNextLineIndex(index));
                continue;
            }

            said[index] = true;
            List<DialogueChoice> choices = GetValidChoices(index);
            if (choices.Count > 0)
            {
                foreach (DialogueChoice choice in choices)
                {
                    if (!choice.endDialogue)
                        pending.Push(GetChoiceTarget(choice, index));
                }
            }
            else if (!activeLines[index].endDialogueAfterLine)
            {
                pending.Push(GetNextLineIndex(index));
            }
        }

        for (int i = 0; i < said.Length; i++)
        {
            if (said[i])
                reachableLines.Add(activeLines[i]);
        }
    }

    public void EndDialogue()
    {
        EndDialogue(activeLineIndex + 1);
    }

    // nextLineIndex: where the dialogue would have gone on, for a later part of it.
    private void EndDialogue(int nextLineIndex)
    {
        if (!dialogueActive)
            return;

        dialogueActive = false;
        activeLines.Clear();
        reachableLines.Clear();
        topicChoices.Clear();
        topicsTalkedAbout.Clear();

        if (view != null)
            view.Hide();

        // Not a Companion Away dialogue (no entry): it does not count as a talk.
        if (activeDialogueEntry != null)
            NpcDialogueProgress.RecordDialogueFinished(activeNpcKey, activeDialogueEntry.Key);

        if (activeNpc != null)
            activeNpc.EndInteraction();

        GameStateManager.SetState(previousState);
        ExecuteDeferredQuestActions();

        activeNpc = null;
        activeDialogueEntry = null;
        activeNpcKey = null;

        System.Action<int> onEnded = activeOnEnded;
        activeOnEnded = null;
        onEnded?.Invoke(nextLineIndex);
    }

    private void ShowCurrentLine()
    {
        ResolvePlayer();

        DialogueLine line = activeLines[activeLineIndex];
        bool playerSpeaking = line.speakerSide == DialogueSpeakerSide.Left;
        Sprite npcPortrait = activeNpc != null ? activeNpc.DialogueSprite : null;
        string speakerName = !string.IsNullOrWhiteSpace(line.speakerName)
            ? line.speakerName
            : playerSpeaking ? PlayerName : activeNpc != null ? activeNpc.DisplayName : string.Empty;
        Sprite fallbackPortrait = playerSpeaking ? PlayerPortrait : npcPortrait;

        ExecuteActions(line.actions, activeNpc, activePlayer);

        if (!dialogueActive)
            return;

        view.ShowLine(
            line,
            KnownNames.Shown(GetSpeaker(line), speakerName),
            fallbackPortrait,
            GetListenerPortrait(DialogueSpeakerSide.Left, PlayerPortrait),
            GetListenerPortrait(DialogueSpeakerSide.Right, npcPortrait),
            GetVoice(line),
            GetValidChoices(activeLineIndex));

        // After the name plate: a name said on this line shows from the next one on.
        KnownNames.Hear(line.text);
    }

    private DialogueVoice GetVoice(DialogueLine line)
    {
        if (line.voiceOverride != null)
            return line.voiceOverride;

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
    // speaks on that side in this dialogue, or in this part of it (e.g. Hideshi on the left in the
    // home intro), else the player on the left and the NPC on the right. In a story event's dialogue
    // (no NPC) a side nobody speaks on stays empty: the player often speaks on the other side there.
    // Lines without a portrait (the TV) do not count.
    private Sprite GetListenerPortrait(DialogueSpeakerSide side, Sprite defaultPortrait)
    {
        foreach (DialogueLine line in reachableLines)
        {
            if (line != null && line.speakerSide == side && !line.hidePortrait && !string.IsNullOrWhiteSpace(line.text))
                return line.portraitOverride != null ? CharacterProfile.AsNow(line.portraitOverride) : defaultPortrait;
        }

        return activeNpc != null ? defaultPortrait : null;
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
        return GetValidChoices(activeLineIndex).Count > 0;
    }

    // Answers without text are left out, and so is one opening the NPC's shop while it does not
    // trade yet, e.g. "Show me what you've got." to Garr before his first errand is done. Where a
    // talk with an NPC ends, the topics the player can talk about with it are offered (AddTopicChoices).
    private List<DialogueChoice> GetValidChoices(int lineIndex)
    {
        List<DialogueChoice> choices = new();
        DialogueLine line = lineIndex >= 0 && lineIndex < activeLines.Count ? activeLines[lineIndex] : null;
        if (line == null)
            return choices;

        if (line.choices != null)
        {
            for (int i = 0; i < line.choices.Count; i++)
            {
                DialogueChoice choice = line.choices[i];
                if (choice != null && !string.IsNullOrWhiteSpace(choice.text) && !OpensClosedShop(choice))
                    choices.Add(choice);
            }
        }

        if (choices.Count == 0 && EndsTalk(lineIndex))
            AddTopicChoices(choices);

        return choices;
    }

    // The line a talk with an NPC ends on: nothing is said after it. Not in story dialogues.
    private bool EndsTalk(int lineIndex)
    {
        if (activeNpc == null)
            return false;

        if (activeLines[lineIndex].endDialogueAfterLine)
            return true;

        int nextLineIndex = SkipUnsaidLines(GetNextLineIndex(lineIndex));
        return nextLineIndex < 0 || nextLineIndex >= activeLines.Count;
    }

    // The topics the player can talk about with the NPC now (NpcData.talkTopics), and an answer that
    // just ends the talk, e.g. "Do you think we'll get out of here?" / "Nothing. I'll get going." to
    // Hideshi waiting in Leymar. No topics: the talk ends as written.
    private void AddTopicChoices(List<DialogueChoice> choices)
    {
        NpcData data = activeNpc.Data;
        if (data == null || data.talkTopics == null || activeWithoutTopics)
            return;

        int count = choices.Count;
        foreach (NpcTalkTopic topic in data.talkTopics)
        {
            if (topic == null || !topic.IsAvailable || topicsTalkedAbout.Contains(topic))
                continue;

            if (!topicChoices.TryGetValue(topic, out DialogueChoice choice))
            {
                choice = new DialogueChoice { text = topic.choiceText, endDialogue = true };
                topicChoices.Add(topic, choice);
            }

            choices.Add(choice);
        }

        if (choices.Count == count)
            return;

        closeTopicChoice ??= new DialogueChoice { endDialogue = true };
        closeTopicChoice.text = data.TalkTopicsCloseText;
        choices.Add(closeTopicChoice);
    }

    // A topic picked (AddTopicChoices): it counts as talked about, and its dialogue is said from here
    // on in the same talk. At its end the topics still left are offered again, unless that dialogue
    // ends the talk (No Talk Topics), e.g. Haru's "Let's go." to Dad.
    private bool TryTalkAbout(DialogueChoice choice)
    {
        foreach (KeyValuePair<NpcTalkTopic, DialogueChoice> pair in topicChoices)
        {
            if (pair.Value != choice)
                continue;

            pair.Key.MarkDone();
            topicsTalkedAbout.Add(pair.Key);
            activeLines.Clear();
            AddDialogueDataLines(pair.Key.dialogue);
            if (pair.Key.dialogue != null && pair.Key.dialogue.noTalkTopics)
                activeWithoutTopics = true;
            int firstLine = SkipUnsaidLines(0);
            if (firstLine < 0 || firstLine >= activeLines.Count)
            {
                EndDialogue();
                return true;
            }

            activeLineIndex = firstLine;
            CollectReachableLines();
            ShowCurrentLine();
            return true;
        }

        return false;
    }

    private bool OpensClosedShop(DialogueChoice choice)
    {
        if (choice.actions == null)
            return false;

        foreach (DialogueAction action in choice.actions)
        {
            if (action != null && action.OpensClosedShop(activeNpc))
                return true;
        }

        return false;
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
