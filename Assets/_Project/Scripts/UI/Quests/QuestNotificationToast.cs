using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Hints, and "Reward received" for a reward the quest tracker does not show, in the bottom right
// corner, one after another. They wait until the player has control again, so they do not slide in
// under the dialogue box, unread. A quest received or done is not a toast: the quest tracker under
// the minimap shows it coming in and going, with its reward.
// Once the first quest of a game has come into the tracker, it asks for one more: "Press J to view
// your quests", with the key the quest log is bound to in the settings now. Once per game
// (hint_quest_log_key, saved with the story); not while the quest log has no key.
public sealed class QuestNotificationToast : MonoBehaviour
{
    private const string QuestLogHintFlag = "hint_quest_log_key";
    private const string RewardReceivedMessage = "Reward received";

    // Rewards given and not shown yet, by quest (kept over battles: shown back in the world).
    private static readonly List<QuestData> UntakenRewards = new();
    private static readonly Color PanelColor = new(0.31f, 0.23f, 0.2f, 0.98f);
    private static readonly Color BorderColor = new(0.94f, 0.68f, 0.34f, 1f);
    private static readonly Color TextColor = new(1f, 0.91f, 0.68f, 1f);
    private static readonly Vector2 VisiblePosition = new(-34f, 34f);
    private static readonly Vector2 HiddenPosition = new(390f, 34f);
    // As wide as the text needs, the quest's name included, within these.
    private const float MinWidth = 360f;
    private const float MaxWidth = 760f;
    // As long as the quest tracker takes to come in.
    private const float SlideDuration = 0.7f;
    private const float HoldDuration = 3f;
    // The quest log hint: a little longer, to read the key.
    private const float HintHoldDuration = 4.5f;
    private static readonly Color KeyColor = new(1f, 1f, 1f, 1f);
    private const float BetweenToastDelay = 0.28f;
    private const float ExplorationSceneToastDelay = 0.5f;
    // How long the player must have had control on end before a toast comes: the dialogue box is gone
    // by then, and a talk following right after another has begun.
    private const float AfterDialogueDelay = 0.5f;

    private static QuestNotificationToast instance;

    private RectTransform toastRect;
    private TMP_Text toastText;
    private Image toastIcon;
    private Coroutine toastCoroutine;
    private Coroutine delayedStartCoroutine;
    // In order; one cut short by a talk goes back to the front.
    private readonly List<ToastPayload> queuedToasts = new();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureAfterSceneLoad()
    {
        EnsureInstance();
    }

    // Once per game, when the player has seen a quest come into the tracker: the key that opens the
    // quest log, as it is bound now. Not while it has no key (asked again later, once it has one).
    public static bool QuestLogHintShown => StoryFlagManager.HasFlag(QuestLogHintFlag);

    // Past the start of the game already (a later save, a test start further on): not to be shown.
    public static void SkipQuestLogHint()
    {
        StoryFlagManager.SetFlag(QuestLogHintFlag);
    }

    public static void ShowQuestLogHint()
    {
        if (QuestLogHintShown || GameInputSettings.GetKey(GameInputAction.Quests) == KeyCode.None)
            return;

        EnsureInstance();
        if (instance == null)
            return;

        StoryFlagManager.SetFlag(QuestLogHintFlag);
        string key = GameInputSettings.GetDisplayName(GameInputAction.Quests);
        instance.Enqueue($"Press <color=#{ColorUtility.ToHtmlStringRGB(KeyColor)}>{key}</color> to view your quests",
            null, HintHoldDuration);
    }

    // A quest's reward was given: the quest tracker shows it in the quest as it goes (TakeReward). One
    // it does not show (slid away, or the quest was never in it, e.g. done in a battle) comes as a
    // toast instead (ShowUntakenRewards).
    public static void NoteRewardReceived(QuestData quest)
    {
        if (quest != null && !UntakenRewards.Contains(quest))
            UntakenRewards.Add(quest);
    }

    // The quest tracker shows this quest's reward itself: no toast for it.
    public static bool TakeReward(QuestData quest)
    {
        return quest != null && UntakenRewards.Remove(quest);
    }

    public static void ShowUntakenRewards()
    {
        foreach (QuestData quest in UntakenRewards)
            ShowRewardReceived(quest);

        UntakenRewards.Clear();
    }

    public static void ShowRewardReceived(QuestData quest)
    {
        if (quest == null)
            return;

        EnsureInstance();
        if (instance != null)
            instance.Enqueue(RewardReceivedMessage, quest.RewardIcon);
    }

    private static void EnsureInstance()
    {
        if (instance != null)
            return;

        GameObject root = new("QuestNotificationToast");
        DontDestroyOnLoad(root);
        instance = root.AddComponent<QuestNotificationToast>();
        instance.Build();
    }

    private void Awake()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;

        if (delayedStartCoroutine != null)
        {
            StopCoroutine(delayedStartCoroutine);
            delayedStartCoroutine = null;
        }

        if (instance == this)
            instance = null;
    }

    private void Build()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 950;

        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        toastRect = new GameObject("Toast", typeof(RectTransform)).GetComponent<RectTransform>();
        toastRect.gameObject.layer = gameObject.layer;
        toastRect.SetParent(transform, false);
        toastRect.anchorMin = new Vector2(1f, 0f);
        toastRect.anchorMax = new Vector2(1f, 0f);
        toastRect.pivot = new Vector2(1f, 0f);
        toastRect.sizeDelta = new Vector2(360f, 64f);
        toastRect.anchoredPosition = HiddenPosition;

        Image image = toastRect.gameObject.AddComponent<Image>();
        image.color = PanelColor;
        image.raycastTarget = false;

        Outline outline = toastRect.gameObject.AddComponent<Outline>();
        outline.effectColor = BorderColor;
        outline.effectDistance = new Vector2(1f, -1f);

        toastText = new GameObject("Text", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
        toastText.transform.SetParent(toastRect, false);
        RectTransform textRect = toastText.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(22f, 0f);
        textRect.offsetMax = new Vector2(-22f, 0f);
        toastText.fontSize = 25f;
        toastText.color = TextColor;
        toastText.alignment = TextAlignmentOptions.MidlineLeft;
        toastText.textWrappingMode = TextWrappingModes.NoWrap;
        toastText.overflowMode = TextOverflowModes.Ellipsis;
        toastText.raycastTarget = false;

        toastIcon = new GameObject("Icon", typeof(RectTransform)).AddComponent<Image>();
        toastIcon.transform.SetParent(toastRect, false);
        RectTransform iconRect = toastIcon.rectTransform;
        iconRect.anchorMin = new Vector2(0f, 0.5f);
        iconRect.anchorMax = new Vector2(0f, 0.5f);
        iconRect.pivot = new Vector2(0f, 0.5f);
        iconRect.anchoredPosition = new Vector2(20f, 0f);
        iconRect.sizeDelta = new Vector2(42f, 42f);
        toastIcon.preserveAspect = true;
        toastIcon.raycastTarget = false;
        toastIcon.gameObject.SetActive(false);

        toastRect.gameObject.SetActive(false);
    }

    private void Enqueue(string message, Sprite icon, float hold = HoldDuration)
    {
        if (toastRect == null || toastText == null)
            Build();

        queuedToasts.Add(new ToastPayload(message, icon, hold));
        TryStartQueue();
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == SceneNames.Exploration)
        {
            ScheduleStartQueueAfterExplorationFade();
            return;
        }

        TryStartQueue();
    }

    private void TryStartQueue()
    {
        if (toastCoroutine != null || delayedStartCoroutine != null || queuedToasts.Count == 0)
            return;

        if (!CanShowInCurrentScene())
        {
            HideToast();
            return;
        }

        if (FadeManager.Instance != null && FadeManager.Instance.IsFading)
        {
            ScheduleStartQueueAfterExplorationFade();
            return;
        }

        toastCoroutine = StartCoroutine(AnimateQueuedToasts());
    }

    private void ScheduleStartQueueAfterExplorationFade()
    {
        if (delayedStartCoroutine != null)
            return;

        delayedStartCoroutine = StartCoroutine(StartQueueAfterExplorationFade());
    }

    private IEnumerator StartQueueAfterExplorationFade()
    {
        yield return null;

        while (CanShowInCurrentScene() && FadeManager.Instance != null && FadeManager.Instance.IsFading)
            yield return null;

        if (CanShowInCurrentScene())
            yield return new WaitForGameSeconds(ExplorationSceneToastDelay);

        delayedStartCoroutine = null;
        TryStartQueue();
    }

    // Each toast waits until the player has had control for a moment on end: a talk following right
    // after another (a quest done, then "Haru, wait a moment") is not a gap to slide in. One cut short by
    // a talk or a cutscene slides away at once and is shown again, whole, once it is over.
    private IEnumerator AnimateQueuedToasts()
    {
        while (queuedToasts.Count > 0)
        {
            float free = 0f;
            while (free < AfterDialogueDelay)
            {
                free = CanShowNow() ? free + Time.unscaledDeltaTime : 0f;
                yield return null;
            }

            ToastPayload payload = queuedToasts[0];
            queuedToasts.RemoveAt(0);
            Vector2 hidden = Show(payload);

            yield return Slide(hidden, VisiblePosition);
            bool cutShort = false;
            for (float held = 0f; held < payload.Hold; held += Time.unscaledDeltaTime)
            {
                if (!CanShowNow())
                {
                    cutShort = true;
                    break;
                }

                yield return null;
            }

            yield return Slide(VisiblePosition, hidden);
            toastRect.gameObject.SetActive(false);

            if (cutShort)
                queuedToasts.Insert(0, payload);
            else if (queuedToasts.Count > 0)
                yield return new WaitForGameSeconds(BetweenToastDelay);
        }

        toastCoroutine = null;
    }

    // Returns where it hides, just past the right edge of the screen at its width.
    private Vector2 Show(ToastPayload payload)
    {
        toastText.text = payload.Message;
        toastRect.gameObject.SetActive(true);

        bool hasIcon = payload.Icon != null;
        if (toastIcon != null)
        {
            toastIcon.sprite = payload.Icon;
            toastIcon.gameObject.SetActive(hasIcon);
        }

        float textLeft = hasIcon ? 76f : 22f;
        RectTransform textRect = toastText.rectTransform;
        textRect.offsetMin = new Vector2(textLeft, 0f);
        textRect.offsetMax = new Vector2(-22f, 0f);

        float width = Mathf.Clamp(textLeft + toastText.GetPreferredValues(payload.Message).x + 22f, MinWidth, MaxWidth);
        toastRect.sizeDelta = new Vector2(width, toastRect.sizeDelta.y);
        return new Vector2(width + 30f, HiddenPosition.y);
    }

    // In or out from the right, eased at both ends, like the quest tracker.
    private IEnumerator Slide(Vector2 from, Vector2 to)
    {
        float elapsed = 0f;
        toastRect.anchoredPosition = from;

        while (elapsed < SlideDuration)
        {
            elapsed += GameClock.DeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / SlideDuration));
            toastRect.anchoredPosition = Vector2.LerpUnclamped(from, to, t);
            yield return null;
        }

        toastRect.anchoredPosition = to;
    }

    private static bool CanShowInCurrentScene()
    {
        return SceneManager.GetActiveScene().name == SceneNames.Exploration;
    }

    // The player has control in the exploration scene: no talk, cutscene or open window, no fade.
    private static bool CanShowNow()
    {
        return CanShowInCurrentScene() && GameStateManager.IsGameplay
            && (FadeManager.Instance == null || !FadeManager.Instance.IsFading);
    }

    private void HideToast()
    {
        if (toastRect == null)
            return;

        toastRect.anchoredPosition = HiddenPosition;
        toastRect.gameObject.SetActive(false);
    }

    private readonly struct ToastPayload
    {
        public ToastPayload(string message, Sprite icon, float hold)
        {
            Message = message;
            Icon = icon;
            Hold = hold;
        }

        public readonly string Message;
        public readonly Sprite Icon;
        // How long it stays in sight, in seconds.
        public readonly float Hold;
    }
}
