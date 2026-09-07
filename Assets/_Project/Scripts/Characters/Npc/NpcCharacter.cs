using System;
using System.Collections.Generic;
using UnityEngine;

public class NpcCharacter : Character
{
    [SerializeField] private NpcData npcData;
    [SerializeField] private NpcState state = NpcState.Idle;

    public override int MaxHealth => npcData != null ? Mathf.Max(1, npcData.maxHealth) : 1;
    public override int StartingArmor => npcData != null ? Mathf.Max(0, npcData.startingArmor) : 0;
    public override CombatTeam Team => CombatTeam.Neutral;

    public NpcData Data => npcData;
    public NpcState State => state;
    public string DisplayName => npcData != null ? npcData.DisplayName : name;
    public NpcDisposition Disposition => npcData != null ? npcData.disposition : NpcDisposition.Neutral;
    public Sprite PortraitSprite => npcData != null ? npcData.PortraitSprite : null;
    public Sprite DialogueSprite => PortraitSprite;
    public DialogueData Dialogue => npcData != null ? npcData.dialogue : null;
    public IReadOnlyList<string> OverheadLines => npcData != null ? npcData.overheadLines : Array.Empty<string>();
    public IReadOnlyList<string> QuestKeys => npcData != null ? npcData.questKeys : Array.Empty<string>();
    public IReadOnlyList<ScriptableObject> TradeStock => npcData != null ? npcData.tradeStock : Array.Empty<ScriptableObject>();
    public bool CanMove => npcData != null && npcData.canMove;
    public float MoveSpeed => npcData != null ? Mathf.Max(0f, npcData.moveSpeed) : 0f;

    public bool CanTalk => npcData != null && npcData.CanTalk;
    public bool CanShowOverheadLines => npcData != null && npcData.CanShowOverheadLines;
    public bool CanTrade => npcData != null && npcData.CanTrade;
    public bool CanOfferQuests => npcData != null && npcData.CanOfferQuests;

    public event Action<NpcState, NpcState> StateChanged;
    public event Action<NpcCharacter, NpcInteractionType> InteractionRequested;

    protected override void Awake()
    {
        base.Awake();
    }

    public void ApplyData(NpcData data)
    {
        if (data == null)
            return;

        npcData = data;
        SetFullHealth();
        SetArmor(StartingArmor);
        SetState(NpcState.Idle);
    }

    public bool CanUseInteraction(NpcInteractionType interaction)
    {
        if (npcData == null || state == NpcState.Disabled || state == NpcState.Busy || interaction == NpcInteractionType.None)
            return false;

        return interaction switch
        {
            NpcInteractionType.Talk => npcData.CanTalk,
            NpcInteractionType.Trade => npcData.CanTrade,
            NpcInteractionType.Quest => npcData.CanOfferQuests,
            _ => false
        };
    }

    public bool TryInteract(NpcInteractionType interaction)
    {
        if (!CanUseInteraction(interaction))
            return false;

        SetState(GetInteractionState(interaction));
        InteractionRequested?.Invoke(this, interaction);
        return true;
    }

    public void EndInteraction()
    {
        if (state == NpcState.Talking || state == NpcState.Trading || state == NpcState.OfferingQuest)
            SetState(NpcState.Idle);
    }

    public bool TryStartWalking()
    {
        if (!CanMove || state != NpcState.Idle)
            return false;

        SetState(NpcState.Walking);
        return true;
    }

    public void StopWalking()
    {
        if (state == NpcState.Walking)
            SetState(NpcState.Idle);
    }

    public void SetBusy(bool busy)
    {
        SetState(busy ? NpcState.Busy : NpcState.Idle);
    }

    public void SetEnabled(bool enabled)
    {
        SetState(enabled ? NpcState.Idle : NpcState.Disabled);
    }

    private void SetState(NpcState nextState)
    {
        if (state == nextState)
            return;

        NpcState previousState = state;
        state = nextState;
        StateChanged?.Invoke(previousState, state);
    }

    private static NpcState GetInteractionState(NpcInteractionType interaction)
    {
        return interaction switch
        {
            NpcInteractionType.Trade => NpcState.Trading,
            NpcInteractionType.Quest => NpcState.OfferingQuest,
            _ => NpcState.Talking
        };
    }

}
