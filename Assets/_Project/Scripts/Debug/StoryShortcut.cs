#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

// Brings the story up to a point as if it had been played there (Debug Test Start). For a story
// flag, the event or the talk that sets it is skipped instead of played, so the quests it starts
// and completes, their rewards and its other flags come with it. What that event or talk waits
// for (an earlier flag, a quest that must be active or done) is reached the same way first; so is
// every flag of the main line before it (StoryFlagOrder.txt, the numbered ones), in order, even
// one nothing after it waits for. So the last flag alone brings everything before it; side flags
// ("-") and states ("*") only when something waits for them. A flag set by picking something up (ForageSpot)
// comes with the thing picked up, card and all. Before a quest is completed it is started and
// the events that happen while it is active (the places it sends the player to) are skipped too.
// A quest nothing in the scene starts or completes (a battle quest) is set directly. Battles and
// shops are left out.
public sealed class StoryShortcut
{
    private const string OrderFile = "Assets/_Project/Scripts/Editor/Inspectors/StoryFlagOrder.txt";

    private readonly List<string> mainLine = ReadMainLine();
    private readonly List<StoryEvent> events = new();
    private readonly List<NpcCharacter> npcs = new();
    private readonly List<ForageSpot> spots = new();
    // Flags, quests, events and talks already dealt with: no going round in circles.
    private readonly HashSet<object> reached = new();

    public StoryShortcut()
    {
        foreach (StoryEvent storyEvent in Object.FindObjectsByType<StoryEvent>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            // A closed exit happens every time and leaves nothing behind.
            if (!storyEvent.HappensEveryTime && !string.IsNullOrWhiteSpace(storyEvent.DoneFlag))
                events.Add(storyEvent);
        }

        npcs.AddRange(Object.FindObjectsByType<NpcCharacter>(FindObjectsInactive.Include, FindObjectsSortMode.None));
        spots.AddRange(Object.FindObjectsByType<ForageSpot>(FindObjectsInactive.Include, FindObjectsSortMode.None));
    }

    public void ReachFlag(string flag)
    {
        if (string.IsNullOrWhiteSpace(flag) || StoryFlagManager.HasFlag(flag) || !reached.Add("flag " + flag))
            return;

        // A step of the main line comes only after all those before it.
        int place = mainLine.IndexOf(flag);
        for (int i = 0; i < place; i++)
            ReachFlag(mainLine[i]);

        StoryEvent setter = events.Find(storyEvent => storyEvent.DoneFlag == flag);
        ForageSpot gathered = spots.Find(spot => spot.SetsFlag(flag));
        if (setter != null)
            SkipEvent(setter);
        else if (gathered != null)
            gathered.Skip();
        else
            SkipWhatDoes(action => action.type == DialogueActionType.SetStoryFlag && action.storyFlag == flag);

        StoryFlagManager.SetFlag(flag);
    }

    // The main line's flags in story order, as StoryFlagOrder.txt has them (as StoryFlagDrawer reads
    // it): the plain lines, not side flags ("-"), states ("*"), chapters ("##") or notes ("#").
    private static List<string> ReadMainLine()
    {
        List<string> flags = new();
        if (!System.IO.File.Exists(OrderFile))
            return flags;

        foreach (string raw in System.IO.File.ReadAllLines(OrderFile))
        {
            string line = raw.Trim();
            int note = line.IndexOf('#');
            if (note >= 0)
                line = line.Substring(0, note).Trim();

            if (line.Length == 0 || line[0] is '-' or '*')
                continue;

            flags.Add(line.Split(' ', '\t')[0]);
        }

        return flags;
    }

    // Active, or also completed: by the event or the talk that does it, else directly.
    public void ReachQuest(QuestData quest, bool completed)
    {
        if (quest == null)
            return;

        QuestStatus status = QuestManager.GetStatus(quest);
        if (status is QuestStatus.Completed or QuestStatus.Failed || status == QuestStatus.Active && !completed)
            return;

        DialogueActionType type = completed ? DialogueActionType.CompleteQuest : DialogueActionType.StartQuest;
        if (!reached.Add((quest, type)))
            return;

        if (completed)
            BeforeCompleting(quest);

        SkipWhatDoes(action => action.type == type && action.quest == quest);

        if (completed)
            QuestManager.CompleteQuest(quest, null, false);
        else
            QuestManager.StartQuest(quest, false);
    }

    // The event, or else the talk with an NPC, with an action like that.
    private void SkipWhatDoes(Predicate<DialogueAction> does)
    {
        StoryEvent storyEvent = events.Find(candidate => Contains(candidate.GetActions(), does));
        if (storyEvent != null)
        {
            SkipEvent(storyEvent);
            return;
        }

        foreach (NpcCharacter npc in npcs)
        {
            if (npc.Data == null || npc.Data.dialogueEntries == null)
                continue;

            foreach (NpcDialogueEntry entry in npc.Data.dialogueEntries)
            {
                if (entry?.dialogue == null || !Contains(entry.dialogue.GetActions(), does))
                    continue;

                SkipTalk(npc, entry, does);
                return;
            }
        }

        // Else a talk topic picked with an NPC, e.g. Hideshi's "Let's go." that ends Survival Basics,
        // starts Heart of the Barrier and has him follow Haru again.
        foreach (NpcCharacter npc in npcs)
        {
            if (npc.Data == null || npc.Data.talkTopics == null)
                continue;

            foreach (NpcTalkTopic topic in npc.Data.talkTopics)
            {
                if (topic?.dialogue == null || !Contains(topic.dialogue.GetActions(), does))
                    continue;

                SkipTopic(topic, does);
                return;
            }
        }
    }

    private void SkipTopic(NpcTalkTopic topic, Predicate<DialogueAction> does)
    {
        if (!reached.Add(topic))
            return;

        Meet(topic.conditions);
        BeforeCompleting(topic.dialogue.GetActions());
        List<DialogueAction> together = FindActionsWith(topic.dialogue, does, out DialogueChoice answer);
        topic.dialogue.Skip(0, answer);
        foreach (DialogueAction action in together)
            action?.ExecuteSkipped();

        topic.MarkDone();
    }

    private void SkipEvent(StoryEvent storyEvent)
    {
        if (StoryFlagManager.HasFlag(storyEvent.DoneFlag) || !reached.Add(storyEvent))
            return;

        Meet(storyEvent.Conditions);
        BeforeCompleting(storyEvent.GetActions());
        storyEvent.Skip();
    }

    // Through the dialogue the way that leads to the action, which is done also if that way
    // passed it by; the NPC then remembers the talk, as after a real one.
    private void SkipTalk(NpcCharacter npc, NpcDialogueEntry entry, Predicate<DialogueAction> does)
    {
        if (!reached.Add(entry))
            return;

        Meet(entry.conditions);
        BeforeCompleting(entry.dialogue.GetActions());
        List<DialogueAction> together = FindActionsWith(entry.dialogue, does, out DialogueChoice answer);
        entry.dialogue.Skip(0, answer);
        foreach (DialogueAction action in together)
            action?.ExecuteSkipped();

        NpcDialogueProgress.RecordDialogueFinished(npc.PersistenceKey, entry.Key);
    }

    private void BeforeCompleting(IEnumerable<DialogueAction> actions)
    {
        List<QuestData> completes = new();
        foreach (DialogueAction action in actions)
        {
            if (action != null && action.type == DialogueActionType.CompleteQuest && action.quest != null)
                completes.Add(action.quest);
        }

        foreach (QuestData quest in completes)
            BeforeCompleting(quest);
    }

    // A quest is active before it is completed, and the events that need it active (getting to
    // where it leads) happen meanwhile, e.g. arriving in Leymar on the road the trader showed.
    private void BeforeCompleting(QuestData quest)
    {
        if (QuestManager.GetStatus(quest) is QuestStatus.Completed or QuestStatus.Failed)
            return;

        ReachQuest(quest, false);
        foreach (StoryEvent storyEvent in events.ToArray())
        {
            if (NeedsActive(storyEvent.Conditions, quest))
                SkipEvent(storyEvent);
        }
    }

    private static bool NeedsActive(IReadOnlyList<StoryCondition> conditions, QuestData quest)
    {
        if (conditions == null)
            return false;

        foreach (StoryCondition condition in conditions)
        {
            if (condition != null && !condition.invert && condition.quest == quest
                && condition.type == StoryConditionType.QuestStatus && condition.questStatus == QuestStatus.Active)
                return true;
        }

        return false;
    }

    // What an event or a talk waits for: flags set, quests active or done. "Not" conditions are
    // left as they are.
    private void Meet(IReadOnlyList<StoryCondition> conditions)
    {
        if (conditions == null)
            return;

        foreach (StoryCondition condition in conditions)
        {
            if (condition == null || condition.invert)
                continue;

            switch (condition.type)
            {
                case StoryConditionType.StoryFlag:
                    ReachFlag(condition.storyFlag);
                    break;
                case StoryConditionType.QuestStatus when condition.questStatus == QuestStatus.Completed:
                    ReachQuest(condition.quest, true);
                    break;
                case StoryConditionType.QuestStatus when condition.questStatus == QuestStatus.Active:
                case StoryConditionType.QuestObjectiveDone:
                    ReachQuest(condition.quest, false);
                    break;
            }
        }
    }

    // The actions of the line or the answer that has the one wanted, and the answer if it is one.
    private static List<DialogueAction> FindActionsWith(DialogueData dialogue, Predicate<DialogueAction> does, out DialogueChoice answer)
    {
        answer = null;
        foreach (DialogueLine line in dialogue.lines)
        {
            if (line == null)
                continue;

            if (line.actions != null && line.actions.Exists(does))
                return line.actions;

            foreach (DialogueChoice choice in line.choices ?? new List<DialogueChoice>())
            {
                if (choice?.actions == null || !choice.actions.Exists(does))
                    continue;

                answer = choice;
                return choice.actions;
            }
        }

        return new List<DialogueAction>();
    }

    private static bool Contains(IEnumerable<DialogueAction> actions, Predicate<DialogueAction> does)
    {
        foreach (DialogueAction action in actions)
        {
            if (action != null && does(action))
                return true;
        }

        return false;
    }
}
#endif
