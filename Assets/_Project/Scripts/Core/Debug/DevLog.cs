using UnityEngine;

public static class DevLog
{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    public static void Log(object message)
    {
        Debug.Log(message);
    }
#else
    public static void Log(object message) { }
#endif
}