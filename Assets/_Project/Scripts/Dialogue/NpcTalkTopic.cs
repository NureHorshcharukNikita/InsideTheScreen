using System;
using System.Collections.Generic;
using UnityEngine;

// Something the player can talk about with an NPC (NpcData.talkTopics): ask Hideshi what he makes of
// this world, or tell him about the broken bridge Haru found alone. Offered as an answer where a talk
// with the NPC ends, while its conditions are met, until it has been talked about, or in every talk
// (Every Talk), e.g. asking him what to do now; picking it says its dialogue in the same talk.
[Serializable]
public sealed class NpcTalkTopic
{
    [Tooltip("The answer the player picks, e.g. \"Do you think we'll get out of here?\"")]
    public string choiceText;

    [Tooltip("All of them must be met, e.g. Hideshi waiting (hideshi_stays_behind), or the flag set when Haru saw the bridge alone. Empty = no conditions.")]
    public List<StoryCondition> conditions = new();

    [Tooltip("Said once it is picked.")]
    public DialogueData dialogue;

    [Tooltip("Set once it has been talked about, so it is offered only once. Empty = never offered (unless Every Talk).")]
    [StoryFlag] public string doneFlag;

    [Tooltip("Offered in every talk, once per talk, e.g. \"What should we do now?\" Done Flag is not needed.")]
    public bool everyTalk;

    public bool IsAvailable => !string.IsNullOrWhiteSpace(choiceText)
        && dialogue != null && dialogue.HasLines
        && (everyTalk || (!string.IsNullOrWhiteSpace(doneFlag) && !StoryFlagManager.HasFlag(doneFlag)))
        && StoryCondition.AreMet(conditions);

    public void MarkDone()
    {
        if (!everyTalk)
            StoryFlagManager.SetFlag(doneFlag);
    }
}
