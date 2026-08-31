using System.Globalization;

public static class BattleEffectDisplayFormatter
{
    public static string Format(
        string template,
        int value,
        int duration,
        int stacks)
    {
        return Format(template, value, duration, stacks, value);
    }

    public static string Format(
        string template,
        int value,
        int duration,
        int stacks,
        int badgeValue)
    {
        if (string.IsNullOrWhiteSpace(template))
            return string.Empty;

        int total = value * stacks;
        return template
            .Replace("{value}", value.ToString(CultureInfo.InvariantCulture))
            .Replace("{total}", total.ToString(CultureInfo.InvariantCulture))
            .Replace("{duration}", duration.ToString(CultureInfo.InvariantCulture))
            .Replace("{stacks}", stacks.ToString(CultureInfo.InvariantCulture))
            .Replace("{badge}", badgeValue.ToString(CultureInfo.InvariantCulture));
    }
}
