using System;
using System.Collections.Generic;

public static class QuestManager
{
    private static readonly List<QuestProgressSaveData> Quests = new();
    private static readonly List<QuestData> KnownQuests = new();

    public static event Action<QuestData, QuestStatus> QuestChanged;
    public static event Action<QuestData> QuestStarted;
    public static event Action<QuestData> QuestCompleted;
    public static event Action<QuestData> QuestRewardGranted;
    public static event Action<QuestData> QuestProgressChanged;

    public static bool StartQuest(QuestData quest, bool showNotification = true)
    {
        if (quest == null || string.IsNullOrWhiteSpace(quest.QuestKey))
            return false;

        RegisterQuest(quest);

        QuestProgressSaveData progress = GetProgress(quest.QuestKey);
        if (progress != null)
            return false;

        Quests.Add(new QuestProgressSaveData
        {
            questKey = quest.QuestKey,
            status = QuestStatus.Active,
            wasOpened = false
        });

        QuestChanged?.Invoke(quest, QuestStatus.Active);
        QuestStarted?.Invoke(quest);

        if (showNotification)
            QuestNotificationToast.ShowQuestAdded(quest);

        ExplorationPlayerSession.SaveQuestProgress();

        // The marked enemy was beaten before anyone asked: done at once.
        if (quest.ObjectiveType == QuestObjectiveType.DefeatEncounter
            && DefeatedEncounters.IsDefeated(quest.TargetEncounterId))
            CompleteQuest(quest, null, showNotification);

        return true;
    }

    public static bool SetStatus(QuestData quest, QuestStatus status)
    {
        return SetStatusInternal(quest, status, true);
    }

    public static bool CompleteQuest(QuestData quest, InventoryData inventory = null, bool showNotification = true)
    {
        if (!SetStatusInternal(quest, QuestStatus.Completed, showNotification))
            return false;

        inventory ??= ExplorationPlayerSession.RuntimeInventory;
        if (quest.ObjectiveType == QuestObjectiveType.CollectCards && inventory != null)
        {
            inventory.RemoveCards(quest.TargetCard, quest.TargetCount);
            ExplorationPlayerSession.TrimDeckToInventory();
        }

        GrantReward(quest, inventory, showNotification);
        return true;
    }

    private static bool SetStatusInternal(QuestData quest, QuestStatus status, bool showNotification)
    {
        if (quest == null || string.IsNullOrWhiteSpace(quest.QuestKey))
            return false;

        RegisterQuest(quest);

        QuestProgressSaveData progress = GetProgress(quest.QuestKey);
        if (progress == null)
        {
            progress = new QuestProgressSaveData
            {
                questKey = quest.QuestKey,
                wasOpened = false
            };
            Quests.Add(progress);
        }

        if (progress.status == status)
            return false;

        progress.status = status;
        QuestChanged?.Invoke(quest, status);

        if (status == QuestStatus.Completed)
        {
            QuestCompleted?.Invoke(quest);

            if (showNotification)
                QuestNotificationToast.ShowQuestCompleted(quest);
        }

        ExplorationPlayerSession.SaveQuestProgress();
        return true;
    }

    public static bool HandleEncounterDefeated(string encounterId, InventoryData inventory)
    {
        if (string.IsNullOrWhiteSpace(encounterId))
            return false;

        bool completedAny = false;
        for (int i = 0; i < KnownQuests.Count; i++)
        {
            QuestData quest = KnownQuests[i];
            if (quest == null
                || quest.ObjectiveType != QuestObjectiveType.DefeatEncounter
                || quest.TargetEncounterId != encounterId
                || GetStatus(quest) != QuestStatus.Active)
                continue;

            if (!CompleteQuest(quest, inventory))
                continue;

            completedAny = true;
        }

        return completedAny;
    }

    // After a won battle, with every enemy of it: Defeat Enemies quests that are active count the
    // ones of their kind, and complete at their count.
    public static void HandleEnemiesDefeated(IReadOnlyList<EnemyData> enemies, InventoryData inventory)
    {
        if (enemies == null || enemies.Count == 0)
            return;

        for (int i = 0; i < KnownQuests.Count; i++)
        {
            QuestData quest = KnownQuests[i];
            if (quest == null
                || quest.ObjectiveType != QuestObjectiveType.DefeatEnemies
                || quest.TargetEnemy == null
                || GetStatus(quest) != QuestStatus.Active)
                continue;

            int defeated = 0;
            for (int e = 0; e < enemies.Count; e++)
            {
                if (IsSameEnemy(enemies[e], quest.TargetEnemy))
                    defeated++;
            }

            if (defeated == 0)
                continue;

            QuestProgressSaveData progress = GetProgress(quest.QuestKey);
            progress.progress = Math.Min(quest.TargetCount, progress.progress + defeated);

            if (progress.progress >= quest.TargetCount)
            {
                CompleteQuest(quest, inventory);
                continue;
            }

            QuestProgressChanged?.Invoke(quest);
            QuestNotificationToast.ShowQuestProgress(quest, progress.progress, quest.TargetCount);
            ExplorationPlayerSession.SaveQuestProgress();
        }
    }

    // How far a Defeat Enemies or Collect Cards quest has got, e.g. 2 of 3. False for other quests.
    public static bool TryGetObjectiveProgress(QuestData quest, out int current, out int target)
    {
        current = 0;
        target = quest != null ? quest.TargetCount : 0;
        if (quest == null)
            return false;

        QuestStatus status = GetStatus(quest);
        switch (quest.ObjectiveType)
        {
            case QuestObjectiveType.DefeatEnemies:
                current = status == QuestStatus.Completed ? target : GetProgress(quest.QuestKey)?.progress ?? 0;
                return true;
            case QuestObjectiveType.CollectCards:
                current = status == QuestStatus.Completed ? target : CountCards(quest.TargetCard);
                current = Math.Min(current, target);
                return true;
            default:
                return false;
        }
    }

    // A Defeat Enemies or Collect Cards quest that is active and has reached its count: time to
    // go back to whoever gave it.
    public static bool IsObjectiveDone(QuestData quest)
    {
        return GetStatus(quest) == QuestStatus.Active
            && TryGetObjectiveProgress(quest, out int current, out int target)
            && current >= target;
    }

    public static bool HasQuest(QuestData quest)
    {
        return quest != null && HasQuest(quest.QuestKey);
    }

    public static bool HasQuest(string questKey)
    {
        return GetProgress(questKey) != null;
    }

    public static QuestStatus GetStatus(QuestData quest)
    {
        if (quest == null)
            return QuestStatus.Available;

        return TryGetStatus(quest.QuestKey, out QuestStatus status)
            ? status
            : QuestStatus.Available;
    }

    public static bool TryGetStatus(string questKey, out QuestStatus status)
    {
        QuestProgressSaveData progress = GetProgress(questKey);
        if (progress == null)
        {
            status = QuestStatus.Available;
            return false;
        }

        status = progress.status;
        return true;
    }

    public static bool IsUnread(QuestData quest)
    {
        if (quest == null)
            return false;

        QuestProgressSaveData progress = GetProgress(quest.QuestKey);
        return progress != null && !progress.wasOpened;
    }

    public static bool MarkOpened(QuestData quest)
    {
        if (quest == null || string.IsNullOrWhiteSpace(quest.QuestKey))
            return false;

        QuestProgressSaveData progress = GetProgress(quest.QuestKey);
        if (progress == null || progress.wasOpened)
            return false;

        progress.wasOpened = true;
        ExplorationPlayerSession.SaveQuestProgress();
        return true;
    }

    // Newest first: Quests holds them in the order they were received, and so do the saves.
    public static List<QuestData> GetReceivedQuests(IReadOnlyList<QuestData> questCatalog)
    {
        List<QuestData> received = new();
        RegisterQuests(questCatalog);
        for (int i = Quests.Count - 1; i >= 0; i--)
        {
            QuestData quest = FindKnownQuest(Quests[i].questKey);
            if (quest != null && !ContainsQuest(received, quest.QuestKey))
                received.Add(quest);
        }

        return received;
    }

    public static void RegisterQuests(IReadOnlyList<QuestData> quests)
    {
        if (quests == null)
            return;

        for (int i = 0; i < quests.Count; i++)
            RegisterQuest(quests[i]);
    }

    public static void RegisterQuest(QuestData quest)
    {
        if (quest == null || string.IsNullOrWhiteSpace(quest.QuestKey))
            return;

        for (int i = 0; i < KnownQuests.Count; i++)
        {
            if (KnownQuests[i] != null && KnownQuests[i].QuestKey == quest.QuestKey)
                return;
        }

        KnownQuests.Add(quest);
    }

    public static QuestProgressSaveData[] ToArray()
    {
        QuestProgressSaveData[] result = new QuestProgressSaveData[Quests.Count];
        for (int i = 0; i < Quests.Count; i++)
        {
            result[i] = new QuestProgressSaveData
            {
                questKey = Quests[i].questKey,
                status = Quests[i].status,
                wasOpened = Quests[i].wasOpened,
                progress = Quests[i].progress
            };
        }

        return result;
    }

    public static void Restore(QuestProgressSaveData[] savedQuests)
    {
        Quests.Clear();
        if (savedQuests == null)
            return;

        for (int i = 0; i < savedQuests.Length; i++)
        {
            QuestProgressSaveData savedQuest = savedQuests[i];
            if (savedQuest == null || string.IsNullOrWhiteSpace(savedQuest.questKey))
                continue;

            Quests.Add(new QuestProgressSaveData
            {
                questKey = savedQuest.questKey,
                status = savedQuest.status,
                wasOpened = savedQuest.wasOpened,
                progress = savedQuest.progress
            });
        }
    }

    // KnownQuests stays: it lists what quests exist, not anyone's progress, and quests register
    // themselves only once, when they are loaded.
    public static void Clear()
    {
        Quests.Clear();
    }

    private static bool IsSameEnemy(EnemyData enemy, EnemyData target)
    {
        return enemy != null && (enemy == target || enemy.CharacterID == target.CharacterID);
    }

    private static int CountCards(CardData card)
    {
        InventoryData inventory = ExplorationPlayerSession.RuntimeInventory;
        if (card == null || inventory == null)
            return 0;

        int count = 0;
        foreach (CardData owned in inventory.Cards)
        {
            if (owned == card)
                count++;
        }

        return count;
    }

    private static QuestData FindKnownQuest(string questKey)
    {
        for (int i = 0; i < KnownQuests.Count; i++)
        {
            if (KnownQuests[i] != null && KnownQuests[i].QuestKey == questKey)
                return KnownQuests[i];
        }

        return null;
    }

    private static bool ContainsQuest(IReadOnlyList<QuestData> quests, string questKey)
    {
        if (string.IsNullOrWhiteSpace(questKey))
            return false;

        for (int i = 0; i < quests.Count; i++)
        {
            if (quests[i] != null && quests[i].QuestKey == questKey)
                return true;
        }

        return false;
    }

    private static QuestProgressSaveData GetProgress(string questKey)
    {
        if (string.IsNullOrWhiteSpace(questKey))
            return null;

        for (int i = 0; i < Quests.Count; i++)
        {
            if (Quests[i] != null && Quests[i].questKey == questKey)
                return Quests[i];
        }

        return null;
    }

    private static void GrantReward(QuestData quest, InventoryData inventory, bool showNotification)
    {
        if (quest == null || inventory == null)
            return;

        bool grantedAnyReward = false;
        for (int i = 0; i < quest.RewardCount; i++)
        {
            QuestRewardData reward = quest.GetReward(i);
            if (reward == null)
                continue;

            if (reward.Card != null && reward.CardCount > 0)
            {
                inventory.AddCards(reward.Card, reward.CardCount);
                ExplorationPlayerSession.AddToDeckIfRoom(reward.Card, reward.CardCount);
                grantedAnyReward = true;
            }

            if (reward.SkillPoints > 0)
            {
                SkillProgression.AddPoints(reward.SkillPoints);
                grantedAnyReward = true;
            }

            if (reward.Gold > 0)
            {
                PlayerWallet.AddGold(reward.Gold);
                grantedAnyReward = true;
            }
        }

        if (!grantedAnyReward)
            return;

        QuestRewardGranted?.Invoke(quest);

        if (showNotification)
            QuestNotificationToast.ShowRewardReceived(quest);
    }
}
