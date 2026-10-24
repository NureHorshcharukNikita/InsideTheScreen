using System;
using System.Collections;
using System.Collections.Generic;

// One step of a StoryEvent: walk, turn, talk, wait... Run() is a coroutine on the GameClock
// (the game is paused while a story event plays). A new kind of step is a [Serializable] subclass
// with a [SubclassMenu] path: it then shows up in the step dropdown of every StoryEvent.
// ToString() is the step's title in the inspector list.
[Serializable]
public abstract class StoryStep
{
    // True for steps that are done at once (actions, sounds...). An event made only of such
    // steps runs right away, without pausing the game or locking the HUD for a frame.
    public virtual bool IsInstant => false;

    public abstract IEnumerator Run(StoryEventContext context);

    // What stays after the step when its event is skipped instead of played (Debug Test Start):
    // the quests and flags of a dialogue or of actions, an object shown or hidden. Walking,
    // turning and waiting leave nothing.
    public virtual void Skip(StoryEventContext context)
    {
    }

    // The quest and flag actions the step can run, e.g. to find the event that starts a quest.
    public virtual IEnumerable<DialogueAction> GetActions()
    {
        yield break;
    }
}
