public readonly struct PlannedEnemyAction
{
    public PlannedEnemyAction(EnemyAbilityData ability, Character primaryTargetForUi)
    {
        Ability = ability;
        PrimaryTargetForUi = primaryTargetForUi;
    }

    public EnemyAbilityData Ability { get; }
    public Character PrimaryTargetForUi { get; }
    public bool HasAbility => Ability != null;

    public string GetIntentLabel()
    {
        if (!HasAbility)
            return "";

        if (!string.IsNullOrWhiteSpace(Ability.intentSummary))
            return Ability.intentSummary.Trim();

        if (!string.IsNullOrWhiteSpace(Ability.displayName))
            return Ability.displayName.Trim();

        return Ability.name;
    }
}
