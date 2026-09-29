using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class DialogueView : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject root;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Speakers")]
    [SerializeField] private GameObject leftSpeakerRoot;
    [SerializeField] private Image leftPortrait;
    [SerializeField] private GameObject rightSpeakerRoot;
    [SerializeField] private Image rightPortrait;
    [SerializeField, Range(0f, 1f)] private float inactivePortraitAlpha = 0.45f;

    [Header("Text")]
    [SerializeField] private RectTransform speakerNamePlate;
    [SerializeField] private TMP_Text speakerNameText;
    [SerializeField] private TMP_Text dialogueText;

    [Header("Text Reveal")]
    [SerializeField, Min(1f)] private float charactersPerSecond = 45f;

    [Header("Input")]
    [SerializeField] private Button continueButton;
    [SerializeField] private Image continueIcon;
    [SerializeField] private RawImage skipTypingIcon;
    [SerializeField] private Button skipDialogueButton;

    [Header("Choices")]
    [SerializeField] private RectTransform choicesRoot;
    [SerializeField] private Button choiceButtonPrefab;

    public event Action ContinueRequested;
    public event Action<int> ChoiceRequested;

    private const float FastForwardMultiplier = 8f;
    private const float DefaultChoiceButtonHeight = 62f;
    private const float DefaultChoiceHorizontalPadding = 44f;
    private const float MinChoiceWidth = 260f;
    private const float MaxChoiceWidth = 860f;
    private readonly List<Button> choiceButtons = new();
    private Vector2 dialogueTextDefaultOffsetMin;
    private IReadOnlyList<DialogueChoice> pendingChoices;
    private Coroutine textRevealRoutine;
    private Sprite lastLeftPortrait;
    private Sprite lastRightPortrait;
    private int revealStartedFrame = -1;
    private bool visible;
    private bool showingChoices;
    private bool revealingText;
    private bool fastForwarding;

    private void Awake()
    {
        WireButtons();
        CacheDialogueTextLayout();
        SetContinueIcon(false);

        if (!visible)
            Hide();
    }

    private void OnDestroy()
    {
        if (continueButton != null)
            continueButton.onClick.RemoveListener(RequestContinue);
        if (skipDialogueButton != null)
            skipDialogueButton.onClick.RemoveListener(BeginFastForward);

        ClearChoices();
    }

    private void Update()
    {
        if (!visible)
            return;

        if (showingChoices)
        {
            SelectChoiceFromKeyboard();
            return;
        }

        if (revealingText && Time.frameCount == revealStartedFrame)
            return;

        if (GameInputSettings.GetKeyDown(GameInputAction.Interact) || Input.GetKeyDown(KeyCode.Space))
            RequestContinue();
    }

    public void ShowLine(
        DialogueLine line,
        string fallbackSpeakerName,
        Sprite fallbackPortrait,
        Sprite leftFallbackPortrait,
        Sprite rightFallbackPortrait,
        IReadOnlyList<DialogueChoice> choices = null)
    {
        if (line == null)
            return;

        WireButtons();
        SetVisible(true);

        string speakerName = !string.IsNullOrWhiteSpace(line.speakerName)
            ? line.speakerName
            : fallbackSpeakerName;

        if (speakerNameText != null)
            speakerNameText.text = speakerName;

        MoveNamePlate(line.speakerSide);

        Sprite portrait = line.portraitOverride != null ? line.portraitOverride : fallbackPortrait;
        if (line.speakerSide == DialogueSpeakerSide.Left)
            lastLeftPortrait = portrait;
        else
            lastRightPortrait = portrait;

        Sprite displayedLeftPortrait = line.speakerSide == DialogueSpeakerSide.Left
            ? portrait
            : lastLeftPortrait != null ? lastLeftPortrait : leftFallbackPortrait;
        Sprite displayedRightPortrait = line.speakerSide == DialogueSpeakerSide.Right
            ? portrait
            : lastRightPortrait != null ? lastRightPortrait : rightFallbackPortrait;

        SetPortrait(
            leftPortrait,
            displayedLeftPortrait,
            line.speakerSide == DialogueSpeakerSide.Left,
            false);

        SetPortrait(
            rightPortrait,
            displayedRightPortrait,
            line.speakerSide == DialogueSpeakerSide.Right,
            true);

        if (leftSpeakerRoot != null)
            leftSpeakerRoot.SetActive(leftPortrait == null || leftPortrait.sprite != null);

        if (rightSpeakerRoot != null)
            rightSpeakerRoot.SetActive(rightPortrait == null || rightPortrait.sprite != null);

        StartTextReveal(line.text, choices);
    }

    public void Hide()
    {
        StopTextReveal();
        fastForwarding = false;
        lastLeftPortrait = null;
        lastRightPortrait = null;
        SetVisible(false);
        ClearChoices();
        RestoreDialogueTextLayout();

        if (speakerNameText != null)
            speakerNameText.text = string.Empty;

        if (dialogueText != null)
        {
            dialogueText.text = string.Empty;
            dialogueText.maxVisibleCharacters = int.MaxValue;
        }
    }

    public bool TryCompleteTextReveal()
    {
        if (!revealingText)
            return false;

        CompleteTextReveal();
        return true;
    }

    private void WireButtons()
    {
        if (continueButton != null)
        {
            continueButton.onClick.RemoveListener(RequestContinue);
            continueButton.onClick.AddListener(RequestContinue);
        }

        if (skipDialogueButton != null)
        {
            skipDialogueButton.onClick.RemoveListener(BeginFastForward);
            skipDialogueButton.onClick.AddListener(BeginFastForward);
        }
    }

    private void RequestContinue()
    {
        if (TryCompleteTextReveal())
            return;

        ContinueRequested?.Invoke();
    }

    private void BeginFastForward()
    {
        if (!visible || showingChoices || fastForwarding)
            return;

        fastForwarding = true;

        if (!revealingText)
            ContinueRequested?.Invoke();
    }

    private void RequestChoice(int index)
    {
        ChoiceRequested?.Invoke(index);
    }

    private void SetVisible(bool value)
    {
        visible = value;

        GameObject targetRoot = root != null ? root : gameObject;
        targetRoot.SetActive(value);

        if (canvasGroup != null)
        {
            canvasGroup.alpha = value ? 1f : 0f;
            canvasGroup.interactable = value;
            canvasGroup.blocksRaycasts = value;
        }

        GameplayHudFadeState.SetMinimapBlocked(value);
    }

    private void CacheDialogueTextLayout()
    {
        if (dialogueText == null)
            return;

        RectTransform rect = dialogueText.rectTransform;
        dialogueTextDefaultOffsetMin = rect.offsetMin;
    }

    private void RestoreDialogueTextLayout()
    {
        if (dialogueText == null)
            return;

        RectTransform rect = dialogueText.rectTransform;
        rect.offsetMin = dialogueTextDefaultOffsetMin;
    }

    private void StartTextReveal(string text, IReadOnlyList<DialogueChoice> choices)
    {
        StopTextReveal();
        ClearChoices();
        pendingChoices = choices;

        if (dialogueText == null)
        {
            ShowChoices(pendingChoices);
            pendingChoices = null;
            return;
        }

        dialogueText.text = text ?? string.Empty;
        dialogueText.maxVisibleCharacters = 0;
        dialogueText.ForceMeshUpdate();

        if (dialogueText.textInfo.characterCount == 0)
        {
            CompleteTextReveal();
            return;
        }

        revealingText = true;
        revealStartedFrame = Time.frameCount;
        if (continueButton != null)
            continueButton.gameObject.SetActive(true);
        if (skipDialogueButton != null)
            skipDialogueButton.gameObject.SetActive(true);
        SetContinueIcon(true);

        textRevealRoutine = StartCoroutine(RevealText());
    }

    private IEnumerator RevealText()
    {
        int characterCount = dialogueText.textInfo.characterCount;
        float visibleCharacters = 0f;

        while (dialogueText != null && dialogueText.maxVisibleCharacters < characterCount)
        {
            float revealSpeed = charactersPerSecond * (fastForwarding ? FastForwardMultiplier : 1f);
            visibleCharacters += revealSpeed * Time.unscaledDeltaTime;
            dialogueText.maxVisibleCharacters = Mathf.Min(
                characterCount,
                Mathf.FloorToInt(visibleCharacters));
            yield return null;
        }

        textRevealRoutine = null;
        CompleteTextReveal();
    }

    private void CompleteTextReveal()
    {
        if (textRevealRoutine != null)
            StopCoroutine(textRevealRoutine);

        textRevealRoutine = null;
        revealingText = false;

        if (dialogueText != null)
            dialogueText.maxVisibleCharacters = int.MaxValue;

        IReadOnlyList<DialogueChoice> choices = pendingChoices;
        pendingChoices = null;
        ShowChoices(choices);

        if (fastForwarding && !showingChoices)
            ContinueRequested?.Invoke();
    }

    private void StopTextReveal()
    {
        if (textRevealRoutine != null)
            StopCoroutine(textRevealRoutine);

        textRevealRoutine = null;
        pendingChoices = null;
        revealingText = false;
        revealStartedFrame = -1;
    }

    private void ShowChoices(IReadOnlyList<DialogueChoice> choices)
    {
        ClearChoices();

        bool hasChoices = choices != null && choices.Count > 0;
        showingChoices = hasChoices;
        if (showingChoices)
            fastForwarding = false;
        SetContinueIcon(false);
        if (continueButton != null)
            continueButton.gameObject.SetActive(!showingChoices);
        if (skipDialogueButton != null)
            skipDialogueButton.gameObject.SetActive(!showingChoices);

        if (!hasChoices)
        {
            RestoreDialogueTextLayout();
            if (choicesRoot != null)
                choicesRoot.gameObject.SetActive(false);
            return;
        }

        if (choicesRoot == null || choiceButtonPrefab == null)
        {
            Debug.LogError("Dialogue choices UI is not assigned. Expected ChoicesRoot and ChoiceButtonTemplate in the DialogueRoot hierarchy.", this);
            return;
        }

        RestoreDialogueTextLayout();
        choicesRoot.gameObject.SetActive(true);

        for (int i = 0; i < choices.Count; i++)
            CreateChoiceButton(choices[i], i);

        ResizeChoicesRoot();
        LayoutRebuilder.ForceRebuildLayoutImmediate(choicesRoot);
    }

    private void ClearChoices()
    {
        showingChoices = false;

        for (int i = 0; i < choiceButtons.Count; i++)
        {
            if (choiceButtons[i] != null)
            {
                choiceButtons[i].gameObject.SetActive(false);
                Destroy(choiceButtons[i].gameObject);
            }
        }

        choiceButtons.Clear();

        if (choicesRoot != null)
            choicesRoot.gameObject.SetActive(false);
    }

    private void CreateChoiceButton(DialogueChoice choice, int index)
    {
        if (choice == null || choicesRoot == null || choiceButtonPrefab == null)
            return;

        Button button = Instantiate(choiceButtonPrefab, choicesRoot);
        button.name = "ChoiceButton";
        button.transition = Selectable.Transition.None;
        button.gameObject.SetActive(true);
        button.onClick.RemoveAllListeners();

        int choiceIndex = index;
        button.onClick.AddListener(() => RequestChoice(choiceIndex));

        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
        {
            label.text = choice.text;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Ellipsis;
        }

        DialogueChoiceButtonFeedback feedback = button.GetComponent<DialogueChoiceButtonFeedback>();
        if (feedback != null)
            feedback.Configure(label, choice.text);

        choiceButtons.Add(button);
    }

    private void ResizeChoicesRoot()
    {
        if (choicesRoot == null)
            return;

        int choiceCount = choiceButtons.Count;
        VerticalLayoutGroup layout = choicesRoot.GetComponent<VerticalLayoutGroup>();
        float spacing = layout != null ? layout.spacing : 0f;
        float padding = layout != null ? layout.padding.top + layout.padding.bottom : 0f;
        float height = choiceCount * GetChoiceButtonHeight() + Mathf.Max(0, choiceCount - 1) * spacing + padding;
        choicesRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
        choicesRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, GetChoicesWidth());
    }

    private float GetChoiceButtonHeight()
    {
        if (choiceButtonPrefab == null)
            return DefaultChoiceButtonHeight;

        LayoutElement layout = choiceButtonPrefab.GetComponent<LayoutElement>();
        if (layout != null && layout.preferredHeight > 0f)
            return layout.preferredHeight;

        RectTransform rect = choiceButtonPrefab.transform as RectTransform;
        if (rect != null)
        {
            if (rect.sizeDelta.y > 0f)
                return rect.sizeDelta.y;

            if (rect.rect.height > 0f)
                return rect.rect.height;
        }

        return DefaultChoiceButtonHeight;
    }

    private float GetChoicesWidth()
    {
        float maxTextWidth = 0f;
        TMP_Text widestLabel = null;
        for (int i = 0; i < choiceButtons.Count; i++)
        {
            if (choiceButtons[i] == null)
                continue;

            TMP_Text label = choiceButtons[i].GetComponentInChildren<TMP_Text>(true);
            if (label == null || string.IsNullOrWhiteSpace(label.text))
                continue;

            float textWidth = label.GetPreferredValues(label.text).x;
            if (textWidth > maxTextWidth)
                widestLabel = label;
            maxTextWidth = Mathf.Max(maxTextWidth, textWidth);
        }

        return Mathf.Ceil(Mathf.Clamp(maxTextWidth + GetChoiceHorizontalPadding(widestLabel), MinChoiceWidth, MaxChoiceWidth));
    }

    private static float GetChoiceHorizontalPadding(TMP_Text label)
    {
        if (label == null)
            return DefaultChoiceHorizontalPadding;

        RectTransform textRect = label.rectTransform;
        float padding = textRect.offsetMin.x - textRect.offsetMax.x;
        return padding > 0f ? padding : DefaultChoiceHorizontalPadding;
    }

    private void SetContinueIcon(bool typing)
    {
        if (continueIcon == null)
            return;

        continueIcon.enabled = !typing || skipTypingIcon == null;

        if (skipTypingIcon != null)
            skipTypingIcon.gameObject.SetActive(typing);
    }

    private void SelectChoiceFromKeyboard()
    {
        int maxChoiceCount = Mathf.Min(choiceButtons.Count, 9);
        for (int i = 0; i < maxChoiceCount; i++)
        {
            KeyCode alphaKey = (KeyCode)((int)KeyCode.Alpha1 + i);
            KeyCode keypadKey = (KeyCode)((int)KeyCode.Keypad1 + i);
            if (Input.GetKeyDown(alphaKey) || Input.GetKeyDown(keypadKey))
            {
                RequestChoice(i);
                return;
            }
        }
    }

    private void SetPortrait(Image image, Sprite sprite, bool activeSpeaker, bool flipped)
    {
        if (image == null)
            return;

        image.sprite = sprite;
        image.enabled = sprite != null;

        RectTransform rect = image.rectTransform;
        Vector3 scale = rect.localScale;
        scale.x = Mathf.Abs(scale.x) * (flipped ? -1f : 1f);
        rect.localScale = scale;

        Color color = image.color;
        color.a = activeSpeaker ? 1f : inactivePortraitAlpha;
        image.color = color;
    }

    private void MoveNamePlate(DialogueSpeakerSide side)
    {
        if (speakerNamePlate == null)
            return;

        if (side == DialogueSpeakerSide.Right)
        {
            speakerNamePlate.anchorMin = new Vector2(1f, 1f);
            speakerNamePlate.anchorMax = new Vector2(1f, 1f);
            speakerNamePlate.pivot = new Vector2(1f, 0.5f);
            speakerNamePlate.anchoredPosition = new Vector2(-72f, -12f);
        }
        else
        {
            speakerNamePlate.anchorMin = new Vector2(0f, 1f);
            speakerNamePlate.anchorMax = new Vector2(0f, 1f);
            speakerNamePlate.pivot = new Vector2(0f, 0.5f);
            speakerNamePlate.anchoredPosition = new Vector2(72f, -12f);
        }

        speakerNamePlate.sizeDelta = new Vector2(360f, 72f);
    }
}
