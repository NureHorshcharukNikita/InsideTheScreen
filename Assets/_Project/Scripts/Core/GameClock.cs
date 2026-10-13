using UnityEngine;

// Real time for what runs while the game is paused (Time.timeScale 0): story events, dialogue,
// doors, menus, toasts. Unlike Time.unscaledTime it leaves out the time the window spent minimized
// or unfocused: the game stops then, and when it comes back no frame counts for more than
// MaxFrameTime, so a cutscene goes on from where it was instead of skipping to its end.
public static class GameClock
{
    private const float MaxFrameTime = 0.1f;

    private static int lastFrame = -1;
    private static float lastUnscaledTime;
    private static float now;
    private static float deltaTime;

    public static float Now
    {
        get
        {
            Advance();
            return now;
        }
    }

    public static float DeltaTime
    {
        get
        {
            Advance();
            return deltaTime;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetForPlayMode()
    {
        lastFrame = -1;
        now = 0f;
        deltaTime = 0f;
    }

    // Once per frame, on first use. Frames nobody asked about still count, up to MaxFrameTime each:
    // while the window is away there are no frames at all.
    private static void Advance()
    {
        int frame = Time.frameCount;
        if (frame == lastFrame)
            return;

        float unscaledTime = Time.unscaledTime;
        if (lastFrame >= 0)
        {
            int frames = frame - lastFrame;
            now += Mathf.Min(unscaledTime - lastUnscaledTime, frames * MaxFrameTime);
        }

        deltaTime = Mathf.Min(Time.unscaledDeltaTime, MaxFrameTime);
        lastFrame = frame;
        lastUnscaledTime = unscaledTime;
    }
}

// WaitForSecondsRealtime on the GameClock: waits out time the game actually ran.
public sealed class WaitForGameSeconds : CustomYieldInstruction
{
    private readonly float until;

    public WaitForGameSeconds(float seconds)
    {
        until = GameClock.Now + seconds;
    }

    public override bool keepWaiting => GameClock.Now < until;
}
