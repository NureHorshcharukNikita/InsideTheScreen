using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewCard", menuName = "Game/Battle/Cards/Card")]
public class CardData : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string cardID;
    [SerializeField] private string cardName;

    [TextArea(3, 6)]
    [SerializeField] private string description;

    [TextArea(2, 4)]
    [SerializeField] private string shortDescription;

    [SerializeField] private Sprite icon;
    [SerializeField] private List<CardCategory> categories = new();

    [Header("Gameplay")]
    [SerializeField] private int cost = 1;
    [SerializeField] private List<BattleCondition> conditions = new();
    [SerializeField] private List<CardInstantEffectEntry> instantEffects = new();
    [SerializeField] private List<CardStatusApplicationEntry> statusApplications = new();

    [Header("Economy")]
    [SerializeField] private int baseGoldValue = 1;

    public string CardID => cardID;
    public string CardName => cardName;
    public string RawDescription => description;
    public string RawShortDescription => shortDescription;
    public string Description => CardDescriptionFormatter.Format(this, description);
    public string ShortDescription
    {
        get
        {
            string formattedShortDescription = CardDescriptionFormatter.Format(this, shortDescription);
            return string.IsNullOrWhiteSpace(formattedShortDescription)
                ? Description
                : formattedShortDescription;
        }
    }

    public Sprite Icon => icon;
    public IReadOnlyList<CardCategory> Categories => categories;
    public int Cost => cost;
    public int BaseGoldValue => baseGoldValue > 0 ? baseGoldValue : Mathf.Max(1, cost * 2);
    public IReadOnlyList<BattleCondition> Conditions => conditions;
    public IReadOnlyList<CardInstantEffectEntry> InstantEffects => instantEffects;
    public IReadOnlyList<CardStatusApplicationEntry> StatusApplications => statusApplications;
}
