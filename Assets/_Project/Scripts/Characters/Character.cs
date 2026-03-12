using System;
using UnityEngine;

public class Character : MonoBehaviour, IEffectTarget
{
    [Header("Health")]
    [SerializeField] private int maxHealth = 30;
    [SerializeField] private int currentHealth = 30;

    public int MaxHealth => maxHealth;
    public int CurrentHealth => currentHealth;

    public event Action<int, int> HealthChanged;

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

        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        HealthChanged?.Invoke(CurrentHealth, MaxHealth);
    }
}