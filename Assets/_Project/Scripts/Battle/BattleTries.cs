using System.Collections.Generic;

// How many times in a row each encounter has been lost this session, e.g. so Dad does not explain the
// first fight all over again on a retry, and the wolf runs off after the third loss (BattleTalkData).
// Not saved: a new session starts over.
public static class BattleTries
{
    private static readonly Dictionary<string, int> Losses = new();

    public static int LossesOf(string encounterId)
    {
        return !string.IsNullOrWhiteSpace(encounterId) && Losses.TryGetValue(encounterId, out int losses) ? losses : 0;
    }

    public static int NoteLoss(string encounterId)
    {
        if (string.IsNullOrWhiteSpace(encounterId))
            return 0;

        int losses = LossesOf(encounterId) + 1;
        Losses[encounterId] = losses;
        return losses;
    }

    public static void Forget(string encounterId)
    {
        if (!string.IsNullOrWhiteSpace(encounterId))
            Losses.Remove(encounterId);
    }
}
