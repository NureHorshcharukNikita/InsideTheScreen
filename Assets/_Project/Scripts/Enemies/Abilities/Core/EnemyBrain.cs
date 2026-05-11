using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class EnemyBrain : MonoBehaviour
{
    [SerializeField] private PlayerCharacter opponent;

    private EnemyCharacter self;
    private readonly EnemyAbilityStateTracker abilityStates = new();

    public PlannedEnemyAction CurrentPlan { get; private set; }

    public event Action PlannedActionChanged;

    private void Awake()
    {
        self = GetComponent<EnemyCharacter>();
        if (self == null)
            DevLog.Log($"{nameof(EnemyBrain)} requires {nameof(EnemyCharacter)} on the same object.");
    }

    public void BindOpponent(PlayerCharacter player)
    {
        opponent = player;
        abilityStates.Clear();
    }

    private BattleTargetingContext CreateTargetingContext()
    {
        if (self == null || opponent == null)
            return new BattleTargetingContext(null, null, null, null);

        return new BattleTargetingContext(
            self, 
            opponent, 
            new ICombatant[] { self }, 
            new ICombatant[] { opponent }
            );
    }

    public void PlanNextAction()
    {
        SetPlan(CreateNextPlan());
    }

    public void ExecutePlanned()
    {
        BattleTargetingContext context = CreateTargetingContext();

        if (context.Self == null || !CurrentPlan.HasAbility)
            return;

        EnemyAbilityData ability = CurrentPlan.Ability;
        EnemyAbilityExecutor.ApplyAbility(ability, context);
        abilityStates.RecordUsed(ability, GetAbilities());
    }

    private PlannedEnemyAction CreateNextPlan()
    {
        IReadOnlyList<EnemyAbilityData> abilities = GetAbilities();
        if (abilities == null || abilities.Count == 0)
            return default;

        BattleTargetingContext context = CreateTargetingContext();
        EnemyAbilityData ability = EnemyAbilityPicker.PickWeighted(abilities, context, abilityStates);
        if (ability == null)
            return default;

        Character primaryTarget = EnemyAbilityTargetPreview.FindPrimaryTarget(ability, context);
        return new PlannedEnemyAction(ability, primaryTarget);
    }

    private IReadOnlyList<EnemyAbilityData> GetAbilities()
    {
        return self != null ? self.Abilities : System.Array.Empty<EnemyAbilityData>();
    }

    private void SetPlan(PlannedEnemyAction plan)
    {
        CurrentPlan = plan;
        PlannedActionChanged?.Invoke();
    }

}
