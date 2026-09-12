using System.Collections.Generic;

public static class EnemyLootRoller
{
    public static List<CardData> Roll(EnemyData enemyData)
    {
        List<CardData> droppedCards = new();
        if (enemyData?.LootDrops == null)
            return droppedCards;

        IReadOnlyList<EnemyLootDrop> lootDrops = enemyData.LootDrops;
        for (int i = 0; i < lootDrops.Count; i++)
        {
            EnemyLootDrop drop = lootDrops[i];
            int amount = drop != null ? drop.RollAmount() : 0;

            for (int copy = 0; copy < amount; copy++)
                droppedCards.Add(drop.card);
        }

        return droppedCards;
    }
}
