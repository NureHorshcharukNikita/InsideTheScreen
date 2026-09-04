using UnityEngine;

internal static class GameRootBootstrapper
{
    private const string GameRootResourcePath = "GameRoot";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void EnsureGameRoot()
    {
        if (FadeManager.Instance != null)
            return;

        GameObject prefab = Resources.Load<GameObject>(GameRootResourcePath);
        if (prefab == null)
            return;

        Object.Instantiate(prefab);
    }
}
