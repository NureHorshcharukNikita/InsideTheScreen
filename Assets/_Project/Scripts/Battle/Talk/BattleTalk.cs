using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Plays the lines of this fight's BattleTalkData at their moments, as BattleSystem tells it what
// happens: one at a time, each once, once the fight is settled (no cards flying, no turn changing).
// A line is said in a bubble or as a dialogue in the dialogue box, with the pointer at what it is
// about; a bubble about something the pointer shows stands right by it, else over the speaker. While a line waits to be read the fight waits too (Is Holding): no card is
// played, the turn does not end.
public sealed class BattleTalk : MonoBehaviour
{
    [SerializeField] private BattleSystem battle;
    [SerializeField] private BattleTalkBubble bubble;
    [SerializeField] private HandUI hand;
    [Tooltip("Every Battle Talk there is; the one for this fight's encounter plays.")]
    [SerializeField] private List<BattleTalkData> talks = new();
    [Tooltip("A bubble that does not wait for a click goes after this long, plus a little for every letter.")]
    [SerializeField, Min(0.5f)] private float bubbleSeconds = 2f;
    [SerializeField, Min(0f)] private float secondsPerLetter = 0.035f;

    private readonly Queue<BattleTalkLine> queue = new();
    private readonly HashSet<BattleTalkLine> said = new();
    private BattleTalkData data;
    private bool afterLoss;
    private bool cardPlayed;
    private bool holding;
    private bool stopped;
    private Coroutine playing;
    private PlayerCharacter watchedPlayer;

    public BattleTalkData Data => data;
    public bool IsHolding => holding;

    private void Awake()
    {
        string encounterId = PendingBattleEnemy.EncounterId;
        foreach (BattleTalkData talk in talks)
        {
            if (talk != null && !string.IsNullOrWhiteSpace(encounterId) && talk.EncounterId == encounterId)
            {
                data = talk;
                break;
            }
        }

        afterLoss = BattleTries.LossesOf(encounterId) > 0;
    }

    private void OnEnable()
    {
        if (battle == null)
            return;

        battle.StartingHandDealt += OnStartingHandDealt;
        battle.PlayerTurnStarted += OnPlayerTurnStarted;
        battle.CardPlayed += OnCardPlayed;
        battle.DeckReshuffled += OnDeckReshuffled;
        battle.AllyFell += OnAllyFell;
        battle.HandChanged += OnHandChanged;
    }

    private void OnDisable()
    {
        if (battle != null)
        {
            battle.StartingHandDealt -= OnStartingHandDealt;
            battle.PlayerTurnStarted -= OnPlayerTurnStarted;
            battle.CardPlayed -= OnCardPlayed;
            battle.DeckReshuffled -= OnDeckReshuffled;
            battle.AllyFell -= OnAllyFell;
            battle.HandChanged -= OnHandChanged;
        }

        WatchPlayer(null);
        Stop();
    }

    // The fight is over: nothing more is said.
    public void Stop()
    {
        stopped = true;
        queue.Clear();
        if (playing != null)
        {
            StopCoroutine(playing);
            playing = null;
        }

        if (bubble != null)
            bubble.Hide();

        UiPointer.HideAll();
        holding = false;
    }

    private void OnStartingHandDealt()
    {
        WatchPlayer(battle.Player);
        Reach(BattleMoment.BattleStart);
    }

    private void OnPlayerTurnStarted(int turn)
    {
        Reach(BattleMoment.TurnStarts, turn);
        CheckLowHealth();
    }

    private void OnCardPlayed()
    {
        if (!cardPlayed)
        {
            cardPlayed = true;
            Reach(BattleMoment.FirstCardPlayed);
        }
    }

    private void OnDeckReshuffled()
    {
        Reach(BattleMoment.DeckReshuffled);
    }

    private void OnAllyFell(EnemyCharacter ally)
    {
        Reach(BattleMoment.AllyFell);
    }

    private void OnHandChanged(DeckManager _)
    {
        CheckLowHealth();
    }

    private void OnActionPointsChanged(int current, int max)
    {
        if (current <= 0 && battle.IsPlayerTurn)
            Reach(BattleMoment.OutOfStrength);
    }

    private void WatchPlayer(PlayerCharacter player)
    {
        if (watchedPlayer != null)
            watchedPlayer.ActionPointsChanged -= OnActionPointsChanged;

        watchedPlayer = player;
        if (watchedPlayer != null)
            watchedPlayer.ActionPointsChanged += OnActionPointsChanged;
    }

    private void CheckLowHealth()
    {
        if (!battle.IsPlayerTurn || hand == null || hand.FindCardView(Heals) == null)
            return;

        if (IsLow(battle.Player))
            Reach(BattleMoment.LowHealth);

        if (IsLow(battle.FirstAlly))
            Reach(BattleMoment.AllyLowHealth);
    }

    private static bool IsLow(Character fighter)
    {
        return fighter != null && fighter.IsAlive && fighter.CurrentHealth * 2 <= fighter.MaxHealth;
    }

    private static bool Heals(CardData card)
    {
        foreach (CardInstantEffectEntry entry in card.InstantEffects)
        {
            if (entry != null && entry.Effect is HealEffect)
                return true;
        }

        return false;
    }

    // Lines of this moment wait their turn, in the order they are listed.
    private void Reach(BattleMoment moment, int turn = 0)
    {
        if (data == null || stopped)
            return;

        foreach (BattleTalkLine line in data.Lines)
        {
            if (line == null || line.moment != moment || said.Contains(line) || !IsSaidThisTry(line))
                continue;

            if (moment == BattleMoment.TurnStarts && line.turn != turn)
                continue;

            said.Add(line);
            queue.Enqueue(line);
        }

        if (queue.Count > 0 && playing == null)
            playing = StartCoroutine(PlayQueue());
    }

    private bool IsSaidThisTry(BattleTalkLine line)
    {
        return line.tries == BattleTry.Every || (line.tries == BattleTry.AfterLoss) == afterLoss;
    }

    private IEnumerator PlayQueue()
    {
        while (queue.Count > 0)
        {
            // Said into a fight that is settled: not over cards flying in, nor a turn changing.
            while (!battle.IsSettled)
            {
                if (battle.CurrentBattleState != BattleState.Running)
                {
                    playing = null;
                    Stop();
                    yield break;
                }

                yield return null;
            }

            BattleTalkLine line = queue.Dequeue();
            yield return Say(line);
            UiPointer.HideAll();
            holding = false;
        }

        playing = null;
    }

    private IEnumerator Say(BattleTalkLine line)
    {
        Point(line);

        if (line.dialogue != null)
        {
            DialogueManager dialogues = DialogueManager.Instance;
            bool done = false;
            holding = true;
            if (dialogues != null && dialogues.StartDialogue(line.dialogue, 0, null, _ => done = true))
                yield return new WaitUntil(() => done);

            yield break;
        }

        Character speaker = line.speaker == BattleSpeaker.Ally ? battle.FirstAlly : battle.Player;
        if (bubble == null || speaker == null || string.IsNullOrWhiteSpace(line.text))
            yield break;

        holding = line.waitForClick;
        bubble.Show(speaker, line.text, line.waitForClick, besidePointer: !string.IsNullOrWhiteSpace(line.pointAt));

        if (line.waitForClick)
        {
            // Not the click that brought this moment about.
            yield return null;
            while (!ContinuePressed())
                yield return null;
        }
        else
        {
            yield return new WaitForSecondsRealtime(bubbleSeconds + line.text.Length * secondsPerLetter);
        }

        bubble.Hide();
    }

    private static bool ContinuePressed()
    {
        return GameInputSettings.GetKeyDown(GameInputAction.DialogueNext)
            || GameInputSettings.GetKeyDown(GameInputAction.Interact)
            || Input.GetMouseButtonDown(0);
    }

    private void Point(BattleTalkLine line)
    {
        UiPointer.HideAll();
        switch (line.pointAt)
        {
            case null:
            case "":
                return;
            case "enemy_intent":
                UiPointer.TryPointAt(battle.GetIntentRect(allySide: false), line.pointerSide);
                return;
            case "ally_intent":
                UiPointer.TryPointAt(battle.GetIntentRect(allySide: true), line.pointerSide);
                return;
            case "healing_card":
                UiPointer.TryPointAt(hand != null ? hand.FindCardView(Heals) : null, line.pointerSide);
                return;
            default:
                UiPointer.TryPointAt(line.pointAt, line.pointerSide);
                return;
        }
    }
}
