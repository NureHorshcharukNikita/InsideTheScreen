using UnityEngine;

public abstract class StatusEffect : ScriptableObject
{
    [Header("Status Info")]
    [SerializeField] private string statusID;
    [SerializeField] private string statusName;

    [TextArea(2, 4)]
    [SerializeField] private string description;

    [SerializeField] private Sprite icon;

    public string StatusID => statusID;
    public string StatusName => statusName;
    public string Description => description;
    public Sprite Icon => icon;

    public virtual void OnApply(ActiveStatusEffect status, ICombatant target, BattleActionContext actionContext)
    {
    }

    public virtual void Tick(ActiveStatusEffect status, ICombatant target, BattleStatusTiming timing, BattleActionContext actionContext)
    {
    }

    public virtual void OnExpire(ActiveStatusEffect status, ICombatant target, BattleActionContext actionContext)
    {
    }
}
