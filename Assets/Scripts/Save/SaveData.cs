using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SaveData
{
    public Vector3 playerPosition;
    public string currentLocationID;
    public List<string> collectedItemIds;
    public List<string> openedChestIds;
    public List<MiniGameItemSaveData> spawnedItemSaveData;
    public List<InventorySaveData> inventorySaveData;
    public List<JournalTopicSaveData> journalTopics;
    public List<QuestProgress> questProgressData;
    public List<string> handinQuestIDs;
    public float sfxVolume;
    public float musicVolume;
    public bool hasSeenIntro;
    public PlayerCharacterType selectedCharacter;
}

[System.Serializable]
public class InventorySaveData
{
    public int itemID;
    public int slotIndex;
    public int quantity = 1;
}

[System.Serializable]
public class MiniGameItemSaveData
{
    public int miniGameItemID;
    public string uniqueID;
    public bool wasSpawned;
    public bool isCompleted;
    public Vector3 spawnPosition;
}

[System.Serializable]
public class JournalTopicSaveData
{
    public string topicId;
    public int unlockedStageIndex;
}