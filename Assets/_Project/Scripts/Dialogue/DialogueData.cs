using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Dialogue/Dialogue Data")]
public sealed class DialogueData : ScriptableObject
{
    public List<DialogueLine> lines = new();

    public bool HasLines
    {
        get
        {
            if (lines == null)
                return false;

            for (int i = 0; i < lines.Count; i++)
            {
                if (lines[i] != null && !string.IsNullOrWhiteSpace(lines[i].text))
                    return true;
            }

            return false;
        }
    }

    // Every quest and flag action of the lines and answers, on any branch.
    public IEnumerable<DialogueAction> GetActions()
    {
        if (lines == null)
            yield break;

        foreach (DialogueLine line in lines)
        {
            if (line == null)
                continue;

            foreach (DialogueAction action in line.actions ?? new List<DialogueAction>())
                yield return action;

            foreach (DialogueChoice choice in line.choices ?? new List<DialogueChoice>())
            {
                foreach (DialogueAction action in choice?.actions ?? new List<DialogueAction>())
                    yield return action;
            }
        }
    }

    // Goes through the dialogue from startLine without showing it, as if read to the end of this
    // part taking the first answer (or the preferred one where it is offered), and does what its
    // lines and answers do, quietly. Quest actions come last, as when the dialogue closes. Returns
    // the line the next part starts from, like the dialogue does. For a talk or a story event
    // skipped instead of played (Debug Test Start).
    public int Skip(int startLine = 0, DialogueChoice preferred = null)
    {
        List<DialogueAction> questActions = new();
        HashSet<int> visited = new();
        int index = startLine;
        while (lines != null)
        {
            index = SkipUnsaidLines(index);
            // Back at a line it has been at: a menu of questions, done with.
            if (index < 0 || index >= lines.Count || !visited.Add(index))
                break;

            DialogueLine line = lines[index];
            DoSkipped(line.actions, questActions);

            List<DialogueChoice> choices = line.choices != null
                ? line.choices.FindAll(choice => choice != null && !string.IsNullOrWhiteSpace(choice.text))
                : new List<DialogueChoice>();
            if (choices.Count > 0)
            {
                DialogueChoice choice = choices.Contains(preferred) ? preferred : choices[0];
                DoSkipped(choice.actions, questActions);
                index = choice.nextLineIndex < 0 ? GetNextLineIndex(index) : choice.nextLineIndex;
                if (choice.endDialogue)
                    break;

                continue;
            }

            bool ends = line.endDialogueAfterLine;
            index = GetNextLineIndex(index);
            if (ends)
                break;
        }

        foreach (DialogueAction action in questActions)
            action.ExecuteSkipped();

        return index;
    }

    private static void DoSkipped(List<DialogueAction> actions, List<DialogueAction> questActions)
    {
        if (actions == null)
            return;

        foreach (DialogueAction action in actions)
        {
            if (action == null)
                continue;

            if (action.type is DialogueActionType.StartQuest or DialogueActionType.CompleteQuest or DialogueActionType.FailQuest)
                questActions.Add(action);
            else
                action.ExecuteSkipped();
        }
    }

    // As DialogueManager: a line whose conditions are not met is passed over.
    private int SkipUnsaidLines(int index)
    {
        for (int skipped = 0; skipped <= lines.Count; skipped++)
        {
            if (index < 0 || index >= lines.Count
                || lines[index] != null && StoryCondition.AreMet(lines[index].conditions))
                return index;

            index = GetNextLineIndex(index);
        }

        return -1;
    }

    private int GetNextLineIndex(int index)
    {
        DialogueLine line = lines[index];
        return line != null && line.nextLineIndex >= 0 ? line.nextLineIndex : index + 1;
    }
}
