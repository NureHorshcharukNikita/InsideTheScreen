using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(ArmorController))]
[RequireComponent(typeof(StatusController))]
public class Character : MonoBehaviour, ICombatant
{
    [SerializeField] private int currentHealth = 30;

    private ArmorController armorController;
    private StatusController statusController;

    public virtual int MaxHealth => 0;
    public virtual int StartingArmor => 0;
    public int CurrentHealth => currentHealth;
    public int Armor => armorController != null ? armorController.Armor : 0;
    public IReadOnlyList<ActiveBattleStatus> Statuses => statusController != null
        ? statusController.Statuses
        : Array.Empty<ActiveBattleStatus>();
    public virtual CombatTeam Team => CombatTeam.Neutral;
    public virtual string DisplayName => name;
    // Shown in battle in place of DisplayName when there is one (CharacterProfile.battleIcon).
    public virtual Sprite BattleIcon => null;
    public bool IsAlive => CurrentHealth > 0;

    public event Action<int, int> HealthChanged;
    // A status kept this character alive at 0 health (e.g. Seal of Immortality).
    public event Action<BattleStatus> DeathPrevented;
    public event Action StatusesChanged
    {
        add
        {
            CacheRequiredComponents();
            if (statusController != null)
                statusController.StatusesChanged += value;
        }
        remove
        {
            if (statusController != null)
                statusController.StatusesChanged -= value;
        }
    }

    public event Action<int> ArmorChanged
    {
        add
        {
            CacheRequiredComponents();
            if (armorController != null)
                armorController.ArmorChanged += value;
        }
        remove
        {
            if (armorController != null)
                armorController.ArmorChanged -= value;
        }
    }

    protected virtual void Awake()
    {
        CacheRequiredComponents();

        SetFullHealth();
        SetArmor(StartingArmor);
    }

    public void TakeDamage(int amount)
    {
        if (amount < 0)
            return;

        int health = Mathf.Max(currentHealth - amount, 0);
        if (health == 0 && currentHealth > 0 && statusController != null
            && statusController.TryPreventDeath(this, out BattleStatus preventedBy))
        {
            DeathPrevented?.Invoke(preventedBy);
            return;
        }

        currentHealth = health;
        HealthChanged?.Invoke(CurrentHealth, MaxHealth);
    }

    public bool HasStatus(BattleStatus status)
    {
        return statusController != null && statusController.HasStatus(status);
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

    public void SetHealth(int health)
    {
        currentHealth = Mathf.Clamp(health, 0, MaxHealth);
        HealthChanged?.Invoke(CurrentHealth, MaxHealth);
    }

    public void AddArmor(int amount)
    {
        armorController?.AddArmor(amount);
    }

    public void SetArmor(int amount)
    {
        armorController?.SetArmor(amount);
    }

    public int AbsorbDamageWithArmor(int amount)
    {
        return armorController != null
            ? armorController.AbsorbDamage(amount)
            : amount;
    }

    public void ClearArmor()
    {
        armorController?.ClearArmor();
    }

    public void ApplyStatus(BattleStatus status, int duration, int stacks, int value, ICombatant source, BattleStatusReapplyMode reapplyMode, BattleActionContext actionContext)
    {
        statusController?.ApplyStatus(status, duration, stacks, value, source, this, reapplyMode, actionContext);
    }

    public void TickStatuses(BattleStatusTiming timing, BattleActionContext actionContext)
    {
        statusController?.TickStatuses(this, timing, actionContext);
    }

    private void CacheRequiredComponents()
    {
        armorController = GetComponent<ArmorController>();
        if (armorController == null)
            Debug.LogError($"{nameof(ArmorController)} is missing on {name}.", this);

        statusController = GetComponent<StatusController>();
        if (statusController == null)
            Debug.LogError($"{nameof(StatusController)} is missing on {name}.", this);
    }
}
