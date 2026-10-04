using System;

[Serializable]
public sealed class SessionSaveData
{
    public bool hasSession;
    public string continueSceneName;
    public long savedAtTicks;

    public bool hasSavedPosition;
    public float positionX;
    public float positionY;

    public bool hasSavedHealth;
    public int savedHealth;

    public bool hasGold;
    public int gold;

    public string pendingEncounterId;
    public bool hasEnemyHealth;
    public int enemyHealth;

    public string[] inventoryCardIds;
    public string[] deckCardIds;
    public string[] defeatedEncounterIds;
    public string[] collectedPickupIds;
    public QuestProgressSaveData[] questProgress;
    public ShopProgressSaveData[] shopProgress;
    public NpcPositionSaveData[] npcPositions;
    public NpcDialogueProgressSaveData[] npcDialogueProgress;
    public string[] storyFlags;
    public bool hasSkillProgression;
    public int skillPoints;
    public SkillProgressSaveData[] skillProgression;
}

[Serializable]
public sealed class QuestProgressSaveData
{
    public string questKey;
    public QuestStatus status;
    public bool wasOpened;
}

[Serializable]
public sealed class ShopProgressSaveData
{
    public string shopKey;
    public int itemIndex;
    public int purchasedCount;
}

[Serializable]
public sealed class NpcPositionSaveData
{
    public string npcKey;
    public float positionX;
    public float positionY;
    public float positionZ;
}

[Serializable]
public sealed class NpcDialogueProgressSaveData
{
    public string npcKey;
    public int talkCount;
    public string[] usedDialogueKeys;
}

[Serializable]
public sealed class SkillProgressSaveData
{
    public string skillId;
    public int rank;
}
