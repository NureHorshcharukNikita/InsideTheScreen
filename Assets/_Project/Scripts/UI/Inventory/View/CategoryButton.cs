using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CategoryButton : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text label;
    [SerializeField] private Image backgroundImage;

    [Header("Colors")]
    [SerializeField] private Color normalBackgroundColor = new Color32(0xE6, 0xD9, 0xC7, 0xFF);
    [SerializeField] private Color selectedBackgroundColor = new Color32(0x40, 0x66, 0x86, 0xFF);

    [SerializeField] private Color normalTextColor = new Color32(0x00, 0x00, 0x00, 0xFF);
    [SerializeField] private Color selectedTextColor = new Color32(0xFF, 0xFF, 0xFF, 0xFF);

    private Action onClick;

    public void Init(string text, Action click, bool selected = false)
    {
        label.text = text;
        onClick = click;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => onClick?.Invoke());

        SetSelected(selected);
    }

    public void SetSelected(bool selected)
    {
        if (backgroundImage != null)
            backgroundImage.color = selected ? selectedBackgroundColor : normalBackgroundColor;

        if (label != null)
            label.color = selected ? selectedTextColor : normalTextColor;
    }
}