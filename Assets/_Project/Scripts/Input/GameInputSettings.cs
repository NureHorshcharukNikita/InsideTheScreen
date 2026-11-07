using System;
using System.Collections.Generic;
using UnityEngine;

public static class GameInputSettings
{
    private const string PlayerPrefsPrefix = "Input.";

    private static readonly Dictionary<GameInputAction, KeyCode> DefaultKeys = new()
    {
        { GameInputAction.MoveUp, KeyCode.W },
        { GameInputAction.MoveDown, KeyCode.S },
        { GameInputAction.MoveLeft, KeyCode.A },
        { GameInputAction.MoveRight, KeyCode.D },
        { GameInputAction.Interact, KeyCode.E },
        { GameInputAction.Inventory, KeyCode.I },
        { GameInputAction.EndTurn, KeyCode.Space },
        { GameInputAction.Close, KeyCode.Escape },
        { GameInputAction.Quests, KeyCode.J },
        { GameInputAction.Crafting, KeyCode.C },
        { GameInputAction.Map, KeyCode.M },
        { GameInputAction.Skills, KeyCode.K },
        { GameInputAction.DialogueNext, KeyCode.Space },
        { GameInputAction.DialogueSkip, KeyCode.Tab }
    };

    // The rows of the Keyboard Bindings list, top to bottom.
    public static readonly GameInputAction[] SettingsOrder =
    {
        GameInputAction.MoveUp,
        GameInputAction.MoveDown,
        GameInputAction.MoveLeft,
        GameInputAction.MoveRight,
        GameInputAction.Interact,
        GameInputAction.Inventory,
        GameInputAction.Quests,
        GameInputAction.Crafting,
        GameInputAction.Map,
        GameInputAction.Skills,
        GameInputAction.DialogueNext,
        GameInputAction.DialogueSkip,
        GameInputAction.EndTurn,
        GameInputAction.Close
    };

    // Where a key is read. Actions never read at the same time can share a key, e.g. End Turn in
    // battle and Next Line in a dialogue, both on Space.
    [Flags]
    private enum InputContext
    {
        Exploration = 1,
        Battle = 2,
        Dialogue = 4
    }

    private static readonly Dictionary<GameInputAction, KeyCode> Keys = new();

    static GameInputSettings()
    {
        Load();
    }

    public static event Action Changed;

    public static KeyCode GetKey(GameInputAction action)
    {
        EnsureLoaded(action);
        return Keys[action];
    }

    public static bool GetKeyDown(GameInputAction action)
    {
        return Input.GetKeyDown(GetKey(action));
    }

    public static bool GetKeyHeld(GameInputAction action)
    {
        return Input.GetKey(GetKey(action));
    }

    public static Vector2 GetMovement()
    {
        float x = 0f;
        float y = 0f;

        if (Input.GetKey(GetKey(GameInputAction.MoveLeft)))
            x -= 1f;
        if (Input.GetKey(GetKey(GameInputAction.MoveRight)))
            x += 1f;
        if (Input.GetKey(GetKey(GameInputAction.MoveDown)))
            y -= 1f;
        if (Input.GetKey(GetKey(GameInputAction.MoveUp)))
            y += 1f;

        return new Vector2(x, y).normalized;
    }

    public static void SetKey(GameInputAction action, KeyCode key)
    {
        if (key != KeyCode.None)
            ClearDuplicateBindings(action, key);

        Keys[action] = key;
        PlayerPrefs.SetInt(GetPrefsKey(action), (int)key);
        PlayerPrefs.Save();
        Changed?.Invoke();
    }

    public static void ResetToDefaults()
    {
        foreach (var defaultKey in DefaultKeys)
        {
            Keys[defaultKey.Key] = defaultKey.Value;
            PlayerPrefs.DeleteKey(GetPrefsKey(defaultKey.Key));
        }

        PlayerPrefs.Save();
        Changed?.Invoke();
    }

    public static string GetDisplayName(GameInputAction action)
    {
        KeyCode key = GetKey(action);
        return key == KeyCode.None ? "None" : key.ToString();
    }

    private static void Load()
    {
        foreach (var defaultKey in DefaultKeys)
        {
            int savedKey = PlayerPrefs.GetInt(GetPrefsKey(defaultKey.Key), (int)defaultKey.Value);
            Keys[defaultKey.Key] = (KeyCode)savedKey;
        }

        // An action added after the player rebound keys gets its default key only while it is free,
        // e.g. Crafting stays unbound when C already does something else.
        bool cleared = false;
        foreach (var defaultKey in DefaultKeys)
        {
            if (PlayerPrefs.HasKey(GetPrefsKey(defaultKey.Key)) || !IsTakenByAnother(defaultKey.Key, defaultKey.Value))
                continue;

            Keys[defaultKey.Key] = KeyCode.None;
            PlayerPrefs.SetInt(GetPrefsKey(defaultKey.Key), (int)KeyCode.None);
            cleared = true;
        }

        if (cleared)
            PlayerPrefs.Save();
    }

    private static bool IsTakenByAnother(GameInputAction action, KeyCode key)
    {
        foreach (var binding in Keys)
        {
            if (binding.Key != action && binding.Value == key && ShareContext(action, binding.Key))
                return true;
        }

        return false;
    }

    private static bool ShareContext(GameInputAction first, GameInputAction second)
    {
        return (GetContext(first) & GetContext(second)) != 0;
    }

    private static InputContext GetContext(GameInputAction action)
    {
        return action switch
        {
            GameInputAction.EndTurn => InputContext.Battle,
            GameInputAction.DialogueNext or GameInputAction.DialogueSkip => InputContext.Dialogue,
            // The inventory window and the pause menu open in battle too.
            GameInputAction.Inventory or GameInputAction.Quests or GameInputAction.Crafting
                or GameInputAction.Map or GameInputAction.Skills or GameInputAction.Close
                => InputContext.Exploration | InputContext.Battle,
            _ => InputContext.Exploration
        };
    }

    private static void EnsureLoaded(GameInputAction action)
    {
        if (Keys.ContainsKey(action))
            return;

        Keys[action] = DefaultKeys.TryGetValue(action, out KeyCode defaultKey)
            ? defaultKey
            : KeyCode.None;
    }

    private static string GetPrefsKey(GameInputAction action)
    {
        return PlayerPrefsPrefix + action;
    }

    private static void ClearDuplicateBindings(GameInputAction action, KeyCode key)
    {
        foreach (GameInputAction inputAction in Enum.GetValues(typeof(GameInputAction)))
        {
            if (inputAction == action)
                continue;

            EnsureLoaded(inputAction);

            if (Keys[inputAction] != key || !ShareContext(action, inputAction))
                continue;

            Keys[inputAction] = KeyCode.None;
            PlayerPrefs.SetInt(GetPrefsKey(inputAction), (int)KeyCode.None);
        }
    }
}
