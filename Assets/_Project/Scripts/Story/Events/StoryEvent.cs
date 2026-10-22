using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum StoryEventStart
{
    Automatically,
    WhenPlayerEnters
}

// Anything that happens in the story: a line when the player finds something, a quest update,
// a whole scene with characters walking and talking. It says when it starts, what must be true
// (conditions), what happens (steps) and which flag remembers that it happened.
// - Automatically: as soon as the conditions are met, wherever the player is
//   (the intro of a new game, a scene right after a dialogue sets a flag).
// - When Player Enters: when the player stands in this object's trigger collider
//   (found the root stone, reached the altar).
// It happens once: doneFlag is set at the end, so it does not run again and dialogues or other
// events can check it. Unless everyTime is on: then it happens whenever it can start (a closed exit). While the steps run, the game is in the Dialogue state: the player has no
// control and Time.timeScale is 0, so steps run on the GameClock. An event made only of instant
// steps (actions, sounds...) runs at once instead. The camera stands still meanwhile, unless
// cameraFollows is on.
public sealed class StoryEvent : MonoBehaviour
{
    [Tooltip("Story flag set when the event is over. Also prevents it from happening twice. Empty = happens again every time the scene loads.")]
    [SerializeField] private string doneFlag;
    [Tooltip("Automatically: as soon as the conditions are met. When Player Enters: when the player stands in this object's trigger collider.")]
    [SerializeField] private StoryEventStart start = StoryEventStart.Automatically;
    [Tooltip("All of them must be met. Empty = no conditions.")]
    [SerializeField] private List<StoryCondition> conditions = new();
    [Tooltip("The camera keeps following the player while the event plays (they walk somewhere together). Off = the camera stays where it is.")]
    [SerializeField] private bool cameraFollows;
    [Tooltip("Happens again every time it can start, e.g. turning the player back at an exit that is not open yet (Done Flag is then ignored). With When Player Enters, a step must take the player out of the trigger, or it starts over at once.")]
    [SerializeField] private bool everyTime;

    [Header("Steps")]
    [SerializeReference, SubclassPicker] private List<StoryStep> steps = new();

    private StoryEventContext context;
    private bool hasControl;
    private bool playedThisScene;

    public bool IsPlaying => context != null;

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
        if (start == StoryEventStart.Automatically && CanStart())
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
        GameStateManager.SetState(GameState.Dialogue);
        if (cameraFollows)
            context.KeepCamerasMoving();
        StartCoroutine(PlaySteps());
    }

    private bool CanStart()
    {
        return !IsPlaying && !IsDone && GameStateManager.IsGameplay && StoryCondition.AreMet(conditions);
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
            GameStateManager.SetState(GameState.Gameplay);
        }
    }
}
