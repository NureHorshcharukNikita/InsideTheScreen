using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class SettingsKeyBindingRow : MonoBehaviour
{
    [SerializeField] private GameInputAction action;
    [SerializeField] private TMP_Text actionLabel;
    [SerializeField] private TMP_Text keyLabel;
    [SerializeField] private Button rebindButton;
    [SerializeField] private string waitingText = "Press key...";
    [SerializeField] private string combinedLabelFormat = "{0}: {1}";

    private static readonly KeyCode[] KeyCodes = (KeyCode[])Enum.GetValues(typeof(KeyCode));

    private bool isWaitingForKey;
    private bool skipCurrentFrame;

    private void Awake()
    {
        WireRebindButton();
        Refresh();
    }

    private void OnEnable()
    {
        GameInputSettings.Changed += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        GameInputSettings.Changed -= Refresh;
        isWaitingForKey = false;
    }

    private void Update()
    {
        if (!isWaitingForKey)
            return;

        if (skipCurrentFrame)
        {
            skipCurrentFrame = false;
            return;
        }

        if (Input.GetKeyDown(KeyCode.Backspace) || Input.GetKeyDown(KeyCode.Delete))
        {
            GameInputSettings.SetKey(action, KeyCode.None);
            isWaitingForKey = false;
            Refresh();
            ClearSelectedButton();
            return;
        }

        foreach (KeyCode keyCode in KeyCodes)
        {
            if (!IsBindableKeyboardKey(keyCode))
                continue;

            if (!Input.GetKeyDown(keyCode))
                continue;

            GameInputSettings.SetKey(action, keyCode);
            isWaitingForKey = false;
            Refresh();
            ClearSelectedButton();
            return;
        }
    }

    public void BeginRebind()
    {
        isWaitingForKey = true;
        skipCurrentFrame = true;
        SelectRebindButton();

        if (keyLabel != null)
            keyLabel.text = actionLabel == null
                ? string.Format(combinedLabelFormat, GetActionName(action), waitingText)
                : waitingText;
    }

    public void Refresh()
    {
        if (actionLabel != null)
            actionLabel.text = GetActionName(action);

        if (keyLabel != null && !isWaitingForKey)
        {
            string keyName = GameInputSettings.GetDisplayName(action);
            keyLabel.text = actionLabel == null
                ? string.Format(combinedLabelFormat, GetActionName(action), keyName)
                : keyName;
        }
    }

    public void Configure(GameInputAction inputAction, TMP_Text inputActionLabel, TMP_Text inputKeyLabel, Button inputRebindButton)
    {
        action = inputAction;
        actionLabel = inputActionLabel;
        keyLabel = inputKeyLabel;
        rebindButton = inputRebindButton;

        WireRebindButton();
        Refresh();
    }

    private void SelectRebindButton()
    {
        if (EventSystem.current == null || rebindButton == null)
            return;

        EventSystem.current.SetSelectedGameObject(rebindButton.gameObject);
    }

    private static void ClearSelectedButton()
    {
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
    }

    private void WireRebindButton()
    {
        if (rebindButton == null)
            return;

        rebindButton.onClick.RemoveListener(BeginRebind);
        rebindButton.onClick.AddListener(BeginRebind);
    }

    private static string GetActionName(GameInputAction inputAction)
    {
        return inputAction switch
        {
            GameInputAction.MoveUp => "Move Up",
            GameInputAction.MoveDown => "Move Down",
            GameInputAction.MoveLeft => "Move Left",
            GameInputAction.MoveRight => "Move Right",
            GameInputAction.Interact => "Interact",
            GameInputAction.Inventory => "Inventory",
            GameInputAction.EndTurn => "End Turn",
            GameInputAction.Close => "Pause",
            _ => inputAction.ToString()
        };
    }

    private static bool IsBindableKeyboardKey(KeyCode keyCode)
    {
        if (keyCode == KeyCode.None)
            return false;

        return (int)keyCode < (int)KeyCode.Mouse0;
    }
}
