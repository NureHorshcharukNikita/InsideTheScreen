using System.Collections.Generic;

// What the enemy will do on its next turn: one or more abilities, paid for with its action points.
public readonly struct PlannedEnemyAction
{
    private readonly EnemyAbilityData[] abilities;

    public PlannedEnemyAction(IReadOnlyList<EnemyAbilityData> abilities, Character primaryTargetForUi)
    {
        this.abilities = abilities != null ? new List<EnemyAbilityData>(abilities).ToArray() : null;
        PrimaryTargetForUi = primaryTargetForUi;
    }

    public IReadOnlyList<EnemyAbilityData> Abilities => abilities ?? System.Array.Empty<EnemyAbilityData>();
    public EnemyAbilityData Ability => abilities != null && abilities.Length > 0 ? abilities[0] : null;
    public Character PrimaryTargetForUi { get; }
    public bool HasAbility => Ability != null;

    public string GetIntentLabel()
    {
        if (!HasAbility)
            return "";

        var labels = new List<string>(abilities.Length);
        foreach (EnemyAbilityData ability in abilities)
        {
            if (ability != null)
                labels.Add(GetLabel(ability));
        }

        return string.Join(" + ", labels);
    }

    private static string GetLabel(EnemyAbilityData ability)
    {
        if (!string.IsNullOrWhiteSpace(ability.IntentSummary))
            return ability.IntentSummary.Trim();

        if (!string.IsNullOrWhiteSpace(ability.displayName))
            return ability.displayName.Trim();

        return ability.name;
    }
}
