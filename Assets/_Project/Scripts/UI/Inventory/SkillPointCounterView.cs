using TMPro;
using UnityEngine;

public sealed class SkillPointCounterView : MonoBehaviour
{
    [SerializeField] private TMP_Text skillPointText;

    private void OnEnable()
    {
        SkillProgression.Changed += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        SkillProgression.Changed -= Refresh;
    }

    private void Refresh()
    {
        if (skillPointText != null)
            skillPointText.text = SkillProgression.AvailablePoints.ToString();
    }
}
