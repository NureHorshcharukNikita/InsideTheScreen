using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

// The names the player has heard. A character with Names Heard (CharacterProfile) is "..." on the
// dialogue name plate and in the quest log until a dialogue line says one of the words or phrases
// of the first step, then shows what the last step heard says: e.g. the father is "..." until Haru
// says "Dad", "Father" until he introduces himself to Garr ("I'm Hideshi"), then Hideshi. A step
// heard on a line shows from the next line on; the speaker's own name on their plate as soon as the
// line types it out, e.g. Sorrel's at "I'm Sorrel" (SaidAt). Remembered as story flags, so saved with them.
public static class KnownNames
{
    public const string Unknown = "...";
    private const string FlagPrefix = "name_known_";

    // The name as the player knows it by now: the name itself, what a step shows instead, or "...".
    public static string Shown(CharacterProfile profile, string name)
    {
        List<CharacterProfile.HeardName> steps = profile != null ? profile.namesHeard : null;
        if (steps == null || steps.Count == 0)
            return name;

        for (int i = steps.Count - 1; i >= 0; i--)
        {
            if (StoryFlagManager.HasFlag(StepFlag(profile, i)))
                return steps[i] == null || string.IsNullOrWhiteSpace(steps[i].shownAs) ? name : steps[i].shownAs;
        }

        return Unknown;
    }

    // A name written somewhere, e.g. a quest's giver: whose it is, by their whole name.
    public static string Shown(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return name;

        foreach (CharacterProfile profile in Resources.FindObjectsOfTypeAll<CharacterProfile>())
        {
            if (string.Equals(profile.DisplayName, name.Trim(), StringComparison.OrdinalIgnoreCase))
                return Shown(profile, name);
        }

        return name;
    }

    // Every step of a name the line says becomes known.
    public static void Hear(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        foreach (CharacterProfile profile in Resources.FindObjectsOfTypeAll<CharacterProfile>())
        {
            List<CharacterProfile.HeardName> steps = profile.namesHeard;
            if (steps == null)
                continue;

            for (int i = 0; i < steps.Count; i++)
            {
                if (steps[i] != null && steps[i].heardAs != null && steps[i].heardAs.Exists(words => Says(text, words)))
                    StoryFlagManager.SetFlag(StepFlag(profile, i));
            }
        }
    }

    // Where a line makes more of someone's name known, counted in the letters the dialogue types out
    // (rich-text tags left out), and how it shows from there on, e.g. "Sorrel" right after "I'm Sorrel"
    // on her own line. -1 when the line makes no more of it known.
    public static int SaidAt(CharacterProfile profile, string name, string text, out string shownThen)
    {
        shownThen = null;
        List<CharacterProfile.HeardName> steps = profile != null ? profile.namesHeard : null;
        if (steps == null || string.IsNullOrWhiteSpace(text))
            return -1;

        int known = -1;
        for (int i = steps.Count - 1; i >= 0 && known < 0; i--)
        {
            if (StoryFlagManager.HasFlag(StepFlag(profile, i)))
                known = i;
        }

        // The last step the line says is the one that shows, from the end of its words on.
        for (int i = steps.Count - 1; i > known; i--)
        {
            int end = EndOfWords(text, steps[i]);
            if (end < 0)
                continue;

            shownThen = string.IsNullOrWhiteSpace(steps[i].shownAs) ? name : steps[i].shownAs;
            return TypedLetters(text, end);
        }

        return -1;
    }

    // Where the first of a step's words or phrases ends in the text; -1 when none is said.
    private static int EndOfWords(string text, CharacterProfile.HeardName step)
    {
        if (step?.heardAs == null)
            return -1;

        int first = -1;
        foreach (string words in step.heardAs)
        {
            if (string.IsNullOrWhiteSpace(words))
                continue;

            Match match = Regex.Match(text, $@"\b{Regex.Escape(words.Trim())}\b", RegexOptions.IgnoreCase);
            if (match.Success && (first < 0 || match.Index + match.Length < first))
                first = match.Index + match.Length;
        }

        return first;
    }

    private static int TypedLetters(string text, int end)
    {
        int letters = 0;
        bool inTag = false;
        for (int i = 0; i < end && i < text.Length; i++)
        {
            if (text[i] == '<')
                inTag = true;
            else if (text[i] == '>' && inTag)
                inTag = false;
            else if (!inTag)
                letters++;
        }

        return letters;
    }

    private static string StepFlag(CharacterProfile profile, int step)
    {
        return $"{FlagPrefix}{profile.CharacterID}_{step}";
    }

    // As whole words: Garr in "Garr's forge", not in "Garrison".
    private static bool Says(string text, string words)
    {
        return !string.IsNullOrWhiteSpace(words)
            && Regex.IsMatch(text, $@"\b{Regex.Escape(words.Trim())}\b", RegexOptions.IgnoreCase);
    }
}
