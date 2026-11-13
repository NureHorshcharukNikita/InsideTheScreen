using System;
using System.Collections.Generic;
using UnityEngine;

// One recipe of the crafting tab in the inventory: the cards it takes and the card it makes.
// Ingredients are used up, except a tool (a knife to cut with), which is only needed at hand.
// An ingredient can have another card that does instead, e.g. a stone or Father's lighter to
// light a torch.
[CreateAssetMenu(fileName = "NewRecipe", menuName = "Game/Crafting/Recipe")]
public sealed class CraftingRecipe : ScriptableObject
{
    [Serializable]
    public sealed class Ingredient
    {
        public CardData card;
        [Min(1)] public int count = 1;
        [Tooltip("Needed at hand but not used up, e.g. a knife.")]
        public bool tool;

        [Header("Or instead")]
        [Tooltip("Another card that does instead, e.g. Father's Lighter for a stone. Empty = only the card above.")]
        public CardData orInstead;
        [Tooltip("The other card is not used up either, e.g. the lighter.")]
        public bool orInsteadIsTool;
    }

    [SerializeField] private CardData result;
    [Tooltip("How many cards one craft makes.")]
    [SerializeField, Min(1)] private int resultCount = 1;
    [SerializeField] private List<Ingredient> ingredients = new();
    [Tooltip("Taught by someone: known once this story flag is set, e.g. Garr's salve (a1_salve_recipe). Empty = Haru thinks of it himself: after his father's talk about making things (CraftingProgress.IdeasFlag), once he has all it takes at once.")]
    [SerializeField, StoryFlag] private string learnedAt;

    public CardData Result => result;
    public string LearnedAt => learnedAt;
    public int ResultCount => resultCount;
    public IReadOnlyList<Ingredient> Ingredients => ingredients;
}
