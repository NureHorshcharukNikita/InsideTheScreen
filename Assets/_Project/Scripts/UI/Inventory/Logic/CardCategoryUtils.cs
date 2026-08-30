using System;
using System.Linq;

public static class CardCategoryUtils
{
    public static bool HasCategory(CardData card, Type category)
    {
        if (category == null)
            return true;

        return card.InstantEffects.Any(effectEntry => effectEntry.effect != null && effectEntry.effect.GetType() == category)
            || card.StatusApplications.Any(statusEntry => statusEntry.status != null && statusEntry.status.GetType() == category);
    }
}
