using System;
using UnityEngine;

public class PlayerCharacter : Character
{
    [SerializeField] private PlayerData playerData;

    private int currentActionPoints;

    public override int MaxHealth => playerData != null ? playerData.maxHealth : 0;
    public override CombatTeam Team => CombatTeam.Player;

    public InventoryData InventoryData => playerData != null ? playerData.Inventory : null;
    public DeckData DeckData => playerData != null ? playerData.Deck : null;
    public int StartHandSize => playerData != null ? playerData.startHandSize : 0;
    public int CardsDrawnPerTurn => playerData != null ? playerData.cardsDrawnPerTurn : 0;
    public int MaxActionPoints => playerData != null ? playerData.maxActionPoints : 0;
    public int CurrentActionPoints => currentActionPoints;

    public event Action<int, int> ActionPointsChanged;

    protected override void Awake()
    {
        base.Awake();

        if (playerData == null)
        {
            Debug.LogError("PlayerData is not assigned.");
            return;
        }

        if (playerData.Deck == null)
        {
            Debug.LogError("DeckData is not assigned in PlayerData.");
            return;
        }

        if (ExplorationPlayerSession.TryGetSavedHealth(out int savedHealth))
            SetHealth(savedHealth);

        currentActionPoints = playerData.startActionPoints;

        ActionPointsChanged?.Invoke(CurrentActionPoints, MaxActionPoints);
    }

    public bool SpendActionPoints(int amount)
    {
        if (!CanSpendActionPoints(amount))
            return false;

        currentActionPoints -= amount;
        ActionPointsChanged?.Invoke(CurrentActionPoints, MaxActionPoints);
        return true;
    }

    public bool CanSpendActionPoints(int amount)
    {
        return amount >= 0 && currentActionPoints >= amount;
    }

    public void RestoreActionPoints()
    {
        if (playerData == null)
            return;

        currentActionPoints = Mathf.Min(currentActionPoints + playerData.actionPointsPerTurn, MaxActionPoints);

        ActionPointsChanged?.Invoke(CurrentActionPoints, MaxActionPoints);
    }
}
