using System;

[Serializable]
public sealed class SessionSaveData
{
    public bool hasSession;
    public string continueSceneName;

    public bool hasSavedPosition;
    public float positionX;
    public float positionY;

    public bool hasSavedHealth;
    public int savedHealth;

    public string pendingEncounterId;
    public bool hasEnemyHealth;
    public int enemyHealth;

    public string[] inventoryCardIds;
    public string[] deckCardIds;
    public string[] defeatedEncounterIds;
}
