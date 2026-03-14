using TMPro;
using UnityEngine;

public class HealthBarUI : MonoBehaviour
{
    [SerializeField] private Character target;
    [SerializeField] private RectTransform background;
    [SerializeField] private RectTransform hpFill;
    [SerializeField] private TMP_Text hpText;

    private void OnEnable()
    {
        if (target == null)
            return;

        target.HealthChanged += RefreshHealth;
        RefreshHealth(target.CurrentHealth, target.MaxHealth);
    }

    private void OnDisable()
    {
        if (target == null)
            return;

        target.HealthChanged -= RefreshHealth;
    }

    private void RefreshHealth(int current, int max)
    {
        float percent = max > 0 ? (float)current / max : 0f;

        float fullWidth = background.rect.width;
        float missingWidth = fullWidth * (1f - percent);

        hpFill.offsetMax = new Vector2(-missingWidth, hpFill.offsetMax.y);

        hpText.text = current + " / " + max;
    }
}