using System.Collections.Generic;
using System.Linq;

public static class CollectedPickups
{
    private static readonly HashSet<string> collectedIds = new();

    public static void MarkCollected(string pickupId)
    {
        if (string.IsNullOrEmpty(pickupId))
            return;

        collectedIds.Add(pickupId);
    }

    public static bool IsCollected(string pickupId)
    {
        return !string.IsNullOrEmpty(pickupId) && collectedIds.Contains(pickupId);
    }

    // E.g. the spots of a ForageSpawner from before its things grew back.
    public static void ForgetWhere(System.Predicate<string> match)
    {
        collectedIds.RemoveWhere(match);
    }

    public static void Clear()
    {
        collectedIds.Clear();
    }

    public static string[] ToArray()
    {
        return collectedIds.ToArray();
    }

    public static void Restore(IEnumerable<string> pickupIds)
    {
        collectedIds.Clear();

        if (pickupIds == null)
            return;

        foreach (string pickupId in pickupIds)
        {
            if (!string.IsNullOrEmpty(pickupId))
                collectedIds.Add(pickupId);
        }
    }
}
