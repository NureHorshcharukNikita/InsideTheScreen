using System.Collections.Generic;
using UnityEngine;

// Shows an object only while its story conditions are met, e.g. the trader's torn pack lying on the
// forest road while Haru is looking for it. Sits on an active object and shows or hides `target`
// (a child), like Show After Story Flag.
public sealed class ShowWhileStory : MonoBehaviour
{
    [SerializeField] private GameObject target;
    [Tooltip("All of them must be met.")]
    [SerializeField] private List<StoryCondition> conditions = new();

    private void Awake()
    {
        StoryFlagManager.Changed += Apply;
        QuestManager.QuestChanged += ApplyForQuest;
        Apply();
    }

    private void OnDestroy()
    {
        StoryFlagManager.Changed -= Apply;
        QuestManager.QuestChanged -= ApplyForQuest;
    }

    private void ApplyForQuest(QuestData quest, QuestStatus status)
    {
        Apply();
    }

    private void Apply()
    {
        if (target != null)
            target.SetActive(StoryCondition.AreMet(conditions));
    }
}
