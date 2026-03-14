using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewCard", menuName = "Cards/Card")]
public class CardData : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string cardID;
    [SerializeField] private string cardName;

    [TextArea(3, 6)]
    [SerializeField] private string description;

    [SerializeField] private Sprite icon;

    [Header("Gameplay")]
    [SerializeField] private int cost = 1;
    [SerializeField] private List<CardEffectEntry> effects = new();

    public string CardID => cardID;
    public string CardName => cardName;
    public string Description => description;
    public Sprite Icon => icon;
    public int Cost => cost;
    public IReadOnlyList<CardEffectEntry> Effects => effects;
}