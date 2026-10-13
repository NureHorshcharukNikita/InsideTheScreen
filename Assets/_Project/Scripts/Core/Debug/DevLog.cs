using UnityEngine;

// Development messages for the console. Off for now: set Enabled to true to see them again.
// Warnings and errors about a broken setup go straight to Debug and stay on.
public static class DevLog
{
    private static readonly bool Enabled = false;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    public static void Log(object message)
    {
        if (Enabled)
            Debug.Log(message);
    }
#else
    public static void Log(object message) { }
#endif
}