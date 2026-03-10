
public static class CardResolver
{
    public static void Resolve(CardData card, IEffectTarget source, IEffectTarget target)
    {
        if (card == null)
            return;

        foreach (var entry in card.Effects)
        {
            if (entry.effect == null)
                continue;

            entry.effect.Execute(source, target, entry.value);
        }
    }
}