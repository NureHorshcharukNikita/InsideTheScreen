using UnityEngine;

public class Character : MonoBehaviour, IEffectTarget
{
    [Header("Health")]
    [SerializeField] private int maxHealth = 30;
    [SerializeField] private int currentHealth = 30;

    public int MaxHealth => maxHealth;
    public int CurrentHealth => currentHealth;

    public void TakeDamage(int amount)
    {
        if (amount < 0)
            return;

        currentHealth = Mathf.Max(currentHealth - amount, 0);
    }

    public void Heal(int amount)
    {
        if (amount < 0)
            return;

        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
    }
}