using System.Linq;

public static class CardCategoryUtils
{
    public static bool HasCategory(CardData card, string category)
    {
        if (string.IsNullOrWhiteSpace(category))
            return true;

        return card.Categories.Any(cardCategory => string.Equals(
            cardCategory?.Trim(),
            category.Trim(),
            System.StringComparison.OrdinalIgnoreCase));
    }
}
