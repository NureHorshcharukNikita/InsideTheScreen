using System.Globalization;
using System.Collections.Generic;

public static class CardDescriptionFormatter
{
    public static string Format(CardData card, string template)
    {
        if (card == null || string.IsNullOrWhiteSpace(template))
            return string.Empty;

        return BattleDescriptionFormatter.Format(template, card.InstantEffects, card.StatusApplications);
    }
}

public static class BattleDescriptionFormatter
{
    public static string Format<TInstant, TStatus>(
        string template,
        IReadOnlyList<TInstant> instantEffects,
        IReadOnlyList<TStatus> statusApplications)
        where TInstant : class, IInstantEffectSpec
        where TStatus : class, IStatusApplicationSpec
    {
        if (string.IsNullOrWhiteSpace(template))
            return string.Empty;

        TInstant instant = GetFirst(instantEffects);
        TStatus status = GetFirst(statusApplications);

        int primaryValue = instant?.Value ?? status?.Value ?? 0;
        string result = template
            .Replace("{value}", FormatInt(primaryValue))
            .Replace("{duration}", FormatInt(status?.Duration ?? 0))
            .Replace("{stacks}", FormatInt(status?.Stacks ?? 1))
            .Replace("{chance}", FormatPercent(instant?.ApplyChance ?? status?.ApplyChance ?? 1f))
            .Replace("{effect}", instant?.Effect != null ? instant.Effect.EffectName : string.Empty)
            .Replace("{status}", status?.Status != null ? status.Status.StatusName : string.Empty);

        if (instantEffects != null)
        {
            for (int i = 0; i < instantEffects.Count; i++)
                result = FormatInstantEffect(result, i, instantEffects[i]);
        }

        if (statusApplications != null)
        {
            for (int i = 0; i < statusApplications.Count; i++)
                result = FormatStatusApplication(result, i, statusApplications[i]);
        }

        return result;
    }

    private static string FormatInstantEffect<TInstant>(string text, int index, TInstant entry)
        where TInstant : class, IInstantEffectSpec
    {
        string prefix = "{instant" + index.ToString(CultureInfo.InvariantCulture) + ".";
        return text
            .Replace(prefix + "value}", FormatInt(entry?.Value ?? 0))
            .Replace(prefix + "chance}", FormatPercent(entry?.ApplyChance ?? 1f))
            .Replace(prefix + "effect}", entry?.Effect != null ? entry.Effect.EffectName : string.Empty);
    }

    private static string FormatStatusApplication<TStatus>(string text, int index, TStatus entry)
        where TStatus : class, IStatusApplicationSpec
    {
        string prefix = "{status" + index.ToString(CultureInfo.InvariantCulture) + ".";
        return text
            .Replace(prefix + "value}", FormatInt(entry?.Value ?? 0))
            .Replace(prefix + "duration}", FormatInt(entry?.Duration ?? 0))
            .Replace(prefix + "stacks}", FormatInt(entry?.Stacks ?? 1))
            .Replace(prefix + "chance}", FormatPercent(entry?.ApplyChance ?? 1f))
            .Replace(prefix + "status}", entry?.Status != null ? entry.Status.StatusName : string.Empty);
    }

    private static T GetFirst<T>(System.Collections.Generic.IReadOnlyList<T> entries)
        where T : class
    {
        return entries != null && entries.Count > 0 ? entries[0] : null;
    }

    private static string FormatInt(int value)
    {
        return value.ToString(CultureInfo.InvariantCulture);
    }

    private static string FormatPercent(float chance)
    {
        return (chance * 100f).ToString("0.#", CultureInfo.InvariantCulture) + "%";
    }
}
