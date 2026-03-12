using TMPro;
using UnityEngine;

public class ActionPointsUI : MonoBehaviour
{
    [SerializeField] private TMP_Text valueText;
    [SerializeField] private PlayerCharacter player;

    private void OnEnable()
    {
        if (player == null)
            return;

        player.ActionPointsChanged += RefreshPoints;
        RefreshPoints(player.CurrentActionPoints, player.MaxActionPoints);
    }

    private void OnDisable()
    {
        if (player == null)
            return;

        player.ActionPointsChanged -= RefreshPoints;
    }

    private void RefreshPoints(int current, int max)
    {
        valueText.text = current + " / " + max;
    }
}