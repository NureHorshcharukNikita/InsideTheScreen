using System;
using System.Collections.Generic;
using UnityEngine;

// What Haru and his side say during one fight, and how that fight goes as a first lesson, e.g. the
// wolf on the south road: Dad and Haru talk it through as it happens. Each line is said once, at its
// moment: in a bubble over the speaker, or as a dialogue in the dialogue box, which holds the fight
// until it is read; the pointer shows what it is about. Picked by its encounter (BattleTalk).
[CreateAssetMenu(fileName = "BT_", menuName = "Game/Battle/Battle Talk")]
public sealed class BattleTalkData : ScriptableObject
{
    [Tooltip("The encounter it is for, e.g. a1_forest_wolves.")]
    [SerializeField] private string encounterId;

    [Header("The fight")]
    [Tooltip("Lost when an ally falls too, not only Haru, e.g. Dad in the first fight.")]
    [SerializeField] private bool lostIfAllyFalls;
    [Tooltip("Lost this many times in a row, the enemy has had enough and runs off: the fight counts as won, without loot. 0 = never.")]
    [SerializeField, Min(0)] private int lossesBeforeEnemyFlees;
    [Tooltip("Shown over the end screen when it runs off.")]
    [SerializeField] private string enemyFledTitle = "IT RAN OFF";
    [Tooltip("Set when it runs off, for the talk after the fight.")]
    [SerializeField, StoryFlag] private string enemyFledFlag;

    [Header("Lines")]
    [SerializeField] private List<BattleTalkLine> lines = new();

    public string EncounterId => encounterId;
    public bool LostIfAllyFalls => lostIfAllyFalls;
    public int LossesBeforeEnemyFlees => lossesBeforeEnemyFlees;
    public string EnemyFledTitle => enemyFledTitle;
    public string EnemyFledFlag => enemyFledFlag;
    public IReadOnlyList<BattleTalkLine> Lines => lines;
}

[Serializable]
public sealed class BattleTalkLine
{
    public BattleMoment moment;
    [Tooltip("Turn Starts: which of the player's turns, 2 = the second.")]
    [Min(1)] public int turn = 2;
    [Tooltip("In which tries of the fight it is said: the first one, the ones after a loss, or all.")]
    public BattleTry tries = BattleTry.First;

    [Header("Said")]
    [Tooltip("A dialogue in the dialogue box instead of a bubble; it holds the fight until it is read. Names and portraits come from its lines.")]
    public DialogueData dialogue;
    public BattleSpeaker speaker;
    [TextArea(2, 4)] public string text;
    [Tooltip("The bubble stays until the player clicks, and the fight waits. Off: it goes by itself and the fight goes on.")]
    public bool waitForClick = true;

    [Header("Pointer")]
    [Tooltip("What the pointer shows meanwhile: a UiPointerTarget id (battle_hand, battle_strength, battle_end_turn, battle_draw_pile), or enemy_intent, ally_intent, healing_card. Empty = nothing.")]
    public string pointAt;
    public PointerSide pointerSide;
}

public enum BattleMoment
{
    // The first hand is dealt.
    BattleStart,
    TurnStarts,
    FirstCardPlayed,
    // No strength left in the player's turn.
    OutOfStrength,
    // At half health or less in the player's turn, with something that heals in the hand.
    LowHealth,
    // The draw pile ran out and the played cards came back.
    DeckReshuffled,
    AllyFell
}

public enum BattleSpeaker
{
    Haru,
    // The first ally fighting at Haru's side, e.g. Dad.
    Ally
}

public enum BattleTry
{
    First,
    AfterLoss,
    Every
}
