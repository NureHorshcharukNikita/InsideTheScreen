using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum StoryEventStart
{
    Automatically,
    WhenPlayerEnters,
    // Only when a step of another event starts it, e.g. Garr walking to his forge (Walk Off) when the
    // player gets in his way.
    WhenCalled
}

// Anything that happens in the story: a line when the player finds something, a quest update,
// a whole scene with characters walking and talking. It says when it starts, what must be true
// (conditions), what happens (steps) and which flag remembers that it happened.
// - Automatically: as soon as the conditions are met, wherever the player is
//   (the intro of a new game, a scene right after a dialogue sets a flag).
// - When Player Enters: when the player stands in this object's trigger collider
//   (found the root stone, reached the altar).
// - When Called: only when something else starts it, e.g. a Walk Off step whose walker the player
//   gets in the way of (Garr on his way to the forge).
// It happens once: doneFlag is set at the end, so it does not run again and dialogues or other
// events can check it. Unless everyTime is on: then it happens whenever it can start (a closed exit). While the steps run, the game is in the Dialogue state: the player has no
// control and Time.timeScale is 0, so steps run on the GameClock. An event made only of instant
// steps (actions, sounds...) runs at once instead. The camera stands still meanwhile, unless
// cameraFollows is on.
public sealed class StoryEvent : MonoBehaviour
{
    [Tooltip("Story flag set when the event is over. Also prevents it from happening twice. Empty = happens again every time the scene loads.")]
    [SerializeField] private string doneFlag;
    [Tooltip("Automatically: as soon as the conditions are met. When Player Enters: when the player stands in this object's trigger collider. When Called: only when something else starts it, e.g. a Walk Off step whose walker the player gets in the way of.")]
    [SerializeField] private StoryEventStart start = StoryEventStart.Automatically;
    [Tooltip("All of them must be met. Empty = no conditions.")]
    [SerializeField] private List<StoryCondition> conditions = new();
    [Tooltip("The camera keeps following the player while the event plays (they walk somewhere together). Off = the camera stays where it is.")]
    [SerializeField] private bool cameraFollows;
    [Tooltip("Happens again every time it can start, e.g. turning the player back at an exit that is not open yet (Done Flag is then ignored). With When Player Enters, a step must take the player out of the trigger, or it starts over at once.")]
    [SerializeField] private bool everyTime;
    [Tooltip("Automatically: once the conditions are met, it waits this long while the player walks about freely (talks and menus do not count), e.g. Haru giving up on the last handful after looking around a while. 0 = at once.")]
    [SerializeField, Min(0f)] private float afterSeconds;
    [Tooltip("After Seconds goes on counting while a dialogue with someone is open too (not in a cutscene, a menu or the pause), and an event done at once (only instant steps, e.g. a Walk Off) then starts right away instead of waiting for the end of it, e.g. Garr coming out of his barn while Haru talks to his father.")]
    [SerializeField] private bool countsDuringTalks;

    [Header("Steps")]
    [SerializeReference, SubclassPicker] private List<StoryStep> steps = new();

    // Events playing that took the game (cutscenes), anywhere.
    private static int playingCutscenes;

    private StoryEventContext context;
    private bool hasControl;
    private bool playedThisScene;
    // How long the player has walked about with the conditions met (After Seconds).
    private float waited;

    public bool IsPlaying => context != null;

    // A story event has the game right now (a cutscene), not just a dialogue with someone.
    public static bool CutscenePlaying => playingCutscenes > 0;

    // Play mode may start again without the statics being reset.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        playingCutscenes = 0;
    }

    public string DoneFlag => doneFlag;

    public bool HappensEveryTime => everyTime;

    public IReadOnlyList<StoryCondition> Conditions => conditions;

    public bool IsDone => !everyTime && (string.IsNullOrWhiteSpace(doneFlag)
        ? playedThisScene
        : StoryFlagManager.HasFlag(doneFlag));

    private void Awake()
    {
        if (start == StoryEventStart.WhenPlayerEnters && (!TryGetComponent(out Collider2D zone) || !zone.isTrigger))
            Debug.LogWarning($"Story event {name} starts when the player enters, but has no trigger collider.", this);
    }

    private void Update()
    {
        if (start != StoryEventStart.Automatically || IsPlaying || IsDone)
            return;

        if (!StoryCondition.AreMet(conditions))
        {
            waited = 0f;
            return;
        }

        // Time stands still in a talk: counted on the game clock then. Not in a cutscene.
        bool talking = countsDuringTalks && GameStateManager.IsDialogue && !CutscenePlaying;
        if (!GameStateManager.IsGameplay && !talking)
            return;

        if (waited < afterSeconds)
        {
            waited += talking ? GameClock.DeltaTime : Time.deltaTime;
            return;
        }

        // In a talk only an event done at once, which takes nothing and no one over.
        if (talking && !steps.TrueForAll(step => step == null || step.IsInstant))
            return;

        Play();
    }

    // Stay instead of Enter: the event also starts when its conditions become true while the
    // player is already standing inside.
    private void OnTriggerStay2D(Collider2D other)
    {
        if (start == StoryEventStart.WhenPlayerEnters && other.CompareTag("Player") && CanStart())
            Play();
    }

    // Stopped halfway (scene unloaded, object turned off): the game must not stay paused.
    private void OnDisable()
    {
        if (IsPlaying)
            Finish(false);
    }

    // Also in the component's context menu, to test the event in Play mode. Runs even when it is
    // already done or its conditions are not met.
    [ContextMenu("Play")]
    public void Play()
    {
        if (!Application.isPlaying || IsPlaying || !isActiveAndEnabled)
            return;

        context = new StoryEventContext(this);
        playedThisScene = true;

        if (steps.TrueForAll(step => step == null || step.IsInstant))
        {
            foreach (StoryStep step in steps)
                context.RunNow(step);

            Finish(true);
            return;
        }

        hasControl = true;
        playingCutscenes++;
        GameStateManager.SetState(GameState.Dialogue);
        if (cameraFollows)
            context.KeepCamerasMoving();
        StartCoroutine(PlaySteps());
    }

    // As if it had played, without playing it: what its steps leave behind (quests, flags, objects
    // shown or hidden) and its done flag. Its conditions are not checked. For Debug Test Start.
    public void Skip()
    {
        StoryEventContext skipping = new(this);
        foreach (StoryStep step in steps)
            step?.Skip(skipping);

        playedThisScene = true;
        if (!string.IsNullOrWhiteSpace(doneFlag))
            StoryFlagManager.SetFlag(doneFlag);
    }

    // The quest and flag actions its steps can run, on any branch of its dialogues.
    public IEnumerable<DialogueAction> GetActions()
    {
        foreach (StoryStep step in steps)
        {
            if (step == null)
                continue;

            foreach (DialogueAction action in step.GetActions())
                yield return action;
        }
    }

    // Plays it if it can start now: not done yet, its conditions met, nothing else going on. E.g.
    // When Called by a Walk Off step. False if it did not start.
    public bool TryPlay()
    {
        if (!CanStart())
            return false;

        Play();
        return true;
    }

    // Not done, not playing and its conditions met: it would start now if nothing else had the game,
    // e.g. Garr's telling-off waiting for the end of a talk he walked into.
    public bool IsReady => !IsPlaying && !IsDone && StoryCondition.AreMet(conditions);

    private bool CanStart()
    {
        return IsReady && GameStateManager.IsGameplay;
    }

    private IEnumerator PlaySteps()
    {
        for (int i = 0; i < steps.Count; i++)
            yield return context.Run(steps[i]);

        Finish(true);
    }

    private void Finish(bool completed)
    {
        if (completed && !string.IsNullOrWhiteSpace(doneFlag))
            StoryFlagManager.SetFlag(doneFlag);

        context.Release();
        context = null;

        if (hasControl)
        {
            hasControl = false;
            playingCutscenes = Mathf.Max(0, playingCutscenes - 1);
            GameStateManager.SetState(GameState.Gameplay);
        }
    }
}
