using UnityEngine;

[CreateAssetMenu(menuName = "Game/Player Data")]
public class PlayerData : ScriptableObject
{
    [Header("Health")]
    public int maxHealth = 30;

    [Header("Action Points")]
    public int maxActionPoints = 5;
    public int startActionPoints = 4;
    public int actionPointsPerTurn = 2;

    [Header("Deck")]
    public DeckData Deck;

    [Header("Inventory")]
    public InventoryData Inventory;
}