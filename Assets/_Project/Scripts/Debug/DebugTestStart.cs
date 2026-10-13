using System.Collections.Generic;
using UnityEngine;

// Editor-only testing shortcut: tick "Use Test Start", drag this object where you want to test
// and press Play. The player starts here, the listed story flags are set (a1_home_intro skips
// the home intro) and the listed quests are started, once per Play (not again on returning from a
// battle). Does nothing in builds or when unticked.
// Runs before other scripts so the intro and the camera bounds already see the new state.
[DefaultExecutionOrder(-1000)]
public sealed class DebugTestStart : MonoBehaviour
{
    [SerializeField] private bool useTestStart;
    [SerializeField, StoryFlag] private List<string> storyFlags = new() { "a1_home_intro" };
    [SerializeField] private List<QuestData> startQuests = new();

#if UNITY_EDITOR
    // Once per Play: coming back from a battle must leave the player where they were, not send
    // them here again.
    private static bool applied;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSessionState()
    {
        applied = false;
    }

    private void Start()
    {
        if (!useTestStart || applied)
            return;

        applied = true;

        foreach (string flag in storyFlags)
            StoryFlagManager.SetFlag(flag);

        foreach (QuestData quest in startQuests)
            QuestManager.StartQuest(quest, false);

        PlayerMovement player = FindFirstObjectByType<PlayerMovement>();
        if (player == null)
            return;

        Vector3 position = transform.position;
        position.z = player.transform.position.z;
        if (player.TryGetComponent(out Rigidbody2D body))
            body.position = position;

        player.transform.position = position;
        Physics2D.SyncTransforms();
        DevLog.Log($"Test start: player moved to {position}.");
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = useTestStart ? Color.green : Color.gray;
        Gizmos.DrawWireSphere(transform.position, 0.8f);
        UnityEditor.Handles.Label(transform.position + Vector3.up, useTestStart ? "TEST START (on)" : "Test start (off)");
    }
#endif
}
