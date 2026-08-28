using System;
using UnityEngine;

public class Character : MonoBehaviour, ICombatant
{
    [SerializeField] private int currentHealth = 30;

    private ArmorController armorController;
    private StatusController statusController;

    public virtual int MaxHealth => 0;
    public virtual int StartingArmor => 0;
    public int CurrentHealth => currentHealth;
    public int Armor => armorController != null ? armorController.Armor : 0;
    public virtual CombatTeam Team => CombatTeam.Neutral;
    public bool IsAlive => CurrentHealth > 0;

    public event Action<int, int> HealthChanged;
    public event Action<int> ArmorChanged
    {
        add
        {
            EnsureArmorController();
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
        EnsureArmorController();
        EnsureStatusController();

        SetFullHealth();
        SetArmor(StartingArmor);
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

    public void SetHealth(int health)
    {
        currentHealth = Mathf.Clamp(health, 0, MaxHealth);
        HealthChanged?.Invoke(CurrentHealth, MaxHealth);
    }

    public void AddArmor(int amount)
    {
        EnsureArmorController();
        armorController.AddArmor(amount);
    }

    public void SetArmor(int amount)
    {
        EnsureArmorController();
        armorController.SetArmor(amount);
    }

    public int AbsorbDamageWithArmor(int amount)
    {
        EnsureArmorController();
        return armorController.AbsorbDamage(amount);
    }

    public void ClearArmor()
    {
        EnsureArmorController();
        armorController.ClearArmor();
    }

    public void ApplyStatus(StatusEffect status, int duration, int stacks, int value, ICombatant source, BattleActionContext actionContext)
    {
        EnsureStatusController();
        statusController.ApplyStatus(status, duration, stacks, value, source, this, actionContext);
    }

    public void TickStatuses(BattleStatusTiming timing, BattleActionContext actionContext)
    {
        EnsureStatusController();
        statusController.TickStatuses(this, timing, actionContext);
    }

    private void EnsureArmorController()
    {
        if (armorController != null)
            return;

        armorController = GetComponent<ArmorController>();
        if (armorController == null)
            armorController = gameObject.AddComponent<ArmorController>();
    }

    private void EnsureStatusController()
    {
        if (statusController != null)
            return;

        statusController = GetComponent<StatusController>();
        if (statusController == null)
            statusController = gameObject.AddComponent<StatusController>();
    }
}
