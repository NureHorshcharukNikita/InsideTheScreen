using TMPro;
using UnityEngine;

public class PlayerHPText : MonoBehaviour
{
    [SerializeField] private TMP_Text hpText;

    private Character target;

    public void SetTarget(Character character)
    {
        if (target != null)
            target.HealthChanged -= Refresh;

        target = character;

        if (target != null)
        {
            target.HealthChanged += Refresh;
            Refresh(target.CurrentHealth, target.MaxHealth);
        }
    }

    private void OnDisable()
    {
        if (target != null)
            target.HealthChanged -= Refresh;
    }

    private void Refresh(int current, int max)
    {
        hpText.text = $"{current}/{max}";
    }
}