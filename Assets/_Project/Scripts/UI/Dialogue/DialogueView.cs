using System;
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

    [Header("Input")]
    [SerializeField] private Button continueButton;
    [SerializeField] private GameObject continueIndicator;

    public event Action ContinueRequested;

    private bool visible;

    private void Awake()
    {
        WireButton();

        if (!visible)
            Hide();
    }

    private void OnDestroy()
    {
        if (continueButton != null)
            continueButton.onClick.RemoveListener(RequestContinue);
    }

    private void Update()
    {
        if (!visible)
            return;

        if (GameInputSettings.GetKeyDown(GameInputAction.Interact))
            RequestContinue();
    }

    public void ShowLine(
        DialogueLine line,
        string fallbackSpeakerName,
        Sprite fallbackPortrait,
        Sprite leftFallbackPortrait,
        Sprite rightFallbackPortrait)
    {
        if (line == null)
            return;

        WireButton();
        SetVisible(true);

        string speakerName = !string.IsNullOrWhiteSpace(line.speakerName)
            ? line.speakerName
            : fallbackSpeakerName;

        if (speakerNameText != null)
            speakerNameText.text = speakerName;

        MoveNamePlate(line.speakerSide);

        if (dialogueText != null)
            dialogueText.text = line.text;

        Sprite portrait = line.portraitOverride != null ? line.portraitOverride : fallbackPortrait;
        SetPortrait(
            leftPortrait,
            line.speakerSide == DialogueSpeakerSide.Left ? portrait : leftFallbackPortrait,
            line.speakerSide == DialogueSpeakerSide.Left,
            false);

        SetPortrait(
            rightPortrait,
            line.speakerSide == DialogueSpeakerSide.Right ? portrait : rightFallbackPortrait,
            line.speakerSide == DialogueSpeakerSide.Right,
            true);

        if (leftSpeakerRoot != null)
            leftSpeakerRoot.SetActive(leftPortrait == null || leftPortrait.sprite != null);

        if (rightSpeakerRoot != null)
            rightSpeakerRoot.SetActive(rightPortrait == null || rightPortrait.sprite != null);

        if (continueIndicator != null)
            continueIndicator.SetActive(true);
    }

    public void Hide()
    {
        SetVisible(false);

        if (speakerNameText != null)
            speakerNameText.text = string.Empty;

        if (dialogueText != null)
            dialogueText.text = string.Empty;
    }

    private void WireButton()
    {
        if (continueButton == null)
            return;

        continueButton.onClick.RemoveListener(RequestContinue);
        continueButton.onClick.AddListener(RequestContinue);
    }

    private void RequestContinue()
    {
        ContinueRequested?.Invoke();
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
            speakerNamePlate.anchoredPosition = new Vector2(-72f, -36f);
        }
        else
        {
            speakerNamePlate.anchorMin = new Vector2(0f, 1f);
            speakerNamePlate.anchorMax = new Vector2(0f, 1f);
            speakerNamePlate.pivot = new Vector2(0f, 0.5f);
            speakerNamePlate.anchoredPosition = new Vector2(72f, -36f);
        }

        speakerNamePlate.sizeDelta = new Vector2(360f, 72f);
    }
}
