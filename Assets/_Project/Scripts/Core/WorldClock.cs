using UnityEngine;
using UnityEngine.SceneManagement;

// How long the player has walked the world in this game, in seconds: only while exploring, not in
// battles, menus, dialogues or with the window away. Saved with the game. Enemies come back after a
// while by it (EnemyEncounter), and so do things lying on the ground (ForageSpawner).
public static class WorldClock
{
    public static float PlaySeconds { get; private set; }

    public static void Restore(float seconds)
    {
        PlaySeconds = Mathf.Max(0f, seconds);
    }

    public static void Clear()
    {
        PlaySeconds = 0f;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateTicker()
    {
        GameObject ticker = new("WorldClock") { hideFlags = HideFlags.HideAndDontSave };
        Object.DontDestroyOnLoad(ticker);
        ticker.AddComponent<Ticker>();
    }

    private sealed class Ticker : MonoBehaviour
    {
        private void Update()
        {
            if (GameStateManager.IsGameplay && Application.isFocused
                && SceneManager.GetActiveScene().name == SceneNames.Exploration)
                PlaySeconds += Time.deltaTime;
        }
    }
}
