using System;
using System.Collections.Generic;
using UnityEngine;

public class NpcCharacter : Character
{
    [SerializeField] private NpcData npcData;
    [SerializeField] private NpcState state = NpcState.Idle;
    [SerializeField] private string persistenceKeyOverride;

    public override int MaxHealth => npcData != null ? Mathf.Max(1, npcData.maxHealth) : 1;
    public override int StartingArmor => npcData != null ? Mathf.Max(0, npcData.startingArmor) : 0;
    public override CombatTeam Team => CombatTeam.Neutral;

    public NpcData Data => npcData;
    public NpcState State => state;
    public string DisplayName => npcData != null ? npcData.DisplayName : name;
    public NpcDisposition Disposition => npcData != null ? npcData.disposition : NpcDisposition.Neutral;
    public Sprite PortraitSprite => npcData != null ? npcData.PortraitSprite : null;
    public Sprite DialogueSprite => PortraitSprite;
    public IReadOnlyList<string> OverheadLines => npcData != null ? npcData.overheadLines : Array.Empty<string>();
    public ShopData Shop => npcData != null ? npcData.Shop : null;
    public bool CanMove => npcData != null && npcData.canMove;
    public float MoveSpeed => npcData != null ? Mathf.Max(0f, npcData.moveSpeed) : 0f;

    public bool CanTalk => npcData != null && npcData.CanTalkWithState(PersistenceKey);
    public bool CanShowOverheadLines => npcData != null && npcData.CanShowOverheadLines;
    public bool CanTrade => npcData != null && npcData.CanTrade;
    public string PersistenceKey => GetPersistenceKey();

    public event Action<NpcState, NpcState> StateChanged;
    public event Action<NpcCharacter, NpcInteractionType> InteractionRequested;

    protected override void Awake()
    {
        base.Awake();
        RestoreSavedPosition();
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
            NpcInteractionType.Talk => CanTalk,
            NpcInteractionType.Trade => CanTrade,
            _ => false
        };
    }

    public DialogueData SelectDialogue(out NpcDialogueEntry selectedEntry)
    {
        selectedEntry = null;
        return npcData != null ? npcData.SelectDialogue(PersistenceKey, out selectedEntry) : null;
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
        if (state == NpcState.Talking || state == NpcState.Trading)
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
            _ => NpcState.Talking
        };
    }

    private void RestoreSavedPosition()
    {
        string key = PersistenceKey;
        if (!string.IsNullOrWhiteSpace(key) && ExplorationPlayerSession.TryGetNpcPosition(key, out Vector3 savedPosition))
            transform.position = savedPosition;
    }

    private string GetPersistenceKey()
    {
        if (!string.IsNullOrWhiteSpace(persistenceKeyOverride))
            return persistenceKeyOverride;

        if (npcData != null && !string.IsNullOrWhiteSpace(npcData.CharacterID))
            return npcData.CharacterID;

        return name;
    }

}
