using System.Linq;

public static class CardCategoryUtils
{
    public static bool HasCategory(CardData card, CardCategory? category)
    {
        if (!category.HasValue)
            return true;

        return card.Categories.Any(cardCategory => cardCategory == category.Value);
    }
}
