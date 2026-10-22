using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class QuestNotificationToast : MonoBehaviour
{
    private const string QuestAddedMessage = "Quest added";
    private const string QuestCompletedMessage = "Quest completed";
    private const string RewardReceivedMessage = "Reward received";
    private const string QuestProgressMessage = "Quest progress";
    private static readonly Color PanelColor = new(0.31f, 0.23f, 0.2f, 0.98f);
    private static readonly Color BorderColor = new(0.94f, 0.68f, 0.34f, 1f);
    private static readonly Color TextColor = new(1f, 0.91f, 0.68f, 1f);
    private static readonly Vector2 VisiblePosition = new(-34f, 34f);
    private static readonly Vector2 HiddenPosition = new(390f, 34f);
    private const float SlideDuration = 0.22f;
    private const float HoldDuration = 2.35f;
    private const float BetweenToastDelay = 0.28f;
    private const float ExplorationSceneToastDelay = 0.5f;

    private static QuestNotificationToast instance;

    private RectTransform toastRect;
    private TMP_Text toastText;
    private Image toastIcon;
    private Coroutine toastCoroutine;
    private Coroutine delayedStartCoroutine;
    private readonly Queue<ToastPayload> queuedToasts = new();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureAfterSceneLoad()
    {
        EnsureInstance();
    }

    public static void ShowQuestAdded(QuestData quest)
    {
        if (quest == null)
            return;

        EnsureInstance();
        if (instance != null)
            instance.Enqueue(QuestAddedMessage, null);
    }

    public static void ShowQuestCompleted(QuestData quest)
    {
        if (quest == null)
            return;

        EnsureInstance();
        if (instance != null)
            instance.Enqueue(QuestCompletedMessage, null);
    }

    // E.g. "Quest progress 2/3" after a battle that counted for a Defeat Enemies quest.
    public static void ShowQuestProgress(QuestData quest, int current, int target)
    {
        if (quest == null)
            return;

        EnsureInstance();
        if (instance != null)
            instance.Enqueue($"{QuestProgressMessage} {current}/{target}", null);
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

    private void Enqueue(string message, Sprite icon)
    {
        if (toastRect == null || toastText == null)
            Build();

        queuedToasts.Enqueue(new ToastPayload(message, icon));
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

    private IEnumerator AnimateQueuedToasts()
    {
        while (queuedToasts.Count > 0)
        {
            ToastPayload payload = queuedToasts.Dequeue();
            Show(payload);

            yield return Slide(HiddenPosition, VisiblePosition);
            yield return new WaitForGameSeconds(HoldDuration);
            yield return Slide(VisiblePosition, HiddenPosition);

            toastRect.gameObject.SetActive(false);

            if (queuedToasts.Count > 0)
                yield return new WaitForGameSeconds(BetweenToastDelay);
        }

        toastCoroutine = null;
    }

    private void Show(ToastPayload payload)
    {
        toastText.text = payload.Message;
        toastRect.gameObject.SetActive(true);

        bool hasIcon = payload.Icon != null;
        if (toastIcon != null)
        {
            toastIcon.sprite = payload.Icon;
            toastIcon.gameObject.SetActive(hasIcon);
        }

        RectTransform textRect = toastText.rectTransform;
        textRect.offsetMin = new Vector2(hasIcon ? 76f : 22f, 0f);
        textRect.offsetMax = new Vector2(-22f, 0f);
    }

    private IEnumerator Slide(Vector2 from, Vector2 to)
    {
        float elapsed = 0f;
        toastRect.anchoredPosition = from;

        while (elapsed < SlideDuration)
        {
            elapsed += GameClock.DeltaTime;
            float t = Mathf.Clamp01(elapsed / SlideDuration);
            t = 1f - Mathf.Pow(1f - t, 3f);
            toastRect.anchoredPosition = Vector2.LerpUnclamped(from, to, t);
            yield return null;
        }

        toastRect.anchoredPosition = to;
    }

    private static bool CanShowInCurrentScene()
    {
        return SceneManager.GetActiveScene().name == SceneNames.Exploration;
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
        public ToastPayload(string message, Sprite icon)
        {
            Message = message;
            Icon = icon;
        }

        public readonly string Message;
        public readonly Sprite Icon;
    }
}
