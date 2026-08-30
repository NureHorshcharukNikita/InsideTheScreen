using UnityEngine;

public abstract class InstantBattleEffect : ScriptableObject
{
    [Header("Effect Info")]
    [SerializeField] private string effectID;
    [SerializeField] private string effectName;

    [TextArea(2, 4)]
    [SerializeField] private string description;

    public string EffectID => effectID;
    public string EffectName => effectName;
    public string Description => description;

    public abstract void Execute(ICombatant source, ICombatant target, int value, BattleActionContext actionContext);

    public virtual void Execute(ICombatant source, ICombatant target, IInstantEffectSpec spec, BattleActionContext actionContext)
    {
        Execute(source, target, spec?.Value ?? 0, actionContext);
    }
}
