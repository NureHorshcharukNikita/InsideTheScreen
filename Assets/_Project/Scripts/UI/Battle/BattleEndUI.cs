using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BattleEndUI : MonoBehaviour
{
    public TMP_Text resultText;

    private bool _lastOutcomeWasVictory;
    private bool _transitionStarted;
    private Button _continueButton;

    private void Awake()
    {
        _continueButton = GetComponentInChildren<Button>(true);
    }

    private void OnEnable()
    {
        _transitionStarted = false;

        if (_continueButton != null)
        {
            _continueButton.onClick.RemoveListener(OnContinueAfterBattle);
            _continueButton.onClick.AddListener(OnContinueAfterBattle);
        }
    }

    private void OnDisable()
    {
        if (_continueButton != null)
            _continueButton.onClick.RemoveListener(OnContinueAfterBattle);
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
        if (_transitionStarted)
            return;

        _transitionStarted = true;
        gameObject.SetActive(false);

        if (_lastOutcomeWasVictory)
        {
            string returnTo = PendingBattleEnemy.ReturnSceneName;
            PendingBattleEnemy.ClearSession();
            FadeManager.TryFadeToScene(returnTo);
        }
        else
            FadeManager.TryLoadSceneWithoutFade(SceneNames.Battle);
    }
}
