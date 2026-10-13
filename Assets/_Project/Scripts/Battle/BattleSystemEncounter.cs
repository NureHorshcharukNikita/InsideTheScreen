using System.Collections.Generic;
using UnityEngine;

// Sets up the fighters of the pending encounter (PendingBattleEnemy). The scene's enemy takes the
// first enemy's data; the other enemies and the allies are copies of it, each with its own HP bar
// and intent, copied from the scene's enemy ones.
internal sealed class BattleEncounterResolver
{
    public List<EnemyCharacter> Allies { get; } = new();
    public List<EnemyCharacter> Enemies { get; } = new();
    public Dictionary<Character, HealthBarUI> HpBars { get; } = new();
    public Dictionary<EnemyCharacter, EnemyIntentView> IntentViews { get; } = new();

    public bool Resolve(EnemyCharacter sceneEnemy, HealthBarUI sceneEnemyHpBar, EnemyIntentView sceneEnemyIntent)
    {
        if (sceneEnemy == null)
            return false;

        IReadOnlyList<EnemyData> enemyData = PendingBattleEnemy.Enemies;
        IReadOnlyList<EnemyData> allyData = PendingBattleEnemy.Allies;

        // Copies first, while the scene's enemy still has its scene scale and sprite.
        var extraEnemies = new List<EnemyCharacter>();
        for (int i = 1; i < enemyData.Count; i++)
            extraEnemies.Add(Copy(sceneEnemy, $"Enemy {i + 1}"));

        var allies = new List<EnemyCharacter>();
        foreach (EnemyData data in allyData)
            allies.Add(Copy(sceneEnemy, $"Ally {data.DisplayName}"));

        // No pending encounter (the battle scene played on its own): the scene's enemy as it is.
        if (enemyData.Count > 0)
            sceneEnemy.ApplyData(enemyData[0]);

        Add(Enemies, sceneEnemy, sceneEnemyHpBar, sceneEnemyIntent, isCopy: false);

        for (int i = 0; i < extraEnemies.Count; i++)
        {
            extraEnemies[i].ApplyData(enemyData[i + 1]);
            Add(Enemies, extraEnemies[i], sceneEnemyHpBar, sceneEnemyIntent, isCopy: true);
        }

        for (int i = 0; i < allies.Count; i++)
        {
            allies[i].ApplyData(allyData[i]);
            allies[i].JoinPlayerSide();
            Add(Allies, allies[i], sceneEnemyHpBar, sceneEnemyIntent, isCopy: true);
        }

        if (enemyData.Count > 0)
        {
            for (int i = 0; i < Enemies.Count; i++)
            {
                if (PendingBattleEnemy.TryGetCurrentEnemyHealth(i, out int health))
                    Enemies[i].SetHealth(health);
            }
        }

        return true;
    }

    private static EnemyCharacter Copy(EnemyCharacter template, string name)
    {
        EnemyCharacter copy = Object.Instantiate(template, template.transform.parent);
        copy.name = name;
        return copy;
    }

    private void Add(
        List<EnemyCharacter> side,
        EnemyCharacter fighter,
        HealthBarUI hpBarTemplate,
        EnemyIntentView intentTemplate,
        bool isCopy)
    {
        side.Add(fighter);

        if (hpBarTemplate != null)
        {
            HealthBarUI hpBar = isCopy ? CopyUi(hpBarTemplate, fighter) : hpBarTemplate;
            hpBar.Bind(fighter);
            HpBars[fighter] = hpBar;
        }

        if (intentTemplate != null)
        {
            IntentViews[fighter] = isCopy
                ? intentTemplate.CreateCopy($"{intentTemplate.name} ({fighter.name})")
                : intentTemplate;
        }
    }

    private static T CopyUi<T>(T template, Character fighter) where T : Component
    {
        T copy = Object.Instantiate(template, template.transform.parent);
        // Next to the template, not last: the UI after it on the canvas (the inventory, menus) stays on top.
        copy.transform.SetSiblingIndex(template.transform.GetSiblingIndex() + 1);
        copy.name = $"{template.name} ({fighter.name})";
        return copy;
    }
}
