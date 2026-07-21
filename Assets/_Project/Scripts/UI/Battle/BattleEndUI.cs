using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BattleEndUI : MonoBehaviour
{
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

    private void Awake()
    {
        ResolveButtons();
    }

    private void OnEnable()
    {
        _transitionStarted = false;
        WireButtons();
    }

    private void OnDisable()
    {
        UnwireButtons();
    }

    public void ShowVictory() => ShowEndScreen(true);

    public void ShowDefeat() => ShowEndScreen(false);

    private void ShowEndScreen(bool victory)
    {
        _lastOutcomeWasVictory = victory;
        gameObject.SetActive(true);
        transform.SetAsLastSibling();
        ClearPointerUiState();
        SuppressOtherUiRaycastsUnderBattleCanvas();
        if (resultText != null)
            resultText.text = victory ? "VICTORY" : "DEFEAT";

        RefreshButtons(victory);
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
    }

    private void RefreshButtons(bool victory)
    {
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
}
