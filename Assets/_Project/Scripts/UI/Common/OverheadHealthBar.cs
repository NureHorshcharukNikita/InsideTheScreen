using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// A health bar over a character's head for a moment, with the damage taken rising off it, e.g. over
// Dad when Haru hits him with the stick to see whether they have health at all (Strike step). Made
// from its prefab (the frame, the fill, the health on it and the damage are objects to edit there); it
// drains the fill by the damage and counts the health down (30/30 to 27/30), lets the damage rise, stays
// a moment and fades. On the GameClock, as story events.
public sealed class OverheadHealthBar : MonoBehaviour
{
    [SerializeField] private Image fill;
    [Tooltip("The health on the bar, e.g. 30/30 going down to 27/30. Empty = none.")]
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private TMP_Text damageText;
    [SerializeField] private CanvasGroup fade;
    [Tooltip("Between the top of the head and the bottom of the bar, in world units.")]
    [SerializeField, Min(0f)] private float gap = 0.2f;
    [SerializeField, Min(0.01f)] private float drainSeconds = 0.35f;
    [Tooltip("How far the number rises off the bar, in the canvas' units.")]
    [SerializeField] private float rise = 40f;
    [SerializeField, Min(0f)] private float staySeconds = 1.6f;
    [SerializeField, Min(0.01f)] private float fadeSeconds = 0.4f;

    public static void Show(OverheadHealthBar prefab, Transform character, int healthBefore, int maxHealth, int damage)
    {
        if (prefab == null || character == null)
            return;

        OverheadHealthBar bar = Instantiate(prefab);
        bar.Place(character);
        bar.StartCoroutine(bar.Play(healthBefore, Mathf.Max(1, maxHealth), damage));
    }

    // Centred over the character, its bottom Gap above the head, wherever the sprite's pivot is.
    private void Place(Transform character)
    {
        SpriteRenderer sprite = character.GetComponentInChildren<SpriteRenderer>();
        float top = sprite != null ? sprite.bounds.max.y : character.position.y;
        float x = sprite != null ? sprite.bounds.center.x : character.position.x;
        transform.position = new Vector3(x, top + gap, character.position.z);
    }

    private IEnumerator Play(int healthBefore, int maxHealth, int damage)
    {
        int healthAfter = Mathf.Max(0, healthBefore - damage);
        float from = Mathf.Clamp01((float)healthBefore / maxHealth);
        float to = Mathf.Clamp01((float)healthAfter / maxHealth);
        if (fill != null)
            fill.fillAmount = from;
        ShowHealth(healthBefore, maxHealth);

        RectTransform number = damageText != null ? damageText.rectTransform : null;
        Vector2 numberStart = number != null ? number.anchoredPosition : Vector2.zero;
        if (damageText != null)
            damageText.text = damage > 0 ? $"-{damage}" : string.Empty;

        for (float time = 0f; time < drainSeconds; time += GameClock.DeltaTime)
        {
            float progress = time / drainSeconds;
            if (fill != null)
                fill.fillAmount = Mathf.Lerp(from, to, progress);
            // Counted down with the fill.
            ShowHealth(Mathf.RoundToInt(Mathf.Lerp(healthBefore, healthAfter, progress)), maxHealth);
            if (number != null)
                number.anchoredPosition = numberStart + Vector2.up * (rise * Mathf.Sin(progress * Mathf.PI / 2f));
            yield return null;
        }

        if (fill != null)
            fill.fillAmount = to;
        ShowHealth(healthAfter, maxHealth);
        if (number != null)
            number.anchoredPosition = numberStart + Vector2.up * rise;

        if (staySeconds > 0f)
            yield return new WaitForGameSeconds(staySeconds);

        for (float time = 0f; time < fadeSeconds; time += GameClock.DeltaTime)
        {
            if (fade != null)
                fade.alpha = 1f - time / fadeSeconds;
            yield return null;
        }

        Destroy(gameObject);
    }

    private void ShowHealth(int health, int maxHealth)
    {
        if (healthText != null)
            healthText.text = $"{health}/{maxHealth}";
    }
}
