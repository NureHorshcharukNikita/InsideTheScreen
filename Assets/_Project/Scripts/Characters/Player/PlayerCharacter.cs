using System;
using System.Collections;
using System.Collections.Generic;
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
    public int StartHandSize => playerData != null
        ? Mathf.Max(0, SkillProgression.ModifyInt(SkillStat.StartingHandSize, playerData.startHandSize))
        : 0;
    public int CardsDrawnPerTurn => playerData != null
        ? Mathf.Max(0, SkillProgression.ModifyInt(SkillStat.CardsDrawnPerTurn, playerData.cardsDrawnPerTurn))
        : 0;
    public int MinimumBattleDeckSize => playerData != null ? playerData.minimumBattleDeckSize : 0;
    public List<CardData> BaseCardsInFillOrder => playerData != null
        ? playerData.GetBaseCardsInFillOrder()
        : new List<CardData>();
    public int CardsPlayedThisTurn { get; private set; }
    public int CardsPlayedThisBattle { get; private set; }
    public bool IsLowOnHealth => CurrentHealth <= MaxHealth * Balance.Config.lowHealthThreshold;

    private bool willToLiveUsed;
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
        DropBaseCardsFromItems();
        PlayerWallet.Initialize(playerData.startingGold);

        SkillProgression.Initialize(playerData.startingSkillPoints, playerData.skills);
        lastKnownMaxHealth = MaxHealth;
        SetArmor(StartingArmor);

        if (ExplorationPlayerSession.TryGetSavedHealth(out int savedHealth))
            SetHealth(savedHealth);
        else
            SetFullHealth();

        currentActionPoints = Mathf.Min(
            SkillProgression.ModifyInt(SkillStat.StartingActionPoints, playerData.startActionPoints), MaxActionPoints);

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

        int perTurn = SkillProgression.ModifyInt(SkillStat.ActionPointsPerTurn, playerData.actionPointsPerTurn);
        currentActionPoints = Mathf.Min(currentActionPoints + perTurn, MaxActionPoints);
        CardsPlayedThisTurn = 0;

        ActionPointsChanged?.Invoke(CurrentActionPoints, MaxActionPoints);
    }

    // What a card costs Haru now: Planning makes the first card of a turn cheaper.
    public int GetActionPointCost(CardData card)
    {
        if (card == null)
            return 0;

        int cost = card.Cost;
        if (CardsPlayedThisTurn == 0 && SkillProgression.HasPassive(SkillPassive.FirstCardDiscount))
            cost -= Balance.Config.firstCardDiscount;

        return Mathf.Max(0, cost);
    }

    // After a card is played: Battle Analysis gives armor on every third card of the battle.
    public void NoteCardPlayed()
    {
        CardsPlayedThisTurn++;
        CardsPlayedThisBattle++;

        int armor = SkillProgression.ModifyInt(SkillStat.ArmorEveryThirdCard, 0);
        if (armor > 0 && CardsPlayedThisBattle % 3 == 0)
            AddArmor(armor);
    }

    // Start of Haru's turn: Emotional Control heals a little.
    public void ApplyTurnStartSkills()
    {
        int healing = SkillProgression.ModifyInt(SkillStat.HealthPerTurn, 0);
        if (healing > 0 && IsAlive)
            Heal(healing);
    }

    // Will to Live: once a battle, a lethal blow leaves Haru on a share of his health.
    protected override bool TryCheatDeath()
    {
        if (willToLiveUsed || !SkillProgression.HasPassive(SkillPassive.WillToLive))
            return false;

        willToLiveUsed = true;
        SetHealth(Mathf.Max(1, Mathf.CeilToInt(MaxHealth * Balance.Config.willToLiveHealthShare)));
        return true;
    }

    // Base cards are not items: copies an older save still holds leave the inventory and the deck.
    private void DropBaseCardsFromItems()
    {
        foreach (PlayerData.BaseCardEntry entry in playerData.baseCards)
        {
            if (entry == null || entry.card == null)
                continue;

            runtimeInventoryData?.RemoveCards(entry.card, int.MaxValue);
            runtimeDeckData?.RemoveCards(entry.card, int.MaxValue);
        }
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
