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
        { GameInputAction.Close, KeyCode.Escape }
    };

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

            if (Keys[inputAction] != key)
                continue;

            Keys[inputAction] = KeyCode.None;
            PlayerPrefs.SetInt(GetPrefsKey(inputAction), (int)KeyCode.None);
        }
    }
}
