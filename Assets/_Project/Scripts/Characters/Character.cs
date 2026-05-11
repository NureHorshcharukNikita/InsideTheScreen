using System;
using UnityEngine;

public class Character : MonoBehaviour, ICombatant
{
    [SerializeField] private int currentHealth = 30;

    public virtual int MaxHealth => 0;
    public int CurrentHealth => currentHealth;
    public virtual CombatTeam Team => CombatTeam.Neutral;
    public bool IsAlive => CurrentHealth > 0;

    public event Action<int, int> HealthChanged;

    protected virtual void Awake()
    {
        SetFullHealth();
    }

    public void TakeDamage(int amount)
    {
        if (amount < 0)
            return;

        currentHealth = Mathf.Max(currentHealth - amount, 0);
        HealthChanged?.Invoke(CurrentHealth, MaxHealth);
    }

    public void Heal(int amount)
    {
        if (amount < 0)
            return;

        currentHealth = Mathf.Min(currentHealth + amount, MaxHealth);
        HealthChanged?.Invoke(CurrentHealth, MaxHealth);
    }

    public void SetFullHealth()
    {
        currentHealth = MaxHealth;
        HealthChanged?.Invoke(CurrentHealth, MaxHealth);
    }
}