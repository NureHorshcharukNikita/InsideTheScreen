using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class QuestRewardItemView : MonoBehaviour
{
    [SerializeField] private TMP_Text labelText;
    [SerializeField] private Image iconImage;

    public void Setup(QuestRewardData reward, bool received)
    {
        if (labelText != null)
        {
            string label = reward != null ? reward.Label : string.Empty;
            labelText.text = received && !string.IsNullOrWhiteSpace(label)
                ? $"Reward received: {label}"
                : label;
        }

        if (iconImage == null)
            return;

        iconImage.sprite = reward != null ? reward.Icon : null;
        iconImage.enabled = iconImage.sprite != null;
        iconImage.color = Color.white;
    }
}
