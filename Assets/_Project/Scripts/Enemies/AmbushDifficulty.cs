using UnityEngine;

// How many enemies spring out of the bushes in the next ambush on a forest road (AmbushZone). It
// follows how strong Haru has grown - the skill points he has earned and what the cards in his deck
// are worth - so the roads are easy at first, then harder, then hard; and a rhythm over the battles
// he wins adds a breather now and then and a harder one after it, so a road never plays the same
// twice. The numbers are in the Balance Config.
public static class AmbushDifficulty
{
    public const int MaxEnemies = PendingBattleEnemy.MaxPerSide;

    // Skill points earned plus the worth of the battle deck, in the Balance Config's units.
    public static float HeroPower
    {
        get
        {
            BalanceConfig config = Balance.Config;
            float power = SkillPointsEarned() * config.powerPerSkillPoint;

            DeckData deck = ExplorationPlayerSession.RuntimeDeck;
            if (deck != null)
            {
                int deckGold = 0;
                foreach (CardData card in deck.Cards)
                    deckGold += Balance.CardValue(card);

                power += deckGold / Mathf.Max(1f, config.deckGoldPerPower);
            }

            return power;
        }
    }

    // How many enemies the next ambush brings, 1 to 3.
    public static int NextGroupSize()
    {
        BalanceConfig config = Balance.Config;
        float power = HeroPower;
        int size = power >= config.powerForThreeEnemies ? 3 : power >= config.powerForTwoEnemies ? 2 : 1;

        if (config.ambushRhythm != null && config.ambushRhythm.Count > 0)
            size += config.ambushRhythm[DefeatedEncounters.BattlesWon % config.ambushRhythm.Count];

        return Mathf.Clamp(size, 1, MaxEnemies);
    }

    private static int SkillPointsEarned()
    {
        int points = SkillProgression.AvailablePoints;
        foreach (SkillData skill in SkillProgression.Skills)
        {
            int rank = SkillProgression.GetRank(skill);
            for (int i = 1; i <= rank; i++)
                points += skill.GetRankData(i)?.Cost ?? 0;
        }

        return points;
    }
}
