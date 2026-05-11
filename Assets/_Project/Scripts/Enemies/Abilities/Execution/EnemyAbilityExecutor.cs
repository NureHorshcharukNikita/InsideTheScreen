using System.Collections.Generic;
using UnityEngine;

public static class EnemyAbilityExecutor
{
    public static BattleTargetingContext BuildTargetingContext(EnemyAbilityBattleContext abilityContext)
    {
        if (abilityContext == null)
            return new BattleTargetingContext(null, null, null, null);

        return new BattleTargetingContext(
            abilityContext.Self,
            abilityContext.Player,
            ToCombatants(abilityContext.GetAllies()),
            ToCombatants(abilityContext.GetEnemies()));
    }

    public static IReadOnlyList<Character> ResolveTargets(EnemyAbilityEffectSpec spec, EnemyAbilityBattleContext abilityContext)
    {
        if (abilityContext == null || abilityContext.Self == null || spec == null)
            return System.Array.Empty<Character>();

        if (spec.targeting != null)
        {
            BattleTargetingContext targetingContext = BuildTargetingContext(abilityContext);
            IReadOnlyList<ICombatant> rawTargets = spec.targeting.ResolveTargets(targetingContext);
            return FilterCharacters(rawTargets);
        }

        return PickSingle(abilityContext.GetEnemies());
    }

    public static void ApplyAbility(EnemyAbilityData ability, EnemyAbilityBattleContext abilityContext, BattleActionContext actionContext = null)
    {
        if (ability == null || abilityContext == null || ability.effects == null)
            return;

        BattleActionContext runtime = actionContext ?? BattleActionContext.CreateDefault();
        BattleTargetingContext targetingContext = BuildTargetingContext(abilityContext);
        foreach (EnemyAbilityEffectSpec spec in ability.effects)
        {
            if (spec == null)
                continue;

            if (!BattleCondition.AllMet(spec.conditions, targetingContext))
                continue;

            if (spec.applyChance < 1f && Random.value > spec.applyChance)
                continue;

            IReadOnlyList<Character> targets = ResolveTargets(spec, abilityContext);
            foreach (Character target in targets)
            {
                if (target == null)
                    continue;

                ApplyEffect(spec, abilityContext, target, runtime);
            }
        }
    }

    private static IReadOnlyList<ICombatant> ToCombatants(IReadOnlyList<Character> list)
    {
        if (list == null || list.Count == 0)
            return System.Array.Empty<ICombatant>();

        var combatants = new ICombatant[list.Count];
        for (int i = 0; i < list.Count; i++)
            combatants[i] = list[i] as ICombatant;
        return combatants;
    }

    private static IReadOnlyList<Character> FilterCharacters(IReadOnlyList<ICombatant> list)
    {
        if (list == null || list.Count == 0)
            return System.Array.Empty<Character>();

        var result = new List<Character>(list.Count);
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i] is Character character)
                result.Add(character);
        }

        return result;
    }

    private static void ApplyEffect(EnemyAbilityEffectSpec spec, EnemyAbilityBattleContext abilityContext, Character target, BattleActionContext actionContext)
    {
        if (spec == null || target == null)
            return;

        if (spec.effect != null)
        {
            spec.effect.Execute(abilityContext?.Self, target, spec.amount, actionContext);
            return;
        }

    }

    private static IReadOnlyList<Character> PickSingle(IReadOnlyList<Character> list)
    {
        if (list == null || list.Count == 0)
            return System.Array.Empty<Character>();

        int randomIndex = Random.Range(0, list.Count);
        return new[] { list[randomIndex] };
    }

}
