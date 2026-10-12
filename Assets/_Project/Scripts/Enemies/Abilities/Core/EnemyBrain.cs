using System;
using System.Collections.Generic;
using UnityEngine;

// Plans and plays the turns of a fighter the game controls, enemy or ally. Each turn it aims at
// one opponent picked at random; should that one fall first, it turns to another.
[DisallowMultipleComponent]
public class EnemyBrain : MonoBehaviour
{
    private EnemyCharacter self;
    private TurnManager battle;
    private Character chosenTarget;
    private readonly EnemyAbilityStateTracker abilityStates = new();
    private readonly List<EnemyAbilityData> usedThisTurn = new();

    public PlannedEnemyAction CurrentPlan { get; private set; }

    public event Action PlannedActionChanged;

    private void Awake()
    {
        self = GetComponent<EnemyCharacter>();
        if (self == null)
            DevLog.Log($"{nameof(EnemyBrain)} requires {nameof(EnemyCharacter)} on the same object.");
    }

    public void BindBattle(TurnManager turnManager)
    {
        battle = turnManager;
        chosenTarget = null;
        abilityStates.Clear();
        usedThisTurn.Clear();
    }

    // Who the planned turn aims at, for the intent; null when it only affects the fighter itself.
    public string GetIntentTargetName()
    {
        Character target = CurrentPlan.PrimaryTargetForUi;
        return target != null && target != self ? target.DisplayName : null;
    }

    private BattleTargetingContext CreateTargetingContext()
    {
        if (self == null || battle == null)
            return new BattleTargetingContext(null, null, null, null);

        IReadOnlyList<ICombatant> opponents = battle.GetEnemiesOf(self);
        return new BattleTargetingContext(self, ResolveTarget(opponents), battle.GetAlliesOf(self), opponents);
    }

    // The chosen opponent while they still stand, otherwise another one at random.
    private Character ResolveTarget(IReadOnlyList<ICombatant> opponents)
    {
        if (chosenTarget != null && BattleTargetingTeams.Contains(chosenTarget, opponents))
            return chosenTarget;

        chosenTarget = opponents.Count > 0
            ? opponents[UnityEngine.Random.Range(0, opponents.Count)] as Character
            : null;
        return chosenTarget;
    }

    public void PlanNextAction()
    {
        // A new turn, a new target.
        chosenTarget = null;
        SetPlan(CreateNextPlan());
    }

    // One ability of the planned turn; FinishTurn closes the turn once they are all done.
    public void ExecuteAbility(EnemyAbilityData ability)
    {
        BattleTargetingContext context = CreateTargetingContext();
        if (context.Self == null || ability == null)
            return;

        EnemyAbilityExecutor.ApplyAbility(ability, context);
        usedThisTurn.Add(ability);
    }

    public void FinishTurn()
    {
        abilityStates.RecordTurn(usedThisTurn, GetAbilities());
        usedThisTurn.Clear();
    }

    public Character GetPrimaryTarget(EnemyAbilityData ability)
    {
        return EnemyAbilityTargetPreview.FindPrimaryTarget(ability, CreateTargetingContext());
    }

    // Stun and the like: drops planned abilities from the end of the turn until enough action
    // points are gone. A two-point ritual is lost to a single lost point.
    public void LoseActionPoints(int points)
    {
        if (points <= 0 || !CurrentPlan.HasAbility)
            return;

        var remaining = new List<EnemyAbilityData>(CurrentPlan.Abilities);
        int lost = 0;
        while (remaining.Count > 0 && lost < points)
        {
            lost += GetCost(remaining[remaining.Count - 1]);
            remaining.RemoveAt(remaining.Count - 1);
        }

        SetPlan(new PlannedEnemyAction(remaining, FindPrimaryTarget(remaining, CreateTargetingContext())));
    }

    private PlannedEnemyAction CreateNextPlan()
    {
        IReadOnlyList<EnemyAbilityData> abilities = GetAbilities();
        if (abilities == null || abilities.Count == 0)
            return default;

        BattleTargetingContext context = CreateTargetingContext();
        var planned = new List<EnemyAbilityData>();
        int pointsLeft = self != null ? self.ActionPointsPerTurn : 1;

        while (pointsLeft > 0)
        {
            int available = pointsLeft;
            EnemyAbilityData ability = EnemyAbilityPicker.PickWeighted(
                abilities,
                context,
                abilityStates,
                candidate => GetCost(candidate) <= available && abilityStates.CanPlanAgain(candidate, planned));
            if (ability == null)
                break;

            planned.Add(ability);
            pointsLeft -= GetCost(ability);
        }

        return planned.Count > 0
            ? new PlannedEnemyAction(planned, FindPrimaryTarget(planned, context))
            : default;
    }

    // The target the intent points at: the first one that is not the enemy itself.
    private Character FindPrimaryTarget(IReadOnlyList<EnemyAbilityData> abilities, BattleTargetingContext context)
    {
        Character fallback = null;
        foreach (EnemyAbilityData ability in abilities)
        {
            Character target = EnemyAbilityTargetPreview.FindPrimaryTarget(ability, context);
            if (target != null && target != self)
                return target;

            fallback ??= target;
        }

        return fallback;
    }

    private static int GetCost(EnemyAbilityData ability)
    {
        return ability != null ? Mathf.Max(1, ability.actionPointCost) : 1;
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
