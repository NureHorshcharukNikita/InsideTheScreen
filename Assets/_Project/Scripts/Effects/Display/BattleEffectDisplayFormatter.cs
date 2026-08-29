using System.Globalization;

public static class BattleEffectDisplayFormatter
{
    public static string Format(
        string template,
        int value,
        int duration,
        int stacks)
    {
        if (string.IsNullOrWhiteSpace(template))
            return string.Empty;

        return template
            .Replace("{value}", value.ToString(CultureInfo.InvariantCulture))
            .Replace("{duration}", duration.ToString(CultureInfo.InvariantCulture))
            .Replace("{stacks}", stacks.ToString(CultureInfo.InvariantCulture));
    }
}
