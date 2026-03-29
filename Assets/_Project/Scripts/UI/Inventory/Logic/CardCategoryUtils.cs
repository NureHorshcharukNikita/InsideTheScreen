using System;
using System.Linq;

public static class CardCategoryUtils
{
    public static bool HasCategory(CardData card, Type category)
    {
        if (category == null)
            return true;

        return card.Effects.Any(e => e.effect != null && e.effect.GetType() == category);
    }
}