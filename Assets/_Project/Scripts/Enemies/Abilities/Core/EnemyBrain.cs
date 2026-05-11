using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class EnemyBrain : MonoBehaviour
{
    [SerializeField] private PlayerCharacter opponent;

    private sealed class AbilityRuntimeState
    {
        public int CooldownRemaining;
        public int Uses;
    }

    private EnemyCharacter _self;
    private EnemyAbilityBattleContext _context;
    private readonly Dictionary<EnemyAbilityData, AbilityRuntimeState> _runtimeState = new();

    public PlannedEnemyAction CurrentPlan { get; private set; }

    public event Action PlannedActionChanged;

    private void Awake()
    {
        _self = GetComponent<EnemyCharacter>();
        if (_self == null)
            DevLog.Log($"{nameof(EnemyBrain)} requires {nameof(EnemyCharacter)} on the same object.");
    }

    public void BindOpponent(PlayerCharacter player)
    {
        opponent = player;
        _runtimeState.Clear();
        RebuildContext();
    }

    private void RebuildContext()
    {
        _context = _self != null && opponent != null
            ? new EnemyAbilityBattleContext(_self, opponent)
            : null;
    }

    public void PlanNextAction()
    {
        if (_self == null || _self.Data == null)
        {
            CurrentPlan = default;
            PlannedActionChanged?.Invoke();
            return;
        }

        if (_context == null && opponent != null)
            RebuildContext();

        IReadOnlyList<EnemyAbilityData> pool = _self.Data.abilities;
        if (pool == null || pool.Count == 0)
        {
            CurrentPlan = default;
            PlannedActionChanged?.Invoke();
            return;
        }

        EnemyAbilityData picked = PickWeighted(pool, _context, _runtimeState);
        if (picked == null)
        {
            CurrentPlan = default;
            PlannedActionChanged?.Invoke();
            return;
        }

        Character primary = ResolvePrimaryTargetForUi(picked, _context);
        CurrentPlan = new PlannedEnemyAction(picked, primary);
        PlannedActionChanged?.Invoke();
    }

    public void ExecutePlanned()
    {
        if (_context == null && _self != null && opponent != null)
            RebuildContext();

        if (_context == null || !CurrentPlan.HasAbility)
            return;

        EnemyAbilityData ability = CurrentPlan.Ability;
        EnemyAbilityExecutor.ApplyAbility(ability, _context);
        AdvanceRuntimeAfterUse(ability);
    }

    private static Character ResolvePrimaryTargetForUi(EnemyAbilityData ability, EnemyAbilityBattleContext ctx)
    {
        if (ability?.effects == null || ctx == null)
            return null;

        foreach (EnemyAbilityEffectSpec spec in ability.effects)
        {
            if (spec == null)
                continue;

            IReadOnlyList<Character> targets = EnemyAbilityExecutor.ResolveTargets(spec, ctx);
            if (targets != null && targets.Count > 0)
                return targets[0];
        }

        return null;
    }

    private static EnemyAbilityData PickWeighted(
        IReadOnlyList<EnemyAbilityData> pool,
        EnemyAbilityBattleContext ctx,
        Dictionary<EnemyAbilityData, AbilityRuntimeState> runtime)
    {
        BattleTargetingContext targetingCtx = EnemyAbilityExecutor.BuildTargetingContext(ctx);
        int total = 0;
        for (int i = 0; i < pool.Count; i++)
        {
            EnemyAbilityData a = pool[i];
            if (a != null && IsAbilityAvailable(a, targetingCtx, runtime))
                total += Mathf.Max(1, a.selectionWeight);
        }

        if (total <= 0)
            return null;

        int roll = UnityEngine.Random.Range(0, total);
        int acc = 0;
        for (int i = 0; i < pool.Count; i++)
        {
            EnemyAbilityData a = pool[i];
            if (a == null)
                continue;
            if (!IsAbilityAvailable(a, targetingCtx, runtime))
                continue;

            acc += Mathf.Max(1, a.selectionWeight);
            if (roll < acc)
                return a;
        }

        return null;
    }

    private void AdvanceRuntimeAfterUse(EnemyAbilityData usedAbility)
    {
        if (usedAbility == null)
            return;

        foreach (EnemyAbilityData ability in _self.Data.abilities)
        {
            if (ability == null)
                continue;

            AbilityRuntimeState state = GetState(_runtimeState, ability);
            if (ability == usedAbility)
            {
                state.Uses++;
                state.CooldownRemaining = Mathf.Max(0, ability.cooldownTurns);
            }
            else if (state.CooldownRemaining > 0)
            {
                state.CooldownRemaining--;
            }
        }
    }

    private static AbilityRuntimeState GetState(Dictionary<EnemyAbilityData, AbilityRuntimeState> runtime, EnemyAbilityData ability)
    {
        if (!runtime.TryGetValue(ability, out AbilityRuntimeState state))
        {
            state = new AbilityRuntimeState();
            runtime[ability] = state;
        }

        return state;
    }

    private static bool IsAbilityAvailable(
        EnemyAbilityData ability,
        BattleTargetingContext ctx,
        Dictionary<EnemyAbilityData, AbilityRuntimeState> runtime)
    {
        AbilityRuntimeState state = GetState(runtime, ability);
        if (state.CooldownRemaining > 0)
            return false;

        if (ability.maxUses >= 0 && state.Uses >= ability.maxUses)
            return false;

        if (!BattleCondition.AllMet(ability.conditions, ctx))
            return false;

        return true;
    }
}
