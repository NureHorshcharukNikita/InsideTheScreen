using System.Collections.Generic;
using System.Text;
using UnityEngine;

public static class BattleDebugPrinter
{
    public static void PrintCards(string title, IReadOnlyList<CardData> cards)
    {
        StringBuilder builder = new();
        builder.AppendLine(title + ":");

        if (cards.Count == 0)
        {
            builder.AppendLine("(empty)");
            DevLog.Log(builder.ToString());
            return;
        }

        int displayIndex = 1;

        for (int i = cards.Count - 1; i >= 0; i--)
        {
            builder.AppendLine($"{displayIndex}: {cards[i].CardName}");
            displayIndex++;
        }

        DevLog.Log(builder.ToString());
    }
}