using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// The end-of-demo screen: black, with the title, a message and the way back to the main menu.
// The demo's last story event ends with an End Of Demo step that fades it in; a game loaded after
// the demo is over (demoCompleteFlag set) opens straight on it.
public sealed class ExplorationEndGameController : MonoBehaviour
{
    // Above the HUD, below quest toasts (950) and scene fades (1000).
    private const int ScreenSortingOrder = 900;

    [Tooltip("Set when the end screen appears. A game loaded with it set opens on the end screen.")]
    [SerializeField, StoryFlag] private string demoCompleteFlag = "demo_complete";
    [SerializeField] private GameObject endGamePanel;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private string title = "End of Demo";
    [Tooltip("<br> starts a new line.")]
    [TextArea(2, 4)]
    [SerializeField] private string message = "Thank you for playing!<br>Haru's story will continue...";
    [SerializeField] private bool clearSaveOnReturnToMenu = true;

    private bool endGameShown;
    private CanvasGroup screenGroup;
    private CanvasGroup panelGroup;

    private IEnumerator Start()
    {
        SetEndGamePanelVisible(false);

        yield return null;

        if (!endGameShown && StoryFlagManager.HasFlag(demoCompleteFlag))
        {
            Show(true);
            StopAmbience(0f);
        }
    }

    // Fades the screen to black, the nature sounds out with it, waits there a moment, then fades
    // the text in. Unscaled time: the game is paused under it.
    public IEnumerator PlayEndOfDemo(float fadeToBlackDuration, float blackHoldDuration, float textFadeDuration)
    {
        if (endGameShown)
            yield break;

        Show(false);
        StopAmbience(fadeToBlackDuration);
        yield return Fade(screenGroup, fadeToBlackDuration);
        yield return new WaitForSecondsRealtime(blackHoldDuration);
        yield return Fade(panelGroup, textFadeDuration);

        if (panelGroup != null)
            panelGroup.interactable = true;
    }

    public void ReturnToMainMenu()
    {
        if (clearSaveOnReturnToMenu)
        {
            ExplorationPlayerSession.Clear();
            PendingBattleEnemy.ClearSession();
            DefeatedEncounters.Clear();
            EnemyEncounter.ResetEncounterIds();
        }

        MainMenuNavigation.GoToMainMenu();
    }

    // Not instantly: shown transparent, for the caller to fade in. The button stays off until then,
    // so a click left over from the last dialogue line cannot skip the screen.
    private void Show(bool instantly)
    {
        endGameShown = true;
        StoryFlagManager.SetFlag(demoCompleteFlag);
        GameStateManager.SetState(GameState.Dialogue);

        if (titleText != null)
            titleText.text = title;
        if (messageText != null)
            messageText.text = message;

        BuildScreen();
        float alpha = instantly ? 1f : 0f;
        screenGroup.alpha = alpha;
        if (panelGroup != null)
        {
            panelGroup.alpha = alpha;
            panelGroup.interactable = instantly;
        }

        SetEndGamePanelVisible(true);
    }

    // A full-screen black backdrop on its own canvas, with the panel moved onto it, so nothing
    // of the HUD shows through.
    private void BuildScreen()
    {
        if (screenGroup != null)
            return;

        Transform parent = endGamePanel != null ? endGamePanel.transform.parent : transform;
        GameObject screen = new("EndOfDemoScreen", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster), typeof(CanvasGroup));
        screen.transform.SetParent(parent, false);
        Stretch((RectTransform)screen.transform);

        Canvas canvas = screen.GetComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingOrder = ScreenSortingOrder;
        screenGroup = screen.GetComponent<CanvasGroup>();

        GameObject backdrop = new("Backdrop", typeof(RectTransform), typeof(Image));
        backdrop.transform.SetParent(screen.transform, false);
        Stretch((RectTransform)backdrop.transform);
        backdrop.GetComponent<Image>().color = Color.black;

        if (endGamePanel == null)
            return;

        endGamePanel.transform.SetParent(screen.transform, false);
        panelGroup = endGamePanel.GetComponent<CanvasGroup>();
        if (panelGroup == null)
            panelGroup = endGamePanel.AddComponent<CanvasGroup>();
    }

    private static void StopAmbience(float fadeDuration)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.StopAmbience(fadeDuration);
    }

    private static IEnumerator Fade(CanvasGroup group, float duration)
    {
        if (group == null)
            yield break;

        for (float elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
        {
            group.alpha = elapsed / duration;
            yield return null;
        }

        group.alpha = 1f;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private void SetEndGamePanelVisible(bool visible)
    {
        if (endGamePanel != null)
            endGamePanel.SetActive(visible);
    }
}
