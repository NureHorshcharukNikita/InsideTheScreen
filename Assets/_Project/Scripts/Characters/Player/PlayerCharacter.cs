using System;
using System.Collections;
using UnityEngine;

[DefaultExecutionOrder(-100)]
public class PlayerCharacter : Character
{
    [SerializeField] private PlayerData playerData;

    private int currentActionPoints;
    private int lastKnownMaxHealth;
    private InventoryData runtimeInventoryData;
    private DeckData runtimeDeckData;

    public override int MaxHealth => playerData != null
        ? Mathf.Max(1, SkillProgression.ModifyInt(SkillStat.MaxHealth, playerData.maxHealth))
        : 0;
    public override int StartingArmor => playerData != null
        ? Mathf.Max(0, SkillProgression.ModifyInt(SkillStat.StartingArmor, playerData.startingArmor))
        : 0;
    public override CombatTeam Team => CombatTeam.Player;

    public InventoryData InventoryData => runtimeInventoryData;
    public DeckData DeckData => runtimeDeckData;
    public PlayerData Data => playerData;
    public override string DisplayName => playerData != null ? playerData.DisplayName : name;
    public override Sprite BattleIcon => playerData != null ? playerData.BattleIcon : null;
    public Sprite PortraitSprite => playerData != null ? playerData.PortraitSprite : null;
    public Sprite DialogueSprite => PortraitSprite;
    public int StartHandSize => playerData != null ? playerData.startHandSize : 0;
    public int CardsDrawnPerTurn => playerData != null ? playerData.cardsDrawnPerTurn : 0;
    public int MaxActionPoints => playerData != null
        ? Mathf.Max(0, SkillProgression.ModifyInt(SkillStat.MaxActionPoints, playerData.maxActionPoints))
        : 0;
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

        runtimeInventoryData = ExplorationPlayerSession.GetOrCreateRuntimeInventory(playerData.Inventory);
        runtimeDeckData = ExplorationPlayerSession.GetOrCreateRuntimeDeck(playerData.Deck);
        PlayerWallet.Initialize(playerData.startingGold);

        SkillProgression.Initialize(playerData.startingSkillPoints, playerData.skills);
        lastKnownMaxHealth = MaxHealth;
        SetArmor(StartingArmor);

        if (ExplorationPlayerSession.TryGetSavedHealth(out int savedHealth))
            SetHealth(savedHealth);
        else
            SetFullHealth();

        currentActionPoints = Mathf.Min(playerData.startActionPoints, MaxActionPoints);

        ActionPointsChanged?.Invoke(CurrentActionPoints, MaxActionPoints);
    }

    private void OnEnable()
    {
        SkillProgression.Changed += HandleSkillProgressionChanged;
    }

    private void OnDisable()
    {
        SkillProgression.Changed -= HandleSkillProgressionChanged;
    }

    private IEnumerator Start()
    {
        yield return new WaitForEndOfFrame();
        ExplorationPlayerSession.TryCreateInitialSave(this);
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

    // Taken away by effects such as Stun, never below zero.
    public void LoseActionPoints(int amount)
    {
        if (amount <= 0 || currentActionPoints == 0)
            return;

        currentActionPoints = Mathf.Max(0, currentActionPoints - amount);
        ActionPointsChanged?.Invoke(CurrentActionPoints, MaxActionPoints);
    }

    public void RestoreActionPoints()
    {
        if (playerData == null)
            return;

        currentActionPoints = Mathf.Min(currentActionPoints + playerData.actionPointsPerTurn, MaxActionPoints);

        ActionPointsChanged?.Invoke(CurrentActionPoints, MaxActionPoints);
    }

    private void HandleSkillProgressionChanged()
    {
        int newMaxHealth = MaxHealth;
        int gainedHealth = Mathf.Max(0, newMaxHealth - lastKnownMaxHealth);
        lastKnownMaxHealth = newMaxHealth;
        SetHealth(CurrentHealth + gainedHealth);

        currentActionPoints = Mathf.Min(currentActionPoints, MaxActionPoints);
        ActionPointsChanged?.Invoke(CurrentActionPoints, MaxActionPoints);
    }
}
