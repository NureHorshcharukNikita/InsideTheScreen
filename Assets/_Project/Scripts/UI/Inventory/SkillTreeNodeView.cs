using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class SkillTreeNodeView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private static readonly Color LockedColor = new(0.48f, 0.48f, 0.48f, 1f);
    private static readonly Color HoverColor = new(1f, 0.9f, 0.66f, 1f);

    [SerializeField] private Button button;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image iconImage;
    [SerializeField] private Image lockImage;
    [SerializeField] private GameObject rankPanel;
    [SerializeField] private TMP_Text rankText;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private Sprite backgroundSprite;
    [SerializeField] private Sprite selectedBackgroundSprite;
    [SerializeField] private Sprite lockedBackgroundSprite;
    [SerializeField] private Sprite lockedSelectedBackgroundSprite;

    private SkillData skill;
    private Action<SkillData> onSelected;
    private Sprite normalBackgroundSprite;
    private bool selected;
    private bool pointerOver;
    private bool locked;
    private bool initialized;

    public RectTransform RectTransform => (RectTransform)transform;
    public SkillData Skill => skill;

    private void Awake()
    {
        EnsureInitialized();
    }

    // Setup can be called by the parent page before this node's Awake runs,
    // so the original background sprite must be cached before the first visual update.
    private void EnsureInitialized()
    {
        if (initialized)
            return;

        initialized = true;
        if (backgroundImage != null)
        {
            normalBackgroundSprite = backgroundSprite != null ? backgroundSprite : backgroundImage.sprite;
            backgroundImage.type = Image.Type.Simple;
            backgroundImage.preserveAspect = false;
        }

        if (button != null)
            button.transition = Selectable.Transition.None;
    }

    public void Setup(SkillData data, Sprite fallbackIcon, bool isSelected, Action<SkillData> selectedCallback)
    {
        skill = data;
        onSelected = selectedCallback;

        if (button != null)
        {
            button.onClick.RemoveListener(Select);
            button.onClick.AddListener(Select);
        }

        if (iconImage != null)
        {
            iconImage.sprite = skill != null && skill.Icon != null ? skill.Icon : fallbackIcon;
            iconImage.enabled = iconImage.sprite != null;
        }

        if (nameText != null)
            nameText.text = skill != null ? skill.DisplayName : string.Empty;

        RefreshProgress();
        SetSelected(isSelected);
    }

    public void RefreshProgress()
    {
        if (skill == null)
            return;

        int rank = SkillProgression.GetRank(skill);
        SkillPurchaseStatus status = SkillProgression.GetPurchaseStatus(skill);
        locked = status is SkillPurchaseStatus.MissingPrerequisites or SkillPurchaseStatus.LockedByProgress;

        if (rankText != null)
            rankText.text = $"{rank}/{skill.MaxRank}";

        if (lockImage != null)
            lockImage.gameObject.SetActive(locked);

        // Locked skills show only the lock, without the rank counter and its panel.
        GameObject rankRoot = rankPanel != null ? rankPanel : rankText != null ? rankText.gameObject : null;
        if (rankRoot != null)
            rankRoot.SetActive(!locked);

        ApplyVisualState();
    }

    public void SetSelected(bool value)
    {
        selected = value;
        ApplyVisualState();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        pointerOver = true;
        ApplyVisualState();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        pointerOver = false;
        ApplyVisualState();
    }

    private void Select()
    {
        onSelected?.Invoke(skill);
    }

    private void ApplyVisualState()
    {
        EnsureInitialized();
        if (backgroundImage != null)
        {
            backgroundImage.sprite = GetBackgroundSprite();
            backgroundImage.color = pointerOver && !selected ? HoverColor : Color.white;
        }

        if (iconImage != null)
            iconImage.color = locked ? LockedColor : Color.white;
    }

    // Locked skills use their own panels; missing sprites fall back to the regular ones.
    private Sprite GetBackgroundSprite()
    {
        Sprite normal = locked && lockedBackgroundSprite != null
            ? lockedBackgroundSprite
            : normalBackgroundSprite;
        if (!selected)
            return normal;

        if (locked && lockedSelectedBackgroundSprite != null)
            return lockedSelectedBackgroundSprite;

        return selectedBackgroundSprite != null ? selectedBackgroundSprite : normal;
    }
}
