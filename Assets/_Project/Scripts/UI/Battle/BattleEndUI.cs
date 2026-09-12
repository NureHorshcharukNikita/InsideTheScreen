using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BattleEndUI : MonoBehaviour
{
    private static readonly Color PanelColor = new(0.45f, 0.32f, 0.28f, 0.96f);
    private static readonly Color FrameColor = new(0.55f, 0.43f, 0.38f, 0.82f);
    private static readonly Color CardColor = new(0.17f, 0.10f, 0.08f, 0.95f);
    private static readonly Color BorderColor = new(0.06f, 0.035f, 0.03f, 1f);
    private static readonly Color ButtonColor = new(0.82f, 0.74f, 0.65f, 1f);
    private static readonly Color ButtonHighlightColor = new(0.91f, 0.84f, 0.74f, 1f);
    private static readonly Color ButtonPressedColor = new(0.68f, 0.56f, 0.46f, 1f);
    private static readonly Color ButtonTextColor = new(0.19f, 0.12f, 0.09f, 1f);
    private const float VictoryCenterHoldSeconds = 0.65f;
    private const float LootRevealSeconds = 0.38f;
    private const float VictoryResultY = 270f;
    private const float LootPanelHiddenScale = 0.86f;

    public TMP_Text resultText;

    [Header("Buttons")]
    [SerializeField] private Button retryButton;
    [SerializeField] private Button leaveButton;
    [SerializeField] private TMP_Text retryButtonText;
    [SerializeField] private TMP_Text leaveButtonText;

    private bool _lastOutcomeWasVictory;
    private bool _transitionStarted;
    private bool _createdLeaveButton;
    private Vector2 _retryButtonStartPosition;
    private Vector2 _resultTextStartPosition;
    private bool _hasResultTextStartPosition;
    private RectTransform _lootPanel;
    private RectTransform _lootContent;
    private TMP_Text _lootEmptyText;
    private Button _lootCollectButton;
    private CanvasGroup _lootPanelCanvasGroup;
    private Coroutine _victorySequence;
    private readonly List<GameObject> _lootItemViews = new();

    private void Awake()
    {
        ResolveButtons();
        CaptureResultTextLayout();
    }

    private void OnEnable()
    {
        _transitionStarted = false;
        WireButtons();
    }

    private void OnDisable()
    {
        StopVictorySequence();
        UnwireButtons();
        ClearLootItems();
        SetLootPanelVisible(false);
        RestoreResultTextLayout();
    }

    public void ShowVictory() => ShowEndScreen(true, null);

    public void ShowVictory(IReadOnlyList<CardData> droppedLoot) => ShowEndScreen(true, droppedLoot);

    public void ShowDefeat() => ShowEndScreen(false);

    private void ShowEndScreen(bool victory, IReadOnlyList<CardData> droppedLoot = null)
    {
        StopVictorySequence();
        _lastOutcomeWasVictory = victory;
        CloseOpenInventoryWindow();
        gameObject.SetActive(true);
        transform.SetAsLastSibling();
        ClearPointerUiState();
        SuppressOtherUiRaycastsUnderBattleCanvas();
        if (resultText != null)
            resultText.text = victory ? "VICTORY" : "DEFEAT";

        if (victory)
        {
            RestoreResultTextLayout();
            BuildLootPanel(droppedLoot);
            PrepareLootPanelForReveal();
            _victorySequence = StartCoroutine(PlayVictoryLootReveal());
        }
        else
        {
            RestoreResultTextLayout();
            SetLootPanelVisible(false);
        }

        RefreshButtons(victory);
    }

    private void CaptureResultTextLayout()
    {
        RectTransform resultRect = resultText != null ? resultText.transform as RectTransform : null;
        if (resultRect == null)
            return;

        _resultTextStartPosition = resultRect.anchoredPosition;
        _hasResultTextStartPosition = true;
    }

    private void RestoreResultTextLayout()
    {
        RectTransform resultRect = resultText != null ? resultText.transform as RectTransform : null;
        if (resultRect != null && _hasResultTextStartPosition)
            resultRect.anchoredPosition = _resultTextStartPosition;
    }

    private void BuildLootPanel(IReadOnlyList<CardData> droppedLoot)
    {
        EnsureLootPanel();
        SetLootPanelVisible(true);
        ClearLootItems();
        if (_lootCollectButton != null)
        {
            _lootCollectButton.onClick.RemoveListener(OnContinueAfterBattle);
            _lootCollectButton.onClick.AddListener(OnContinueAfterBattle);
        }

        List<CardLootStack> stacks = CreateLootStacks(droppedLoot);
        bool hasLoot = stacks.Count > 0;
        if (_lootEmptyText != null)
            _lootEmptyText.gameObject.SetActive(!hasLoot);

        for (int i = 0; i < stacks.Count; i++)
            CreateLootItemView(stacks[i]);
    }

    private void PrepareLootPanelForReveal()
    {
        if (_lootPanel == null)
            return;

        _lootPanel.localScale = Vector3.one * LootPanelHiddenScale;
        if (_lootPanelCanvasGroup != null)
        {
            _lootPanelCanvasGroup.alpha = 0f;
            _lootPanelCanvasGroup.interactable = false;
            _lootPanelCanvasGroup.blocksRaycasts = false;
        }
    }

    private IEnumerator PlayVictoryLootReveal()
    {
        yield return new WaitForSecondsRealtime(VictoryCenterHoldSeconds);

        RectTransform resultRect = resultText != null ? resultText.transform as RectTransform : null;
        Vector2 resultStart = resultRect != null ? resultRect.anchoredPosition : _resultTextStartPosition;
        Vector2 resultTarget = new(_resultTextStartPosition.x, VictoryResultY);

        float elapsed = 0f;
        while (elapsed < LootRevealSeconds)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / LootRevealSeconds);
            float eased = Mathf.SmoothStep(0f, 1f, t);
            if (resultRect != null)
                resultRect.anchoredPosition = Vector2.LerpUnclamped(resultStart, resultTarget, eased);
            if (_lootPanel != null)
                _lootPanel.localScale = Vector3.LerpUnclamped(Vector3.one * LootPanelHiddenScale, Vector3.one, eased);
            if (_lootPanelCanvasGroup != null)
                _lootPanelCanvasGroup.alpha = eased;

            yield return null;
        }

        if (resultRect != null)
            resultRect.anchoredPosition = resultTarget;
        if (_lootPanel != null)
            _lootPanel.localScale = Vector3.one;
        if (_lootPanelCanvasGroup != null)
        {
            _lootPanelCanvasGroup.alpha = 1f;
            _lootPanelCanvasGroup.interactable = true;
            _lootPanelCanvasGroup.blocksRaycasts = true;
        }

        _victorySequence = null;
    }

    private void StopVictorySequence()
    {
        if (_victorySequence == null)
            return;

        StopCoroutine(_victorySequence);
        _victorySequence = null;
    }

    private static List<CardLootStack> CreateLootStacks(IReadOnlyList<CardData> droppedLoot)
    {
        List<CardLootStack> stacks = new();
        if (droppedLoot == null)
            return stacks;

        Dictionary<CardData, int> counts = new();
        for (int i = 0; i < droppedLoot.Count; i++)
        {
            CardData card = droppedLoot[i];
            if (card == null)
                continue;

            if (!counts.ContainsKey(card))
                stacks.Add(new CardLootStack(card, 0));

            counts[card] = counts.TryGetValue(card, out int count) ? count + 1 : 1;
        }

        for (int i = 0; i < stacks.Count; i++)
        {
            CardLootStack stack = stacks[i];
            stacks[i] = new CardLootStack(stack.Card, counts[stack.Card]);
        }

        return stacks;
    }

    private static string GetCardDisplayName(CardData card)
    {
        if (card == null)
            return "Card";

        return string.IsNullOrWhiteSpace(card.CardName) ? card.name : card.CardName;
    }

    private void EnsureLootPanel()
    {
        if (_lootPanel != null)
            return;

        _lootPanel = CreateRect("LootPanel", transform);
        _lootPanel.anchorMin = new Vector2(0.5f, 0.5f);
        _lootPanel.anchorMax = new Vector2(0.5f, 0.5f);
        _lootPanel.pivot = new Vector2(0.5f, 0.5f);
        _lootPanel.sizeDelta = new Vector2(640f, 420f);
        _lootPanel.anchoredPosition = new Vector2(0f, -70f);
        Image panelImage = _lootPanel.gameObject.AddComponent<Image>();
        panelImage.color = PanelColor;
        panelImage.raycastTarget = true;
        _lootPanelCanvasGroup = _lootPanel.gameObject.AddComponent<CanvasGroup>();

        TMP_Text title = CreateText("Title", _lootPanel, "You received", 40f, TextAlignmentOptions.Center);
        RectTransform titleRect = title.transform as RectTransform;
        StretchTop(titleRect, 0f, 0f, 18f, 74f);

        RectTransform frame = CreateRect("LootFrame", _lootPanel);
        Stretch(frame, 30f, 86f, 30f, 104f);
        Image frameImage = frame.gameObject.AddComponent<Image>();
        frameImage.color = FrameColor;
        frameImage.raycastTarget = true;
        Outline frameOutline = frame.gameObject.AddComponent<Outline>();
        frameOutline.effectColor = BorderColor;
        frameOutline.effectDistance = new Vector2(2f, -2f);

        RectTransform viewport = CreateRect("Viewport", frame);
        Stretch(viewport, 10f, 10f, 10f, 10f);
        viewport.gameObject.AddComponent<RectMask2D>();

        _lootContent = CreateRect("Content", viewport);
        _lootContent.anchorMin = new Vector2(0f, 0f);
        _lootContent.anchorMax = new Vector2(0f, 1f);
        _lootContent.pivot = new Vector2(0f, 0.5f);
        _lootContent.anchoredPosition = Vector2.zero;
        _lootContent.sizeDelta = new Vector2(0f, 0f);
        HorizontalLayoutGroup layout = _lootContent.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(14, 14, 16, 16);
        layout.spacing = 14f;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = _lootContent.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

        ScrollRect scroll = frame.gameObject.AddComponent<ScrollRect>();
        scroll.viewport = viewport;
        scroll.content = _lootContent;
        scroll.horizontal = true;
        scroll.vertical = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 28f;

        _lootEmptyText = CreateText("EmptyText", frame, "No loot received", 26f, TextAlignmentOptions.Center);
        RectTransform emptyRect = _lootEmptyText.transform as RectTransform;
        Stretch(emptyRect, 12f, 12f, 12f, 12f);

        _lootCollectButton = CreateButton("CollectButton", _lootPanel, "Collect");
        RectTransform collectRect = _lootCollectButton.transform as RectTransform;
        collectRect.anchorMin = new Vector2(0.5f, 0f);
        collectRect.anchorMax = new Vector2(0.5f, 0f);
        collectRect.pivot = new Vector2(0.5f, 0f);
        collectRect.sizeDelta = new Vector2(220f, 44f);
        collectRect.anchoredPosition = new Vector2(0f, 28f);
        _lootCollectButton.onClick.AddListener(OnContinueAfterBattle);

        SetChildrenLayer(_lootPanel, gameObject.layer);
    }

    private void CreateLootItemView(CardLootStack stack)
    {
        RectTransform item = CreateRect("LootItem", _lootContent);
        item.sizeDelta = new Vector2(120f, 168f);
        _lootItemViews.Add(item.gameObject);

        LayoutElement layoutElement = item.gameObject.AddComponent<LayoutElement>();
        layoutElement.preferredWidth = 120f;
        layoutElement.preferredHeight = 168f;

        Image background = item.gameObject.AddComponent<Image>();
        background.color = CardColor;
        background.raycastTarget = false;
        Outline outline = item.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.78f, 0.68f, 0.58f, 0.95f);
        outline.effectDistance = new Vector2(2f, -2f);

        TMP_Text nameText = CreateText("Name", item, GetCardDisplayName(stack.Card), 14f, TextAlignmentOptions.Center);
        RectTransform nameRect = nameText.transform as RectTransform;
        StretchTop(nameRect, 8f, 8f, 6f, 34f);
        nameText.textWrappingMode = TextWrappingModes.NoWrap;
        nameText.overflowMode = TextOverflowModes.Ellipsis;
        nameText.color = new Color(0.94f, 0.89f, 0.78f, 1f);

        Image icon = CreateRect("Icon", item).gameObject.AddComponent<Image>();
        RectTransform iconRect = icon.transform as RectTransform;
        iconRect.anchorMin = new Vector2(0.5f, 0.5f);
        iconRect.anchorMax = new Vector2(0.5f, 0.5f);
        iconRect.pivot = new Vector2(0.5f, 0.5f);
        iconRect.sizeDelta = new Vector2(76f, 76f);
        iconRect.anchoredPosition = new Vector2(0f, -4f);
        icon.sprite = stack.Card != null ? stack.Card.Icon : null;
        icon.enabled = icon.sprite != null;
        icon.preserveAspect = true;
        icon.raycastTarget = false;

        TMP_Text countText = CreateText("Count", item, stack.Count > 1 ? $"x{stack.Count}" : string.Empty, 20f, TextAlignmentOptions.Right);
        RectTransform countRect = countText.transform as RectTransform;
        countRect.anchorMin = new Vector2(0f, 0f);
        countRect.anchorMax = new Vector2(1f, 0f);
        countRect.pivot = new Vector2(0.5f, 0f);
        countRect.offsetMin = new Vector2(8f, 8f);
        countRect.offsetMax = new Vector2(-8f, 36f);
        countText.color = Color.white;
    }

    private static RectTransform CreateRect(string objectName, Transform parent)
    {
        GameObject instance = new(objectName, typeof(RectTransform));
        instance.transform.SetParent(parent, false);
        return instance.transform as RectTransform;
    }

    private TMP_Text CreateText(string objectName, Transform parent, string text, float fontSize, TextAlignmentOptions alignment)
    {
        TextMeshProUGUI label = CreateRect(objectName, parent).gameObject.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = fontSize;
        label.alignment = alignment;
        label.color = Color.white;
        label.raycastTarget = false;
        if (resultText != null)
        {
            label.font = resultText.font;
            label.fontSharedMaterial = resultText.fontSharedMaterial;
        }

        return label;
    }

    private Button CreateButton(string objectName, Transform parent, string text)
    {
        RectTransform buttonRect = CreateRect(objectName, parent);
        Image image = buttonRect.gameObject.AddComponent<Image>();
        image.color = ButtonColor;
        Outline outline = buttonRect.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.25f, 0.16f, 0.12f, 0.75f);
        outline.effectDistance = new Vector2(1f, -1f);
        Button button = buttonRect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.normalColor = ButtonColor;
        colors.highlightedColor = ButtonHighlightColor;
        colors.pressedColor = ButtonPressedColor;
        colors.selectedColor = ButtonHighlightColor;
        colors.disabledColor = new Color(0.55f, 0.48f, 0.42f, 0.65f);
        colors.colorMultiplier = 1f;
        button.colors = colors;

        TMP_Text label = CreateText("Label", buttonRect, text, 24f, TextAlignmentOptions.Center);
        label.color = ButtonTextColor;
        RectTransform labelRect = label.transform as RectTransform;
        Stretch(labelRect, 6f, 4f, 6f, 4f);

        return button;
    }

    private static void Stretch(RectTransform rect, float left, float bottom, float right, float top)
    {
        if (rect == null)
            return;

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, -top);
    }

    private static void StretchTop(RectTransform rect, float left, float right, float top, float height)
    {
        if (rect == null)
            return;

        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.offsetMin = new Vector2(left, -top - height);
        rect.offsetMax = new Vector2(-right, -top);
    }

    private void ClearLootItems()
    {
        for (int i = 0; i < _lootItemViews.Count; i++)
        {
            if (_lootItemViews[i] != null)
                Destroy(_lootItemViews[i]);
        }

        _lootItemViews.Clear();
    }

    private void SetLootPanelVisible(bool visible)
    {
        if (_lootPanel != null)
            _lootPanel.gameObject.SetActive(visible);
    }

    private static void SetChildrenLayer(Transform root, int layer)
    {
        if (root == null)
            return;

        root.gameObject.layer = layer;
        for (int i = 0; i < root.childCount; i++)
            SetChildrenLayer(root.GetChild(i), layer);
    }

    private static void CloseOpenInventoryWindow()
    {
        InventoryWindow inventoryWindow = FindFirstObjectByType<InventoryWindow>(FindObjectsInactive.Include);
        if (inventoryWindow != null && inventoryWindow.IsOpen)
            inventoryWindow.Close();
    }

    private static void ClearPointerUiState()
    {
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
    }

    private void SuppressOtherUiRaycastsUnderBattleCanvas()
    {
        Canvas root = GetComponentInParent<Canvas>()?.rootCanvas;
        if (root == null)
            return;

        foreach (Graphic g in root.GetComponentsInChildren<Graphic>(true))
        {
            if (g == null)
                continue;
            Transform t = g.transform;
            if (t == transform || t.IsChildOf(transform))
                continue;
            g.raycastTarget = false;
        }
    }

    public void OnContinueAfterBattle()
    {
        if (_lastOutcomeWasVictory)
            ReturnAfterVictory();
        else
            RetryBattle();
    }

    public void RetryBattle()
    {
        if (_transitionStarted)
            return;

        _transitionStarted = true;
        gameObject.SetActive(false);

        FadeManager.TryLoadSceneWithoutFade(SceneNames.Battle);
    }

    public void LeaveBattleAfterDefeat()
    {
        if (_transitionStarted)
            return;

        _transitionStarted = true;
        gameObject.SetActive(false);

        PendingBattleEnemy.ClearSession();
        EnemyEncounter.ResetEncounterIds();
        FadeManager.TryFadeToScene(SceneNames.Exploration);
    }

    private void ReturnAfterVictory()
    {
        if (_transitionStarted)
            return;

        _transitionStarted = true;
        gameObject.SetActive(false);

        PendingBattleEnemy.ClearSession();
        EnemyEncounter.ResetEncounterIds();
        FadeManager.TryFadeToScene(SceneNames.Exploration);
    }

    private void ResolveButtons()
    {
        Button[] buttons = GetComponentsInChildren<Button>(true);
        if (retryButton == null && buttons.Length > 0)
            retryButton = buttons[0];
        if (retryButton != null)
            _retryButtonStartPosition = ((RectTransform)retryButton.transform).anchoredPosition;
        if (leaveButton == null && buttons.Length > 1)
            leaveButton = buttons[1];
        if (leaveButton == null && retryButton != null)
        {
            leaveButton = Instantiate(retryButton, retryButton.transform.parent);
            leaveButton.name = "LeaveBattleButton";
            leaveButton.onClick = new Button.ButtonClickedEvent();
            _createdLeaveButton = true;
            ArrangeCreatedButtons();
        }

        if (retryButtonText == null && retryButton != null)
            retryButtonText = retryButton.GetComponentInChildren<TMP_Text>(true);
        if (leaveButtonText == null && leaveButton != null)
            leaveButtonText = leaveButton.GetComponentInChildren<TMP_Text>(true);
    }

    private void WireButtons()
    {
        if (retryButton != null)
        {
            retryButton.onClick.RemoveListener(OnContinueAfterBattle);
            retryButton.onClick.RemoveListener(RetryBattle);
            retryButton.onClick.RemoveListener(ReturnAfterVictory);
            retryButton.onClick.AddListener(OnContinueAfterBattle);
        }

        if (leaveButton != null)
        {
            leaveButton.onClick.RemoveListener(LeaveBattleAfterDefeat);
            leaveButton.onClick.AddListener(LeaveBattleAfterDefeat);
        }
    }

    private void UnwireButtons()
    {
        if (retryButton != null)
        {
            retryButton.onClick.RemoveListener(OnContinueAfterBattle);
            retryButton.onClick.RemoveListener(RetryBattle);
            retryButton.onClick.RemoveListener(ReturnAfterVictory);
        }

        if (leaveButton != null)
            leaveButton.onClick.RemoveListener(LeaveBattleAfterDefeat);

        if (_lootCollectButton != null)
            _lootCollectButton.onClick.RemoveListener(OnContinueAfterBattle);
    }

    private void RefreshButtons(bool victory)
    {
        if (retryButton != null)
            retryButton.gameObject.SetActive(!victory);

        if (retryButtonText != null)
            retryButtonText.text = victory ? "Continue" : "Retry";

        if (leaveButton != null)
            leaveButton.gameObject.SetActive(!victory);

        if (leaveButtonText != null)
            leaveButtonText.text = "Leave";

        if (victory)
            CenterRetryButton();
        else
            ArrangeCreatedButtons();
    }

    private void ArrangeCreatedButtons()
    {
        if (!_createdLeaveButton || retryButton == null || leaveButton == null)
            return;

        RectTransform retryRect = retryButton.transform as RectTransform;
        RectTransform leaveRect = leaveButton.transform as RectTransform;
        if (retryRect == null || leaveRect == null)
            return;

        float spacing = retryRect.rect.width + 32f;
        retryRect.anchoredPosition = _retryButtonStartPosition + Vector2.left * (spacing * 0.5f);
        leaveRect.anchoredPosition = _retryButtonStartPosition + Vector2.right * (spacing * 0.5f);
    }

    private void CenterRetryButton()
    {
        if (retryButton == null)
            return;

        RectTransform retryRect = retryButton.transform as RectTransform;
        if (retryRect != null)
            retryRect.anchoredPosition = _retryButtonStartPosition;
    }

    private readonly struct CardLootStack
    {
        public CardLootStack(CardData card, int count)
        {
            Card = card;
            Count = count;
        }

        public CardData Card { get; }
        public int Count { get; }
    }
}
