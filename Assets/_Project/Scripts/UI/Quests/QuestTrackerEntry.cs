using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// One quest in the quest tracker: its title, then the steps of the stage under way (QuestTrackerStep,
// cloned from the template under Steps), or the objective alone when it has none. Steps going With
// Previous make one stage and show together (QuestData). The count goes on the step it belongs to; a
// completed quest has its steps ticked and struck out.
// A stage done: its steps are struck out, stay a moment, slide away to the right, the room under the
// title becomes as tall as the next stage needs, and the next stage slides in from the right. The
// Steps object does that: its LayoutElement holds its height, its layout's padding slides it, its
// RectMask2D cuts off what is past the edge and its CanvasGroup fades it.
// A quest coming in first makes room, the quests below moving down, then slides in from the right.
// One that is done stays while its last steps are struck out (its reward, if the tracker shows it,
// then comes in under them the same way, the Reward row), slides away to the right, and its room
// closes up. The same parts on the quest itself do that. Its own layout group says how tall it is, the
// gap below it included (its bottom padding: the list has none of its own, so no gap is left over
// when it goes).
public sealed class QuestTrackerEntry : MonoBehaviour
{
    private const float RoomSeconds = 0.5f;
    private const float SlideInSeconds = 0.8f;
    private const float SlideOutSeconds = 0.9f;
    // Done: how long it stays before sliding away, the strike-out of its last steps included.
    private const float StayDoneSeconds = 1.4f;
    // A stage done: how long its steps stay struck out, the strike-out included.
    private const float StageStaySeconds = 1.1f;
    private const float StepsSlideSeconds = 0.6f;
    private const float StepsResizeSeconds = 0.4f;
    // Done with a reward: after the strike-out its row makes room, slides in, and stays a while before
    // the quest goes with it.
    private const float RewardAfterSeconds = 0.8f;
    private const float RewardRoomSeconds = 0.35f;
    private const float RewardSlideSeconds = 0.6f;
    private const float RewardStaySeconds = 1.8f;

    private enum StageChange
    {
        None,
        // The stage done stays, its steps struck out.
        Staying,
        // Its steps slide away.
        Going,
        // The room becomes as tall as the next stage needs.
        Resizing,
        // The next stage slides in.
        Coming
    }

    [SerializeField] private TMP_Text title;
    [Tooltip("A step, turned off: cloned beside itself for each step.")]
    [SerializeField] private QuestTrackerStep stepTemplate;
    [SerializeField] private Color titleColor = new(1f, 0.83f, 0.5f, 1f);

    [Header("Reward")]
    [Tooltip("The row under the steps showing the reward as a done quest goes (its LayoutElement, RectMask2D, CanvasGroup and layout move it, as for the steps). Turned off otherwise.")]
    [SerializeField] private RectTransform reward;
    [SerializeField] private Image rewardIcon;
    [Tooltip("What it gives, e.g. +1 Skill Point; several rewards one after another.")]
    [SerializeField] private TMP_Text rewardText;

    private readonly List<QuestTrackerStep> steps = new();
    private RectTransform rect;
    private LayoutElement size;
    private RectMask2D cut;
    private CanvasGroup fade;
    private VerticalLayoutGroup layout;
    private RectTransform stepsRect;
    private LayoutElement stepsSize;
    private RectMask2D stepsCut;
    private CanvasGroup stepsFade;
    private VerticalLayoutGroup stepsLayout;
    private RectOffset stepsPadding;
    // 0 = no room, 1 = as tall as it is.
    private float room = 1f;
    // 0 = past the right edge, 1 = in place.
    private float slidIn = 1f;
    private bool closing;
    private float stayLeft;
    // The stage whose steps are on show; below zero = none yet.
    private int shownStage = -1;
    private int nextStage;
    private StageChange change;
    private float changeLeft;
    // The steps: 0 = past the right edge, 1 = in place.
    private float stepsIn = 1f;
    private float resize;
    private float fromHeight;
    private LayoutElement rewardSize;
    private RectMask2D rewardCut;
    private CanvasGroup rewardFade;
    private HorizontalLayoutGroup rewardLayout;
    private RectOffset rewardPadding;
    // Done with its reward to show; how long it has stayed so far.
    private bool showsReward;
    private float stayed;
    // The reward row: its room (0 to 1), then its slide in (0 = past the right edge, 1 = in place).
    private float rewardRoom;
    private float rewardIn;

    public QuestData Quest { get; private set; }
    public bool IsClosing => closing;
    // In its place, nothing moving: the player can read it.
    public bool IsIn => Quest != null && !closing && room >= 1f && slidIn >= 1f;
    // Done and still to be seen going: struck out, or sliding away (not just its room closing up).
    public bool IsLeaving => closing && (stayLeft > 0f || slidIn > 0f);
    // Filled in for its quest at least once (a new one is empty, or the template's sample).
    public bool HasShown { get; private set; }

    // The template step shows in the editor as a sample; in the game only its clones do.
    private void Awake()
    {
        rect = (RectTransform)transform;
        size = GetComponent<LayoutElement>();
        cut = GetComponent<RectMask2D>();
        fade = GetComponent<CanvasGroup>();
        layout = GetComponent<VerticalLayoutGroup>();

        if (reward != null)
        {
            reward.gameObject.SetActive(false);
            rewardSize = reward.GetComponent<LayoutElement>();
            rewardCut = reward.GetComponent<RectMask2D>();
            rewardFade = reward.GetComponent<CanvasGroup>();
            rewardLayout = reward.GetComponent<HorizontalLayoutGroup>();
            if (rewardLayout != null)
                rewardPadding = new RectOffset(rewardLayout.padding.left, rewardLayout.padding.right,
                    rewardLayout.padding.top, rewardLayout.padding.bottom);
        }

        if (stepTemplate == null)
            return;

        stepTemplate.gameObject.SetActive(false);
        stepsRect = (RectTransform)stepTemplate.transform.parent;
        stepsSize = stepsRect.GetComponent<LayoutElement>();
        stepsCut = stepsRect.GetComponent<RectMask2D>();
        stepsFade = stepsRect.GetComponent<CanvasGroup>();
        stepsLayout = stepsRect.GetComponent<VerticalLayoutGroup>();
        if (stepsLayout != null)
            stepsPadding = new RectOffset(stepsLayout.padding.left, stepsLayout.padding.right, stepsLayout.padding.top,
                stepsLayout.padding.bottom);
    }

    // Given a quest to show: coming in, or in place at once (what was there when the scene loaded).
    public void Begin(QuestData quest, bool comingIn)
    {
        Quest = quest;
        HasShown = false;
        closing = false;
        room = comingIn ? 0f : 1f;
        slidIn = comingIn ? 0f : 1f;
        shownStage = -1;
        showsReward = false;
        foreach (QuestTrackerStep step in steps)
            step.Forget();

        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        if (reward != null)
            reward.gameObject.SetActive(false);

        EndStageChange();
        Apply();
    }

    // Done (or failed): it goes, after a moment with its steps struck out, and with its reward shown
    // under them first if it has one to show. A stage on its way out or in is just there.
    public void Close(bool withReward)
    {
        if (closing)
            return;

        closing = true;
        showsReward = withReward && reward != null && Quest != null && Quest.RewardCount > 0;
        stayed = 0f;
        stayLeft = showsReward
            ? RewardAfterSeconds + RewardRoomSeconds + RewardSlideSeconds + RewardStaySeconds
            : StayDoneSeconds;
        EndStageChange();
        if (showsReward)
            FillReward();
    }

    public void Show(bool completed)
    {
        if (Quest == null)
            return;

        HasShown = true;
        if (title != null)
        {
            if (title.text != Quest.Title)
                title.text = Quest.Title;

            title.color = titleColor;
        }

        IReadOnlyList<QuestStep> questSteps = Quest.Steps;
        if (questSteps == null || questSteps.Count == 0)
        {
            GetStep(0).Show(Quest.ObjectiveLabel, Count(),
                completed ? QuestTrackerStepState.Done : QuestTrackerStepState.Current);
            HideStepsFrom(1);
            return;
        }

        // A new stage under way: the one on show goes first (Update), then it comes.
        int current = Quest.CurrentStage;
        if (shownStage < 0)
            shownStage = current;
        else if (!completed && !closing && change == StageChange.None && current != shownStage)
        {
            change = StageChange.Staying;
            changeLeft = StageStaySeconds;
            nextStage = current;
        }

        ShowStage(shownStage, completed);
    }

    // Coming in: room first, then the slide. Going: the stay, the slide, then the room closes. Meanwhile
    // a stage may give way to the next.
    private void Update()
    {
        float step = Time.unscaledDeltaTime;
        if (closing)
        {
            if (stayLeft > 0f)
            {
                stayLeft -= step;
                stayed += step;
                if (showsReward && stayed >= RewardAfterSeconds)
                    BringReward(step);

                return;
            }

            if (slidIn > 0f)
                slidIn = Mathf.MoveTowards(slidIn, 0f, step / SlideOutSeconds);
            else
                room = Mathf.MoveTowards(room, 0f, step / RoomSeconds);

            Apply();
            if (room <= 0f)
            {
                Quest = null;
                closing = false;
                gameObject.SetActive(false);
            }

            return;
        }

        if (change != StageChange.None)
            ChangeStage(step);

        if (room >= 1f && slidIn >= 1f)
            return;

        if (room < 1f)
            room = Mathf.MoveTowards(room, 1f, step / RoomSeconds);
        else
            slidIn = Mathf.MoveTowards(slidIn, 1f, step / SlideInSeconds);

        Apply();
    }

    private void ChangeStage(float step)
    {
        switch (change)
        {
            case StageChange.Staying:
                changeLeft -= step;
                if (changeLeft <= 0f)
                    change = StageChange.Going;
                break;
            case StageChange.Going:
                stepsIn = Mathf.MoveTowards(stepsIn, 0f, step / StepsSlideSeconds);
                if (stepsIn <= 0f)
                {
                    // Out of sight: the next stage's steps take their place, the room held as it was.
                    fromHeight = stepsLayout != null ? stepsLayout.preferredHeight : 0f;
                    shownStage = nextStage;
                    foreach (QuestTrackerStep shown in steps)
                        shown.Forget();

                    ShowStage(shownStage, false);
                    resize = 0f;
                    change = StageChange.Resizing;
                }

                break;
            case StageChange.Resizing:
                resize = Mathf.MoveTowards(resize, 1f, step / StepsResizeSeconds);
                if (resize >= 1f)
                    change = StageChange.Coming;
                break;
            case StageChange.Coming:
                stepsIn = Mathf.MoveTowards(stepsIn, 1f, step / StepsSlideSeconds);
                if (stepsIn >= 1f)
                    change = StageChange.None;
                break;
        }

        ApplySteps();
    }

    // What the quest gives: the first reward's icon, and every reward's words, e.g. +1 Skill Point.
    private void FillReward()
    {
        List<string> parts = new();
        for (int i = 0; i < Quest.RewardCount; i++)
        {
            QuestRewardData given = Quest.GetReward(i);
            if (given != null && !string.IsNullOrWhiteSpace(given.Label))
                parts.Add(given.Label);
        }

        if (rewardText != null)
            rewardText.text = string.Join(", ", parts);

        if (rewardIcon != null)
        {
            rewardIcon.sprite = Quest.RewardIcon;
            rewardIcon.enabled = rewardIcon.sprite != null;
        }
    }

    // The reward row: room first, then the slide in.
    private void BringReward(float step)
    {
        if (!reward.gameObject.activeSelf)
        {
            rewardRoom = 0f;
            rewardIn = 0f;
            reward.gameObject.SetActive(true);
        }
        else if (rewardRoom < 1f)
            rewardRoom = Mathf.MoveTowards(rewardRoom, 1f, step / RewardRoomSeconds);
        else if (rewardIn < 1f)
            rewardIn = Mathf.MoveTowards(rewardIn, 1f, step / RewardSlideSeconds);
        else
            return;

        bool moving = rewardRoom < 1f || rewardIn < 1f;
        if (rewardCut != null && rewardCut.enabled != moving)
            rewardCut.enabled = moving;

        if (rewardSize != null)
        {
            bool held = rewardRoom < 1f && rewardLayout != null;
            rewardSize.preferredHeight = held ? rewardLayout.preferredHeight * Mathf.SmoothStep(0f, 1f, rewardRoom) : -1f;
            rewardSize.minHeight = held ? 0f : -1f;
        }

        float slide = Mathf.SmoothStep(0f, 1f, rewardIn);
        if (rewardLayout != null && rewardPadding != null)
        {
            int away = Mathf.RoundToInt((1f - slide) * reward.rect.width);
            if (rewardLayout.padding.left != rewardPadding.left + away)
                rewardLayout.padding = new RectOffset(rewardPadding.left + away, rewardPadding.right - away,
                    rewardPadding.top, rewardPadding.bottom);
        }

        if (rewardFade != null)
            rewardFade.alpha = slide;
    }

    private void EndStageChange()
    {
        change = StageChange.None;
        stepsIn = 1f;
        ApplySteps();
    }

    // The steps of one stage: done ones ticked and struck out, the others to do.
    private void ShowStage(int stage, bool completed)
    {
        string count = Count();
        int used = 0;
        IReadOnlyList<QuestStep> questSteps = Quest.Steps;
        for (int i = 0; i < questSteps.Count; i++)
        {
            if (questSteps[i] == null || string.IsNullOrWhiteSpace(questSteps[i].label) || Quest.StageOf(i) != stage)
                continue;

            QuestTrackerStepState state = completed || Quest.IsStepDone(i)
                ? QuestTrackerStepState.Done
                : QuestTrackerStepState.Current;
            GetStep(used++).Show(questSteps[i].label, Quest.StepShowsCount(i) ? count : string.Empty, state);
        }

        HideStepsFrom(used);
    }

    private string Count()
    {
        return QuestManager.TryGetObjectiveProgress(Quest, out int current, out int target)
            ? $"{current}/{target}"
            : string.Empty;
    }

    private void HideStepsFrom(int used)
    {
        for (int i = used; i < steps.Count; i++)
        {
            if (steps[i].gameObject.activeSelf)
                steps[i].gameObject.SetActive(false);
        }
    }

    private void Apply()
    {
        bool moving = room < 1f || slidIn < 1f;
        if (cut != null && cut.enabled != moving)
            cut.enabled = moving;

        // Eased at both ends, so it neither jumps when it starts closing nor snaps shut at the end. Its
        // least height goes too meanwhile (the box of a step would keep it at that), or the list would
        // give it that much at once and it would grow from there, and close up only down to it.
        if (size != null)
        {
            bool held = room < 1f && layout != null;
            size.preferredHeight = held ? layout.preferredHeight * Mathf.SmoothStep(0f, 1f, room) : -1f;
            size.minHeight = held ? 0f : -1f;
        }

        float slide = Mathf.SmoothStep(0f, 1f, slidIn);
        if (layout != null)
        {
            int away = Mathf.RoundToInt((1f - slide) * rect.rect.width);
            if (layout.padding.left != away)
                layout.padding = new RectOffset(away, -away, layout.padding.top, layout.padding.bottom);
        }

        if (fade != null)
            fade.alpha = slide;
    }

    // The steps sliding and their room changing between two stages, as Apply does for the whole quest.
    private void ApplySteps()
    {
        if (stepsRect == null)
            return;

        bool moving = change != StageChange.None;
        if (stepsCut != null && stepsCut.enabled != moving)
            stepsCut.enabled = moving;

        if (stepsSize != null)
        {
            bool held = change == StageChange.Resizing && stepsLayout != null;
            stepsSize.preferredHeight = held
                ? Mathf.Lerp(fromHeight, stepsLayout.preferredHeight, Mathf.SmoothStep(0f, 1f, resize))
                : -1f;
            stepsSize.minHeight = held ? 0f : -1f;
        }

        float slide = Mathf.SmoothStep(0f, 1f, stepsIn);
        if (stepsLayout != null && stepsPadding != null)
        {
            int away = Mathf.RoundToInt((1f - slide) * stepsRect.rect.width);
            if (stepsLayout.padding.left != stepsPadding.left + away)
                stepsLayout.padding = new RectOffset(stepsPadding.left + away, stepsPadding.right - away,
                    stepsPadding.top, stepsPadding.bottom);
        }

        if (stepsFade != null)
            stepsFade.alpha = slide;
    }

    private QuestTrackerStep GetStep(int index)
    {
        while (steps.Count <= index)
        {
            QuestTrackerStep step = Instantiate(stepTemplate, stepTemplate.transform.parent);
            step.name = "Step";
            steps.Add(step);
        }

        QuestTrackerStep shown = steps[index];
        if (!shown.gameObject.activeSelf)
            shown.gameObject.SetActive(true);

        return shown;
    }
}
