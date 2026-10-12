using System;
using System.Collections.Generic;
using UnityEngine;

// The one place card category icons live: the inventory's category buttons show them, and a battle
// effect tied to a category (Armor, Burning, ...) shows the same icon on its badge.
// Kept in Resources so effect displays find it without each holding a reference.
[CreateAssetMenu(menuName = "Game/Cards/Category Icons", fileName = ResourceName)]
public sealed class CardCategoryIcons : ScriptableObject
{
    public const string ResourceName = "CardCategoryIcons";

    [Serializable]
    private sealed class Entry
    {
        public CardCategory category;
        public Sprite icon;
    }

    [Tooltip("The inventory's \"All\" button.")]
    [SerializeField] private Sprite allIcon;
    [SerializeField] private List<Entry> icons = new();

    private static CardCategoryIcons instance;

    public static CardCategoryIcons Instance =>
        instance != null ? instance : instance = Resources.Load<CardCategoryIcons>(ResourceName);

    public Sprite AllIcon => allIcon;

    public Sprite GetIcon(CardCategory category)
    {
        foreach (Entry entry in icons)
        {
            if (entry != null && entry.category == category)
                return entry.icon;
        }

        return null;
    }
}
