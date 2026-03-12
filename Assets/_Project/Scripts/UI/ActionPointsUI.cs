using TMPro;
using UnityEngine;

public class ActionPointsUI : MonoBehaviour
{
    [SerializeField] private TMP_Text valueText;

    public void UpdatePoints(int current, int max)
    {
        valueText.text = current + " / " + max;
    }
}