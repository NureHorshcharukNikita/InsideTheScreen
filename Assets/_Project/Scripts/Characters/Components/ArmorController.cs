using System;
using UnityEngine;

public class ArmorController : MonoBehaviour
{
    private int armor;

    public int Armor => armor;

    public event Action<int> ArmorChanged;

    public void AddArmor(int amount)
    {
        if (amount < 0)
            return;

        armor += amount;
        ArmorChanged?.Invoke(Armor);
    }

    public void SetArmor(int amount)
    {
        armor = Mathf.Max(amount, 0);
        ArmorChanged?.Invoke(Armor);
    }

    public int AbsorbDamage(int amount)
    {
        if (amount <= 0 || armor <= 0)
            return amount;

        int absorbed = Mathf.Min(armor, amount);
        armor -= absorbed;
        ArmorChanged?.Invoke(Armor);
        return amount - absorbed;
    }

    public void ClearArmor()
    {
        if (armor == 0)
            return;

        armor = 0;
        ArmorChanged?.Invoke(Armor);
    }
}
