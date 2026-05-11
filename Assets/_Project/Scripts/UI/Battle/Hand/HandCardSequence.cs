using System.Collections.Generic;
using UnityEngine;

public static class HandCardSequence
{
    public static bool TryComputeSuffixDraw(
        IReadOnlyList<CardData> prev, 
        IReadOnlyList<CardData> next, 
        out int drawCount)
    {
        drawCount = 0;
        if (next == null || next.Count == 0)
            return false;

        if (prev == null || prev.Count == 0)
        {
            drawCount = next.Count;
            return true;
        }

        if (next.Count <= prev.Count)
            return false;

        for (int i = 0; i < prev.Count; i++)
        {
            if (prev[i] != next[i])
                return false;
        }

        drawCount = next.Count - prev.Count;
        return true;
    }

    public static void CopySnapshot(IReadOnlyList<CardData> hand, List<CardData> destination)
    {
        destination.Clear();
        for (int i = 0; i < hand.Count; i++)
            destination.Add(hand[i]);
    }
}
