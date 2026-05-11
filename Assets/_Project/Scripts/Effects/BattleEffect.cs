using UnityEngine;

public abstract class BattleEffect : ScriptableObject
{
    [Header("Effect Info")]
    [SerializeField] private string effectName;

    [TextArea(2, 4)]
    [SerializeField] private string description;

    public string EffectName => effectName;
    public string Description => description;

    public abstract void Execute(ICombatant source, ICombatant target, int value, BattleActionContext actionContext);
}