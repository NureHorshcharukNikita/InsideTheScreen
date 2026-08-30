using UnityEngine;

public sealed class ActiveBattleStatus
{
    public ActiveBattleStatus(BattleStatus status, int duration, int stacks, int value, ICombatant source)
    {
        Status = status;
        Duration = Mathf.Max(duration, 0);
        Stacks = Mathf.Max(stacks, 1);
        Value = Mathf.Max(value, 0);
        Source = source;
    }

    public BattleStatus Status { get; }
    public int Duration { get; private set; }
    public int Stacks { get; private set; }
    public int Value { get; private set; }
    public ICombatant Source { get; private set; }
    public bool IsExpired => Duration == 0;

    public void Refresh(int duration, int stacks, int value, ICombatant source, BattleStatusReapplyMode reapplyMode)
    {
        if (reapplyMode == BattleStatusReapplyMode.AddDuration)
        {
            Duration += Mathf.Max(duration, 0);
            Stacks = Mathf.Max(Stacks, Mathf.Max(stacks, 1));
        }
        else
        {
            Duration = Mathf.Max(Duration, duration);
            Stacks += Mathf.Max(stacks, 1);
        }

        Value = Mathf.Max(Value, value);
        Source = source ?? Source;
    }

    public void ReduceDuration()
    {
        if (Duration > 0)
            Duration--;
    }
}
