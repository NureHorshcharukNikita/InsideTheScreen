using UnityEngine;

public abstract class CardEffect : ScriptableObject
{
    [Header("Effect Info")]
    [SerializeField] private string effectName;

    [TextArea(2, 4)]
    [SerializeField] private string description;

    public string EffectName => effectName;
    public string Description => description;

    public abstract void Execute(IEffectTarget source, IEffectTarget target, int value);
}