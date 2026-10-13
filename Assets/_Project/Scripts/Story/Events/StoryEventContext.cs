using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

// What the steps of one playing story event share: the player, the characters under event
// control and the effects that last over several steps. Release() puts everything back.
public sealed class StoryEventContext
{
    // The characters story events are moving right now, all events together.
    private static readonly HashSet<Transform> ControlledActors = new();

    private readonly MonoBehaviour runner;
    private readonly Dictionary<Transform, ActorState> actors = new();
    private readonly List<Component> effects = new();
    private readonly List<Action> cleanups = new();
    private Transform player;

    public StoryEventContext(MonoBehaviour runner)
    {
        this.runner = runner;
    }

    public Transform Player
    {
        get
        {
            if (player == null)
            {
                PlayerMovement movement = Object.FindFirstObjectByType<PlayerMovement>();
                player = movement != null ? movement.transform : null;
            }

            return player;
        }
    }

    public Coroutine StartCoroutine(IEnumerator routine)
    {
        return runner.StartCoroutine(routine);
    }

    // Runs one step. A step that throws is logged and skipped, so a broken step never leaves
    // the game paused in the middle of an event.
    public IEnumerator Run(StoryStep step)
    {
        if (step == null)
            yield break;

        IEnumerator routine = step.Run(this);
        while (true)
        {
            object current;
            try
            {
                if (!routine.MoveNext())
                    yield break;

                current = routine.Current;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, runner);
                yield break;
            }

            yield return current;
        }
    }

    // Runs an instant step to its end right away, outside of a coroutine.
    public void RunNow(StoryStep step)
    {
        IEnumerator routine = Run(step);
        while (routine.MoveNext())
        {
        }
    }

    // Call before moving or animating a character. Its animator switches to unscaled time (the
    // game is paused during story events) and its physics turn off, so walls and furniture do not
    // push it off its path. Release() puts both back.
    public void TakeControl(Transform actor)
    {
        if (actor != null && !actors.ContainsKey(actor))
        {
            actors.Add(actor, new ActorState(actor));
            ControlledActors.Add(actor);
        }
    }

    // E.g. a door opens for them even where the player has to press Interact (Door).
    public static bool IsControlledByStory(Transform actor)
    {
        return actor != null && ControlledActors.Contains(actor);
    }

    // Normal size and colour again after an effect such as Pull Into Screen.
    public void RestoreLook(Transform actor)
    {
        if (actor != null && actors.TryGetValue(actor, out ActorState state))
            state.RestoreLook();
    }

    // The character stays where the event left it, also when the scene is loaded again.
    public void SavePosition(Transform actor)
    {
        if (actor == null)
            return;

        if (actor == Player)
            ExplorationPlayerSession.SavePosition(actor.position);
        else if (actor.TryGetComponent(out NpcCharacter npc))
            ExplorationPlayerSession.SaveNpcPosition(npc.PersistenceKey, actor.position);
    }

    // An effect several steps work with, e.g. the TV static. Destroyed when the event ends.
    public void AddEffect(Component effect)
    {
        if (effect != null)
            effects.Add(effect);
    }

    public T GetEffect<T>() where T : Component
    {
        foreach (Component effect in effects)
        {
            if (effect is T match && match != null)
                return match;
        }

        return null;
    }

    // Runs when the event ends, e.g. to clear a fade the steps left on screen.
    public void OnRelease(Action cleanup)
    {
        if (cleanup != null)
            cleanups.Add(cleanup);
    }

    public void Release()
    {
        foreach (KeyValuePair<Transform, ActorState> actor in actors)
        {
            actor.Value.Release();
            ControlledActors.Remove(actor.Key);
        }
        actors.Clear();
        Physics2D.SyncTransforms();

        foreach (Component effect in effects)
        {
            if (effect != null)
                Object.Destroy(effect.gameObject);
        }
        effects.Clear();

        foreach (Action cleanup in cleanups)
            cleanup();
        cleanups.Clear();
    }

    public static Animator GetAnimator(Transform actor)
    {
        return actor != null ? actor.GetComponentInChildren<Animator>() : null;
    }

    public static SpriteRenderer GetSprite(Transform actor)
    {
        return actor != null ? actor.GetComponentInChildren<SpriteRenderer>() : null;
    }

    private sealed class ActorState
    {
        private readonly Transform actor;
        private readonly Vector3 scale;
        private readonly SpriteRenderer sprite;
        private readonly Color color;
        private readonly Animator animator;
        private readonly AnimatorUpdateMode updateMode;
        private readonly Rigidbody2D body;
        private readonly bool simulated;

        public ActorState(Transform actor)
        {
            this.actor = actor;
            scale = actor.localScale;

            sprite = GetSprite(actor);
            color = sprite != null ? sprite.color : Color.white;

            animator = GetAnimator(actor);
            if (animator != null)
            {
                updateMode = animator.updateMode;
                animator.updateMode = AnimatorUpdateMode.UnscaledTime;
            }

            body = actor.GetComponent<Rigidbody2D>();
            if (body != null)
            {
                simulated = body.simulated;
                body.simulated = false;
            }
        }

        public void RestoreLook()
        {
            if (actor != null)
                actor.localScale = scale;

            if (sprite != null)
                sprite.color = color;
        }

        public void Release()
        {
            RestoreLook();

            if (animator != null)
                animator.updateMode = updateMode;

            if (body != null)
                body.simulated = simulated;
        }
    }
}
