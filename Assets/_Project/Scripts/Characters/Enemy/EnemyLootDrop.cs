using System;
using UnityEngine;

[Serializable]
public sealed class EnemyLootDrop
{
    public CardData card;

    [Range(0f, 100f)]
    public float dropChancePercent = 100f;

    [Min(1)]
    public int minAmount = 1;

    [Min(1)]
    public int maxAmount = 1;

    public int RollAmount()
    {
        if (card == null || dropChancePercent <= 0f)
            return 0;

        if (dropChancePercent < 100f && UnityEngine.Random.value * 100f > dropChancePercent)
            return 0;

        int min = Mathf.Max(1, minAmount);
        int max = Mathf.Max(min, maxAmount);
        return UnityEngine.Random.Range(min, max + 1);
    }

    public void Validate()
    {
        dropChancePercent = Mathf.Clamp(dropChancePercent, 0f, 100f);
        minAmount = Mathf.Max(1, minAmount);
        maxAmount = Mathf.Max(minAmount, maxAmount);
    }
}
