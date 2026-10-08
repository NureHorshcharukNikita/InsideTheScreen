using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class SkillPageView : MonoBehaviour
{
    private const int CategoryCount = 3;
    private const int VisibleTierCount = 5;
    private const int TreePageCount = 5;
    private const float HeaderHeight = 185f;
    private const float CategoryHeaderY = -44f;
    private const float TierSpacing = 235f;
    private const float NodeWidth = 132f;
    private const float NodeHeight = 148f;
    private const float NodeLabelClearance = 28f;
    // The bar is as thick as the arrow shaft (25 of 76 sprite pixels at ArrowWidth),
    // so bars and arrows join without steps.
    private const float ConnectionThickness = 13f;
    private const float ArrowWidth = 35f;
    private const float ArrowMinLength = 39f;
    // A branching link drops a stem from the source, runs a bar this far below the label
    // and continues with an arrow from the bar down to the target.
    private const float BarDrop = 16f;
    // A skill with several incoming links gets one arrow per link, spread across its top
    // edge in the order of the source columns, so every link stays a separate line.
    private const float EntrySpacing = 64f;
    // Joint patch geometry, from the sprites' 7 px outline / 11 px fill shaft profile.
    // Offsets are from the bar top: the patch opens the bar's top outline where a stem or
    // arrow enters from above and the bottom outline where an arrow leaves below.
    private const float JointFillWidth = 5f;
    private const float JointFillAbove = 0.5f;
    private const float JointFillMiddle = 6f;
    private const float JointFillBelow = 13.5f;
    private static readonly Color ConnectionFillColor = new(0.95f, 0.65f, 0.29f, 1f);
    private const float PageButtonWidth = 106f;
    private const float PageButtonSpacing = 14f;
    private const float TreeRailWidth = 105f;
    private static readonly Color LockedConnectionColor = new(0.42f, 0.42f, 0.42f, 1f);

    [Header("Tree")]
    [SerializeField] private ScrollRect treeScroll;
    [SerializeField] private RectTransform treeContent;
    [SerializeField] private RectTransform connectionsRoot;
    [SerializeField] private RectTransform nodesRoot;
    [SerializeField] private RectTransform categoryHeadersRoot;
    [SerializeField] private RectTransform tierMarkersRoot;
    [SerializeField] private SkillTreeNodeView nodeTemplate;
    [SerializeField] private Image connectionTemplate;
    [SerializeField] private TMP_Text emptyTreeText;

    [Header("Tree Navigation")]
    [SerializeField] private List<Button> pageButtons = new();
    [SerializeField] private List<Image> pageButtonBackgrounds = new();
    [SerializeField] private List<RectTransform> tierMarkers = new();
    [SerializeField] private List<RectTransform> categoryHeaders = new();
    [SerializeField] private Sprite tierBackgroundSprite;
    [SerializeField] private Sprite selectedTierBackgroundSprite;
    [SerializeField] private Sprite lockedTierBackgroundSprite;
    [SerializeField] private Sprite lockedSelectedTierBackgroundSprite;

    [Header("Tree Art")]
    [SerializeField] private Sprite horizontalConnectionSprite;
    [SerializeField] private Sprite downArrowSprite;
    // Shown for skills without their own icon, so no skill repeats another icon.
    [SerializeField] private Sprite emptySkillIcon;

    [Header("Details")]
    [SerializeField] private GameObject detailsRoot;
    [SerializeField] private TMP_Text emptyDetailsText;
    [SerializeField] private Image skillIcon;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text rankText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text currentRankEffectText;
    [SerializeField] private TMP_Text nextRankEffectText;
    [SerializeField] private TMP_Text requirementsText;
    [SerializeField] private ScrollRect detailsScroll;

    [Header("Upgrade")]
    [SerializeField] private Button upgradeButton;
    [SerializeField] private TMP_Text upgradeButtonText;
    [SerializeField] private GameObject upgradeCostRoot;
    [SerializeField] private TMP_Text upgradeCostText;
    [SerializeField] private TMP_Text availablePointsText;

    private readonly List<SkillData> visibleSkills = new();
    private readonly List<SkillTreeNodeView> nodeViews = new();
    private readonly List<Image> connectionViews = new();
    private readonly List<(float y, float left, float right, float source, Color color)> pendingBars = new();
    private readonly List<(SkillTreeNodeView node, Color color)> incomingLinks = new();
    private readonly List<(float x, float top, float bottom, Color color)> verticals = new();
    private readonly List<(Vector2 top, Color color)> pendingStems = new();
    private readonly Dictionary<SkillData, SkillTreeNodeView> nodeBySkill = new();
    private SkillData selectedSkill;
    private int selectedTreePage = 1;
    private int usedConnectionCount;
    private Coroutine scrollResetRoutine;
    private readonly List<(RectTransform rect, float gap, float height)> detailsLayout = new();
    private float detailsBottomPadding;

    private void Awake()
    {
        EnsureDragSurface(treeScroll);
        EnsureDragSurface(detailsScroll);
        CacheDetailsLayout();

        if (upgradeButton != null)
        {
            upgradeButton.onClick.RemoveListener(UpgradeSelectedSkill);
            upgradeButton.onClick.AddListener(UpgradeSelectedSkill);
        }

        ConfigurePageButtons();
        for (int i = 0; i < pageButtons.Count; i++)
        {
            if (pageButtons[i] == null)
                continue;

            int page = i + 1;
            pageButtons[i].onClick.RemoveAllListeners();
            pageButtons[i].onClick.AddListener(() => SelectTreePage(page));
        }
    }

    private void OnEnable()
    {
        selectedSkill = null;
        selectedTreePage = 1;
        SkillProgression.Changed += HandleProgressionChanged;
        RefreshPage(true);
    }

    private void OnDisable()
    {
        SkillProgression.Changed -= HandleProgressionChanged;

        if (scrollResetRoutine != null)
        {
            StopCoroutine(scrollResetRoutine);
            scrollResetRoutine = null;
        }
    }

#if UNITY_EDITOR
    // In Edit Mode the vertical Scrollbar re-sends its stored value to the ScrollRect,
    // so a scrollbar left at the bottom shifts TreeContent up. Keep the preview at the top.
    private void OnValidate()
    {
        if (!Application.isPlaying)
            UnityEditor.EditorApplication.delayCall += ResetTreeContentInEditor;
    }

    private void ResetTreeContentInEditor()
    {
        if (this == null || Application.isPlaying || treeContent == null)
            return;

        if (treeScroll != null && treeScroll.verticalScrollbar != null)
            treeScroll.verticalScrollbar.SetValueWithoutNotify(1f);

        Vector2 position = treeContent.anchoredPosition;
        if (Mathf.Approximately(position.y, 0f))
            return;

        position.y = 0f;
        treeContent.anchoredPosition = position;
    }
#endif

    public static string GetCategoryLabel(SkillCategory category)
    {
        return category switch
        {
            SkillCategory.PhysicalKnowledge => "Physical + Knowledge",
            SkillCategory.KnowledgeMental => "Knowledge + Mental",
            _ => category.ToString()
        };
    }

    private void RefreshPage(bool resetScroll)
    {
        RefreshSkillCatalog();
        RefreshPoints();
        BuildTree();

        if (selectedSkill != null && !visibleSkills.Contains(selectedSkill))
            selectedSkill = null;

        if (selectedSkill == null)
            ShowEmptyDetails();
        else
            ShowSelectedSkill();

        // Page locks depend on progression, so the page buttons refresh every time.
        SetSelectedPage(selectedTreePage);
        if (resetScroll)
            ScheduleScrollReset(true, true);
    }

    private void RefreshSkillCatalog()
    {
        visibleSkills.Clear();
        IReadOnlyList<SkillData> skills = SkillProgression.Skills;
        for (int i = 0; i < skills.Count; i++)
        {
            SkillData skill = skills[i];
            if (skill != null && skill.TreePage == selectedTreePage)
                visibleSkills.Add(skill);
        }

        visibleSkills.Sort((left, right) =>
        {
            int orderComparison = left.TreeOrder.CompareTo(right.TreeOrder);
            if (orderComparison != 0)
                return orderComparison;

            int categoryComparison = left.Category.CompareTo(right.Category);
            return categoryComparison != 0
                ? categoryComparison
                : string.CompareOrdinal(left.DisplayName, right.DisplayName);
        });
    }

    private void BuildTree()
    {
        if (treeContent == null || nodesRoot == null || nodeTemplate == null)
            return;

        bool hasSkills = visibleSkills.Count > 0;
        int highestTier = VisibleTierCount;
        for (int i = 0; i < visibleSkills.Count; i++)
            highestTier = Mathf.Max(highestTier, visibleSkills[i].TreeOrder);

        float contentHeight = hasSkills
            ? HeaderHeight + highestTier * TierSpacing + 75f
            : GetTreeViewportHeight();
        treeContent.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, contentHeight);
        ConfigureTreeLayer(categoryHeadersRoot, contentHeight);
        ConfigureTreeLayer(tierMarkersRoot, contentHeight);
        ConfigureTreeLayer(connectionsRoot, contentHeight);
        ConfigureTreeLayer(nodesRoot, contentHeight);

        Canvas.ForceUpdateCanvases();
        float contentWidth = Mathf.Max(1f, treeContent.rect.width);
        SetTreeDecorationsVisible(hasSkills);
        PositionTreeDecorations();

        List<SkillTreeNodeView> reusableNodes = GetReusableNodes();
        nodeViews.Clear();
        nodeBySkill.Clear();

        for (int i = 0; i < visibleSkills.Count; i++)
        {
            SkillData skill = visibleSkills[i];
            SkillTreeNodeView node = i < reusableNodes.Count
                ? reusableNodes[i]
                : Instantiate(nodeTemplate, nodesRoot);

            node.gameObject.name = $"SkillNode_{skill.name}";
            node.gameObject.SetActive(true);
            node.RectTransform.anchorMin = new Vector2(0f, 1f);
            node.RectTransform.anchorMax = new Vector2(0f, 1f);
            node.RectTransform.pivot = new Vector2(0.5f, 0.5f);
            node.RectTransform.sizeDelta = new Vector2(NodeWidth, NodeHeight);
            node.RectTransform.anchoredPosition = GetNodePosition(skill, i, contentWidth);
            node.Setup(skill, emptySkillIcon, skill == selectedSkill, SelectSkill);
            nodeViews.Add(node);
            nodeBySkill[skill] = node;
        }

        for (int i = visibleSkills.Count; i < reusableNodes.Count; i++)
            reusableNodes[i].gameObject.SetActive(false);

        if (emptyTreeText != null)
        {
            emptyTreeText.text = "No skills are available on this page";
            emptyTreeText.gameObject.SetActive(!hasSkills);
            if (!hasSkills)
                CenterEmptyTreeText();
        }

        BuildConnections();
    }

    private RectTransform GetTreeViewport()
    {
        if (treeScroll == null)
            return null;

        return treeScroll.viewport != null ? treeScroll.viewport : (RectTransform)treeScroll.transform;
    }

    private float GetTreeViewportHeight()
    {
        RectTransform viewport = GetTreeViewport();
        return viewport != null ? Mathf.Max(1f, viewport.rect.height) : HeaderHeight + TierSpacing;
    }

    private void SetTreeDecorationsVisible(bool visible)
    {
        if (categoryHeadersRoot != null)
            categoryHeadersRoot.gameObject.SetActive(visible);
        if (tierMarkersRoot != null)
            tierMarkersRoot.gameObject.SetActive(visible);

        for (int i = 0; i < categoryHeaders.Count; i++)
        {
            if (categoryHeaders[i] != null)
                categoryHeaders[i].gameObject.SetActive(visible);
        }

        for (int i = 0; i < tierMarkers.Count; i++)
        {
            if (tierMarkers[i] != null)
                tierMarkers[i].gameObject.SetActive(visible);
        }
    }

    private void CenterEmptyTreeText()
    {
        // Parent to the viewport so the text is centered in the visible area,
        // independent of the content height and scroll position.
        RectTransform rect = emptyTreeText.rectTransform;
        RectTransform viewport = GetTreeViewport();
        if (viewport != null && rect.parent != viewport)
            rect.SetParent(viewport, false);

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
        emptyTreeText.alignment = TextAlignmentOptions.Center;
    }

    private static void ConfigureTreeLayer(RectTransform layer, float height)
    {
        if (layer == null)
            return;

        layer.anchorMin = new Vector2(0f, 1f);
        layer.anchorMax = new Vector2(1f, 1f);
        layer.pivot = new Vector2(0f, 1f);
        layer.anchoredPosition = Vector2.zero;
        layer.sizeDelta = new Vector2(0f, height);
    }

    private void ConfigurePageButtons()
    {
        float step = PageButtonWidth + PageButtonSpacing;
        float center = (Mathf.Min(pageButtons.Count, TreePageCount) - 1) * 0.5f;
        for (int i = 0; i < pageButtons.Count; i++)
        {
            if (pageButtons[i] == null)
                continue;

            RectTransform rect = (RectTransform)pageButtons[i].transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2((i - center) * step, 0f);
            rect.sizeDelta = new Vector2(PageButtonWidth, 54f);
        }
    }

    private void PositionTreeDecorations()
    {
        for (int i = 0; i < categoryHeaders.Count; i++)
        {
            RectTransform header = categoryHeaders[i];
            if (header == null)
                continue;

            // Anchored relative to the content width, so headers stay in their columns at any width.
            float anchorX = GetCategoryAnchor(i);
            header.anchorMin = header.anchorMax = new Vector2(anchorX, 1f);
            header.pivot = new Vector2(0.5f, 0.5f);
            header.anchoredPosition = new Vector2(TreeRailWidth * (1f - anchorX), CategoryHeaderY);
        }

        for (int i = 0; i < tierMarkers.Count; i++)
        {
            RectTransform marker = tierMarkers[i];
            if (marker == null)
                continue;

            marker.anchorMin = marker.anchorMax = new Vector2(0f, 1f);
            marker.pivot = new Vector2(0.5f, 0.5f);
            marker.anchoredPosition = new Vector2(54f, GetTierY(i + 1));
            StretchTierLine(marker);
        }
    }

    // The connector hangs from the marker bottom and ends exactly at the next marker's top.
    private static void StretchTierLine(RectTransform marker)
    {
        RectTransform line = marker.Find("Line") as RectTransform;
        if (line == null)
            return;

        line.anchorMin = line.anchorMax = new Vector2(0.5f, 0f);
        line.pivot = new Vector2(0.5f, 1f);
        line.anchoredPosition = Vector2.zero;
        line.sizeDelta = new Vector2(line.sizeDelta.x, TierSpacing - marker.rect.height);
    }

    private Vector2 GetNodePosition(SkillData skill, int skillIndex, float contentWidth)
    {
        int countAtPosition = 0;
        int indexAtPosition = 0;
        for (int i = 0; i < visibleSkills.Count; i++)
        {
            SkillData candidate = visibleSkills[i];
            if (candidate.Category != skill.Category || candidate.TreeOrder != skill.TreeOrder)
                continue;

            if (i < skillIndex)
                indexAtPosition++;
            countAtPosition++;
        }

        float branchOffset = (indexAtPosition - (countAtPosition - 1) * 0.5f) * 145f;
        float usableWidth = contentWidth - TreeRailWidth;
        return new Vector2(
            TreeRailWidth + usableWidth * GetColumnAnchor(skill.Category) + branchOffset,
            GetTierY(skill.TreeOrder));
    }

    // Header anchor of the main categories (Physical, Knowledge, Mental).
    private static float GetCategoryAnchor(int categoryIndex) => (categoryIndex + 0.5f) / CategoryCount;

    // Columns from left to right: Physical | Physical+Knowledge | Knowledge | Knowledge+Mental | Mental.
    // Hybrid skills sit halfway between their two parent categories.
    private static float GetColumnAnchor(SkillCategory category)
    {
        return category switch
        {
            SkillCategory.PhysicalKnowledge => 2f / 6f,
            SkillCategory.Knowledge => 3f / 6f,
            SkillCategory.KnowledgeMental => 4f / 6f,
            SkillCategory.Mental => 5f / 6f,
            _ => 1f / 6f
        };
    }

    private static float GetTierY(int tier) => -HeaderHeight - (tier - 1) * TierSpacing;

    private void BuildConnections()
    {
        CacheConnectionViews();
        usedConnectionCount = 0;
        pendingBars.Clear();
        verticals.Clear();
        pendingStems.Clear();
        for (int i = 0; i < visibleSkills.Count; i++)
        {
            SkillData targetSkill = visibleSkills[i];
            if (!nodeBySkill.TryGetValue(targetSkill, out SkillTreeNodeView targetNode))
                continue;

            IReadOnlyList<SkillPrerequisite> prerequisites = targetSkill.Prerequisites;
            if (prerequisites == null)
                continue;

            incomingLinks.Clear();
            for (int requirementIndex = 0; requirementIndex < prerequisites.Count; requirementIndex++)
            {
                SkillPrerequisite requirement = prerequisites[requirementIndex];
                if (requirement?.Skill == null
                    || !nodeBySkill.TryGetValue(requirement.Skill, out SkillTreeNodeView sourceNode)
                    || IsShownThroughOtherPrerequisite(targetSkill, requirement.Skill))
                    continue;

                bool unlocked = SkillProgression.GetRank(requirement.Skill) >= requirement.RequiredRank;
                incomingLinks.Add((sourceNode, unlocked ? Color.white : LockedConnectionColor));
            }

            DrawIncomingLinks(targetNode.RectTransform);
        }

        // Bars go after every arrow and stem so they lie on top and hide their ends;
        // joint patches go last and open the bar into each stem and shaft.
        DrawStems();
        DrawBars();
        DrawJoints();

        for (int i = usedConnectionCount; i < connectionViews.Count; i++)
            connectionViews[i].gameObject.SetActive(false);
    }

    // Sources are ordered left to right and enter the target side by side. A link from
    // directly above keeps the center slot when possible, so it stays a straight arrow.
    private void DrawIncomingLinks(RectTransform target)
    {
        if (incomingLinks.Count == 0)
            return;

        incomingLinks.Sort((left, right) =>
            left.node.RectTransform.anchoredPosition.x.CompareTo(right.node.RectTransform.anchoredPosition.x));

        float spacing = incomingLinks.Count > 1
            ? Mathf.Min(EntrySpacing, (NodeWidth - ArrowWidth) / (incomingLinks.Count - 1))
            : 0f;
        float anchorSlot = (incomingLinks.Count - 1) * 0.5f;
        for (int i = 0; i < incomingLinks.Count; i++)
        {
            if (Mathf.Abs(incomingLinks[i].node.RectTransform.anchoredPosition.x - target.anchoredPosition.x) <= 2f)
                anchorSlot = i;
        }

        float maxOffset = (NodeWidth - ArrowWidth) * 0.5f;
        for (int i = 0; i < incomingLinks.Count; i++)
        {
            float offset = Mathf.Clamp((i - anchorSlot) * spacing, -maxOffset, maxOffset);
            DrawConnection(incomingLinks[i].node.RectTransform, target, offset, incomingLinks[i].color);
        }
    }

    // Bars of one source on one row with the same state are drawn as one bar, so a source
    // feeding several targets forms one fork. Bars of different sources are never joined.
    private void DrawBars()
    {
        pendingBars.Sort((left, right) =>
        {
            int rowComparison = right.y.CompareTo(left.y);
            if (rowComparison != 0)
                return rowComparison;

            int sourceComparison = left.source.CompareTo(right.source);
            if (sourceComparison != 0)
                return sourceComparison;

            int colorComparison = left.color.r.CompareTo(right.color.r);
            return colorComparison != 0 ? colorComparison : left.left.CompareTo(right.left);
        });

        int index = 0;
        while (index < pendingBars.Count)
        {
            (float y, float left, float right, float source, Color color) bar = pendingBars[index++];
            while (index < pendingBars.Count
                && Mathf.Abs(pendingBars[index].y - bar.y) < 0.5f
                && Mathf.Abs(pendingBars[index].source - bar.source) < 0.5f
                && pendingBars[index].color == bar.color
                && pendingBars[index].left <= bar.right + 0.5f)
            {
                bar.right = Mathf.Max(bar.right, pendingBars[index].right);
                index++;
            }

            AddSegment(horizontalConnectionSprite,
                new Vector2((bar.left + bar.right) * 0.5f, bar.y - ConnectionThickness * 0.5f),
                new Vector2(bar.right - bar.left + ConnectionThickness, ConnectionThickness), bar.color, true);
        }
    }

    // A stem is the bar sprite turned upright, so it has the same profile as the bar
    // and the arrow shafts. It ends in the middle of the bar, where the bar hides its end.
    private void DrawStems()
    {
        for (int i = 0; i < pendingStems.Count; i++)
        {
            (Vector2 top, Color color) = pendingStems[i];
            float length = BarDrop + ConnectionThickness * 0.5f;
            AddSegment(horizontalConnectionSprite, new Vector2(top.x, top.y - length * 0.5f),
                new Vector2(length, ConnectionThickness), color, true, 90f);
            verticals.Add((top.x, top.y, top.y - length, color));
        }
    }

    private void DrawJoints()
    {
        for (int i = 0; i < verticals.Count; i++)
        {
            (float x, float top, float bottom, Color color) = verticals[i];
            for (int barIndex = 0; barIndex < pendingBars.Count; barIndex++)
            {
                (float barTop, float left, float right, float _, Color _) = pendingBars[barIndex];
                if (x < left - 0.5f || x > right + 0.5f || top < barTop - ConnectionThickness || bottom > barTop)
                    continue;

                float patchTop = top > barTop + 0.5f ? barTop + JointFillAbove : barTop - JointFillMiddle;
                float patchBottom = bottom < barTop - ConnectionThickness ? barTop - JointFillBelow : barTop - JointFillMiddle;
                if (patchTop - patchBottom < 0.5f)
                    continue;

                AddFill(new Vector2(x, (patchTop + patchBottom) * 0.5f),
                    new Vector2(JointFillWidth, patchTop - patchBottom), ConnectionFillColor * color);
                break;
            }
        }
    }

    // A link A -> C is skipped when C also requires B and B itself (transitively) requires A:
    // the chain A -> B -> C already shows it, and a direct line would cross B.
    private static bool IsShownThroughOtherPrerequisite(SkillData target, SkillData source)
    {
        IReadOnlyList<SkillPrerequisite> prerequisites = target.Prerequisites;
        if (prerequisites == null)
            return false;

        for (int i = 0; i < prerequisites.Count; i++)
        {
            SkillData other = prerequisites[i]?.Skill;
            if (other != null && other != source && RequiresSkill(other, source, 0))
                return true;
        }

        return false;
    }

    private static bool RequiresSkill(SkillData skill, SkillData required, int depth)
    {
        if (skill == null || depth > 16)
            return false;

        IReadOnlyList<SkillPrerequisite> prerequisites = skill.Prerequisites;
        if (prerequisites == null)
            return false;

        for (int i = 0; i < prerequisites.Count; i++)
        {
            SkillData prerequisite = prerequisites[i]?.Skill;
            if (prerequisite == required || RequiresSkill(prerequisite, required, depth + 1))
                return true;
        }

        return false;
    }

    private void DrawConnection(RectTransform source, RectTransform target, float entryOffset, Color color)
    {
        // A straight link is one arrow from just below the source name label to the target frame.
        // A branching link drops a stem from the source to a bar, the bar runs to the target
        // column, and the same arrow continues from the bar down to the target.
        Vector2 start = source.anchoredPosition + new Vector2(0f, -NodeHeight * 0.5f - NodeLabelClearance);
        Vector2 tip = target.anchoredPosition + new Vector2(entryOffset, NodeHeight * 0.5f);
        bool branching = Mathf.Abs(start.x - tip.x) > 2f;
        float arrowTop = branching ? start.y - BarDrop : start.y;

        float length = Mathf.Max(ArrowMinLength, arrowTop - tip.y);
        AddSegment(downArrowSprite, new Vector2(tip.x, tip.y + length * 0.5f), new Vector2(ArrowWidth, length), color, true);
        verticals.Add((tip.x, arrowTop, tip.y, color));

        if (!branching)
            return;

        pendingBars.Add((arrowTop, Mathf.Min(start.x, tip.x), Mathf.Max(start.x, tip.x), start.x, color));
        if (!pendingStems.Contains((start, color)))
            pendingStems.Add((start, color));
    }

    private void AddSegment(Sprite sprite, Vector2 position, Vector2 size, Color color, bool sliced = false, float rotation = 0f)
    {
        if (sprite != null)
            AddImage(sprite, position, size, color, sliced, rotation);
    }

    // A sprite-less image is a plain rectangle in its color, used for the joint patches.
    private void AddFill(Vector2 position, Vector2 size, Color color) => AddImage(null, position, size, color, false, 0f);

    private void AddImage(Sprite sprite, Vector2 position, Vector2 size, Color color, bool sliced, float rotation)
    {
        if (connectionTemplate == null || connectionsRoot == null)
            return;

        Image image;
        if (usedConnectionCount < connectionViews.Count)
        {
            image = connectionViews[usedConnectionCount];
        }
        else
        {
            image = Instantiate(connectionTemplate, connectionsRoot);
            connectionViews.Add(image);
        }

        usedConnectionCount++;
        // Draw order follows the call order, so bars added last render over the arrows.
        image.transform.SetAsLastSibling();
        image.gameObject.SetActive(true);
        image.sprite = sprite;
        image.color = color;
        image.preserveAspect = false;
        image.raycastTarget = false;
        // Sliced keeps the arrow head and the bar ends undistorted and stretches only the middle;
        // the multiplier scales the sprite borders to the drawn thickness (the shorter side).
        image.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
        image.pixelsPerUnitMultiplier = sliced
            ? (size.x >= size.y ? sprite.rect.height / size.y : sprite.rect.width / size.x)
            : 1f;

        RectTransform rect = image.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        rect.localRotation = Quaternion.Euler(0f, 0f, rotation);
    }

    private void SelectSkill(SkillData skill)
    {
        if (skill == null)
            return;

        selectedSkill = skill;
        for (int i = 0; i < nodeViews.Count; i++)
            nodeViews[i].SetSelected(nodeViews[i].Skill == selectedSkill);

        ShowSelectedSkill();
        ScheduleScrollReset(false, true);
    }

    private void ShowSelectedSkill()
    {
        if (selectedSkill == null)
        {
            ShowEmptyDetails();
            return;
        }

        if (detailsRoot != null)
        {
            RectTransform detailsRect = detailsRoot.transform as RectTransform;
            if (detailsRect != null)
            {
                detailsRect.anchorMin = new Vector2(0f, 1f);
                detailsRect.anchorMax = new Vector2(1f, 1f);
                detailsRect.pivot = new Vector2(0f, 1f);
                detailsRect.anchoredPosition = Vector2.zero;
            }

            detailsRoot.SetActive(true);
        }
        if (emptyDetailsText != null)
            emptyDetailsText.gameObject.SetActive(false);

        int rank = SkillProgression.GetRank(selectedSkill);
        SkillRankData currentRank = selectedSkill.GetRankData(rank);
        SkillRankData nextRank = selectedSkill.GetRankData(rank + 1);
        SkillPurchaseStatus purchaseStatus = SkillProgression.GetPurchaseStatus(selectedSkill);

        if (skillIcon != null)
        {
            skillIcon.sprite = selectedSkill.Icon != null ? selectedSkill.Icon : emptySkillIcon;
            skillIcon.enabled = skillIcon.sprite != null;
        }
        if (titleText != null)
            titleText.text = selectedSkill.DisplayName;
        if (rankText != null)
            rankText.text = $"Rank {rank} / {selectedSkill.MaxRank}";
        if (descriptionText != null)
            descriptionText.text = selectedSkill.Description;
        if (currentRankEffectText != null)
            currentRankEffectText.text = currentRank != null ? currentRank.Description : "Not learned";
        if (nextRankEffectText != null)
            nextRankEffectText.text = nextRank != null ? nextRank.Description : "Maximum rank";
        if (requirementsText != null)
        {
            // Requirements only matter while they block the skill; otherwise the cost block is enough.
            bool locked = purchaseStatus is SkillPurchaseStatus.MissingPrerequisites or SkillPurchaseStatus.LockedByProgress;
            requirementsText.gameObject.SetActive(locked);
            requirementsText.text = locked ? BuildRequirementsText(selectedSkill) : string.Empty;
        }

        ApplyPurchaseState(purchaseStatus, nextRank);

        // The texts and the cost block visibility are final now, so the blocks can be stacked.
        LayoutDetails();
        FitDetailsContent(true);
    }

    // Reads the prefab layout once: each block's gap to the block above, its height,
    // and the padding below the last block.
    private void CacheDetailsLayout()
    {
        if (detailsLayout.Count > 0 || detailsRoot == null)
            return;

        float previousBottom = 0f;
        foreach (Transform child in detailsRoot.transform)
        {
            if (child is not RectTransform rect || rect.anchorMin.y < 1f || rect.anchorMax.y < 1f)
                continue;

            float height = rect.sizeDelta.y;
            float top = -rect.anchoredPosition.y - height * (1f - rect.pivot.y);
            detailsLayout.Add((rect, top - previousBottom, height));
            previousBottom = top + height;
        }

        detailsBottomPadding = Mathf.Max(0f, ((RectTransform)detailsRoot.transform).sizeDelta.y - previousBottom);
    }

    // Stacks the detail blocks top to bottom with their prefab gaps. The description and
    // requirements keep their prefab height as a minimum and grow to fit longer text,
    // pushing everything below them down; hidden blocks take no space.
    private void LayoutDetails()
    {
        CacheDetailsLayout();
        if (detailsLayout.Count == 0)
            return;

        float cursor = 0f;
        for (int i = 0; i < detailsLayout.Count; i++)
        {
            (RectTransform rect, float gap, float height) = detailsLayout[i];
            if (!rect.gameObject.activeSelf)
                continue;

            TMP_Text growingText = descriptionText != null && rect == descriptionText.rectTransform ? descriptionText
                : requirementsText != null && rect == requirementsText.rectTransform ? requirementsText
                : null;
            if (growingText != null)
                height = Mathf.Max(height, growingText.GetPreferredValues(growingText.text, rect.rect.width, 0f).y);

            float top = cursor + gap;
            rect.sizeDelta = new Vector2(rect.sizeDelta.x, height);
            rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, -(top + height * (1f - rect.pivot.y)));
            cursor = top + height;
        }

        ((RectTransform)detailsRoot.transform).SetSizeWithCurrentAnchors(
            RectTransform.Axis.Vertical, cursor + detailsBottomPadding);
    }

    private void ApplyPurchaseState(SkillPurchaseStatus status, SkillRankData nextRank)
    {
        bool canUpgrade = status == SkillPurchaseStatus.Available;
        if (upgradeButton != null)
            upgradeButton.interactable = canUpgrade;

        if (upgradeButtonText != null)
        {
            upgradeButtonText.text = status switch
            {
                SkillPurchaseStatus.MaxRank => "Mastered",
                SkillPurchaseStatus.MissingPrerequisites => "Locked",
                SkillPurchaseStatus.LockedByProgress => "Locked",
                _ => "Upgrade"
            };
        }

        bool showCost = nextRank != null && status != SkillPurchaseStatus.MaxRank;
        if (upgradeCostRoot != null)
            upgradeCostRoot.SetActive(showCost);
        if (upgradeCostText != null)
            upgradeCostText.text = showCost
                ? (nextRank.Cost == 1 ? "1 skill point" : $"{nextRank.Cost} skill points")
                : string.Empty;
    }

    private void SelectTreePage(int page)
    {
        page = Mathf.Clamp(page, 1, TreePageCount);
        if (selectedTreePage == page)
        {
            ScheduleScrollReset(true, false);
            return;
        }

        selectedTreePage = page;
        selectedSkill = null;
        RefreshPage(true);
    }

    private void SetSelectedPage(int page)
    {
        for (int i = 0; i < pageButtonBackgrounds.Count; i++)
        {
            Image background = pageButtonBackgrounds[i];
            if (background == null)
                continue;

            // Locked pages have their own pair of panels; missing sprites fall back to the regular ones.
            bool selected = i == page - 1;
            Sprite normal = tierBackgroundSprite;
            Sprite highlighted = selectedTierBackgroundSprite;
            if (!IsTreePageUnlocked(i + 1) && lockedTierBackgroundSprite != null)
            {
                normal = lockedTierBackgroundSprite;
                highlighted = lockedSelectedTierBackgroundSprite;
            }

            background.sprite = selected && highlighted != null ? highlighted : normal;
        }
    }

    // Page 1 is always open; every next page opens once the previous page is open
    // and all of its skills are at max rank.
    private static bool IsTreePageUnlocked(int page)
    {
        if (page <= 1)
            return true;

        bool hasSkills = false;
        IReadOnlyList<SkillData> skills = SkillProgression.Skills;
        for (int i = 0; i < skills.Count; i++)
        {
            SkillData skill = skills[i];
            if (skill == null || skill.TreePage != page - 1)
                continue;

            hasSkills = true;
            if (SkillProgression.GetRank(skill) < skill.MaxRank)
                return false;
        }

        return hasSkills && IsTreePageUnlocked(page - 1);
    }

    private void UpgradeSelectedSkill()
    {
        if (selectedSkill != null)
            SkillProgression.TryPurchase(selectedSkill);
    }

    private void HandleProgressionChanged() => RefreshPage(false);

    private void RefreshPoints()
    {
        if (availablePointsText != null)
            availablePointsText.text = $"Available: {SkillProgression.AvailablePoints}";
    }

    private List<SkillTreeNodeView> GetReusableNodes()
    {
        List<SkillTreeNodeView> reusableNodes = new();
        if (nodesRoot == null)
            return reusableNodes;

        for (int i = 0; i < nodesRoot.childCount; i++)
        {
            Transform child = nodesRoot.GetChild(i);
            if (child.TryGetComponent(out SkillTreeNodeView node))
                reusableNodes.Add(node);
        }

        return reusableNodes;
    }

    private void CacheConnectionViews()
    {
        if (connectionsRoot == null || connectionViews.Count > 0)
            return;

        for (int i = 0; i < connectionsRoot.childCount; i++)
        {
            Transform child = connectionsRoot.GetChild(i);
            if (connectionTemplate != null && child == connectionTemplate.transform)
                continue;

            if (child.TryGetComponent(out Image image))
                connectionViews.Add(image);
        }
    }

    private void ScheduleScrollReset(bool resetTree, bool resetDetails)
    {
        ResetScrolls(resetTree, resetDetails);
        if (!isActiveAndEnabled)
            return;

        if (scrollResetRoutine != null)
            StopCoroutine(scrollResetRoutine);
        scrollResetRoutine = StartCoroutine(ResetScrollsAfterLayout(resetTree, resetDetails));
    }

    private IEnumerator ResetScrollsAfterLayout(bool resetTree, bool resetDetails)
    {
        yield return null;
        Canvas.ForceUpdateCanvases();
        ResetScrolls(resetTree, resetDetails);
        scrollResetRoutine = null;
    }

    private void ResetScrolls(bool resetTree, bool resetDetails)
    {
        if (resetTree)
            ResetScrollToTop(treeScroll);
        if (resetDetails)
            ResetScrollToTop(detailsScroll);
    }

    // ScrollRect only receives drags that hit a raycast target. Empty parts of the
    // viewport have no graphic, so add an invisible one covering the whole area.
    private static void EnsureDragSurface(ScrollRect scroll)
    {
        if (scroll == null)
            return;

        if (!scroll.TryGetComponent(out Graphic surface))
        {
            surface = scroll.gameObject.AddComponent<Image>();
            surface.color = Color.clear;
        }

        surface.raycastTarget = true;
    }

    private static void ResetScrollToTop(ScrollRect scroll)
    {
        if (scroll == null || scroll.content == null)
            return;

        scroll.StopMovement();
        Vector2 position = scroll.content.anchoredPosition;
        position.y = 0f;
        scroll.content.anchoredPosition = position;
        scroll.verticalNormalizedPosition = 1f;
    }

    private void ShowEmptyDetails()
    {
        if (detailsRoot != null)
            detailsRoot.SetActive(false);
        if (emptyDetailsText != null)
        {
            emptyDetailsText.text = "Select a skill to view details";
            emptyDetailsText.gameObject.SetActive(true);
        }

        FitDetailsContent(false);
    }

    // The details content matches the selected skill block; with nothing selected it
    // matches the viewport, so there is nothing to scroll and the scrollbar auto-hides.
    private void FitDetailsContent(bool showingDetails)
    {
        if (detailsScroll == null || detailsScroll.content == null)
            return;

        RectTransform viewport = detailsScroll.viewport != null
            ? detailsScroll.viewport
            : (RectTransform)detailsScroll.transform;
        RectTransform details = detailsRoot != null ? detailsRoot.transform as RectTransform : null;
        float height = showingDetails && details != null
            ? details.rect.height
            : viewport.rect.height;

        detailsScroll.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Max(1f, height));
    }

    private static string BuildRequirementsText(SkillData skill)
    {
        StringBuilder builder = new();
        IReadOnlyList<SkillPrerequisite> prerequisites = skill.Prerequisites;
        if (prerequisites != null)
        {
            for (int i = 0; i < prerequisites.Count; i++)
            {
                SkillPrerequisite requirement = prerequisites[i];
                if (requirement?.Skill == null)
                    continue;

                int currentRank = SkillProgression.GetRank(requirement.Skill);
                AppendRequirement(builder,
                    $"{requirement.Skill.DisplayName} Rank {requirement.RequiredRank}",
                    currentRank >= requirement.RequiredRank);
            }
        }

        IReadOnlyList<SkillUnlockRequirement> progressionRequirements = skill.ProgressionRequirements;
        if (progressionRequirements != null)
        {
            for (int i = 0; i < progressionRequirements.Count; i++)
            {
                SkillUnlockRequirement requirement = progressionRequirements[i];
                if (requirement == null)
                    continue;

                string label;
                bool met;
                switch (requirement.Type)
                {
                    case SkillUnlockRequirementType.StoryFlag:
                        label = $"Story progress: {requirement.StoryFlag}";
                        met = StoryFlagManager.HasFlag(requirement.StoryFlag);
                        break;
                    case SkillUnlockRequirementType.QuestStatus:
                        label = requirement.Quest != null
                            ? $"{requirement.Quest.Title}: {requirement.RequiredQuestStatus}"
                            : "Quest requirement";
                        met = requirement.Quest != null
                            && QuestManager.GetStatus(requirement.Quest) == requirement.RequiredQuestStatus;
                        break;
                    case SkillUnlockRequirementType.DefeatedEncounter:
                        label = $"Defeat: {requirement.EncounterId}";
                        met = DefeatedEncounters.IsDefeated(requirement.EncounterId);
                        break;
                    default:
                        label = "Unknown requirement";
                        met = false;
                        break;
                }

                AppendRequirement(builder, label, met);
            }
        }

        return builder.Length == 0 ? "No requirements" : builder.ToString();
    }

    private static void AppendRequirement(StringBuilder builder, string label, bool met)
    {
        if (builder.Length > 0)
            builder.AppendLine();

        string color = met ? "#8ED07A" : "#C79578";
        builder.Append($"<color={color}>{(met ? "Met" : "Locked")}</color>  {label}");
    }
}
