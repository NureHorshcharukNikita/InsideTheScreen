using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class QuestNotificationToast : MonoBehaviour
{
    private const string QuestAddedMessage = "Quest added";
    private static readonly Color PanelColor = new(0.31f, 0.23f, 0.2f, 0.98f);
    private static readonly Color BorderColor = new(0.94f, 0.68f, 0.34f, 1f);
    private static readonly Color TextColor = new(1f, 0.91f, 0.68f, 1f);
    private static readonly Vector2 VisiblePosition = new(-34f, 34f);
    private static readonly Vector2 HiddenPosition = new(390f, 34f);
    private const float SlideDuration = 0.22f;
    private const float HoldDuration = 1.65f;

    private static QuestNotificationToast instance;

    private RectTransform toastRect;
    private TMP_Text toastText;
    private Coroutine toastCoroutine;

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
            instance.Show(QuestAddedMessage);
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

        toastRect.gameObject.SetActive(false);
    }

    private void Show(string message)
    {
        if (toastRect == null || toastText == null)
            Build();

        if (toastCoroutine != null)
            StopCoroutine(toastCoroutine);

        toastText.text = message;
        toastRect.gameObject.SetActive(true);
        toastCoroutine = StartCoroutine(AnimateToast());
    }

    private IEnumerator AnimateToast()
    {
        yield return Slide(HiddenPosition, VisiblePosition);
        yield return new WaitForSecondsRealtime(HoldDuration);
        yield return Slide(VisiblePosition, HiddenPosition);

        toastRect.gameObject.SetActive(false);
        toastCoroutine = null;
    }

    private IEnumerator Slide(Vector2 from, Vector2 to)
    {
        float elapsed = 0f;
        toastRect.anchoredPosition = from;

        while (elapsed < SlideDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / SlideDuration);
            t = 1f - Mathf.Pow(1f - t, 3f);
            toastRect.anchoredPosition = Vector2.LerpUnclamped(from, to, t);
            yield return null;
        }

        toastRect.anchoredPosition = to;
    }
}
