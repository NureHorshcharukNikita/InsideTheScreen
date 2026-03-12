using TMPro;
using UnityEngine;

public class ActionPointsUI : MonoBehaviour
{
    [SerializeField] private TMP_Text valueText;

    public void UpdatePoints(PlayerCharacter player)
    {
        valueText.text = player.CurrentActionPoints + " / " + player.MaxActionPoints;
    }
}