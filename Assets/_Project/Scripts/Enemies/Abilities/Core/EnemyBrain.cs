using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class EnemyBrain : MonoBehaviour
{
    [SerializeField] private PlayerCharacter opponent;

    private EnemyCharacter _self;
    private EnemyAbilityBattleContext _context;
    private readonly Dictionary<EnemyAbilityData, EnemyBrainRuntime.AbilityRuntimeState> _runtimeState = new();

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

        EnemyAbilityData picked = EnemyBrainSelection.PickWeighted(pool, _context, _runtimeState);
        if (picked == null)
        {
            CurrentPlan = default;
            PlannedActionChanged?.Invoke();
            return;
        }

        Character primary = EnemyBrainSelection.ResolvePrimaryTargetForUi(picked, _context);
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

    private void AdvanceRuntimeAfterUse(EnemyAbilityData usedAbility)
    {
        if (usedAbility == null)
            return;

        foreach (EnemyAbilityData ability in _self.Data.abilities)
        {
            if (ability == null)
                continue;

            EnemyBrainRuntime.AbilityRuntimeState state = EnemyBrainRuntime.GetState(_runtimeState, ability);
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

}
