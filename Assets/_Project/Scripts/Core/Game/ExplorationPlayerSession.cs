using UnityEngine;

public static class ExplorationPlayerSession
{
    private static Vector3 savedPosition;
    private static bool hasSavedPosition;
    private static int savedHealth;
    private static bool hasSavedHealth;

    public static void SavePosition(Vector3 position)
    {
        savedPosition = position;
        hasSavedPosition = true;
    }

    public static bool TryGetSavedPosition(out Vector3 position)
    {
        position = savedPosition;
        return hasSavedPosition;
    }

    public static void SaveHealth(int health)
    {
        savedHealth = health;
        hasSavedHealth = true;
    }

    public static bool TryGetSavedHealth(out int health)
    {
        health = savedHealth;
        return hasSavedHealth;
    }

    public static void Clear()
    {
        savedPosition = default;
        hasSavedPosition = false;
        savedHealth = default;
        hasSavedHealth = false;
    }
}
