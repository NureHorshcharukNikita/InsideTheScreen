using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

// What the Balance Config adds up to: for every enemy, how many turns a fight takes and how much of
// Haru's health it costs, alone or with his father, against one to three of them; what every card
// is worth and costs in shops; what every enemy's loot sells for. Change the config (or a card, an enemy) and
// the tables follow. Green = close to the targets in the config, yellow = off, red = far off or lost.
public sealed class BalanceReportWindow : EditorWindow
{
    private static readonly string[] Tabs = { "Fights", "Cards", "Enemies", "Skills" };
    private static readonly Color Good = new(0.55f, 0.85f, 0.55f);
    private static readonly Color Off = new(0.95f, 0.85f, 0.45f);
    private static readonly Color Bad = new(0.95f, 0.5f, 0.45f);

    private int tab;
    private int deckIndex = 1;
    private int skillPoints;
    private List<SkillData> skills = new();
    private List<QuestData> quests = new();
    private Vector2 scroll;
    private BalanceConfig config;
    private PlayerData player;
    private EnemyData father;
    private List<EnemyData> enemies = new();
    private List<CardData> cards = new();

    [MenuItem("Tools/Balance/Balance Report")]
    private static void Open()
    {
        GetWindow<BalanceReportWindow>("Balance Report").Show();
    }

    private void OnEnable()
    {
        Reload();
    }

    private void OnFocus()
    {
        Reload();
    }

    private void Reload()
    {
        config = Balance.Config;
        player = LoadAll<PlayerData>().FirstOrDefault(p => p.name == "PlayerData") ?? LoadAll<PlayerData>().FirstOrDefault();
        List<EnemyData> fighters = LoadAll<EnemyData>();
        father = fighters.FirstOrDefault(e => AssetDatabase.GetAssetPath(e).Contains("/Allies/"));
        enemies = fighters.Where(e => !AssetDatabase.GetAssetPath(e).Contains("/Allies/"))
            .OrderBy(e => e.maxHealth).ToList();
        cards = LoadAll<CardData>().Where(c => !AssetDatabase.GetAssetPath(c).Contains("/Test")).OrderBy(c => c.name).ToList();
        skills = LoadAll<SkillData>().OrderBy(s => s.Category).ThenBy(s => s.TreeOrder).ToList();
        quests = LoadAll<QuestData>().Where(q => !AssetDatabase.GetAssetPath(q).Contains("/Test")).ToList();
    }

    private int QuestSkillPoints => quests.Sum(q => Enumerable.Range(0, q.RewardCount).Sum(i => q.GetReward(i)?.SkillPoints ?? 0));
    private int TreeCost => skills.Sum(s => Enumerable.Range(1, s.MaxRank).Sum(r => s.GetRankData(r)?.Cost ?? 0));

    private void OnGUI()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            tab = GUILayout.Toolbar(tab, Tabs, EditorStyles.toolbarButton, GUILayout.Width(240f));
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Select Balance Config", EditorStyles.toolbarButton))
                SelectConfig();
        }

        scroll = EditorGUILayout.BeginScrollView(scroll);
        switch (tab)
        {
            case 0: DrawFights(); break;
            case 1: DrawCards(); break;
            case 2: DrawEnemies(); break;
            default: DrawSkills(); break;
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawFights()
    {
        List<BalanceConfig.ReferenceDeck> decks = config.referenceDecks;
        if (decks == null || decks.Count == 0)
        {
            EditorGUILayout.HelpBox("Add Reference Decks to the Balance Config: the cards Haru fights with early, mid and late.", MessageType.Info);
            return;
        }

        deckIndex = Mathf.Clamp(deckIndex, 0, decks.Count - 1);
        deckIndex = EditorGUILayout.Popup("Haru's deck", deckIndex, decks.Select(d => d.name).ToArray());
        int pointsInArc = QuestSkillPoints;
        skillPoints = EditorGUILayout.IntSlider(
            new GUIContent("Skill points spent", "Spent on the Reference Build in the Balance Config, in its order."),
            skillPoints, 0, Mathf.Max(1, pointsInArc));
        BalanceSimulator.SkillBonuses build = BalanceSimulator.SkillBonuses.FromBuild(config.referenceBuild, skillPoints);
        EditorGUILayout.LabelField(DescribeBuild(build), EditorStyles.wordWrappedMiniLabel);
        List<CardData> battleDeck = DealAsBattle(decks[deckIndex].cards, out int baseCardsAdded);
        BalanceSimulator.DeckProfile deck = BalanceSimulator.ProfileDeck(battleDeck);
        EditorGUILayout.LabelField(
            $"{battleDeck.Count} cards" + (baseCardsAdded > 0 ? $" ({baseCardsAdded} of them fists and legs)" : string.Empty) + ".  " +
            $"Per card played: {deck.damageToOne:0.#} damage, {deck.damageToAll:0.#} to all, {deck.defense:0.#} armor/healing, " +
            $"cost {deck.averageCost:0.#} AP.  Haru: {player?.maxHealth} HP, draws {player?.cardsDrawnPerTurn}/turn, " +
            $"+{player?.actionPointsPerTurn} AP/turn (start {player?.startActionPoints}, max {player?.maxActionPoints}).",
            EditorStyles.wordWrappedMiniLabel);
        EditorGUILayout.HelpBox(
            "What this page shows\n" +
            "An estimate of every fight, worked out from averages (not a real battle).\n" +
            "• Haru's deck: the cards Haru fights with, picked from the Reference Decks in the Balance Config. " +
            "A deck under the minimum gets his fists and legs, as in battle.\n" +
            "• Skill points spent: skills bought in the order of the Reference Build in the Balance Config.\n" +
            "• The line above: how strong the deck is on average per card, and Haru's numbers from Player Data.\n\n" +
            "How to read the table\n" +
            "• Row: the enemy. Column: how many enemies there are, and whether the father fights alongside Haru.\n" +
            "• \"5t / 13%\": the fight lasts 5 turns, Haru loses 13% of his health.\n" +
            "• \"(51%)\": how much health the father loses. 100% = he falls.\n" +
            "• \"LOST t4\": Haru dies on turn 4.\n\n" +
            "Color: how good the fight is\n" +
            $"• Green: good. Target: against 1 enemy ~{TargetTurns(1):0.#} turns and ~{TargetLoss(1):P0} health, " +
            $"against 2 ~{TargetTurns(2):0.#} and {TargetLoss(2):P0}, against 3 ~{TargetTurns(3):0.#} and {TargetLoss(3):P0}.\n" +
            "• Yellow: okay, but the fight is a bit long or short, or Haru loses a bit too much health.\n" +
            "• Red: bad. The fight drags on, Haru loses almost all his health, or dies.\n" +
            "The father's health doesn't change the color. Hover a cell to see its target and why it has that color.",
            MessageType.None);
        EditorGUILayout.Space();

        string[] headers = { "Enemy", "Alone x1", "Alone x2", "Alone x3", "With father x1", "With father x2", "With father x3" };
        using (new EditorGUILayout.HorizontalScope())
        {
            foreach (string header in headers)
                GUILayout.Label(header, EditorStyles.boldLabel, GUILayout.Width(header == "Enemy" ? 150f : 120f));
        }

        BalanceSimulator.FighterProfile fatherProfile = father != null ? BalanceSimulator.ProfileFighter(father) : null;
        foreach (EnemyData enemy in enemies)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label(enemy.DisplayName, GUILayout.Width(150f));
                for (int withFather = 0; withFather < 2; withFather++)
                {
                    for (int count = 1; count <= 3; count++)
                    {
                        float multiplier = Balance.EnemyHealthMultiplier(count, withFather == 1);
                        var group = Enumerable.Range(0, count).Select(_ => BalanceSimulator.ProfileFighter(enemy, multiplier)).ToList();
                        BalanceSimulator.Result result = BalanceSimulator.Simulate(player, deck, withFather == 1 ? fatherProfile : null, group, build);
                        DrawResult(result, count);
                    }
                }
            }
        }
    }

    // The deck as a battle deals it (DeckManager): one short of the minimum gets Haru's fists and
    // legs from Player Data, in their fill order.
    private List<CardData> DealAsBattle(IReadOnlyList<CardData> items, out int baseCardsAdded)
    {
        var battleDeck = items != null ? items.Where(card => card != null).ToList() : new List<CardData>();
        int itemCount = battleDeck.Count;
        if (player != null)
        {
            List<CardData> baseCards = player.GetBaseCardsInFillOrder();
            for (int i = 0; i < baseCards.Count && battleDeck.Count < player.minimumBattleDeckSize; i++)
                battleDeck.Add(baseCards[i]);
        }

        baseCardsAdded = battleDeck.Count - itemCount;
        return battleDeck;
    }

    private void DrawResult(BalanceSimulator.Result result, int enemyCount)
    {
        string text = result.won
            ? $"{result.turns}t / {result.haruHealthLost:P0}" + (result.fatherHealthLost >= 0f ? $" ({result.fatherHealthLost:P0})" : string.Empty)
            : $"LOST t{result.turns}";

        float targetTurns = TargetTurns(enemyCount);
        float targetLoss = TargetLoss(enemyCount);
        Color color;
        string verdict;
        if (!result.won)
        {
            color = Bad;
            verdict = "Haru dies before the enemies do.";
        }
        else if (result.turns > targetTurns * 1.8f)
        {
            color = Bad;
            verdict = "The fight drags on.";
        }
        else if (result.haruHealthLost > Mathf.Min(0.95f, targetLoss * 1.8f))
        {
            color = Bad;
            verdict = "Haru loses far too much health.";
        }
        else
        {
            var problems = new List<string>();
            if (result.turns > targetTurns * 1.35f)
                problems.Add("the fight is a bit long");
            else if (result.turns < targetTurns * 0.65f)
                problems.Add("the fight is a bit short");
            if (result.haruHealthLost > targetLoss * 1.3f)
                problems.Add("Haru loses a bit too much health");

            color = problems.Count == 0 ? Good : Off;
            verdict = problems.Count == 0 ? "Close to the target." : char.ToUpper(problems[0][0]) + string.Join(", ", problems).Substring(1) + ".";
        }

        string tooltip = $"Target against {enemyCount}: ~{targetTurns:0.#} turns and ~{targetLoss:P0} of Haru's health. {verdict}";

        Color previous = GUI.color;
        GUI.color = color;
        GUILayout.Label(new GUIContent(text, tooltip), EditorStyles.helpBox, GUILayout.Width(120f));
        GUI.color = previous;
    }

    // The targets grow with the number of enemies: a fight against three may take longer and cost more.
    private float TargetTurns(int enemyCount)
    {
        return config.targetTurnsStandard * (1f + 0.5f * (enemyCount - 1));
    }

    private float TargetLoss(int enemyCount)
    {
        return Mathf.Min(0.9f, config.targetHealthLost * (1f + 0.6f * (enemyCount - 1)));
    }

    private void DrawCards()
    {
        EditorGUILayout.HelpBox(
            "What this page shows\n" +
            "Every card: what it is worth in gold and what it costs in shops.\n\n" +
            "What the columns mean\n" +
            "• Type: the categories set in the card (Damage, Healing, Mind...). \"-\" = none set.\n" +
            "• AP: action points the card costs to play.\n" +
            "• Worth: what the card is worth in gold. It is worked out from the card's effects with the Balance Config numbers: " +
            $"1 damage = {config.goldPerDamage:0.##} gold, 1 armor = {config.goldPerArmor:0.##}, 1 healing = {config.goldPerHeal:0.##}, " +
            $"every AP above 1 takes {config.goldPerExtraActionPoint:0.##} off. A card with no battle effect is worth {config.valueWithoutEffects}.\n" +
            $"• Shop sells: what the player pays for the card in a shop (worth x{config.buyMarkup:0.##}).\n" +
            $"• Shop buys: what a shop pays the player for it (worth x{config.sellShare:0.##}). \"can't sell\" = e.g. the father's keepsakes. " +
            "Each shop can make both prices higher or lower.\n" +
            "• Worth/AP: worth for each action point: how strong the card is for its cost. Higher = better value.\n" +
            "• Own value: \"yes\" = the gold value is typed into the card itself and used instead; \"from effects\" = worked out as above.\n" +
            "• What it does: the card's effects, e.g. \"Damage 4\" or \"Burning 2, 3 turns\". " +
            "\"(all)\" = on every enemy or ally, \"(50%)\" = the chance it works.\n" +
            "Click a card's name to select it.",
            MessageType.None);
        EditorGUILayout.Space();

        string[] headers = { "Card", "Type", "AP", "Worth", "Shop sells", "Shop buys", "Worth/AP", "Own value", "What it does" };
        float[] widths = { 170f, 110f, 40f, 60f, 80f, 80f, 70f, 85f, 320f };
        DrawHeader(headers, widths);
        foreach (CardData card in cards)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                int worth = card.BaseGoldValue;
                bool own = new SerializedObject(card).FindProperty("baseGoldValue").intValue > 0;
                if (GUILayout.Button(card.name, EditorStyles.label, GUILayout.Width(widths[0])))
                    Selection.activeObject = card;
                string type = card.Categories.Count > 0 ? string.Join(", ", card.Categories) : "-";
                GUILayout.Label(new GUIContent(type, type), GUILayout.Width(widths[1]));
                GUILayout.Label(card.Cost.ToString(), GUILayout.Width(widths[2]));
                GUILayout.Label(worth.ToString(), GUILayout.Width(widths[3]));
                GUILayout.Label(Balance.BuyPrice(card).ToString(), GUILayout.Width(widths[4]));
                GUILayout.Label(Balance.CanSell(card) ? Balance.SellPrice(card).ToString() : "can't sell", GUILayout.Width(widths[5]));
                GUILayout.Label(card.Cost > 0 ? (worth / (float)card.Cost).ToString("0.#") : "-", GUILayout.Width(widths[6]));
                GUILayout.Label(own ? "yes" : "from effects", GUILayout.Width(widths[7]));
                string effects = DescribeEffects(card.InstantEffects, card.StatusApplications);
                GUILayout.Label(new GUIContent(effects, effects), GUILayout.Width(widths[8]));
            }
        }
    }

    // What a card does, from its effects rather than its description text (some cards have none).
    private static string DescribeEffects<TInstant, TStatus>(IReadOnlyList<TInstant> instants, IReadOnlyList<TStatus> statuses)
        where TInstant : class, IInstantEffectSpec
        where TStatus : class, IStatusApplicationSpec
    {
        var parts = new List<string>();
        if (instants != null)
        {
            foreach (TInstant spec in instants)
            {
                if (spec?.Effect != null)
                    parts.Add($"{spec.Effect.EffectName} {spec.Value}" + EffectNotes(spec.Targeting, spec.ApplyChance));
            }
        }

        if (statuses != null)
        {
            foreach (TStatus spec in statuses)
            {
                if (spec?.Status == null)
                    continue;

                string stacks = spec.Stacks > 1 ? $" x{spec.Stacks}" : string.Empty;
                string turns = spec.Duration > 0 ? $", {spec.Duration} turns" : string.Empty;
                parts.Add($"{spec.Status.StatusName} {spec.Value}{stacks}{turns}" + EffectNotes(spec.Targeting, spec.ApplyChance));
            }
        }

        return parts.Count > 0 ? string.Join(", ", parts) : "no battle effect";
    }

    private static string EffectNotes(BattleTargetingProfile targeting, float chance)
    {
        return (Balance.HitsEveryone(targeting) ? " (all)" : string.Empty)
            + (chance < 1f ? $" ({chance:P0})" : string.Empty);
    }

    private void DrawEnemies()
    {
        EditorGUILayout.HelpBox(
            "What this page shows\n" +
            "Every enemy: how strong it is on an average turn, and what beating it is worth. An average turn = its abilities " +
            "used as often as their weights say, less often with a cooldown, half as often when they need a condition.\n\n" +
            "What the columns mean\n" +
            "• HP: health, before the group and father multipliers in the Balance Config.\n" +
            "• Armor: armor it starts the battle with.\n" +
            "• AP: action points per turn: how many abilities it can use.\n" +
            "• Dmg/turn: damage it deals to one target on an average turn.\n" +
            "• Dmg all: damage it deals to everyone on the other side on an average turn.\n" +
            "• Guard/turn: armor and healing it gives itself and its side on an average turn.\n" +
            "• Loot sells: gold its loot would bring in a shop, on average (drop chance x amount x shop price). " +
            "Battles pay no gold, so this is all a fight is worth.\n" +
            "The father is listed last, as (ally). Click a name to select the enemy.",
            MessageType.None);
        EditorGUILayout.Space();

        string[] headers = { "Enemy", "HP", "Armor", "AP", "Dmg/turn", "Dmg all", "Guard/turn", "Loot sells" };
        float[] widths = { 150f, 40f, 50f, 35f, 70f, 60f, 75f, 70f };
        DrawHeader(headers, widths);
        foreach (EnemyData enemy in enemies.Concat(father != null ? new[] { father } : new EnemyData[0]))
        {
            BalanceSimulator.FighterProfile profile = BalanceSimulator.ProfileFighter(enemy);
            float lootValue = enemy.lootDrops?.Sum(drop => drop?.card == null ? 0f
                : drop.dropChancePercent / 100f * (drop.minAmount + drop.maxAmount) * 0.5f * Balance.SellPrice(drop.card)) ?? 0f;
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(enemy == father ? $"{enemy.DisplayName} (ally)" : enemy.DisplayName, EditorStyles.label, GUILayout.Width(widths[0])))
                    Selection.activeObject = enemy;
                GUILayout.Label(enemy.maxHealth.ToString(), GUILayout.Width(widths[1]));
                GUILayout.Label(enemy.startingArmor.ToString(), GUILayout.Width(widths[2]));
                GUILayout.Label(enemy.actionPointsPerTurn.ToString(), GUILayout.Width(widths[3]));
                GUILayout.Label(profile.damageToOne.ToString("0.#"), GUILayout.Width(widths[4]));
                GUILayout.Label(profile.damageToAll.ToString("0.#"), GUILayout.Width(widths[5]));
                GUILayout.Label((profile.selfDefense + profile.supportHealing).ToString("0.#"), GUILayout.Width(widths[6]));
                GUILayout.Label(lootValue.ToString("0.#"), GUILayout.Width(widths[7]));
            }
        }
    }

    // Every skill, rank by rank: what it costs and what it gives, and whether Arc 1 pays for a build.
    private void DrawSkills()
    {
        int questPoints = QuestSkillPoints;
        int treeCost = TreeCost;
        EditorGUILayout.LabelField(
            $"Skill points in Arc 1: {questPoints}, all from quests.  " +
            $"The whole tree costs {treeCost}, so Arc 1 pays for {questPoints / (float)Mathf.Max(1, treeCost):P0} of it.",
            EditorStyles.wordWrappedLabel);
        EditorGUILayout.HelpBox(
            "What this page shows\n" +
            "Every skill: what it costs and what it gives, and whether the skill points from quests are enough for the tree.\n\n" +
            "What the columns mean\n" +
            "• Branch: which part of the skill tree the skill is in.\n" +
            "• Rank costs: skill points for each rank in turn, e.g. 1 / 2 / 3. Total: points for all ranks.\n" +
            "• Rank 1 gives: what the first rank gives. Ranks stack: every next rank adds its own bonus on top " +
            "(the skill itself has the exact numbers).\n" +
            "• \"passive\" = a special ability rather than a number, e.g. Will to Live.\n" +
            "• \"(percent: avoid)\" = a percent bonus. Battle numbers are small, so percents round away to nothing: use flat numbers.\n" +
            "Click a skill's name to select it.",
            MessageType.None);
        EditorGUILayout.Space();

        string[] headers = { "Skill", "Branch", "Rank costs", "Total", "Rank 1 gives" };
        float[] widths = { 150f, 120f, 90f, 45f, 420f };
        DrawHeader(headers, widths);
        foreach (SkillData skill in skills)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                var costs = Enumerable.Range(1, skill.MaxRank).Select(r => skill.GetRankData(r)?.Cost ?? 0).ToList();
                if (GUILayout.Button(skill.DisplayName, EditorStyles.label, GUILayout.Width(widths[0])))
                    Selection.activeObject = skill;
                GUILayout.Label(skill.Category.ToString(), GUILayout.Width(widths[1]));
                GUILayout.Label(string.Join(" / ", costs), GUILayout.Width(widths[2]));
                GUILayout.Label(costs.Sum().ToString(), GUILayout.Width(widths[3]));
                GUILayout.Label(DescribeRank(skill.GetRankData(1)), GUILayout.Width(widths[4]));
            }
        }
    }

    private static string DescribeRank(SkillRankData rank)
    {
        if (rank == null)
            return "-";

        return string.Join(", ", rank.Effects.Where(e => e != null).Select(e => e.Kind == SkillEffectKind.Passive
            ? $"passive {e.Passive}"
            : $"{e.Stat} {(e.Value >= 0 ? "+" : string.Empty)}{e.Value}{(e.ModifierType == SkillModifierType.Percent ? "% (percent: avoid)" : string.Empty)}"));
    }

    private static string DescribeBuild(BalanceSimulator.SkillBonuses build)
    {
        if (build.pointsSpent == 0)
            return "No skills.";

        IEnumerable<string> parts = build.stats.Where(s => s.Value != 0f).Select(s => $"{s.Key} {(s.Value > 0 ? "+" : string.Empty)}{s.Value}")
            .Concat(build.passives.Select(p => p.ToString()));
        return $"{build.pointsSpent} points: " + string.Join(", ", parts);
    }

    private static void DrawHeader(string[] headers, float[] widths)
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            for (int i = 0; i < headers.Length; i++)
                GUILayout.Label(headers[i], EditorStyles.boldLabel, GUILayout.Width(widths[i]));
        }
    }

    private void SelectConfig()
    {
        BalanceConfig asset = Resources.Load<BalanceConfig>(BalanceConfig.ResourcePath);
        if (asset != null)
            Selection.activeObject = asset;
        else
            EditorUtility.DisplayDialog("Balance Report", "There is no Resources/BalanceConfig asset yet.", "OK");
    }

    private static List<T> LoadAll<T>() where T : Object
    {
        return AssetDatabase.FindAssets($"t:{typeof(T).Name}")
            .Select(AssetDatabase.GUIDToAssetPath)
            .Select(AssetDatabase.LoadAssetAtPath<T>)
            .Where(asset => asset != null)
            .ToList();
    }
}
