using UnityEngine;
using UnityEngine.TextCore.Text;

public class PlayerCharacter : Character
{
    [Header("Action Points")]
    [SerializeField] private int maxActionPoints = 5;
    [SerializeField] private int currentActionPoints = 4;
    [SerializeField] private int actionPointsPerTurn = 2;

    public int MaxActionPoints => maxActionPoints;
    public int CurrentActionPoints => currentActionPoints;

    public bool SpendActionPoints(int amount)
    {
        if (!CanSpendActionPoints(amount))
            return false;

        currentActionPoints -= amount;
        return true;
    }
    public bool CanSpendActionPoints(int amount)
    {
        return amount >= 0 && currentActionPoints >= amount;
    }

    public void RestoreActionPoints()
    {
        currentActionPoints = Mathf.Min(currentActionPoints + actionPointsPerTurn, maxActionPoints);
    }
}