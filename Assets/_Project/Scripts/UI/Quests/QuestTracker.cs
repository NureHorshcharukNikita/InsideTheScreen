using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// The quests under way, on screen under the minimap (ExplorationCanvas/QuestTracker): a see-through
// panel headed "Quests", and for each quest its title and its steps with the boxes of the quest log
// (QuestTrackerEntry, cloned from the template under List). Newest first, like the quest log; past
// five, a line says how many more there are. The counts move as Haru gathers or wins a battle.
// It is what tells the player a quest was received or done (no toast does): a new quest comes in at
// the top, sliding in from the right; a quest done gets its steps struck out and slides away to the
// right (QuestTrackerEntry). One after the other: a new quest waits for the one done to go. The panel itself slides in from the right with the first quest and away
// with the last. Back in sight after a talk or a cutscene, it waits a moment before any of that.
// What was there when the scene loaded is just there. Once the first quest is in, the "Press J to
// view your quests" toast follows, once per game (QuestNotificationToast).
// Clicking the tab with the arrow on the left slides the panel off to the right, the tab alone left
// at the edge of the screen (arrow pointing left), and clicking it again brings it back (arrow
// pointing right); saved with the game (ui_quest_tracker_hidden), so a new game starts with it out
// and its first quest is seen coming in. Slid away, it does not come out by itself: when a quest
// comes in or is done, the tab hops out from the edge a couple of times, its arrow flashing, and for
// a new quest a dot stays on the tab (Badge) until the panel is brought back.
// It is there while the player has control and a quest is in it: not in a talk, a cutscene or an
// open window. Where the minimap is hidden (indoors), it moves up into its place.
[RequireComponent(typeof(RectTransform))]
public sealed class QuestTracker : MonoBehaviour, IPointerClickHandler
{
    // Slid away by the arrow: saved with the game, so a new game starts with it out.
    private const string HiddenFlag = "ui_quest_tracker_hidden";
    private const float GapBelowMinimap = 18f;
    // Without a minimap: from the top right corner of the screen.
    private const float ScreenMargin = 45f;
    private const int MaxQuests = 5;
    private const float RefreshSeconds = 0.5f;
    // Slid away or back by the arrow.
    private const float SlideSeconds = 0.25f;
    // Coming in with the first quest, going with the last.
    private const float ComeAndGoSeconds = 0.7f;
    // Back in sight after a talk or a cutscene: how long it shows the quests as they were before
    // what changed meanwhile is ticked, struck out or counted, or a new one comes in.
    private const float SettleSeconds = 0.3f;
    // The first quest in: how long before the "Press J to view your quests" toast.
    private const float HintDelaySeconds = 0.6f;
    private const float ArrowTurnSeconds = 0.18f;
    // Slid away, something new: the tab hops out from the edge twice, this far, in this long.
    private const float NudgeSeconds = 0.9f;
    private const float NudgeDistance = 16f;
    private const float BadgePulseSpeed = 4f;
    private const float BadgePulseScale = 0.12f;
    private static readonly Color ArrowFlashColor = Color.white;
    private static readonly Vector3[] Corners = new Vector3[4];

    // A quest came in while the panel was slid away, and it has not been brought back since: kept over
    // battles (the scene loads anew).
    private static bool newWhileHidden;

    [Tooltip("The minimap (MinimapMask): the tracker stands under it, or in its place while it is hidden.")]
    [SerializeField] private RectTransform minimap;

    [Header("Parts")]
    [Tooltip("The panel and the tab, which slide together. Off while there is nothing to show.")]
    [SerializeField] private RectTransform slider;
    [SerializeField] private RectTransform panel;
    [Tooltip("Clicked to slide the panel away or bring it back.")]
    [SerializeField] private RectTransform tab;
    [Tooltip("Drawn pointing right; turned to point left while the panel is slid away.")]
    [SerializeField] private RectTransform arrow;
    [Tooltip("The dot on the tab: a quest came in while the panel was slid away. Turned off otherwise.")]
    [SerializeField] private RectTransform badge;
    [Tooltip("A quest, turned off: cloned beside itself for each quest.")]
    [SerializeField] private QuestTrackerEntry entryTemplate;
    [Tooltip("\"+2 more in the quest log\", past five quests.")]
    [SerializeField] private TMP_Text moreText;

    private readonly List<QuestData> active = new();
    private readonly List<QuestTrackerEntry> entries = new();
    private RectTransform rect;
    private bool hidden;
    // Past the first look: quests coming in from now on open out.
    private bool lookedBefore;
    // 0 = slid away, the tab alone at the edge of the screen; 1 = out under the minimap.
    private float shown = 1f;
    // 0 = all of it past the edge of the screen, tab and all (no quest in it); 1 = there.
    private float present;
    // 0 = pointing right (slide it away), 180 = pointing left (bring it out).
    private float arrowAngle;
    private bool dirty = true;
    private float nextRefresh;
    // When it last came into sight (unscaled time).
    private float visibleSince = float.NegativeInfinity;
    // A quest under way is not in it yet.
    private bool waiting;
    // When to show the quest log hint (unscaled time); below zero = not now.
    private float hintAt = -1f;
    // How far the tab's hops have got, 0 to 1; 1 = still.
    private float nudge = 1f;
    // Where the tab stands beside the panel (it hops out from there).
    private float tabX;
    private Image arrowImage;
    private Color arrowColor = Color.white;

    private void Awake()
    {
        rect = (RectTransform)transform;
        TakeHiddenFromSave();
        if (arrow != null && arrow.TryGetComponent(out arrowImage))
            arrowColor = arrowImage.color;

        if (tab != null)
            tabX = tab.anchoredPosition.x;

        if (badge != null)
            badge.gameObject.SetActive(false);

        if (entryTemplate != null)
            entryTemplate.gameObject.SetActive(false);

        if (moreText != null)
            moreText.gameObject.SetActive(false);

        if (slider != null)
            slider.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        QuestManager.QuestChanged += HandleQuestChanged;
        QuestManager.QuestProgressChanged += HandleQuest;
        StoryFlagManager.Changed += MarkDirty;
        GameStateManager.StateChanged += MarkDirty;
        dirty = true;
    }

    private void OnDisable()
    {
        QuestManager.QuestChanged -= HandleQuestChanged;
        QuestManager.QuestProgressChanged -= HandleQuest;
        StoryFlagManager.Changed -= MarkDirty;
        GameStateManager.StateChanged -= MarkDirty;
    }

    // Now and then besides the events: a Collect Cards count follows the inventory, which says
    // nothing when cards come or go (a battle, the shop, crafting). Every frame while a quest is
    // closing up, to let the panel go as soon as the last one has.
    private void Update()
    {
        if (slider == null || entryTemplate == null)
            return;

        if (hintAt >= 0f && Time.unscaledTime >= hintAt && GameStateManager.IsGameplay)
        {
            hintAt = -1f;
            QuestNotificationToast.ShowQuestLogHint();
        }

        if (!dirty && !AnyClosing() && Time.unscaledTime < nextRefresh)
            return;

        dirty = false;
        nextRefresh = Time.unscaledTime + RefreshSeconds;
        Refresh();
    }

    private void LateUpdate()
    {
        PlaceUnderMinimap();
        if (slider != null && slider.gameObject.activeSelf)
            Slide();
    }

    // The tab slides it away or brings it back; the panel itself does nothing.
    public void OnPointerClick(PointerEventData eventData)
    {
        if (tab == null || eventData.button != PointerEventData.InputButton.Left
            || !RectTransformUtility.RectangleContainsScreenPoint(tab, eventData.position, eventData.pressEventCamera))
            return;

        hidden = !hidden;
        if (hidden)
        {
            StoryFlagManager.SetFlag(HiddenFlag);
        }
        else
        {
            newWhileHidden = false;
            StoryFlagManager.ClearFlag(HiddenFlag);
        }
    }

    // As the game was saved: out unless slid away then (a new game has it out). At once, no sliding.
    private void TakeHiddenFromSave()
    {
        hidden = StoryFlagManager.HasFlag(HiddenFlag);
        shown = hidden ? 0f : 1f;
        arrowAngle = hidden ? 180f : 0f;
        if (!hidden)
            newWhileHidden = false;
    }

    private void HandleQuestChanged(QuestData quest, QuestStatus status)
    {
        dirty = true;
    }

    private void HandleQuest(QuestData quest)
    {
        dirty = true;
    }

    private void MarkDirty()
    {
        dirty = true;
    }

    private void Refresh()
    {
        active.Clear();
        foreach (QuestData quest in QuestManager.GetReceivedQuests(null))
        {
            if (QuestManager.GetStatus(quest) == QuestStatus.Active)
                active.Add(quest);
        }

        // The first look of the scene: what is under way is just there, the panel with it (or not).
        if (!lookedBefore)
        {
            lookedBefore = true;
            // The save may be read after Awake: out or slid away as it says.
            TakeHiddenFromSave();
            Admit(false);
            present = HasQuests() ? 1f : 0f;
        }

        // A quest not in yet keeps it in sight (past the edge while nothing else is), to come in.
        waiting = false;
        foreach (QuestData quest in active)
            waiting |= Find(quest) == null;

        float now = Time.unscaledTime;
        bool visible = GameStateManager.IsGameplay && (HasQuests() || present > 0f || waiting);
        if (slider.gameObject.activeSelf != visible)
        {
            slider.gameObject.SetActive(visible);
            if (visible)
                visibleSince = now;
        }

        if (!visible)
            return;

        // Just back in sight (after a talk or a cutscene): the quests as they were for a moment, then
        // what changed meanwhile: done ones struck out and going, new ones coming in, counts moving.
        float settled = visibleSince + SettleSeconds;
        bool settling = now < settled;
        if (settling)
        {
            nextRefresh = Mathf.Min(nextRefresh, settled);
        }
        else
        {
            // Done: its reward shown in it as it goes, unless the panel is slid away (then a toast).
            int closed = 0;
            foreach (QuestTrackerEntry entry in entries)
            {
                if (entry.Quest != null && !entry.IsClosing && !active.Contains(entry.Quest))
                {
                    entry.Close(!hidden && QuestNotificationToast.TakeReward(entry.Quest));
                    closed++;
                }
            }

            // Rewards of quests not in it (done in a battle, started and done in one talk): toasts.
            QuestNotificationToast.ShowUntakenRewards();

            // One at a time: a quest done goes first (struck out, sliding away), then the new one comes
            // in, its room opening as the other's closes, as if it took its place.
            // With the panel away (the first quest of the game), the quest is in it at once and the
            // panel slides in with it, one movement.
            int admitted = entries.Exists(entry => entry.IsLeaving) ? 0 : Admit(present > 0f);
            // Slid away: the tab calls for a look instead (and keeps a dot for a new quest).
            if (hidden && (admitted > 0 || closed > 0))
            {
                nudge = 0f;
                newWhileHidden |= admitted > 0;
            }
        }

        foreach (QuestTrackerEntry entry in entries)
        {
            if (entry.Quest != null && (!settling || !entry.HasShown))
                entry.Show(QuestManager.GetStatus(entry.Quest) == QuestStatus.Completed);
        }

        int more = Mathf.Max(0, active.Count - MaxQuests);
        if (moreText != null)
        {
            if (moreText.gameObject.activeSelf != more > 0)
                moreText.gameObject.SetActive(more > 0);

            if (more > 0)
            {
                moreText.text = $"+{more} more in the quest log";
                moreText.transform.SetAsLastSibling();
            }
        }

        // The player has seen the first quest of the game in it: how to open the quest log, a moment
        // later. With more quests received than that (a later save, a test start further on), the
        // start of the game is behind them: no hint.
        if (hintAt < 0f && present >= 1f && !QuestNotificationToast.QuestLogHintShown && entries.Exists(entry => entry.IsIn))
        {
            if (QuestManager.GetReceivedQuests(null).Count > 1)
                QuestNotificationToast.SkipQuestLogHint();
            else
                hintAt = now + HintDelaySeconds;
        }
    }

    // Under way and not in yet: comes in at the top (the newest first, as the list is), up to five.
    // The ones already in stay where they are, so none jumps while another closes up. Returns how
    // many came in.
    private int Admit(bool comingIn)
    {
        int top = entryTemplate.transform.GetSiblingIndex() + 1;
        int inside = 0;
        int admitted = 0;
        foreach (QuestData quest in active)
        {
            QuestTrackerEntry entry = Find(quest);
            if (entry == null)
            {
                if (inside >= MaxQuests)
                    continue;

                entry = Take();
                entry.transform.SetSiblingIndex(top++);
                entry.Begin(quest, comingIn);
                admitted++;
            }

            inside++;
        }

        return admitted;
    }

    private QuestTrackerEntry Find(QuestData quest)
    {
        foreach (QuestTrackerEntry entry in entries)
        {
            if (entry.Quest == quest)
                return entry;
        }

        return null;
    }

    // One showing no quest (closed up before), or a new one from the template.
    private QuestTrackerEntry Take()
    {
        foreach (QuestTrackerEntry entry in entries)
        {
            if (entry.Quest == null)
                return entry;
        }

        QuestTrackerEntry made = Instantiate(entryTemplate, entryTemplate.transform.parent);
        made.name = "Quest";
        entries.Add(made);
        return made;
    }

    private bool HasQuests()
    {
        foreach (QuestTrackerEntry entry in entries)
        {
            if (entry.Quest != null)
                return true;
        }

        return false;
    }

    private bool AnyClosing()
    {
        foreach (QuestTrackerEntry entry in entries)
        {
            if (entry.IsClosing)
                return true;
        }

        return false;
    }

    // A little further out or away each frame. Away, the panel is just past the right edge of the
    // screen and the tab against it; the arrow turns to point the way the panel will go on a click.
    // With no quest left in it, the whole of it slides away past the edge, tab and all, and goes;
    // with one coming in, it slides back.
    private void Slide()
    {
        bool hasQuests = HasQuests();
        shown = Mathf.MoveTowards(shown, hidden ? 0f : 1f, Time.unscaledDeltaTime / SlideSeconds);
        // It comes in with a quest in it, never empty first; a quest waiting to come in keeps it from
        // going (switched on, past the edge).
        present = Mathf.MoveTowards(present, hasQuests ? 1f : 0f, Time.unscaledDeltaTime / ComeAndGoSeconds);
        if (!hasQuests && !waiting && present <= 0f)
        {
            slider.gameObject.SetActive(false);
            return;
        }

        float width = panel != null ? panel.rect.width : 0f;
        float gap = panel != null && tab != null ? -tabX - width : 0f;
        float margin = Mathf.Max(0f, -rect.anchoredPosition.x);
        float tabOnly = width + gap + margin;
        float allAway = tabOnly + (tab != null ? tab.rect.width : 0f) + 10f;
        float x = Mathf.Lerp(tabOnly, 0f, Mathf.SmoothStep(0f, 1f, shown));

        // The tab's hops out from the edge, each smaller than the one before, the arrow flashing with
        // them. Only while it is slid away (brought back halfway through, they stop).
        float hop = 0f;
        if (nudge < 1f)
        {
            nudge = hidden ? Mathf.Min(1f, nudge + Time.unscaledDeltaTime / NudgeSeconds) : 1f;
            hop = Mathf.Abs(Mathf.Sin(nudge * Mathf.PI * 2f)) * (1f - nudge * 0.5f);
        }

        Vector2 position = new(Mathf.Lerp(allAway, x, Mathf.SmoothStep(0f, 1f, present)), 0f);
        if (slider.anchoredPosition != position)
            slider.anchoredPosition = position;

        if (tab != null)
        {
            Vector2 tabPosition = new(tabX - hop * NudgeDistance, tab.anchoredPosition.y);
            if (tab.anchoredPosition != tabPosition)
                tab.anchoredPosition = tabPosition;
        }

        ShowBadge();
        if (arrow == null)
            return;

        if (arrowImage != null)
            arrowImage.color = Color.Lerp(arrowColor, ArrowFlashColor, hop);

        arrowAngle = Mathf.MoveTowards(arrowAngle, hidden ? 180f : 0f, Time.unscaledDeltaTime * 180f / ArrowTurnSeconds);
        Quaternion pointing = Quaternion.Euler(0f, 0f, arrowAngle);
        if (arrow.localRotation != pointing)
            arrow.localRotation = pointing;
    }

    // On the tab while a quest that came in with the panel slid away has not been seen; breathing a
    // little, so the eye finds it.
    private void ShowBadge()
    {
        if (badge == null)
            return;

        bool on = hidden && newWhileHidden;
        if (badge.gameObject.activeSelf != on)
            badge.gameObject.SetActive(on);

        if (!on)
            return;

        float scale = 1f + BadgePulseScale * Mathf.Sin(Time.unscaledTime * BadgePulseSpeed);
        badge.localScale = new Vector3(scale, scale, 1f);
    }

    // Right under the minimap, its right edge in line with the minimap's; in its place while it is
    // hidden (indoors).
    private void PlaceUnderMinimap()
    {
        if (rect.parent is not RectTransform parent)
            return;

        Vector2 position = new(-ScreenMargin, -ScreenMargin);
        if (minimap != null)
        {
            minimap.GetWorldCorners(Corners);
            Vector3 bottomRight = parent.InverseTransformPoint(Corners[3]);
            Vector3 topRight = parent.InverseTransformPoint(Corners[2]);
            float top = minimap.gameObject.activeInHierarchy ? bottomRight.y - GapBelowMinimap : topRight.y;
            position = new Vector2(topRight.x - parent.rect.xMax, top - parent.rect.yMax);
        }

        if (rect.anchoredPosition != position)
            rect.anchoredPosition = position;
    }
}
