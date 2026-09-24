using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class MainMenuSaveSlotsPanel : MonoBehaviour
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private MainMenuSaveSlotRow[] rows;
    [SerializeField] private Button backButton;
    [SerializeField] private Image modeIcon;
    [SerializeField] private Sprite newGameIcon;
    [SerializeField] private Sprite continueIcon;
    [SerializeField] private Sprite backPanelSprite;
    [SerializeField] private Sprite backPanelLightedSprite;
    [SerializeField] private Sprite savePanelSprite;
    [SerializeField] private Sprite loadPanelSprite;

    [Header("Animation")]
    [SerializeField] private RectTransform animatedPanel;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField, Min(0f)] private float verticalOffset = 90f;
    [SerializeField, Min(0.01f)] private float enterDuration = 0.5f;
    [SerializeField, Min(0.01f)] private float exitDuration = 0.5f;

    private MainMenuController controller;
    private MainMenuSaveSlotMode mode;
    private Coroutine animationRoutine;
    private Vector2 visiblePosition;
    private bool isExiting;

    private void Awake()
    {
        if (animatedPanel == null)
            animatedPanel = transform as RectTransform;
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();
        if (animatedPanel != null)
            visiblePosition = animatedPanel.anchoredPosition;

        if (backButton != null)
            backButton.onClick.AddListener(Hide);

        BindRows();
    }

    private void OnEnable()
    {
        isExiting = false;
        StartAnimation(PlayEnter());
    }

    private void OnDisable()
    {
        if (animationRoutine != null)
            StopCoroutine(animationRoutine);

        animationRoutine = null;
        isExiting = false;
    }

    public void Show(MainMenuController owner, MainMenuSaveSlotMode slotMode)
    {
        controller = owner;
        mode = slotMode;
        gameObject.SetActive(true);
        Refresh();
    }

    public void Hide()
    {
        if (controller != null)
            controller.HideSaveSlots();
        else
            gameObject.SetActive(false);
    }

    public void PlayExit(Action onComplete)
    {
        if (isExiting)
            return;

        isExiting = true;
        StartAnimation(PlayExitRoutine(onComplete));
    }

    private void BindRows()
    {
        if (rows == null)
            return;

        for (int i = 0; i < rows.Length; i++)
        {
            int slotIndex = i;
            Button button = rows[i] != null ? rows[i].Button : null;
            if (button == null)
                continue;

            button.onClick.AddListener(() => SelectSlot(slotIndex));
        }
    }

    private void Refresh()
    {
        bool isNewGame = mode == MainMenuSaveSlotMode.NewGame;

        if (titleText != null)
            titleText.text = isNewGame ? "Select Slot" : "Continue";

        if (modeIcon != null)
            modeIcon.sprite = isNewGame ? newGameIcon : continueIcon;

        if (rows == null)
            return;

        for (int i = 0; i < rows.Length; i++)
        {
            if (rows[i] == null)
                continue;

            bool occupied = PersistentSessionSave.TryGetSlotInfo(i, out SessionSaveData data);
            rows[i].SetText(GetSlotText(i, occupied, data));
            rows[i].SetPanelSprites(
                backPanelSprite,
                backPanelLightedSprite,
                isNewGame ? savePanelSprite : loadPanelSprite);

            if (occupied && PersistentSessionSave.TryLoadPreview(i, out Texture2D preview))
                rows[i].SetPreview(preview);

            rows[i].SetInteractable(isNewGame || occupied);
        }
    }

    private string GetSlotText(int slotIndex, bool occupied, SessionSaveData data)
    {
        string prefix = $"Slot {slotIndex + 1}";

        if (occupied)
            return $"{prefix}\n{FormatSaveDate(data)}";

        if (mode == MainMenuSaveSlotMode.NewGame)
            return $"{prefix}\nNew Game";

        return $"{prefix}\nEmpty";
    }

    private static string FormatSaveDate(SessionSaveData data)
    {
        if (data == null || data.savedAtTicks <= 0)
            return "Saved";

        DateTime savedAt = new DateTime(data.savedAtTicks);
        return savedAt.ToString("dd.MM.yyyy  HH:mm");
    }

    private void SelectSlot(int slotIndex)
    {
        if (controller == null)
            return;

        if (mode == MainMenuSaveSlotMode.NewGame)
            controller.StartNewGameInSlot(slotIndex);
        else
            controller.ContinueGameFromSlot(slotIndex);
    }

    private IEnumerator PlayEnter()
    {
        if (animatedPanel == null || canvasGroup == null)
            yield break;

        Vector2 hiddenPosition = visiblePosition + Vector2.down * verticalOffset;
        animatedPanel.anchoredPosition = hiddenPosition;
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;

        yield return Animate(hiddenPosition, visiblePosition, 0f, 1f, enterDuration, true);

        animatedPanel.anchoredPosition = visiblePosition;
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
        animationRoutine = null;
    }

    private IEnumerator PlayExitRoutine(Action onComplete)
    {
        if (animatedPanel == null || canvasGroup == null)
        {
            onComplete?.Invoke();
            yield break;
        }

        canvasGroup.blocksRaycasts = false;
        Vector2 startPosition = animatedPanel.anchoredPosition;
        Vector2 hiddenPosition = visiblePosition + Vector2.down * verticalOffset;

        yield return Animate(startPosition, hiddenPosition, canvasGroup.alpha, 0f, exitDuration, false);

        animatedPanel.anchoredPosition = hiddenPosition;
        canvasGroup.alpha = 0f;
        animationRoutine = null;
        onComplete?.Invoke();
    }

    private IEnumerator Animate(
        Vector2 fromPosition,
        Vector2 toPosition,
        float fromAlpha,
        float toAlpha,
        float duration,
        bool easeOut)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float easedProgress = easeOut
                ? 1f - Mathf.Pow(1f - progress, 3f)
                : Mathf.SmoothStep(0f, 1f, progress);

            animatedPanel.anchoredPosition = Vector2.LerpUnclamped(fromPosition, toPosition, easedProgress);
            canvasGroup.alpha = Mathf.LerpUnclamped(fromAlpha, toAlpha, easedProgress);
            yield return null;
        }
    }

    private void StartAnimation(IEnumerator routine)
    {
        if (animationRoutine != null)
            StopCoroutine(animationRoutine);

        animationRoutine = StartCoroutine(routine);
    }
}
