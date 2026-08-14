public static class LoadingScreenRequest
{
    private static string targetSceneName;

    public static void SetTargetScene(string sceneName)
    {
        targetSceneName = sceneName;
    }

    public static string ConsumeTargetScene()
    {
        if (string.IsNullOrWhiteSpace(targetSceneName))
            return SceneNames.Exploration;

        string sceneName = targetSceneName;
        targetSceneName = null;
        return sceneName;
    }
}
